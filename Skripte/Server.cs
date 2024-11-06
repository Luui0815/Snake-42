using Godot;
using System;
using Snake42;
using System.Text;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Data;
using System.Linq;

namespace Snake42
{
    class msg
    {
        public Nachricht state;
        public int publisher;
        public int target;
        public string data;
        public msg(Nachricht state,int publisherid, int target, string data)
        {
            this.state = state;
            publisher = publisherid;
            this.target = target;  
            this.data = data; 
        }
    }

}


public class Server : Control
{
    [Signal]
    public delegate void ServerInfo(Nachricht state, string msg);
    private class ConnectedClients
    {
        public ConnectedClients(int id, string name)
        {
            this.id = id;
            this.name = name;
        }

        public int GetId
        {
            get { return this.id; }
        }
        public string Name
        {
            get { return this.name; }
            set { this.name = value; }
        }
        int id;
        string name;
    }

    private class RoomMatesOnStarting // wird nur benutzt um zu sagen das 2 Spieler eine RTC Verbindung aufgebaut haben und das Spiel starten
    {
        public Raum room;
        public string PlayerOneName;
        public string PlayerTwoName; 

        public RoomMatesOnStarting(Raum room, string NameOfPlayerOne, string NameOfPlayerTwo)
        {
            this.room = room;
            PlayerOneName = NameOfPlayerOne;
            PlayerTwoName = NameOfPlayerTwo;
        }
    }

    private WebSocketServer _WSPeer = new WebSocketServer();
    private List<ConnectedClients> _ConnectedClients = new List<ConnectedClients>();
    private List<RoomMatesOnStarting> _MatesOnStartingGame = new List<RoomMatesOnStarting>();
    private List<Raum> _RaumList = new List<Raum>();
    public Error Error {get;set;} = Error.Ok;

    public override void _Ready()
    {
        //Signale verbinden
        _WSPeer.Connect("client_connected",this,"ClientConnected");
        _WSPeer.Connect("client_disconnected", this, "ClientDisconnected");
        _WSPeer.Connect("client_close_request", this, "ConnectionCloseRequest");
        _WSPeer.Connect("data_received",this,"ReceiveData");
    }

    public void ClientConnected(int id, string proto)
    {
        // Id welcher der Server dem Client vergibt an Client senden
        _ConnectedClients.Add(new ConnectedClients(id, "unkown"));
        
        msg message = new msg(Nachricht.checkIn,0,id,"");
        SendDataToOne(JsonConvert.SerializeObject(message),id);
        // Nachricht das er auf Server gekommen ist wird erst gesendet wenn der Name bekannt ist
    }

    public void ConnectionCloseRequest(int id, int code, string reason)
    {
        GD.Print("Server: Client " + id + " hat sich abgetrennt mit " + code + " weil " + reason);
        // das interresiert andere nicht
    }

    public void ClientDisconnected(int id, bool was_clean=false)
    {
        GD.Print("Server: Client " + id + "ist " + was_clean +" getrennt");
        if(_ConnectedClients.Exists(x => x.GetId==id))
        {
            string DisconnectedClientName=_ConnectedClients.Find(x => x.GetId==id).Name;
            _ConnectedClients.Remove(_ConnectedClients.Find(x => x.GetId==id));
            msg msg;
            if(was_clean == true)
                msg = new msg(Nachricht.chatMSG,0,999,"System: Der Spieler " + DisconnectedClientName + " hat sich vom Server getrennt");
            else
                msg = new msg(Nachricht.chatMSG,0,999,"System: Der Spieler " + DisconnectedClientName + " hat sich aufgrund eines Verbindungsfehlers vom Server getrennt");
            SendDataToAll(JsonConvert.SerializeObject(msg));
            EmitSignal(nameof(ServerInfo), msg.state, msg.data);
            // Prüfen ob sie sich in einem Raum befunden haben!
            foreach(Raum r in _RaumList)
            {
                if(r.PlayerOneId == id)
                {
                    // Prüfen ob noch ein 2. drin ist
                    if(r.PlayerTwoId != 0)
                    {
                        // Weiterer wilder Fehlertripp:
                        // Nachdem beied Spieler erfolgreich eine WebRTC Verbindung aufgebaut haben trennen sich beide
                        // 
                        // Spieler 2 wird zum Spieler 1
                        int index = _RaumList.IndexOf(r);
                        _RaumList[index].Raumname = "Raum von: " + _ConnectedClients.Find(x => x.GetId == r.PlayerTwoId).Name;
                        _RaumList[index].PlayerOneId = r.PlayerTwoId;
                        _RaumList[index].PlayerTwoId = 0;
                    }
                    else
                    {
                        // wenn kein Spiler 2 raum löschen
                        _RaumList.Remove(r);
                    }
                }
                else if(r.PlayerTwoId == id)
                {
                    // einfach id aus Raumlöschen
                    _RaumList[_RaumList.IndexOf(r)].PlayerTwoId = 0;
                }
                SendRaumListToAllClients();
            }
        }

    }

