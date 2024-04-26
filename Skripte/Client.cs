using Godot;
using System;
using Snake42;
using System.Runtime.Serialization.Json;
using System.Text;

namespace Snake42
{
    enum Nachricht
    {
        id,
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
    public override void _Ready()
    {
        //Signale mit Methoden verknüpfen
        _WSPeer.Connect("connection_closed",this,"ConnectionClosed");
        _WSPeer.Connect("connection_error", this, "ConnectionClosed");
        _WSPeer.Connect("connection_established", this, "ConnectionOpened");
        _WSPeer.Connect("data_received", this, "ReceiveData");

        _clientFormPopup = (PackedScene)ResourceLoader.Load("res://Szenen/ClientFormPopup.tscn");
    }

    public void ConnectionClosed(bool was_clean=false)
    {
        GD.Print("Client: Verbindung geschlossen. Geplant: " + was_clean);
    }

    public void ConnectionOpened(string proto)
    {
        GD.Print("Client: Verbunden durch Protokoll: " + proto + "\n--------------------------------------------------");
    }

    public void ReceiveData()
    {
        GD.Print("Client: Daten vom Server sind angekommen: " + _WSPeer.GetPeer(1).GetPacket().GetStringFromUTF8());
    }

    public void _on_Client_starten_pressed()
    {
        ShowClientPopup();
    }

    private void ShowClientPopup()
    {
        Popup popupInstance = (Popup)_clientFormPopup.Instance();
        GetTree().Root.AddChild(popupInstance);
        popupInstance.PopupCentered();

        LineEdit portInput = popupInstance.GetNode<LineEdit>("PortInput");
        LineEdit ipInput = popupInstance.GetNode<LineEdit>("IpInput");
        portInput.Text = "";
        ipInput.Text = "";

        popupInstance.Connect("Confirmed", this, "OnPopupConfirmed");
    }

    private void OnPopupConfirmed(string ip, int port)
    {
        GD.Print("Portnummer: " + port);
        GD.Print("IP-Adresse: " + ip);
        ConnectToServer("ws://" + ip + ":" + port.ToString());
    }

    public void ConnectToServer(String ip)
    {
        Error error = _WSPeer.ConnectToUrl(ip);
        if(error == Error.Ok)
            GD.Print("Client: Client gestartet \n--------------------------------------------------");
        else
            GD.Print("Client: Fehler beim verbinden: " + error.ToString());
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

    public void _on_Testdaten_senden_pressed()
    {
        SendData("{\"Nachricht\": \"" + Nachricht.join + "\", \"data\": \"Hallo Welt\"}");

    }

    public override void _Process(float delta)
    {
        // Port/Peer offen halten
        _WSPeer.Poll();
    }
}
