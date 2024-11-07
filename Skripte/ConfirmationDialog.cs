using Godot;
using System;

public class ConfirmationDialog : AcceptDialog
{
    private Label _MessageBox;
    private string _message, _title;
    public override void _Ready()
    {
        _MessageBox = GetNode<Label>("Message");
        _MessageBox.Text = _message;
        WindowTitle = _title;
        PauseMode = PauseModeEnum.Process;
    }

    public void Init(string title, string msg)
    {
        _message = msg;
        _title = title;
    }


}
