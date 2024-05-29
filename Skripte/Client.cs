using Godot;
using System;
using Snake42;
using System.Runtime.Serialization.Json;
using System.Text;
using Newtonsoft.Json;
using System.Data.Common;
using System.Threading;
using System.Collections.Generic;

namespace Snake42
{
    public enum Nachricht
    {
        name,// sendet Name des Clients an den Server
        checkIn, //Server sendet Client seine id zurück
        chatMSG, // User Nachrichten werden ausgetauscht
        RoomCreate, // Wenn neuer Raum Erstellt wird, geht nur an Serer um seine Raumlisteaktuell zu halten
        RoomJoin, // Wenn jemand anderes einem Raum beitritt
        RoomDelete, // entweder wurde das Spiel gestartet oder der Raum geschlossen
        OfferRoomData, // Clients fordern Liste aller Räume vom Server an
        AnswerRoomData, //nur der Client welcher die Raumliste angefordert hat bekommt sie vom Server geschickt, wird auch genutzt um die RaumListe zu updaten
        RoomLeft,// wen Client Raum verlässt, kann dazu führen das Raum gelöscht wird
        SDPData, // wird vom Raumhost an Player 2 gesendet,damit er auch die SDP bekommt
        ICECandidate, // für WebRTC ICe Candidate austauschen

    }
}
public class Client : Control
{
    [Signal]
    public delegate void MSGReceived(Nachricht state,string msg);

    private WebSocketClient _WSPeer = new WebSocketClient();
    private PackedScene _clientFormPopup;
    //private RichTextLabel _chatLog;
    private string _playerName;
    private int _clientId;
    private Godot.Timer _sendTimer = new Godot.Timer();
    private List<string> _DataSendBuffer = new List<string>(); // Wenn zu viel nachrichten gleichzeitig gesendet werden wollen, wird es hier zwischengespeichert
    public override void _Ready()
    {
        //Signale mit Methoden verknüpfen
        _WSPeer.Connect("connection_closed",this,"ConnectionClosed");
        _WSPeer.Connect("connection_error", this, "ConnectionClosed");
        _WSPeer.Connect("connection_established", this, "ConnectionOpened");
        _WSPeer.Connect("data_received", this, "ReceiveData");

        _clientFormPopup = (PackedScene)ResourceLoader.Load("res://Szenen/ClientFormPopup.tscn");
        // FOLGENDES MUSS IN VERBINDUNGSEINSTELLUNGEN VERLGET WREDEN; SIEHE AUCH SERVER
        //_chatLog = GetParent().GetNode<RichTextLabel>("ErrorMSGBox/ErrorLog");

        // Timer stellen, da Nachrichten zu schnell gesendet werden können
        _sendTimer.WaitTime = 1;
        _sendTimer.OneShot=true;
        _sendTimer.Connect("timeout",this,"SendDataFromBuffer");
        AddChild(_sendTimer);
    }

    public void ConnectionClosed(bool was_clean=false)
    {
        GD.Print("Client: Verbindung geschlossen. Geplant: " + was_clean);
        //_chatLog.Text += "Client: Verbindung geschlossen. Geplant: " + was_clean + "\n";
    }

    public void ConnectionOpened(string proto)
    {
        GD.Print("Client: Verbunden durch Protokoll: " + proto + "\n--------------------------------------------------");
        //_chatLog.Text += "Client: Verbunden durch Protokoll: " + proto + "\n";
    }

    public void ReceiveData()
    {
        string recievedMessage = ConvertDataToString(_WSPeer.GetPeer(1).GetPacket());        
        string chatMessage = "Client: " + recievedMessage +"\n";
        GD.Print("Client: Nachricht erhalten:");
        GD.Print(recievedMessage);

        //_chatLog.Text += chatMessage + "\n";

        msg Message=JsonConvert.DeserializeObject<msg>(recievedMessage);
        if(Message.state== Nachricht.checkIn)
        {
            _clientId=Message.target;
            //Name senden
            msg msg2 = new msg(Nachricht.name,_clientId,0,_playerName);
            SendData(JsonConvert.SerializeObject(msg2));
        }

        if(Message.state == Nachricht.chatMSG || Message.state == Nachricht.RoomCreate || Message.state == Nachricht.AnswerRoomData || Message.state == Nachricht.SDPData || Message.state == Nachricht.ICECandidate)// hier weitere Bedingungen hinzufügen
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
    }

    public void ConnectToServer(String ip)
    {
        Error error = _WSPeer.ConnectToUrl(ip);
        if(error == Error.Ok && true==false)
        {
            GD.Print("Client: Client gestartet \n--------------------------------------------------");
            // wenn erfolgreich kann zur lobby gewechselt werden
            // ToDo: vereinfachen, kommt bei Verbindungseinstellungen nochmal 
            PackedScene lobby = (PackedScene)ResourceLoader.Load("res://Szenen/Lobby.tscn");
            Lobby lobbyInstance = (Lobby)lobby.Instance();
            Verbindungseinstellungen vb = (Verbindungseinstellungen)GetParent();
            vb.RemoveChild(this);
            lobbyInstance.AddChild(this);
            vb.GetTree().Root.AddChild(lobbyInstance);

        vb.QueueFree();
        }
        else
        {
            GD.Print("Client: Fehler beim verbinden: " + error.ToString());
            ConfirmationDialog ErrorPopup = (ConfirmationDialog)GlobalVariables.Instance.ConfirmationDialog.Instance();
            ErrorPopup.Init("Verbindungsfehler","Der Client kann sich nicht auf die Adresse/URL " + ip + " verbinden");
            GetTree().Root.AddChild(ErrorPopup);
            ErrorPopup.PopupCentered();
            ErrorPopup.Show();
        }
    }


    public void SendData(string Data)
    {
        if(_sendTimer.TimeLeft==0)
        {

            _WSPeer.GetPeer(1).PutPacket(Data.ToString().ToUTF8());
            GD.Print("Client: Nachricht gesendet: " + Data);
            _sendTimer.Start();
        }
        else
        {
            // Zu viele daten auf einmal, in Wartebuffer schreiben
            _DataSendBuffer.Add(Data);
            //Geht nicht, Client wartet bis der Timer 0 ist, Server kommt solang nicht zum Zug
            // bessert sich wenn man alles in ein Skript packt denk ich
        }
    }

    private void SendDataFromBuffer()
    {
        // wird aufgerufen wenn SendTimer 0 ist
        if(_DataSendBuffer.Count !=0 )
        {
            //Daten nachträglich senden
            SendData(_DataSendBuffer[0]);
            _DataSendBuffer.RemoveAt(0);
        }
    }

    public string PlayerName
    {
        get
        {
            return _playerName;
        }
    }

    public void StopConnection()
    {
        _WSPeer.DisconnectFromHost();
    }

    public override void _Process(float delta)
    {
        // Port/Peer offen halten
        _WSPeer.Poll();
    }
}