    public void ReceiveData(int id)
    {
        string recievedMessage = ConvertDataToString(_WSPeer.GetPeer(id).GetPacket());        
        string chatMessage = $"[Client] {id}: " + recievedMessage +"\n";
        GD.Print("Server: Nachricht erhalten:");
        GD.Print(recievedMessage);

        msg Message = JsonConvert.DeserializeObject<msg>(recievedMessage);
        if(Message.state==Nachricht.name)
        {
            _ConnectedClients.Find(x => x.GetId==id).Name = Message.data;
            // Nachricht an alle senden das neuer Client sich verbunden hat
            Message = new msg(Nachricht.chatMSG,0,999,"System: Der Spieler " + Message.data + " ist dem Server beigetreten");
            SendDataToAll(JsonConvert.SerializeObject(Message));
            // das dann auch als ServerInfo dem Serverbetreiber mitteilen, falls er nicht auch Client ist
            EmitSignal(nameof(ServerInfo), Message.state, Message.data);
        }
        else if (Message.state==Nachricht.chatMSG)
        {
            // Nachricht an alle anderen Clients senden
            Message = new msg(Nachricht.chatMSG,0,999,Message.data);
            // 999 -> alle Clients sind das Ziel
            SendDataToAll(JsonConvert.SerializeObject(Message));
            EmitSignal(nameof(ServerInfo), Message.state, Message.data);
        }
        else if (Message.state == Nachricht.RoomCreate)
        {
            //zur RoomList hinzufügen
            Raum room = new Raum(Message.publisher);
            string[] arguments = Message.data.Split("|");
            room.Raumname = arguments[1];
            _RaumList.Add(room);

            SendRaumListToAllClients();
        }
        else if(Message.state == Nachricht.OfferRoomData)
        {
            msg MSG = new msg(Nachricht.AnswerRoomData,0,Message.publisher,JsonConvert.SerializeObject(_RaumList));
            SendDataToOne(JsonConvert.SerializeObject(MSG),Message.publisher);
        }
        else if(Message.state == Nachricht.RoomJoin)
        {
            int PlayerOneId = Convert.ToInt32(Message.data);
            _RaumList.Find(x => x.PlayerOneId == PlayerOneId).PlayerTwoId = Message.publisher;

            SendRaumListToAllClients();
        }
        else if(Message.state == Nachricht.RoomLeft)
        {
            Raum room = JsonConvert.DeserializeObject<Raum>(Message.data);

            int index = -1;
            for (int i = 0; i < _RaumList.Count; i++)
            {
                if (_RaumList[i].PlayerOneId == room.PlayerOneId && _RaumList[i].PlayerTwoId == room.PlayerTwoId && _RaumList[i].Raumname == room.Raumname)
                {
                    index = i;
                    break;
                }
            }

            //Wenn Index -1 dann gibts den Raum nicht, sollte nicht vorkommen
            if(index == -1)
            {
                GD.Print("Fehler bei Raum verlassen Raumname:" + room.Raumname);
                return;
            }

            if(Message.publisher == room.PlayerOneId)
            {
                //Prüfen ob es einen 2. Spieler gibt
                if(room.PlayerTwoId == 0)
                {
                    //Raum löschen, da keiner mehr drin
                    _RaumList.RemoveAt(index);
                }
                else
                {
                    //Player2 wird Host
                    _RaumList[index].PlayerOneId = room.PlayerTwoId;
                    _RaumList[index].PlayerTwoId = 0;
                    _RaumList[index].Raumname = "Raum von: " + _ConnectedClients.Find(x => x.GetId == room.PlayerTwoId).Name;
                }
            }
            else
            {
                //Spieler 2 hat den Raum verlassen -> d.h. es gibt noch 1 Spieler
                _RaumList[index].PlayerTwoId = 0;
            }

            //geupdatete Liste an alle Clients senden
            SendRaumListToAllClients();
        }
        else if (Message.state == Nachricht.SDPData || Message.state == Nachricht.ICECandidate)
        {
            SendDataToOne(recievedMessage, Message.target);
        }
        else if(Message.state == Nachricht.StartGame)
        {
            // Nachricht wird von beiden gesendet!, aber nur einmal soll eine Nachricht kommen und auch keine Verbindungsabbruc nachricht
            // prüfen ob sein Kollege nicht schon schneller war und es den Eintrag schon gibt!

            int index = -1;
            Raum room = JsonConvert.DeserializeObject<Raum>(Message.data);
            foreach(RoomMatesOnStarting mr in _MatesOnStartingGame)
            {
                if(mr.room.PlayerOneId == room.PlayerOneId && mr.room.PlayerTwoId == room.PlayerTwoId)
                {
                    index = _MatesOnStartingGame.IndexOf(mr);
                }
            }
            if(index != -1)
            {
                // Nachricht wird zunächst an alle gesndet auch an die, die sich gleich trennen, wenn sie es nicht schon getan haben
                string p1 = _MatesOnStartingGame[index].PlayerOneName;
                string p2 = _MatesOnStartingGame[index].PlayerTwoName;

                msg msg = new msg(Nachricht.chatMSG,0,999, "Die Spieler: " + p1 +" und " + p2 + " haben ein Spiel gestartet!");
                SendDataToAll(JsonConvert.SerializeObject(msg));
                EmitSignal(nameof(ServerInfo), msg.state, msg.data);

                _MatesOnStartingGame.RemoveAt(index);
            }
            else
            {
                // dieser Client ist der 1. der den Request sendet
                // Index in RaumListe bestimmen
                int RoomID = -1;
                foreach(Raum r in _RaumList)
                {
                    if(r.PlayerOneId == room.PlayerOneId && r.PlayerTwoId == room.PlayerTwoId)
                    {
                        RoomID = _RaumList.IndexOf(r);
                    }
                }
                if(RoomID == -1)
                {
                    GD.PrintErr("Server: 2 Clients waren in einem Raum und haben eine RTC Verbindung aufgebaut. Beim Trennvorgang hat sich rausgestellt das sie in keinem echten Serverrau sind. Das ist eigentlich unmöglich");
                    return;
                }
                // Raum löschen
                _RaumList.RemoveAt(RoomID);
                // Name rausfinden, dabei kann es passieren das sich der eien schon geternn hat, dann vergib den NAme Spieler + id
                string p1 = string.Empty;
                string p2 = string.Empty;
                var playerOne = _ConnectedClients.Find(x => x.GetId == room.PlayerOneId);
                var playerTwo = _ConnectedClients.Find(x => x.GetId == room.PlayerTwoId);

                if (playerOne != null)
                {
                    p1 = playerOne.Name;
                }
                else
                {
                    p1 = "Spieler " + room.PlayerOneId;
                }

                if (playerOne != null)
                {
                    p2 = playerTwo.Name;
                }
                else
                {
                    p2= "Spieler " + room.PlayerTwoId;
                }

                _MatesOnStartingGame.Add(new RoomMatesOnStarting(room, p1, p2));
            }
            // _ConnectedClients.Remove(_ConnectedClients.Find(x => x.GetId == Message.publisher)); => trennst sich selber
        }
    }

