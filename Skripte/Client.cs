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
        ServerWillClosed, // wenn Server abgeschaltet wird
        StartGame, // wird von beiden RTC Tilnehmer geschickt und allen anderen als ChatMsg Weitergeleite: Spiler1 und Spiler2 haben ein Spiel gestartet
        KeepAlivePing, // wird jede Sekunde vom Server an alle Clients gesendet, das Disconnected Event wird nur ausgelöst wenn man Lobby schließen drückt und nicht die Anwendung hart beendet
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
    private List<string> _DataSendBuffer = new List<string>(); // Wenn zu viel nachrichten gleichzeitig gesendet werden wollen, wird es hier zwischengespeichert
    bool _DisconnectFromHost = false;
    public override void _Ready()
    {
        //Signale mit Methoden verknüpfen
        _WSPeer.Connect("connection_closed",this,nameof(ConnectionClosed));
        _WSPeer.Connect("connection_error", this, nameof(ConnectionClosed));
        _WSPeer.Connect("connection_established", this, nameof(ConnectionOpened));
        _WSPeer.Connect("data_received", this, "ReceiveData");

        _clientFormPopup = (PackedScene)ResourceLoader.Load("res://Szenen/ClientFormPopup.tscn");
    }

    public void ConnectionClosed(bool was_clean=false)
    {
        GD.Print("Client: Verbindung geschlossen. Geplant: " + was_clean);
        // nur wenn die Verbindung unerwartet abreist Meldung geben
        if(was_clean == false && _DisconnectFromHost == false)
        {
            ConfirmationDialog ErrorPopup = (ConfirmationDialog)GlobalVariables.Instance.ConfirmationDialog.Instance();
            ErrorPopup.Init("Verbindungsabbruch","Verbindung zum Server wurde verloren");
            // Signal verbinden, welches den Anwender auf das Hauptmenü zurückführt
            ErrorPopup.Connect("confirmed", this, nameof(BackToMainMenu));
            //Noch ein Signal hinzufügen wenn auf das Kreuz gedrückt wird
            //ErrorPopup.Connect("confirmed", this, nameof(BackToMainMenu))
            GetTree().Root.AddChild(ErrorPopup);
            ErrorPopup.PopupCentered();
            ErrorPopup.Show();
        }
    }

    public void BackToMainMenu()
    {
        // Lobby löschen
        Lobby l = GetTree().Root.GetNodeOrNull<Lobby>("Lobby");
        if(l != null)
            l.QueueFree();
        StopConnection();
        QueueFree();
        GetTree().ChangeScene("res://Szenen/MainMenu.tscn");
    }

    private void ConnectionOpened(string proto)
    {
        GD.Print("Client: Verbunden durch Protokoll: " + proto + "\n--------------------------------------------------");
        //_chatLog.Text += "Client: Verbunden durch Protokoll: " + proto + "\n";
    }

    private void ReceiveData()
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

        if(Message.state == Nachricht.chatMSG || Message.state == Nachricht.RoomCreate || Message.state == Nachricht.AnswerRoomData || Message.state == Nachricht.SDPData || Message.state == Nachricht.ICECandidate || Message.state == Nachricht.ServerWillClosed || Message.state == Nachricht.checkIn || Message.state == Nachricht.KeepAlivePing)// hier weitere Bedingungen hinzufügen
        {
            EmitSignal(nameof(MSGReceived), Message.state,Message.data);
        }
    }

    private String ConvertDataToString(byte[] packet)
    {
        return Encoding.UTF8.GetString(packet);
    }

    public string PlayerName
    {
        get{ return _playerName; }
        set{_playerName=value;}
    }

    public int id
    {
        get{ return _clientId; }
    }

    public void OnPopupConfirmed(string ip, int port, string playerName)
    {
        GD.Print("Portnummer: " + port);
        GD.Print("IP-Adresse: " + ip);
        ConnectToServer("ws://" + ip + ":" + port.ToString());
        _playerName=playerName;
    }

    public Error ConnectToServer(String ip)
    {
        if(_WSPeer.ConnectToUrl(ip) == Error.Ok)
        {
            GD.Print("Client: Client gestartet \n--------------------------------------------------");
            return Error.Ok;
        }
        else
        {
            GD.Print("Client: Fehler beim verbinden: ");
            ConfirmationDialog ErrorPopup = (ConfirmationDialog)GlobalVariables.Instance.ConfirmationDialog.Instance();
            ErrorPopup.Init("Verbindungsfehler","Der Client kann sich nicht auf die Adresse/URL " + ip + " verbinden");
            GetTree().Root.AddChild(ErrorPopup);
            ErrorPopup.PopupCentered();
            ErrorPopup.Show();
            return Error.Failed;
        }
    }


    public void SendData(string Data)
    {
        try
        {
            _WSPeer.GetPeer(1).PutPacket(Data.ToString().ToUTF8());
        }
        catch{}
        GD.Print("Client: Nachricht gesendet: " + Data);
    }

    public void StopConnection()
    {
        _DisconnectFromHost = true;
        _WSPeer.DisconnectFromHost();
        QueueFree();
    }

    public override void _Process(float delta)
    {
        // Port/Peer offen halten
        _WSPeer.Poll();
    }
}
