using Godot;
using System;
using Snake42;
using System.Text;


public class Server : Control
{
    private WebSocketServer _WSPeer = new WebSocketServer();

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        //Signale verbinden
        GD.Print(_WSPeer.Connect("data_received",this,"PrintReceivedData"));
        //_WSPeer.EmitSignal("data_received");
    }

    public void StartServer()
    {
        _WSPeer.Listen(8915);
        GD.Print("Server lauscht");
    }

    public void _on_Server_starten_pressed()
    {
        StartServer();
    }

    private String ConvertDataToString(byte[] packet)
    {
        string Nachricht = Encoding.UTF8.GetString(packet);
        return JSON.Parse(Nachricht).ToString();
    }

    public void PrintReceivedData()
    {
        GD.Print(ConvertDataToString(_WSPeer.GetPeer(1).GetPacket()));
    }

    public override void _Process(float delta)
    {
        // gesendte Nachrichten empfangen
        _WSPeer.Poll();

        if(_WSPeer.HasSignal("data_received"))
        {
            //GD.Print("Ja");
        }
    }
}
