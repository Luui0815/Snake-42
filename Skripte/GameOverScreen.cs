using Godot;
using System;

public class GameOverScreen : Control
{
    private Label _headline;
    private Label _loseMessage;
    private Button _restartButton;
    private Button _backButton;
    private bool _isGamePaused;

    public override void _Ready()
    {
        _headline = GetNode<Label>("Headline");
        _loseMessage = GetNode<Label>("LoseMessage");
        _restartButton = GetNode<Button>("RestartLevel");
        _restartButton.Connect("pressed", this, nameof(_on_RestartLevel_pressed));
        _backButton = GetNode<Button>("Back");
        _backButton.Connect("pressed", this, nameof(_on_Back_pressed));
    }

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
            _headline.Text = "Game Over";
            _loseMessage.Text = loseMessage;
            _restartButton.Text = "Neu starten";
        }
    }

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

    private void _on_Back_pressed()
    {
        GetTree().Paused = false;
        GetTree().ChangeScene("res://Szenen/Einstellungen.tscn");
    }
}
