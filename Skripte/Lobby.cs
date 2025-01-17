using Godot;
using Godot.Collections;
using Newtonsoft.Json;
using Snake42;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;
using System.Linq;

namespace Snake42
{
    public class Raum
    {
        public int PlayerOneId;
        public int PlayerTwoId;
        public string Raumname;

        public Raum(int PlayerOneId)
        {
            this.PlayerOneId=PlayerOneId;
            PlayerTwoId = 0;
        }
    }
}

public class Lobby : Control
{
    private RichTextLabel _ChatLog;
    private Label _PlayerNameLabel;
    private LineEdit _MSGInput;
    private Client _client;
    private Server _server;
    private List<Raum> _roomList; // Liste der Räume welcher der Client hat
    private ItemList _RaumListe;
    private WebRTCPeerConnection WebRTCPeer;
    private WebRTCMultiplayer WebRTCMultiplayer;
    private bool RTCConnectionEstablished;
    public Server Server {get { return _server;}}
    public Client Client {get { return _client;}}
    private HTTPRequest _httpRequest;
    private Timer _TimeWithoutServerPing; // wird nur initialiert wenn nur client da ist!

    public override void _Ready()
    {
        _ChatLog= GetNode<RichTextLabel>("ChatMSGBox/ChatLog");
        _PlayerNameLabel= GetNode<Label>("ChatMSGBox/HBoxContainer/PlayerNameLabel");
        _MSGInput = GetNode<LineEdit>("ChatMSGBox/HBoxContainer/MessageInput");
        _RaumListe = GetNode<ItemList>("RaumListe");

        //Client und (Server) werden vor dem Aufruf als Nodes der Szenen hinzugefügt, daher geht folgendes
        _client = GetNodeOrNull<Client>("Client");
        _server= GetNodeOrNull<Server>("Server");
        // verschiedene Szenarien fordern eine unterschiedliche Gestaltung der Lobby
        //Version 1: Anwender hat Client und Server gestartet
        if(_server != null && _client != null)
        {
            GetNode<CheckButton>("ServerOffenLassen").Visible = true;
            // IP Adresse des Servers bestimmen_httpRequest = new HTTPRequest();
            IPAdressCheck ipchecker = GetNodeOrNull<IPAdressCheck>("IPAdressCheck");
            if(ipchecker != null)
            {
                ipchecker.Visible = true;
                ipchecker.CheckIPAdress();
            }
            _RaumListe.Connect("item_activated", this, "JoinRoom");
        }
        //Version 2: Anwender hat nur Server gestartet und kann Lobby passiv beobachten
        else if(_server != null && _client == null)
        {
            GetNode<HBoxContainer>("ChatMSGBox/HBoxContainer").Visible = false;
            GetNode<Button>("RaumErstellen").Visible=false;
            GetNode<Button>("RaumVerlassen").Visible=false;
            GetNode<Button>("SpielStarten").Visible=false;
            GetNode<Button>("Lobby verlassen").Text = "Server beenden";

            // IP Adresse des Servers bestimmen_httpRequest = new HTTPRequest();
            IPAdressCheck ipchecker = GetNodeOrNull<IPAdressCheck>("IPAdressCheck");
            if(ipchecker != null)
            {
                ipchecker.Visible = true;
                ipchecker.CheckIPAdress();
            }
        }
        //Version 3: Anwender ist reiner Client und hat sich auf einen Server verbunden
        else if(_server == null && _client != null)
        {
            _RaumListe.Connect("item_activated", this, "JoinRoom");
            _TimeWithoutServerPing = new Timer();
            _TimeWithoutServerPing.OneShot = true;
            _TimeWithoutServerPing.WaitTime  = 5f;
            _TimeWithoutServerPing.Connect("timeout", this, nameof(TooLongTimeWithoutServerPing));
            AddChild(_TimeWithoutServerPing);
            _TimeWithoutServerPing.Start();
        }

        if(_client != null)//kann null sein wenn nur server gestartet wurde
        {
            _client.Connect(nameof(Client.MSGReceived), this, nameof(MSGReceived));
            _PlayerNameLabel.Text = _client.PlayerName;
            //jeder Client muss die Liste der Räume vom Server zu beginn anfordern
            _client.SendData(JsonConvert.SerializeObject(new msg(Nachricht.OfferRoomData,_client.id,0,"")));
            // hier ist der Fehler der immer am Anfang auftritt der ist nicht schlimm!
        }
        else
        {
            // der Serverbetriber soll auch alle Nachrichten in der Lobby sehen!
            GetNode<Button>("RäumeAkt").Visible = false;
            _server.Connect(nameof(Server.ServerInfo), this, nameof(MSGReceived));
        }

        GetNode<Button>("Lobby verlassen").Connect("pressed", this, nameof(BackToVerbindungseinstellung));
        InitRTCConnection();
    }

