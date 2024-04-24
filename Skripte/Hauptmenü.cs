using Godot;
using System;

public class Hauptmenü : Control
{

    public override void _Ready()
    {
        GD.Print("Hallo Welt!");
    }

    public void _on_Verbindung_erstellen_pressed()
    {
        GetTree().ChangeScene("res://Szenen/Verbindungseinstellungen.tscn");
    }

}
