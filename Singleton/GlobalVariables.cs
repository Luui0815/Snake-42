using Godot;
using Snake42;
using System;
public partial class GlobalVariables : Node
{
    public static GlobalVariables Instance { get; private set; }

    public Lobby Lobby { get; set; }
    public PackedScene ConfirmationDialog { get; set; }

    public override void _Ready()
    {
        Instance = this;
    }

    public void WebRTCConnectionFailed()
    {
        ConfirmationDialog ErrorPopup = (ConfirmationDialog)GlobalVariables.Instance.ConfirmationDialog.Instance();
        ErrorPopup.Init("Verbindungsabbruch","Peer to Peer Verbindung ist abgebrochen");
        ErrorPopup.Connect("confirmed", this, nameof(BackToMainMenuOrLobby));
        GetTree().Root.AddChild(ErrorPopup);
        ErrorPopup.PopupCentered();
        ErrorPopup.Show();
    }
    private void BackToMainMenuOrLobby()
    {
        GetTree().Root.QueueFree(); // alle Szenen löschen
        if(Instance.Lobby == null)
            GetTree().ChangeScene("res://Szenen/Hauptmenü.tscn");
        else
        {
            GetTree().Root.AddChild(Lobby);
            Lobby.Show();
        }
    }
}