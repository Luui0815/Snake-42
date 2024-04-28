using Godot;
using System;

public class ClientFormPopup : Popup
{
    [Signal]
    public delegate void Confirmed(string ip, int port, string Spielername);

    private LineEdit ipInput;
    private LineEdit portInput;
    private LineEdit playerNameInput;

    public override void _Ready()
    {
        ipInput = GetNode<LineEdit>("IpInput");
        portInput = GetNode<LineEdit>("PortInput");
        playerNameInput = GetNode<LineEdit>("PlayerNameInput");
    }

    private void _on_ConfirmButton_pressed()
    {
        string ip = ipInput.Text;
        string port = portInput.Text;
        string playerName = playerNameInput.Text;  
        if (ValidatePort(port) && ValidateIp(ip))
        {
            //EmitSignal(nameof(Confirmed), ip, int.Parse(port));
            EmitSignal("Confirmed", ip, port, playerName);
            Hide();
        }
    }

    private bool ValidatePort(string portStr)
    {
        if (int.TryParse(portStr, out int port))
        {
            if (port >= 1 && port <= 65535)
            {
                GD.Print("Vom Client eingegebener Port ist gueltig");
                return true;
            }
        }
        GD.Print("Vom Client eingegebener Port ist ungueltig");
        portInput.Text = "Port ist ungueltig!";
        return false;
    }

    private bool ValidateIp(string ip)
    {
        System.Text.RegularExpressions.Regex regexIPv4 = new System.Text.RegularExpressions.Regex(@"^\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}$");
        System.Text.RegularExpressions.Regex regexIPv6 = new System.Text.RegularExpressions.Regex(@"^([0-9a-fA-F]{1,4}:){7,7}[0-9a-fA-F]{1,4}$");

        if (regexIPv4.IsMatch(ip) || regexIPv6.IsMatch(ip))
        {
            GD.Print("Vom Client eingegebene IP ist gueltig");
            return true;
        }
        GD.Print("Vom Client eingegebene IP ist ungueltig");
        ipInput.Text = "IP ist ungueltig!";
        return false;
    }

    private void _on_CancelButton_pressed()
    {
        portInput.Text = "";
        ipInput.Text = "";
        Hide();
    }
}
