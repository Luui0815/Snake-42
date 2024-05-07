using Godot;
using Newtonsoft.Json;
using Snake42;
using System;

public class RaumButton : Button
{
    [Signal]
    public delegate void RaumBeitreten(string room);
    private Label _Raumbeschreibung;
    private Label _Spieleranahl;
    private Button _Beitreten;
    private Raum room;
    public override void _Ready()
    {
        _Raumbeschreibung = GetNode<Label>("Raumbeschreibung");
        _Spieleranahl = GetNode<Label>("Spieleranzahl");
        _Beitreten = GetNode<Button>("Beitreten");
    }

    public void SetRaumbeschreibung(string text)
    {
        _Raumbeschreibung.Text = text; 
    }

    public void SetSpieleranzahl(int Anzahl)
    {
        _Spieleranahl.Text = "Spieler " + Anzahl + "/2";
    }

    public void SetRaumId(Raum room)
    {
        this.room = room;
    }

    public void SetAttributes(Raum room)
    {
        _Raumbeschreibung = GetNode<Label>("Raumbeschreibung");
        _Spieleranahl = GetNode<Label>("Spieleranzahl");
        _Beitreten = GetNode<Button>("Beitreten");

        _Raumbeschreibung.Text = room.Raumname; 
        _Spieleranahl.Text = "Spieler " + (room.PlayerTwoId == 0 ? 1 : 2) + "/2";
        this.room = room;
    }

    private void _on_Beitreten_pressed()
    {
        EmitSignal("RaumBeitreten",JsonConvert.SerializeObject(room));
    }
}
