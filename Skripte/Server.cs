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
    private RichTextLabel _chatLog;
    private LineEdit _messageInput;
    private List<ConnectedClients> _ConnectedClients = new List<ConnectedClients>();

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        //Signale verbinden
        _WSPeer.Connect("client_connected",this,"ClientConnected");
        _WSPeer.Connect("client_disconnected", this, "ClientDisconnected");
        _WSPeer.Connect("client_close_request", this, "ConnectionCloseRequest");
        _WSPeer.Connect("data_received",this,"ReceiveData");

        _serverFormPopup = (PackedScene)ResourceLoader.Load("res://Szenen/ServerFormPopup.tscn");
        _chatLog = GetParent().GetNode<RichTextLabel>("ErrorMSGBox/ErrorLog");
        _messageInput = GetParent().GetNode<LineEdit>("ErrorMSGBox/HBoxContainer/MessageInput");
    }

    public void ClientConnected(int id, string proto)
    {
        GD.Print("Server: Client " + id + " hat sich mit Protokoll: " + proto + " verbunden");
        _chatLog.Text += "Server: Client " + id + " hat sich mit Protokoll: " + proto + " verbunden\n";
        // Id welcher der Server dem Client vergibt an Client senden
        // ToDo: prüfen ob der Name einmailg ist
        _ConnectedClients.Add(new ConnectedClients(id, "unkown"));
        
        msg message = new msg(Nachricht.checkIn,0,id,"");
        SendDataToOne(JsonConvert.SerializeObject(message),id);
        
    }

    public void ConnectionCloseRequest(int id, int code, string reason)
    {
        GD.Print("Server: Client " + id + " hat sich abgetrennt mit " + code + " weil " + reason);
        _chatLog.Text += "Server: Client " + id + " hat sich abgetrennt mit " + code + " weil " + reason +"\n";
    }

    public void ClientDisconnected(int id, bool was_clean=false)
    {
        GD.Print("Server: Client " + id + "ist " + was_clean +" getrennt");
        _chatLog.Text += "Server: Client " + id + "ist " + was_clean +" getrennt\n";
    }

    public void ReceiveData(int id)
    {
        string recievedMessage = ConvertDataToString(_WSPeer.GetPeer(id).GetPacket());        
        string chatMessage = $"[Client] {id}: " + recievedMessage +"\n";
        GD.Print("Server: Nachricht erhalten:");
        GD.Print(recievedMessage);

        _chatLog.Text += chatMessage + "\n";

        msg Message = JsonConvert.DeserializeObject<msg>(recievedMessage);
        if(Message.state==Nachricht.name)
        {
            _ConnectedClients.Find(x => x.GetId==id).Name = Message.data;
        }
    }

    public void _on_Server_starten_pressed()
    {
        ShowServerFormPopup();
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
        GD.Print("Portnummer: " + port);
        StartServer(port);
    }

    public void SendDataToOne(string Data, int id)
    {
        // Prüfen ob die Nachricht gültiges Json Format hat
        JSONParseResult JsonParseFehler = JSON.Parse(Data);
        if(JsonParseFehler.ErrorString=="")
        {
            _WSPeer.GetPeer(id).PutPacket(Data.ToString().ToUTF8());
            GD.Print("Server: Nachricht gesendet: " + Data);
        }
        else
            GD.Print("Server: Nachricht ist kein Json Dokument");
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
            _chatLog.Text += "Server: Server lauscht auf Port "+ port +"\n";
        }
        else
        {
            GD.Print("Server: Server konnte nicht gestartet werden");
            _chatLog.Text += "Server: Server konnte nicht gestartet werden\nFehler: " + error.ToString() + "\n";
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
