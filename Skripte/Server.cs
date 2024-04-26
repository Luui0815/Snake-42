using Godot;
using System;
using Snake42;
using System.Text;


public class Server : Control
{
    private WebSocketServer _WSPeer = new WebSocketServer();
    private PackedScene _serverFormPopup;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        //Signale verbinden
        _WSPeer.Connect("client_connected",this,"ClientConnected");
        _WSPeer.Connect("client_disconnected", this, "ClientDisconnected");
        _WSPeer.Connect("client_close_request", this, "ConnectionCloseRequest");
        _WSPeer.Connect("data_received",this,"ReceiveData");

        _serverFormPopup = (PackedScene)ResourceLoader.Load("res://Szenen/ServerFormPopup.tscn");
    }

    public void ClientConnected(int id, string proto)
    {
        GD.Print("Server: Client " + id + " hat sich mit Protokoll: " + proto + " verbunden");
    }

    public void ConnectionCloseRequest(int id, int code, string reason)
    {
        GD.Print("Server: Client " + id + " hat sich abgetrennt mit " + code + " weil " + reason);
    }

    public void ClientDisconnected(int id, bool was_clean=false)
    {
        GD.Print("Server: Client " + id + "ist " + was_clean +" getrennt");
    }

    public void ReceiveData(int id)
    {
        GD.Print("Server: Nachricht erhalten:");
        GD.Print(ConvertDataToString(_WSPeer.GetPeer(id).GetPacket()));

    }

    public void _on_Server_starten_pressed()
    {
        ShowServerFormPopup();
    }

    private void ShowServerFormPopup()
    {
        Popup popupInstance = (Popup)_serverFormPopup.Instance();
        GetTree().Root.AddChild(popupInstance);
        popupInstance.PopupCentered();

        LineEdit portInput = popupInstance.GetNode<LineEdit>("PortInput");
        portInput.Text = ""; 

        popupInstance.Connect("Confirmed", this, "OnPopupConfirmed");
    }

    private void OnPopupConfirmed(int port)
    {
        GD.Print("Portnummer: " + port);
        StartServer(port);
    }

    public void StartServer(int port)
    {
        Error error=_WSPeer.Listen(port);
        if(error==Error.Ok)
            GD.Print("Server: Server lauscht");
        else
            GD.Print("Server: Server konnte nicht gestartet werden");
    }

   

    private String ConvertDataToString(byte[] packet)
    {
        return Encoding.UTF8.GetString(packet);
    }

    public override void _Process(float delta)
    {
        // gesendte Nachrichten empfangen
        _WSPeer.Poll();
    }
}
