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
        public int publisher; // 0 Server, alles andere ID von Clients
        public int target;
        public string data;

        public msg(Nachricht state,int publisherid, int target, string data)
        {
            this.state = state;
            this.publisher = publisherid;
            this.target = target;  
            this.data = data; 
        }
    }

}


public class Server : Control
{
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

    private WebSocketServer _WSPeer = new WebSocketServer();
    private List<ConnectedClients> _ConnectedClients = new List<ConnectedClients>();
    private List<Raum> _RaumList=new List<Raum>();
    public Error Error {get;set;} = Error.Ok;

    // Called when the node enters the scene tree for the first time.
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
        // ToDo: prüfen ob der Name einmailg ist
        _ConnectedClients.Add(new ConnectedClients(id, "unkown"));
        
        msg message = new msg(Nachricht.checkIn,0,id,"");
        SendDataToOne(JsonConvert.SerializeObject(message),id);
        // Nachricht das er sich auf Server gekommen ist wird erst gesendet wenn der Name bekannt ist
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
            if(was_clean == true)
                SendDataToAll(JsonConvert.SerializeObject(new msg(Nachricht.chatMSG,0,999,"System: Der Spieler " + DisconnectedClientName + " hat sich vom Server getrennt")));
            else
                SendDataToAll(JsonConvert.SerializeObject(new msg(Nachricht.chatMSG,0,999,"System: Der Spieler " + DisconnectedClientName + " hat sich aufgrund eines Verbindungsfehlers vom Server getrennt")));
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
            SendDataToAll(JsonConvert.SerializeObject(new msg(Nachricht.chatMSG,0,999,"System: Der Spieler " + Message.data + " ist dem Server beigetreten")));
        }
        else if (Message.state==Nachricht.chatMSG)
        {
            // Nachricht an alle anderen Clients senden
            Message = new msg(Nachricht.chatMSG,0,999,Message.data);
            // 999 -> alle Clients sind das Ziel
            SendDataToAll(JsonConvert.SerializeObject(Message));
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
        else if(Message.state == Nachricht.PeerToPeerConnectionEstablished)
        {
            // Roomhost hat seine id und die seines Mitspielers gesendet
            // beide müssen von Connected Clients getrennt werden, sonst wird an den Mitspieler noch gesendet das der roomhost getrennt wurde
            // das passiert in SendDataToAll(), da es aber den Mitspieler beriets auch nciht mehr gibt führt das zu einem gravierenden fehler
            // Clients löschen sich nicht selbst, daher muss server bestätigen das er empfange hat das sie eine peer to perr haben, wenn dies passiert löschen sich clients
            string PlayerOneName = _ConnectedClients.Find(x => x.GetId==Message.publisher).Name;
            string PlayerTwoName = _ConnectedClients.Find(x => x.GetId==Convert.ToInt32(Message.data)).Name;
            //SendDataToOne(JsonConvert.SerializeObject(new msg(Nachricht.PeerToPeerConnectionEstablished,0,Message.publisher,"")),Message.publisher);
            //SendDataToOne(JsonConvert.SerializeObject(new msg(Nachricht.PeerToPeerConnectionEstablished,0,Convert.ToInt32(Message.data),"")),Convert.ToInt32(Message.data));
            _WSPeer.DisconnectPeer(Message.publisher);
            _WSPeer.DisconnectPeer(Convert.ToInt32(Message.data));
            _ConnectedClients.Remove(_ConnectedClients.Find(x => x.GetId == Message.publisher));
            _ConnectedClients.Remove(_ConnectedClients.Find(x => x.GetId == Convert.ToInt32(Message.data)));
            SendDataToAll(JsonConvert.SerializeObject(new msg(Nachricht.chatMSG,0,999,"System: Die Spieler " + PlayerOneName+ " und " + PlayerTwoName + " habben eine runde gestartet")));
        }
    }

    private void SendRaumListToAllClients()
    {
        msg MSG = new msg(Nachricht.AnswerRoomData,0,999,JsonConvert.SerializeObject(_RaumList));
        SendDataToAll(JsonConvert.SerializeObject(MSG));
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
        _WSPeer.GetPeer(id).PutPacket(Data.ToString().ToUTF8());
    }

    public void SendDataToAll(string Data)
    {
        foreach(ConnectedClients cc in _ConnectedClients)
        {

            _WSPeer.GetPeer(cc.GetId).PutPacket(Data.ToString().ToUTF8());
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
        if(_WSPeer != null)
            _WSPeer.Poll();
    }
}
