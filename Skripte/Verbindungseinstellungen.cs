using Godot;
using System;
using System.Collections.Generic;

public class Verbindungseinstellungen : Control
{
    private Server _server;
    private Client _client;
    private bool _bootServer=false;
    private bool _bootClient = false;

    private PackedScene _clientFormPopup;
    private PackedScene _serverFormPopup;

    public override void _Ready()
    {
        _clientFormPopup = (PackedScene)ResourceLoader.Load("res://Szenen/ClientFormPopup.tscn");
        _serverFormPopup = (PackedScene)ResourceLoader.Load("res://Szenen/ServerFormPopup.tscn");
        _server = GetNode<Server>("Server");
        _client = GetNode<Client>("Client");
    }

    public void _on_ServerundClient_pressed()
    {
        _bootServer=true;
        _bootClient = true;
        ShowClientPopup();
    }

    private void _on_Server_starten_pressed()
    {
        _bootServer = true;
        _bootClient = false;
        ShowServerFormPopup();
    }

    private void _on_Client_starten_pressed()
    {
        _bootClient = true;
        _bootServer = false;
        ShowClientPopup();
    }

    private void ShowServerFormPopup()
    {
        Popup popupInstance = (Popup)_serverFormPopup.Instance();
        GetTree().Root.AddChild(popupInstance);
        popupInstance.PopupCentered();

        LineEdit portInput = popupInstance.GetNode<LineEdit>("PortInput");
        portInput.Text = "8915"; 

        popupInstance.Connect(nameof(ServerFormPopup.Confirmed), this, "OnPopupConfirmed");
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
        Error error = Error.Ok;
        if (_bootServer)
            error = _server.StartServer(port);

        if (_bootClient && error == Error.Ok)
        {
            error = _client.ConnectToServer("ws://" + ip + ":" + port);
            _client.playerName = playerName;
        }

        if (error != Error.Ok)
        {
            GD.PrintErr("Error: " + error);
            return;
        }

        PackedScene lobby = (PackedScene)ResourceLoader.Load("res://Szenen/Lobby.tscn");
        Lobby lobbyInstance = (Lobby)lobby.Instance();

        if (_bootServer)
        {
            RemoveChild(_server);
            lobbyInstance.AddChild(_server);
        }
        if (_bootClient)
        {
            RemoveChild(_client);
            lobbyInstance.AddChild(_client);
        }
        GetTree().Root.AddChild(lobbyInstance);
        QueueFree();
    }

    private void _on_Button_pressed()
    {
        GetTree().ChangeScene("res://Szenen/ManuelleRTCVerbindung.tscn");
    }

    private void _on_Back_pressed()
    {
        GetTree().ChangeScene("res://Szenen/MainMenu.tscn");
    }
}
