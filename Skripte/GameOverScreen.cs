using Godot;
using System;

public class GameOverScreen : Control
{
    private Label _headline, _loseMessage, _volumeLabel;
    private Button _restartButton;
    private Button _backButton;
    private bool _isGamePaused;

    public override void _Ready()
    {
        _headline = GetNode<Label>("Headline");
        _loseMessage = GetNode<Label>("LoseMessage");
        _volumeLabel = GetNode<Label>("VolumeLabel");
        _restartButton = GetNode<Button>("RestartLevel");
        _restartButton.Connect("pressed", this, nameof(_on_RestartLevel_pressed));
        _backButton = GetNode<Button>("Back");
        _backButton.Connect("pressed", this, nameof(_on_Back_pressed));
    }

    //ueberprueft, ob Pause oder GameOver und passt Labels an
    public void SetScreenMode(bool isGamePaused, string loseMessage)
    {
        _isGamePaused = isGamePaused;

        if (_isGamePaused)
        {
            _headline.Text = "Pause";
            _restartButton.Text = "Fortsetzen";
        }
        else
        {
            _volumeLabel.Visible = false;
            _headline.Text = "Game Over";
            _loseMessage.Text = loseMessage;
            _restartButton.Text = "Neu starten";
        }
    }

    //Neustarten des levels
    private void _on_RestartLevel_pressed()
    {
        if (_isGamePaused)
        {
            GetTree().Paused = false;
            QueueFree();
        }
        else
        {
            GetTree().Paused = false;
            GetTree().ReloadCurrentScene();
        }
    }

    //Zurueck zum hauptmenu
    private void _on_Back_pressed()
    {
        GetTree().Paused = false;
        GetTree().ChangeScene("res://Szenen/Einstellungen.tscn");
    }
}
