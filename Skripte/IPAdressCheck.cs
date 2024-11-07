using Godot;
using System;
using System.Net;

public class IPAdressCheck : Control
{
    private HTTPRequest _httpRequestAPICheck;
    private HTTPRequest _httpRequestPublicIPAdress;
    private HTTPRequest _httpRequestCheckIfAdressAndPortIsPublic;
    private TextEdit _infoBox;
    private string _IPAdress;
    private int _CheckIfAPIisAvailableState = 0; // <0: API lieferte keine Antwort ServerResponsecode*-1, 1: Anfrage erfolgreich, Antwort ist neg., 2: Anfrage erfolgreich Antwort ist pos.
    private int _PublicIPAdressState = 0;
    private int _CheckIfAdressAndPortIsPublicState = 0;
    private bool _IPAdressCheckMaintaning = false;
    public override void _Ready()
    {
        _infoBox = GetNode<TextEdit>("InfoBox");

        _httpRequestAPICheck = new HTTPRequest();
        _httpRequestPublicIPAdress = new HTTPRequest();
        _httpRequestCheckIfAdressAndPortIsPublic = new HTTPRequest();

        AddChild(_httpRequestAPICheck);
        AddChild(_httpRequestPublicIPAdress);
        AddChild(_httpRequestCheckIfAdressAndPortIsPublic);

        _httpRequestAPICheck.Connect("request_completed", this, nameof(ResponseCheckIfAPIisAvailable));
        _httpRequestPublicIPAdress.Connect("request_completed", this, nameof(ResponsePublicIPAdress));
        _httpRequestCheckIfAdressAndPortIsPublic.Connect("request_completed", this, nameof(ResponseCheckIfAdressAndPortIsPublic));
    }

    public override void _Process(float delta)
    {
        if(_IPAdressCheckMaintaning)
        {
            // Warten bis _CheckIfAPIisAvailableState != 0
            if(_CheckIfAPIisAvailableState != 0)
            {
                if(_CheckIfAPIisAvailableState < 0)
                {
                    _infoBox.Text = "API zur IP Adressermittlung ist nicht erreichbar.\nServercode: " + (-1 * _CheckIfAPIisAvailableState);
                    _IPAdressCheckMaintaning = false;
                }
                else if(_CheckIfAPIisAvailableState == 1)
                {
                    _infoBox.Text = "API zur IP Adressermittlung ist nicht verfügbar.";
                    _IPAdressCheckMaintaning = false;
                }
                else
                {
                    // APi existiert noch => Öffentliche IP Adresse bestimmen
                    RequestPublicIPAdress();
                }
                _CheckIfAPIisAvailableState = 0;
            }

            // Warten bis _PublicIPAdressState != 0
            if(_PublicIPAdressState != 0)
            {
                if(_PublicIPAdressState < 0)
                {
                    _infoBox.Text = "API zur Ermittlung der öffentlichen IP Adresse ist nicht erreichbar.\nServercode: " + (-1 * _PublicIPAdressState * -1);
                    _IPAdressCheckMaintaning = false;
                }
                else
                {
                    // 1 gibt es nicht da kiene neg. Antwort
                    RequestCheckIfAdressAndPortIsPublic();
                }
                _PublicIPAdressState = 0;
            }

            if(_CheckIfAdressAndPortIsPublicState != 0)
            {
                if(_CheckIfAdressAndPortIsPublicState < 0)
                {
                    _infoBox.Text = "API zur Ermittlung der Erreichbarkeit der öffentlichen IP Adresse und Port ist nicht erreichbar.\nServercode: " + (-1 * _CheckIfAdressAndPortIsPublicState);
                }
                else if(_CheckIfAdressAndPortIsPublicState == 1)
                {
                    _infoBox.Text =    "Der Server verfügt über keine öffentliche IP Adresse.\nEr ist nur von Geräten innerhalb dieses Netzwerkes erreichbar";
                    _infoBox.Text += "\nlokale IP-Adresse: " + GetLocalIPAddress();
                    _infoBox.Text += "\nPort: " + GlobalVariables.Instance.WebSocketServerPort;
                    _infoBox.Text += "\nTipp: Richten Sie an ihrem Router eine Portfreigabe für dieses Gerät ein!";
                }
                else
                {
                    _infoBox.Text =    "Der Server verfügt über eine öffentliche IP Adresse.\nEr ist auch von Geräten außerhalb dieses Netzwerkes erreichbar";
                    _infoBox.Text += "\nöffentliche IP-Adresse: " + _IPAdress;
                    _infoBox.Text += "\nPort: " + GlobalVariables.Instance.WebSocketServerPort;
                }
                _CheckIfAdressAndPortIsPublicState = 0;
                _IPAdressCheckMaintaning = false;
            }
        }
    }

    public void CheckIPAdress()
    {
        // Die Methode prüft ob
        // 1.) Es die API noch gibt
        // 2.) Ermittelt die öffentliche IP Adresse
        // 3.) Prüft ob der Server unter der IP und Port von außen erreichbar ist
        // => alle Ergebnisse werden in InfoBox geschrieben
        // API: https://portchecker.io/docs#tag/default/GET/api/me

        _IPAdressCheckMaintaning = true;
        RequestCheckIfAPIisAvailable();
    }

    private void RequestCheckIfAPIisAvailable()
    {
        var url = "https://portchecker.io/healthz";
        _httpRequestAPICheck.Request(url);
    }
    private void ResponseCheckIfAPIisAvailable(int result, int responseCode, string[] headers, byte[] body)
    {
        if (responseCode == 200)
        {
            string responseBody = body.GetStringFromUTF8();
            if(responseBody == "true")
            {
                _CheckIfAPIisAvailableState = 2;
            }
            else
            {
                // API geht nicht verfügbar/geändert
                _CheckIfAPIisAvailableState = 1;
            }
        }
        else
        {
            _CheckIfAPIisAvailableState = responseCode * -1;
        }
    }


    private void RequestPublicIPAdress()
    {
        var url = "https://portchecker.io/api/me";
        _httpRequestPublicIPAdress.Request(url);
    }
    private void ResponsePublicIPAdress(int result, int responseCode, string[] headers, byte[] body)
    {
        if (responseCode == 200)
        {
            string responseBody = body.GetStringFromUTF8();
            _IPAdress = responseBody;
            _PublicIPAdressState = 2; // Die IP Adresse sollte immer gültig sein
        }
        else
        {
            _PublicIPAdressState = responseCode * -1;
        }
    }

    private void RequestCheckIfAdressAndPortIsPublic()
    {
        var url = $"https://portchecker.io/api/{_IPAdress}/{GlobalVariables.Instance.WebSocketServerPort}";
        _httpRequestCheckIfAdressAndPortIsPublic.Request(url);
    }
    private void ResponseCheckIfAdressAndPortIsPublic(int result, int responseCode, string[] headers, byte[] body)
    {
        if (responseCode == 200)
        {
            string responseBody = body.GetStringFromUTF8();
            if(responseBody == "True")
                _CheckIfAdressAndPortIsPublicState = 2;
            else
                _CheckIfAdressAndPortIsPublicState = 1;
        }
        else
        {
            _CheckIfAPIisAvailableState = responseCode * -1;
        }
    }

    public string GetLocalIPAddress()
    {
        string localIP = string.Empty;

        // Hole alle IP-Adressen des Hosts
        var host = Dns.GetHostEntry(Dns.GetHostName());

        // Suche nach der IPv4-Adresse
        foreach (var ip in host.AddressList)
        {
            if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
            {
                localIP = ip.ToString();
                break;
            }
        }

        return localIP;
    }
}
