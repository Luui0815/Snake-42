using Godot;
using System;

public class Hauptmenü : Control
{
	public override void _Ready()
	{

	}

    public override void _Process(float delta)
    {
      
    }

	public void _on_VerbindungAufbauen_pressed()
	{
		//GetTree().ChangeSceneToFile("res://scene/ui/LevelSelectionMenu.tscn"); -> so war es in Godot4
		GetTree().ChangeScene("res://Szenen/Verbindungseinstellungen.tscn");
	}
}
