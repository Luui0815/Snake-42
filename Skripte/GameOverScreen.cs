using Godot;
using System;

public class GameOverScreen : Popup
{

    public override void _Ready()
    {
        PopupExclusive = true;
    }
    private void _on_RestartLevel_pressed()
    {

    }

}