    private void TooLongTimeWithoutServerPing()
    {
        ErrorMessage("Verbindungsabbruch", "Die Verbindung zum Server wurde unterbrochen.").Connect("popup_hide", _client, nameof(_client.BackToMainMenu));
    }

    private ConfirmationDialog ErrorMessage(string titel, string description)
    {
        ConfirmationDialog ErrorPopup = (ConfirmationDialog)GlobalVariables.Instance.ConfirmationDialog.Instance();
        ErrorPopup.Init(titel,description);
        GetTree().Root.AddChild(ErrorPopup);
        ErrorPopup.PopupCentered();
        ErrorPopup.Show();
        return ErrorPopup;
    }

    public void InitRTCConnection()
    {
        WebRTCPeer =  new WebRTCPeerConnection();
        WebRTCMultiplayer = new WebRTCMultiplayer();
        WebRTCPeer.Connect("session_description_created", this, nameof(WebRTCPeerSDPCreated));
        WebRTCPeer.Connect("ice_candidate_created", this, nameof(WebRTCPeerIceCandidateCreated));

        WebRTCPeer.Initialize(GlobalVariables.IceServers);
        // Es wird zuerst WebRTcMultiplayer mit id 2 initialisiert
        // Drückt ein Spieler aber auf SpielStarten wird er neu mit id = 1 initialisiiert
        // d.h. derjenige welche die verbindung initialisiert hat id 1, ist dann Spieler 1
        // der andere in der Verbindung ist dann id2 = Spieler 2!
        WebRTCMultiplayer.Initialize(2,false); 
        WebRTCMultiplayer.AddPeer(WebRTCPeer,2);

        RTCConnectionEstablished = false;
    }

    private void BackToVerbindungseinstellung()
    {
        if(_server != null)
            _server.StopServer();
        if(_client != null)
            _client.StopConnection();
        Hide();
        QueueFree();
        GetTree().ChangeScene("res://Szenen/Verbindungseinstellungen.tscn");
    }

    public override void _Process(float delta)
    {
        WebRTCMultiplayer.Poll();
        
        if(WebRTCPeerConnection.ConnectionState.Connected == WebRTCPeer.GetConnectionState() && RTCConnectionEstablished == false)
        {
            bool roomfound = false;
            RTCConnectionEstablished = true;
            GlobalVariables.Instance.WebRTC = WebRTCMultiplayer;
            GlobalVariables.Instance.OnlineGame = true;
            NetworkManager.NetMan.Init(WebRTCMultiplayer);
            // Raum suchen in dem sich der Spieler noch befindet!
            foreach(Raum room in _roomList)
            {
                if(room.PlayerOneId == _client.id || room.PlayerTwoId == _client.id)
                {
                    // Raumeigenschaften werden vom Server gerade gebogen
                    _client.SendData(JsonConvert.SerializeObject( new msg(Nachricht.StartGame, _client.id, 0, JsonConvert.SerializeObject(room))));
                    roomfound = true;
                }   
            }
            if(roomfound == false)
                GD.PrintErr("Der Spieler will ein Spiel starten befindet sich aber in keinem Raum! Das ist unmöglich!");
            SwitchToLevelSelectionMenu();
        }
    }

