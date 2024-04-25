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
    public override void _Ready()
    {
        
    }

    public void ConnectToServer(String ip)
    {
       _WSPeer.ConnectToUrl(ip);
       GD.Print("Client gestartet");
    }

    public void _on_Client_starten_pressed()
    {
        ConnectToServer("ws://127.0.0.1:8915");
    }

    private void SendData(string Data)
    {
        JSONParseResult JsonNachricht = JSON.Parse(Data);
        _WSPeer.GetPeer(1).PutPacket(JsonNachricht.ToString().ToUTF8());
    }

    public void _on_Testdaten_senden_pressed()
    {
        SendData("'Nachricht':" + Nachricht.join + "'data':" + "Hallo Welt");
    }

    public override void _Process(float delta)
    {
        // Port/Peer offen halten
        _WSPeer.Poll();
    }
}
