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
    private PackedScene _serverFormPopup;
    //private RichTextLabel _chatLog;
    //private LineEdit _messageInput;
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

        _serverFormPopup = (PackedScene)ResourceLoader.Load("res://Szenen/ServerFormPopup.tscn");
        //folgende Meldungen müssen in Verbindungseinstellungen verlegt werden. NICHT SERVER, das POPUP AUCH
        //_chatLog = GetParent().GetNode<RichTextLabel>("ErrorMSGBox/ErrorLog");
        //_messageInput = GetParent().GetNode<LineEdit>("ErrorMSGBox/HBoxContainer/MessageInput");
    }

    public void ClientConnected(int id, string proto)
    {
        // Id welcher der Server dem Client vergibt an Client senden
        // ToDo: prüfen ob der Name einmailg ist
        _ConnectedClients.Add(new ConnectedClients(id, "unkown"));
        
        msg message = new msg(Nachricht.checkIn,0,id,"");
        SendDataToOne(JsonConvert.SerializeObject(message),id);
        
    }

    public void ConnectionCloseRequest(int id, int code, string reason)
    {
        GD.Print("Server: Client " + id + " hat sich abgetrennt mit " + code + " weil " + reason);
    }

    public void ClientDisconnected(int id, bool was_clean=false)
    {
        GD.Print("Server: Client " + id + "ist " + was_clean +" getrennt");
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

            //Wenn Index -1 dann gibts den Raum nicht
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
    }

    private void SendRaumListToAllClients()
    {
        msg MSG = new msg(Nachricht.AnswerRoomData,0,999,JsonConvert.SerializeObject(_RaumList));
        SendDataToAll(JsonConvert.SerializeObject(MSG));
    }

    public void _on_Server_starten_pressed()
    {
        ShowServerFormPopup();
    }

    public void StopServer()
    {
        _WSPeer.Stop();
    }

    private void ShowServerFormPopup()
    {
        Popup popupInstance = (Popup)_serverFormPopup.Instance();
        GetTree().Root.AddChild(popupInstance);
        popupInstance.PopupCentered();

        LineEdit portInput = popupInstance.GetNode<LineEdit>("PortInput");
        portInput.Text = "8915"; 

        popupInstance.Connect("Confirmed", this, "OnPopupConfirmed");
    }

    private void OnPopupConfirmed(int port)
    {
        StartServer(port);
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

    public void _on_Sende_Hallo_zu_Clients_pressed()
    {
        //SendDataToAll("{\"Nachricht\": \"" + Nachricht.answer + "\", \"data\": \"Hallöchen\"}");
    }

    public void StartServer(int port)
    {
        Error error=_WSPeer.Listen(port);
        if(error==Error.Ok)
        {
            GD.Print("Server: Server lauscht \n--------------------------------------------------");
            Error = Error.Ok;
        }
        else
        {
            GD.Print("Server: Server konnte nicht gestartet werden");
            ConfirmationDialog ErrorPopup = (ConfirmationDialog)GlobalVariables.Instance.ConfirmationDialog.Instance();
            ErrorPopup.Init("Verbindungsfehler","Der Server konnt auf dem Port " + port + " nicht gestartet werden");
            GetTree().Root.AddChild(ErrorPopup);
            ErrorPopup.PopupCentered();
            ErrorPopup.Show();
            Error=Error.CantOpen;
        }
    }

    private String ConvertDataToString(byte[] packet)
    {
        return Encoding.UTF8.GetString(packet);
    }

    public override void _Process(float delta)
    {
        // gesendte Nachrichten empfangen
        _WSPeer.Poll();
    }
}
