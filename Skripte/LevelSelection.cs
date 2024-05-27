using Godot;
using System;

public class LevelSelection : Control
{
    [Export]
    public int SelectedLevel;

    private CheckBox[] _CheckBoxes = new CheckBox[3];

    public override void _Ready()
    {
        _CheckBoxes[0] = GetNode<CheckBox>("Level1");
        // Level 1 = Standartlevel
        _CheckBoxes[0].Pressed = true;
        SelectedLevel = 1;
        _CheckBoxes[1] = GetNode<CheckBox>("Level2");
        _CheckBoxes[2] = GetNode<CheckBox>("Level3");
    }

    private void _on_Level1_pressed()
    {
        SwitchSelectedLevel(0);
    }

    private void _on_Level2_pressed()
    {
        SwitchSelectedLevel(1);
    }

    private void _on_Level3_pressed()
    {
        SwitchSelectedLevel(2);
    }

    private void SwitchSelectedLevel(int LevelNr)
    {
        if(SelectedLevel != LevelNr + 1)
        {
            for (int i = 0; i <= 2; i++)
            {
                if(i != LevelNr)
                {
                    _CheckBoxes[i].Pressed = false;
                }
            }
            SelectedLevel = LevelNr + 1; // da von 0 beginnend
        }
        else
            _CheckBoxes[LevelNr].Pressed = true;
    }



}