    private void MSGReceived(Nachricht state, string msg)
    {
        if(state == Nachricht.chatMSG)
        {
            updateChatLog(msg);
        }
        else if(state == Nachricht.AnswerRoomData)
        {
            _roomList=JsonConvert.DeserializeObject<List<Raum>>(msg);
            CreateRoomButtons();
        }
        else if (state == Nachricht.SDPData)
        {
            string[] data = msg.Split("|");
            // 0:type, 1:sdp
            WebRTCPeer.SetRemoteDescription(data[0],data[1]);
        }
        else if (state == Nachricht.ICECandidate)
        {
            string[] data = msg.Split('|');
            WebRTCPeer.AddIceCandidate(data[0],Convert.ToInt32(data[1]),data[2]);
            //AddPeerToWebRTC();
        }
        else if (state == Nachricht.ServerWillClosed)
        {
            ConfirmationDialog ErrorPopup = (ConfirmationDialog)GlobalVariables.Instance.ConfirmationDialog.Instance();
            ErrorPopup.Init("Verbindung verloren","Der Server wurde planmäßig heruntergefahren!\nBitte wenden Sie sich an den Serverbetreiber");
            ErrorPopup.Connect("confirmed",this,nameof(BackToVerbindungseinstellung));
            AddChild(ErrorPopup);
            ErrorPopup.PopupCentered();
            ErrorPopup.Show();
        }
        else if (state == Nachricht.KeepAlivePing)
        {
            if(_client != null && _server == null)
                _TimeWithoutServerPing.Start();
        }
    }

    public void _on_MessageInput_text_entered(string new_text)
    {
        if(new_text != "")
        {
            msg message = new msg(Nachricht.chatMSG,_client.id,0,_client.PlayerName + ": " + new_text);
            _client.SendData(JsonConvert.SerializeObject(message));

            _MSGInput.Text = "";
        }
    }

    private void updateChatLog(string txt)
    {
        _ChatLog.Text+= txt + "\n";
    }

    private void _on_RaumErstellen_pressed()
    {
        string[] text = new string[3];
        text[0] = Convert.ToString(_client.id);
        text[1] = "Raum von: " + _client.PlayerName;
        text[2] = "Spieler 1/2";
        msg message = new msg(Nachricht.RoomCreate,_client.id,999,string.Join("|", text));
        _client.SendData(JsonConvert.SerializeObject(message));
    }

    private void CreateRoomButtons()
    {
        _RaumListe.Clear();
        bool ClientInRaum = false;
        foreach(Raum room in _roomList)
        {
            // Wenn Client da dann alles ok
            // Wenn nur Server da, dann etwas anders anzeigen!
            if(_client != null)
            {
                _RaumListe.AddItem(room.Raumname + "      " + (room.PlayerTwoId == 0 ? 1 : 2) + "/2 Spieler" + (room.PlayerTwoId == 0 && room.PlayerOneId != _client.id ? "       Beitretbar": ""));
                if(room.PlayerOneId == _client.id || room.PlayerTwoId == _client.id)
                {
                    ClientInRaum = true;
                }

                //wenn RAum voll ist kann Host das Spiel starten
                if(room.PlayerOneId == _client.id && room.PlayerTwoId != 0)
                {
                    GetNode<Button>("SpielStarten").Disabled = false;
                }
                else
                {
                    GetNode<Button>("SpielStarten").Disabled = true;
                }
            }
            else
            {
                // spezille Ansicht für nur Server
                _RaumListe.AddItem(room.Raumname + "      " + (room.PlayerTwoId == 0 ? 1 : 2) + "/2 Spieler");
            }
        }

        // folgendes nur Interresant wenn Client
        if(_client != null)
        {
            if(ClientInRaum)
            {
                //Spieler ist ebereits in einem raum
                GetNode<Button>("RaumErstellen").Disabled = true;
                GetNode<Button>("RaumVerlassen").Disabled = false;
            }
            else
            {
                GetNode<Button>("RaumErstellen").Disabled = false;
                GetNode<Button>("RaumVerlassen").Disabled = true;
            }
        }
    }

    public List<Raum> RoomList
    {
        get
        {
            return _roomList;
        }
    }

