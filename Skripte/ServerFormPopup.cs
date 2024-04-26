using Godot;
using System;

public class ServerFormPopup : Popup
{
    [Signal]
    public delegate void Confirmed(int port);

    private LineEdit portInput;

    public override void _Ready()
    {
        portInput = GetNode<LineEdit>("PortInput");
    }

    private void _on_ConfirmButton_pressed()
    {
        string port = portInput.Text;
        if (ValidatePort(port))
        {
            EmitSignal(nameof(Confirmed), int.Parse(port));
            Hide();
        }
    }

    private bool ValidatePort(string portStr)
    {
        int port;
        if (int.TryParse(portStr, out port))
        {
            if (port >= 1 && port <= 65535)
            {
                GD.Print("Port ist gueltig");
                return true;
            }
        }
        GD.Print("Port ist ungueltig");
        portInput.Text = "Port ist ungueltig!";
        return false;
    }

    private void _on_CancelButton_pressed()
    {
        portInput.Text = "";
        Hide();
    }
}
