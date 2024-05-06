using Godot;
using System;
using System.Collections.Generic;

public class Verbindungseinstellungen : Control
{
    private Server _server;
    private Client _client;

    private PackedScene _clientFormPopup;
    public override void _Ready()
    {
        _clientFormPopup = (PackedScene)ResourceLoader.Load("res://Szenen/ClientFormPopup.tscn");
        _server = GetNode<Server>("Server");
        _client = GetNode<Client>("Client");
    }

    public void _on_ServerundClient_pressed()
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
        LineEdit playername = popupInstance.GetNode<LineEdit>("PlayerNameInput");
        portInput.Text = "8915";
        ipInput.Text = "127.0.0.1";
        playername.Text = "Test1";


        popupInstance.Connect(nameof(ClientFormPopup.Confirmed), this, "OnPopupConfirmed" );

    }

    private void OnPopupConfirmed(string ip, int port, string playerName)
    {
        _server.StartServer(port);
        _client.ConnectToServer("ws://" + ip + ":" + port);
        _client.playerName  = playerName;

        // ToDo: vereinfachen, kommt bei Client nochmal 
        PackedScene lobby = (PackedScene)ResourceLoader.Load("res://Szenen/Lobby.tscn");
        Lobby lobbyInstance = (Lobby)lobby.Instance();
        lobbyInstance.Init(_client);
        GetTree().Root.AddChild(lobbyInstance);
        
        Hide();
        //Free();
    }
}
