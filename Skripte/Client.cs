using Godot;
using System;
using Snake42;
using System.Runtime.Serialization.Json;
using System.Text;
using Newtonsoft.Json;
using System.Data.Common;

namespace Snake42
{
    enum Nachricht
    {
        id,
        name,
        join,
        userConnected,
        userDisconnected,
        lobby,
        candidate,
        offer,
        answer,
        checkIn
    }
}
public class Client : Control
{
    private WebSocketClient _WSPeer = new WebSocketClient();
    private PackedScene _clientFormPopup;
    private RichTextLabel _chatLog;
    private string _playerName;
    private int _clientId;
    public override void _Ready()
    {
        //Signale mit Methoden verknüpfen
        _WSPeer.Connect("connection_closed",this,"ConnectionClosed");
        _WSPeer.Connect("connection_error", this, "ConnectionClosed");
        _WSPeer.Connect("connection_established", this, "ConnectionOpened");
        _WSPeer.Connect("data_received", this, "ReceiveData");

        _clientFormPopup = (PackedScene)ResourceLoader.Load("res://Szenen/ClientFormPopup.tscn");
        _chatLog = GetParent().GetNode<RichTextLabel>("ErrorMSGBox/ErrorLog");
    }

    public void ConnectionClosed(bool was_clean=false)
    {
        GD.Print("Client: Verbindung geschlossen. Geplant: " + was_clean);
        _chatLog.Text += "Client: Verbindung geschlossen. Geplant: " + was_clean + "\n";
    }

    public void ConnectionOpened(string proto)
    {
        GD.Print("Client: Verbunden durch Protokoll: " + proto + "\n--------------------------------------------------");
        _chatLog.Text += "Client: Verbunden durch Protokoll: " + proto + "\n";
    }

    public void ReceiveData()
    {
        string recievedMessage = ConvertDataToString(_WSPeer.GetPeer(1).GetPacket());        
        string chatMessage = "Client: " + recievedMessage +"\n";
        GD.Print("Client: Nachricht erhalten:");
        GD.Print(recievedMessage);

        _chatLog.Text += chatMessage + "\n";

        msg Message=JsonConvert.DeserializeObject<msg>(recievedMessage);
        if(Message.state== Nachricht.checkIn)
        {
            _clientId=Message.target;
            //Name senden
            msg msg2 = new msg(Nachricht.name,_clientId,0,_playerName);
            SendData(JsonConvert.SerializeObject(msg2));
        }
    }

    private String ConvertDataToString(byte[] packet)
    {
        return Encoding.UTF8.GetString(packet);
    }

    public void _on_Client_starten_pressed()
    {
        ShowClientPopup();
    }

    public string playerName
    {
        get{ return _playerName; }
        set{_playerName=value;}
    }

    private void ShowClientPopup()
    {
        Popup popupInstance = (Popup)_clientFormPopup.Instance();
        GetTree().Root.AddChild(popupInstance);
        popupInstance.PopupCentered();

        LineEdit portInput = popupInstance.GetNode<LineEdit>("PortInput");
        LineEdit ipInput = popupInstance.GetNode<LineEdit>("IpInput");
        LineEdit playername = popupInstance.GetNode<LineEdit>("PlayerNameInput");
        portInput.Text = "8915";
        ipInput.Text = "127.0.0.1";
        playername.Text = "Test1";


        popupInstance.Connect(nameof(ClientFormPopup.Confirmed), this, "OnPopupConfirmed" );

    }

    public void OnPopupConfirmed(string ip, int port, string playerName)
    {
        GD.Print("Portnummer: " + port);
        GD.Print("IP-Adresse: " + ip);
        ConnectToServer("ws://" + ip + ":" + port.ToString());
        _playerName=playerName;
    }

    public void ConnectToServer(String ip)
    {
        Error error = _WSPeer.ConnectToUrl(ip);
        if(error == Error.Ok)
        {
            GD.Print("Client: Client gestartet \n--------------------------------------------------");
            _chatLog.Text += "Client: Client gestartet \n";
        }
        else
        {
            GD.Print("Client: Fehler beim verbinden: " + error.ToString());
            _chatLog.Text += "Client: Fehler beim verbinden: " + error.ToString() + "\n";
        }
    }

    

    private void SendData(string Data)
    {
        // Prüfen ob die Nachricht gültiges Json Format hat
        JSONParseResult JsonParseFehler = JSON.Parse(Data);
        if(JsonParseFehler.ErrorString=="")
        {
            _WSPeer.GetPeer(1).PutPacket(Data.ToString().ToUTF8());
            GD.Print("Client: Nachricht gesendet: " + Data);
        }
        else
            GD.Print("Client: Nachricht ist kein Json Dokument");

    }

    private void SendJoinData()
    {
        // alle die sich verbinden wollen haben erstmal die id 0, später wird es dann vom Server korrigiert
        msg msg= new msg(Nachricht.join,_clientId,0,"");
        SendData(JsonConvert.SerializeObject(msg));
    }

    public void _on_Testdaten_senden_pressed()
    {
        SendData("{\"Nachricht\": \"" + "Test" + "\", \"data\": \"Hallo Welt\"}");
    }

    public override void _Process(float delta)
    {
        // Port/Peer offen halten
        _WSPeer.Poll();
    }
}
