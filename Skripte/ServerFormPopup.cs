using Godot;
using System;

public class ServerFormPopup : Popup
{
    [Signal]
    public delegate void Confirmed(string ip, int port, string Spielername);

    private LineEdit _portInput;

    public override void _Ready()
    {
        _portInput = GetNode<LineEdit>("PortInput");
    }

    private void _on_ConfirmButton_pressed()
    {
        string port = _portInput.Text;
        if (ValidatePort(port))
        {
            try
            {
                EmitSignal(nameof(Confirmed),0, int.Parse(port),"");
            QueueFree();
        }
    }

    private bool ValidatePort(string portStr)
    {
        int port;
        if (int.TryParse(portStr, out port))
        {
            if (port >= 1 && port <= 65535)
            {
                GD.Print("Vom Server eingegebener Port ist gueltig");
                return true;
            }
        }
        GD.Print("Vom Server eingegebener Port ist ungueltig");
        _portInput.Text = "Port ist ungueltig!";
        return false;
    }

    private void _on_CancelButton_pressed()
    {
        _portInput.Text = "";
        Hide();
    }
}
