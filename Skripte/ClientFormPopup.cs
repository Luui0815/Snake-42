using Godot;
using System;

public class ClientFormPopup : Popup
{
    [Signal]
    public delegate void Confirmed(string ip, int port, string Spielername);

    private LineEdit _ipInput;
    private LineEdit _portInput;
    private LineEdit _playerNameInput;
    private bool _StartServerToo = false;

    public override void _Ready()
    {
        _ipInput = GetNode<LineEdit>("IpInput");
        _portInput = GetNode<LineEdit>("PortInput");
        _playerNameInput = GetNode<LineEdit>("PlayerNameInput");
        if(_StartServerToo)
        {
            GetNode<Label>("IpLabel").QueueFree();
            GetNode<LineEdit>("IpInput").QueueFree();
            
            Vector2 newPos = GetNode<Label>("PortLabel").RectPosition;
            GetNode<Label>("PortLabel").SetPosition(new Vector2(newPos.x, newPos.y + 24));
            newPos = _portInput.RectPosition;
            _portInput.SetPosition(new Vector2(newPos.x, newPos.y + 24));

            newPos = GetNode<Label>("Spielername").RectPosition;
            GetNode<Label>("Spielername").SetPosition(new Vector2(newPos.x, newPos.y + 24));
            newPos = _playerNameInput.RectPosition;
            _playerNameInput.SetPosition(new Vector2(newPos.x, newPos.y + 24));

            _ipInput = null;
        }
    }

    public void ConfigForServerAndClient()
    {
        // Wenn Server gestartet wird darf nicht eine IP Adresse ausgewählt werden
        _StartServerToo = true;
    }

    private void _on_ConfirmButton_pressed()
    {
        if(_StartServerToo)
        {
            if(ValidatePort(_portInput.Text))
            {
                EmitSignal("Confirmed", "127.0.0.1", _portInput.Text, _playerNameInput.Text);
                QueueFree();
            }
        }
        else
        {
            if (ValidatePort(_portInput.Text) && ValidateIp(_ipInput.Text))
            {
                EmitSignal("Confirmed", _ipInput.Text, _portInput.Text, _playerNameInput.Text);
                QueueFree();
            }
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
        _portInput.Text = "Port ist ungueltig!";
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
        _ipInput.Text = "IP ist ungueltig!";
        return false;
    }

    private void _on_CancelButton_pressed()
    {
        _portInput.Text = "";
        _ipInput.Text = "";
        Hide();
    }
}
