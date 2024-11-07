using Godot;
using System;

public class GameOverScreen : Control
{
    private Label _headline, _loseMessage, _volumeLabel;
    private Button _restartButton;
    private Button _backButton;
    private bool _isGamePaused;
    private bool _isOnlineGame;

    public override void _Ready()
    {
        _headline = GetNode<Label>("Headline");
        _loseMessage = GetNode<Label>("LoseMessage");
        _volumeLabel = GetNode<Label>("VolumeLabel");
        _restartButton = GetNode<Button>("RestartLevel");
        _restartButton.Connect("pressed", this, nameof(_on_RestartLevel_pressed));
        _backButton = GetNode<Button>("Back");
        _backButton.Connect("pressed", this, nameof(_on_Back_pressed));
        PauseMode = PauseModeEnum.Process;
    }

    //ueberprueft, ob Pause oder GameOver und passt Labels an
    public void SetScreenMode(bool isGamePaused, string loseMessage, bool IsOnline = false)
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
        _isOnlineGame = IsOnline;
    }

    //Neustarten des levels
    private void _on_RestartLevel_pressed()
    {
        if (_isGamePaused)
        {
            if(_isOnlineGame)
            {
                // Jeder darf das Spiel wieder starten!
                NetworkManager.NetMan.rpc(GetPath(), nameof(ContinueOnlineGame));
            }
            else
            {
                GetTree().Paused = false;
                QueueFree();
            }
        }
        else
        {
            if(_isOnlineGame)
            {
                // Wenn einer drauf drückt wird bei beiden das Spiel neugestartet
                // Der schnellere gewinnt!
                NetworkManager.NetMan.rpc(GetPath(), nameof(RestartOnlineGame));
            }
            else
            {
                GetTree().Paused = false;
                GetTree().ReloadCurrentScene();
            }
        }
    }

    private void RestartOnlineGame()
    {
        GetTree().Paused = false;
        GetTree().ReloadCurrentScene();
    }

    private void ContinueOnlineGame()
    {
        GetTree().Paused = false;
        QueueFree();
    }

    //Zurueck zum hauptmenu
    private void _on_Back_pressed()
    {
        if(_isOnlineGame)
        {
            // Jeder darf in Einstellungen zurück gehen, dabei wird der 2. immer mitgezogen, egal ob er will oder nicht
            NetworkManager.NetMan.rpc(GetPath(), nameof(GetBackToOptions));
        }
        else
        {
            GetTree().Paused = false;
            GetTree().ChangeScene("res://Szenen/Einstellungen.tscn");
        }
        
    }

    private void GetBackToOptions()
    {
        GetTree().Paused = false;
        GetTree().ChangeScene("res://Szenen/Einstellungen.tscn");
    }
}
