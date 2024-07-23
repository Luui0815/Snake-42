using Godot;
using System;

public class LevelSelectionSingle : Node2D
{
    private LevelSelection _levelSelection;

    public override void _Ready()
    {
        _levelSelection = GetNode<LevelSelection>("LevelSelection");
    }

    private void OnStartLevelButtonPressed()
    {
        if (_levelSelection.SelectedLevel > 0)
        {
            string levelPath = $"res://Szenen/Levels/Level{_levelSelection.SelectedLevel}.tscn";
            GetTree().ChangeScene(levelPath);
        }
        else
        {
            GD.Print("No level selected.");
        }
    }

    private void _on_Back_pressed()
    {
        GetTree().ChangeScene("res://Szenen/MainMenu.tscn");
    }
}
