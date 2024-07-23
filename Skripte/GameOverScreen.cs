using Godot;
using System;

public class GameOverScreen : Control
{
    public override void _Ready()
    {
        GetNode<Button>("RestartLevel").Connect("pressed", this, nameof(_on_RestartLevel_pressed));
        GetNode<Button>("Back").Connect("pressed", this, nameof(_on_Back_pressed));
    }

    private void _on_RestartLevel_pressed()
    {
        GetTree().Paused = false;
        GD.Print("Button gedrueckt");
        GetTree().ReloadCurrentScene();
    }

    private void _on_Back_pressed()
    {
        GetTree().Paused = false;
        GD.Print("Button gedrueckt");
        GetTree().ChangeScene("res://Szenen/LevelSelectionSingle.tscn");
    }
}
