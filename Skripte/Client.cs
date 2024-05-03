using Godot;
using System;
using Snake42;
using System.Runtime.Serialization.Json;
using System.Text;
using Newtonsoft.Json;
using System.Data.Common;

namespace Snake42
{
    public enum Nachricht
    {
        name,// sendet Name des Clients an den Server
        checkIn, //Server sendet Client seine id zurück
        chatMSG // User Nachrichten werden ausgetauscht
    }
}
public class Client : Control
{
    [Signal]
    public delegate void MSGReceived(Nachricht state,string msg);

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

        if(Message.state== Nachricht.chatMSG)// hier weitere Bedingungen hinzufügen
        {
            EmitSignal(nameof(MSGReceived), Message.state,Message.data);
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

    public int id
    {
        get{ return _clientId; }
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

        // ToDo: vereinfachen, kommt bei Verbindungseinstellungen nochmal 
        PackedScene lobby = (PackedScene)ResourceLoader.Load("res://Szenen/Lobby.tscn");
        Lobby lobbyInstance = (Lobby)lobby.Instance();
        lobbyInstance.Init(this);
        GetParent().GetTree().Root.AddChild(lobbyInstance);
        Verbindungseinstellungen vb = (Verbindungseinstellungen)GetParent();
        vb.Hide();
        //Free hier nicht, ka. warum hier nicht geht und bei Verbindungseinstellung es geht
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


    public void SendData(string Data)
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

    public void _on_Testdaten_senden_pressed()
    {
        SendData("{\"Nachricht\": \"" + "Test" + "\", \"data\": \"Hallo Welt\"}");
    }

    public string PlayerName
    {
        get
        {
            return _playerName;
        }
    }

    public override void _Process(float delta)
    {
        // Port/Peer offen halten
        _WSPeer.Poll();
    }
}
