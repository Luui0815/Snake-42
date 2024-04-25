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
        checkIn
    }
}
public class Client : Control
{
    private WebSocketClient WSPeer = new WebSocketClient();
    public override void _Ready()
    {
        ConnectToServer("127.0.0.1:8915");
    }

    public void ConnectToServer(String ip)
    {
       WSPeer.ConnectToUrl(ip);
       GD.Print("Client gestartet");
    }

//  // Called every frame. 'delta' is the elapsed time since the previous frame.
//  public override void _Process(float delta)
//  {
//      
//  }
}
