using Godot;
using System;

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
        checkIn,
    }
}

public class Client : Control
{
    private WebSocketMultiplayerPeer WSPeer =new WebSocketClient();
    public override void _Ready()
    {
        ConnectToServer("127.0.0.1");
        GD.Print("hallo");
    }

    public override void _Process(float delta)
    {
        ConnectToServer("127.0.0.1");
        
    }

    public void ConnectToServer(string ip)
    {
        WSPeer.Call("connect_to_url", ip);
    }


}