    private void SendRaumListToAllClients()
    {
        msg MSG = new msg(Nachricht.AnswerRoomData,0,999,JsonConvert.SerializeObject(_RaumList));
        SendDataToAll(JsonConvert.SerializeObject(MSG));
        EmitSignal(nameof(ServerInfo), MSG.state, MSG.data);
    }


    public void StopServer()
    {
        // nochmal an alle Clients senden das es gleich vorbei ist
        SendDataToAll(JsonConvert.SerializeObject(new msg(Nachricht.ServerWillClosed,0,999,"System: Server wird heruntergefahren!")));
        _WSPeer.Stop();
        QueueFree();
    }

    public void SendDataToOne(string Data, int id)
    {
        try
        {
            _WSPeer.GetPeer(id).PutPacket(Data.ToString().ToUTF8());
        }
        catch
        {
            GD.PrintErr("Ein Client befindet sich auf dem Server noch in der Clientliste obwohl er getrennt ist");
        }
    }

    public void SendDataToAll(string Data)
    {
        foreach(ConnectedClients cc in _ConnectedClients)
        {
            try
            {
                _WSPeer.GetPeer(cc.GetId).PutPacket(Data.ToString().ToUTF8());
            }
            catch
            {
                GD.PrintErr("Ein Client befindet sich auf dem Server noch in der Clientliste obwohl er getrennt ist");
            }
        }
    }

    public Error StartServer(int port)
    {
        if(_WSPeer.Listen(port)==Error.Ok)
        {
            GD.Print("Server: Server lauscht \n--------------------------------------------------");
            return Error.Ok;
        }
        else
        {
            GD.Print("Server: Server konnte nicht gestartet werden");
            ConfirmationDialog ErrorPopup = (ConfirmationDialog)GlobalVariables.Instance.ConfirmationDialog.Instance();
            ErrorPopup.Init("Verbindungsfehler","Der Server konnt auf dem Port " + port + " nicht gestartet werden");
            GetTree().Root.AddChild(ErrorPopup);
            ErrorPopup.PopupCentered();
            ErrorPopup.Show();
            return Error.Failed;
        }
    }

    private String ConvertDataToString(byte[] packet)
    {
        return Encoding.UTF8.GetString(packet);
    }

    public override void _Process(float delta)
    {
        // gesendte Nachrichten empfangen
        try
        {
            _WSPeer.Poll();
        }
        catch{}
    }

    public void AddForeignClient(int id, string name)
    {
        // Da man bei einem Verbindungsabbruch wieder zur lobby kommt nachdem man die RTC gestartet hat und den Serv weiterlaufen hat
        // Der Server hat aber den eigenen Client bereits gelöscht, daher sendet er keiene Daten mehr an ihn, ich weiß langer Trip
        // daher füg ihn einfach hinzu wenn du eine id hast die du nicht kennst!
        // _ConnectedClients.Add(new ConnectedClients(id, name));
        // der Client der nie disconnected ist, sendet nachdem die Lobby wieder in den Vordergrund getreten ist den Namen nach!
        // Wow was für eine Fehlerkette
        // Die Methode wird von der Lobby aus ausgerufen! von GlobalVariables BAcktoMainMenuorLobby()
    }
}
