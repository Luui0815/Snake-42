using Godot;
using System;

public class Hauptmenü : Control
{
    public void _on_Einzelspieler_pressed()
    {
        GetTree().ChangeScene("res://Szenen/Levels/Level1.tscn");
    }

    public void _on_Verbindung_erstellen_pressed()
    {
        GetTree().ChangeScene("res://Szenen/Verbindungseinstellungen.tscn");
        GlobalVariables.Instance.ConfirmationDialog = (PackedScene)ResourceLoader.Load("res://Szenen/ConfirmationDialog.tscn");
    }

}
