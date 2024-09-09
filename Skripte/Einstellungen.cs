using Godot;
using System;

public class Einstellungen : Control
{
    private OptionSelection _SelectDifficulty;
    private OptionSelection _SelectLevel;
    private OptionSelection _SelectMode;

    public override void _Ready()
    {
        // Für SelectDificullty:
        // Suche den ursprünglichen Node
        CheckBox originalSelectDifficulty = GetNode<CheckBox>("SelectDifficulty");

        // Erstelle eine neue Instanz von OptionSelection
        _SelectDifficulty = new OptionSelection(4, new string[] { "Leicht", "Mittel", "Schwer", "Profi" });
        _SelectDifficulty.Name = originalSelectDifficulty.Name; // macht es einfacher


        // Setze die Position von _SelectDifficulty auf die Position des ursprünglichen Nodes
        _SelectDifficulty.RectPosition = originalSelectDifficulty.RectPosition;

        // Entferne den ursprünglichen Node und füge den neuen hinzu
        RemoveChild(originalSelectDifficulty);
        originalSelectDifficulty.QueueFree();
        AddChild(_SelectDifficulty);


        //Für SelectLevel-------------------------------------------------------------------------
        // Suche den ursprünglichen Node
        CheckBox originalSelectLevel = GetNode<CheckBox>("SelectLevel");

        // Erstelle eine neue Instanz von OptionSelection
        _SelectLevel = new OptionSelection(3, new string[] { "Level 1", "Level 2", "Level 3"});
        _SelectLevel.Name = originalSelectLevel.Name; // macht es einfacher


        // Setze die Position von _SelectDifficulty auf die Position des ursprünglichen Nodes
        _SelectLevel.RectPosition = originalSelectLevel.RectPosition;

        // Entferne den ursprünglichen Node und füge den neuen hinzu
        RemoveChild(originalSelectLevel);
        originalSelectLevel.QueueFree();
        AddChild(_SelectLevel);


        //Für SelectMode-------------------------------------------------------------------------
        // Suche den ursprünglichen Node
        CheckBox originalSelectMode = GetNode<CheckBox>("SelectMode");

        // Erstelle eine neue Instanz von OptionSelection
        _SelectMode = new OptionSelection(3, new string[] { "Miteinander", "Gegeneiander", "Einzelspieler"}, 2);
        _SelectMode.Name = originalSelectMode.Name; // macht es einfacher


        // Setze die Position von _SelectDifficulty auf die Position des ursprünglichen Nodes
        _SelectMode.RectPosition = originalSelectMode.RectPosition;

        // Entferne den ursprünglichen Node und füge den neuen hinzu
        RemoveChild(originalSelectMode);
        originalSelectMode.QueueFree();
        AddChild(_SelectMode);
    }

    private void _on_Start_pressed()
    {
        GlobalVariables.Instance.LevelDifficulty = _SelectDifficulty.SelectedOption;
        GlobalVariables.Instance.LevelMode = _SelectMode.SelectedOption;
        GetTree().ChangeScene($"res://Szenen/Levels/Level{_SelectLevel.SelectedOption + 1}.tscn");
    }

    private void _on_Back_pressed()
    {
        GetTree().ChangeScene("res://Szenen/MainMenu.tscn");
    }
}