    private void JoinRoom(int index)
    {
        //Prüfen ob der Client nicht selbst schon in nem Raum ist und ob der Raum nicht schon voll ist
        if(GetNode<Button>("RaumVerlassen").Disabled == true && _roomList[index].PlayerTwoId == 0)
        {
            //msg MSG = new msg(Nachricht.RoomJoin,_client.id,0,"");
            _client.SendData(JsonConvert.SerializeObject(new msg(Nachricht.RoomJoin,_client.id,0,Convert.ToString(_roomList[index].PlayerOneId))));
        }
    }

    public void _on_RumeAkt_pressed()
    {
        //fordert Liste der Räume vom Server an
        _client.SendData(JsonConvert.SerializeObject(new msg(Nachricht.OfferRoomData,_client.id,0,"")));
    }

    private void _on_RaumVerlassen_pressed()
    {
        foreach(Raum room in _roomList)
        {
            if(room.PlayerOneId == _client.id || room.PlayerTwoId == _client.id)
            {
                // Raumeigenschaften werden vom Server gerade gebogen
                _client.SendData(JsonConvert.SerializeObject(new msg(Nachricht.RoomLeft,_client.id,0,JsonConvert.SerializeObject(room))));
            }
        }
    }

    private void _on_SpielStarten_pressed()
    {
        WebRTCMultiplayer.Initialize(1,false); // Verbindung wird mit id = 1 gestartet, da nur 2 spieler und nur 1 Verbindung benötigt wird!
        // WebRTCMultiplayer.AddPeer(WebRTCPeer,1); => hier würden auf dem der hier rein kommt 2 Verbindungen aufgebaut werden!

        if(WebRTCPeer.CreateOffer() != Error.Ok)
        {
            GD.Print("Fehler bei Erstellung SPD");
            ConfirmationDialog ErrorPopup = (ConfirmationDialog)GlobalVariables.Instance.ConfirmationDialog.Instance();
            ErrorPopup.Init("Verbindungsfehler","Peer to Peer verbindung konnt nicht aufgebaut werden");
            GetTree().Root.AddChild(ErrorPopup);
            ErrorPopup.PopupCentered();
            ErrorPopup.Show();
        }
    }

    private void WebRTCPeerSDPCreated(string type, string sdp)
    {
        WebRTCPeer.SetLocalDescription(type,sdp);
        msg m = new msg(Nachricht.SDPData,_client.id,FindOtherRoomMate(),type + "|" + sdp);
        _client.SendData(JsonConvert.SerializeObject(m));

    }

    private int FindOtherRoomMate()
    {
        foreach (Raum r in _roomList)
        {
            if(r.PlayerOneId == _client.id)
                return r.PlayerTwoId;
            if(r.PlayerTwoId == _client.id)
                return r.PlayerOneId;
        }
        return -1;
    }

    private void WebRTCPeerIceCandidateCreated(string media, int index, string name)
    {
        msg m = new msg(Nachricht.ICECandidate,_client.id,FindOtherRoomMate(),media + "|" + index + "|" + name);
        _client.SendData(JsonConvert.SerializeObject(m));
    }

    private void SwitchToLevelSelectionMenu()
    {
        // Wenn der Server offen gelassen werden soll, d,h, andere können sich während man selbst spielt noch auf den server verbinden und spiele starten, dann Lobby nicht löschen
        // in allen andern Fällen weg

        if(_server != null && GetNode<CheckButton>("ServerOffenLassen").Pressed == true)
        {
            // Lobby muss mit Server erhalten werden
            // in Lobby ist auch noch ein Client, der bleibt als einziger erhalten!, mit der Lobby!
            GlobalVariables.Instance.Lobby = this;
            GetNode<Button>("SpielStarten").Disabled = true;
            Hide();
        }
        else
        {
            GlobalVariables.Instance.Lobby = null;
            if(_server != null)
                _server.StopServer();
            if(_client != null)
                _client.StopConnection();
            QueueFree();
        }
        // GetTree().ChangeScene("res://Szenen/RTCTest.tscn");
        GetTree().ChangeScene("res://Szenen/Einstellungen.tscn");
    }

    private void _on_PrintRTC_pressed()
    {
        GetNode<Label>("RTCVerbindungen").Text = WebRTCMultiplayer.GetPeers().ToString();
    }
}
