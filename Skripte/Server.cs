using Godot;
using System;
using Snake42;

public class Server : Control
{
    private WebSocketServer WSPeer = new WebSocketServer();

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        StartServer();
    }

    public void StartServer()
    {
        WSPeer.Listen(8915);
        GD.Print("Server lauscht");
    }

//  // Called every frame. 'delta' is the elapsed time since the previous frame.
//  public override void _Process(float delta)
//  {
//      
//  }
}
