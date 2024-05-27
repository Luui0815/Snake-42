using Godot;
using System;

public class Hauptmenü : Control
{

    public void _on_Verbindung_erstellen_pressed()
    {
        GetTree().ChangeScene("res://Szenen/Verbindungseinstellungen.tscn");
    }

}
