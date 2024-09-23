using Godot;
using System;

public class Hauptmenü : Control
{
	private AudioStreamPlayer2D _audioplayer;
	private Button _creditsButton;
	private Label _creditsLabel;

    public override void _Ready()
    {
		_creditsButton = GetNode<Button>("CreditsButton");
		_creditsLabel = GetNode<Label>("CreditsLabel");
		_creditsLabel.Visible = false;
		_audioplayer = GetNode<AudioStreamPlayer2D>("MainTheme");
		_audioplayer.Play();
    }

    public void _on_Einzelspieler_pressed()
	{
		GetTree().ChangeScene("res://Szenen/Einstellungen.tscn");
		GlobalVariables.Instance.OnlineGame = false;
	}

	public void _on_Verbindung_erstellen_pressed()
	{
		GetTree().ChangeScene("res://Szenen/Verbindungseinstellungen.tscn");
		GlobalVariables.Instance.ConfirmationDialog = (PackedScene)ResourceLoader.Load("res://Szenen/ConfirmationDialog.tscn");
	}

    private void _on_CreditsButton_pressed()
	{
		_creditsLabel.Visible = !_creditsLabel.Visible;
	}
}
