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
        public bool ReadyPlayerOne;
        public int PlayerTwoId;
        public bool ReadyPlayerTwo;
        public string Raumname;

        public Raum(int PlayerOneId)
        {
            this.PlayerOneId=PlayerOneId;
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
    private Raum _myRoom;
    private ItemList _RaumListe;
    private WebRTCPeerConnection WebRTCPeer = new WebRTCPeerConnection();
    private WebRTCMultiplayer WebRTCMultiplayer = new WebRTCMultiplayer();
    private bool _RTCconnected = false; 

    public override void _Ready()
    {
        _ChatLog= GetNode<RichTextLabel>("ChatMSGBox/ChatLog");
        _PlayerNameLabel= GetNode<Label>("ChatMSGBox/HBoxContainer/PlayerNameLabel");
        _MSGInput = GetNode<LineEdit>("ChatMSGBox/HBoxContainer/MessageInput");
        _RaumListe = GetNode<ItemList>("RaumListe");
        _RaumListe.Connect("item_activated", this, "JoinRoom");

        //Client und (Server) werden vor dem Aufruf als Nodes der Szenen hinzugefügt, daher geht folgendes
        _client = GetNode<Client>("Client");
        _server= GetNode<Server>("Server");
        // verschiedene Szenarien fordern eine unterschiedliche Gestaltung der Lobby
        //Version 1: Anwender hat Client und Server gestartet
        if(_server != null && _client != null)
            GetNode<CheckButton>("ServerOffenLassen").Visible = true;
        //Version 2: Anwender hat nur Server gestartet und kann Lobby passiv beobachten
        else if(_server != null && _client == null)
        {
            GetNode<HBoxContainer>("ChatMSGBox/HBoxContainer").Visible = false;
            GetNode<Button>("RaumErstellen").Visible=false;
            GetNode<Button>("RaumVerlassen").Visible=false;
            GetNode<Button>("SpielStarten").Visible=false;
            GetNode<Button>("Lobby verlassen").Text = "Server beenden";
        }

        if(_client != null)//kann null sein wenn nur server gestartet wurde
        {
            _client.Connect(nameof(Client.MSGReceived), this, "CLientReceivedMSG" );
            _PlayerNameLabel.Text = _client.PlayerName;
            //jeder Client muss die Liste der Räume vom Server zu beginn anfordern
            _client.SendData(JsonConvert.SerializeObject(new msg(Nachricht.OfferRoomData,_client.id,0,"")));
        }

        GetNode<Button>("Lobby verlassen").Connect("pressed", this, nameof(BackToVerbindungseinstellung));
        // Signale zur Verbindungsabbruch behandeln
        WebRTCMultiplayer.Connect("peer_disconnected",GlobalVariables.Instance, nameof(GlobalVariables.WebRTCConnectionFailed));
        WebRTCMultiplayer.Connect("server_disconnected",GlobalVariables.Instance, nameof(GlobalVariables.WebRTCConnectionFailed));
        
        WebRTCPeer.Connect("session_description_created", this, nameof(WebRTCPeerSDPCreated));
        WebRTCPeer.Connect("ice_candidate_created", this, nameof(WebRTCPeerIceCandidateCreated));

        WebRTCPeer.Initialize(GlobalVariables.IceServers);
        WebRTCMultiplayer.Initialize(1,false); // Verbindung wird mit id = 1 gestartet, da nur 2 spieler und nur 1 Verbindung benötigt wird!
        WebRTCMultiplayer.AddPeer(WebRTCPeer,1);
    }

    // ToDo: Folgende Methode geht nicht richtig, denk ich
    private void BackToVerbindungseinstellung()
    {
        if(_server != null)
            _server.StopServer();
        if(_client != null)
            _client.StopConnection();
        QueueFree();
        GetTree().ChangeScene("res://Szenen/Verbindungseinstellungen.tscn");
    }

    public override void _Process(float delta)
    {
        WebRTCMultiplayer.Poll();
        /*
        if(WebRTCPeerConnection.ConnectionState.Connected == WebRTCPeer.GetConnectionState() && _RTCState == 0)
        {
            // RTC Server wurde verbunden!
            // nun Spiler 1 verbinden
            _RTCState=1;
            WebRTCPeer = new WebRTCPeerConnection();
            WebRTCPeer.Connect("session_description_created", this, nameof(WebRTCPeerSDPCreated));
            WebRTCPeer.Connect("ice_candidate_created", this, nameof(WebRTCPeerIceCandidateCreated));
            WebRTCPeer.Initialize(_iceServers);
            WebRTCMultiplayer.AddPeer(WebRTCPeer, _myRoom.PlayerOneId);
            
            // CreateOffer nur wenn client==playerone
            if(_client.id == _myRoom.PlayerOneId)
                WebRTCPeer.CreateOffer();

        }

        if(WebRTCPeerConnection.ConnectionState.Connected == WebRTCPeer.GetConnectionState() && _RTCState == 0)
        {
            // Spieler1 wurde verbunden!
            // nun Spiler 2 verbinden
            _RTCState=2;
            WebRTCPeer = new WebRTCPeerConnection();
            WebRTCPeer.Connect("session_description_created", this, nameof(WebRTCPeerSDPCreated));
            WebRTCPeer.Connect("ice_candidate_created", this, nameof(WebRTCPeerIceCandidateCreated));
            WebRTCPeer.Initialize(_iceServers);
            WebRTCMultiplayer.AddPeer(WebRTCPeer, _myRoom.PlayerTwoId);
            
            // CreateOffer nur wenn client==player2
            if(_client.id == _myRoom.PlayerOneId)
                WebRTCPeer.CreateOffer();
            
        }

        if(WebRTCPeerConnection.ConnectionState.Connected == WebRTCPeer.GetConnectionState() && _RTCState == 2)
        {
            // Spielr2 verbunden Verbindung fertig!
             GlobalVariables.Instance.WebRTC = WebRTCMultiplayer;
        }
        */

        if(WebRTCPeerConnection.ConnectionState.Connected == WebRTCPeer.GetConnectionState())
        {
            GlobalVariables.Instance.WebRTCPeer = WebRTCPeer;
            GlobalVariables.Instance.WebRTC = WebRTCMultiplayer;
        }
        
    }

    private void CLientReceivedMSG(Nachricht state, string msg)
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
        else if(state == Nachricht.checkIn)
        {
            // erst jetzt weiß der Client seine richtige ID und kann WebRTC aufbauen
            // Error eee= WebRTCMultiplayer.Initialize(1,true);
            /*
            WebRTCPeer = new WebRTCPeerConnection();
            WebRTCPeer.Connect("session_description_created", this, nameof(WebRTCPeerSDPCreated));
            WebRTCPeer.Connect("ice_candidate_created", this, nameof(WebRTCPeerIceCandidateCreated));
            WebRTCPeer.Initialize(_iceServers);
            */
            // WebRTCMultiplayer.AddPeer(WebRTCPeer, _client.id);
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

    public List<Raum> RoomList
    {
        get
        {
            return _roomList;
        }
    }

    private void JoinRoom(int index)
    {
        //Prüfen ob der Client nicht selbst schon in nem Raum ist
        if(GetNode<Button>("RaumVerlassen").Disabled == true)
        {
            //msg MSG = new msg(Nachricht.RoomJoin,_client.id,0,"");
            _client.SendData(JsonConvert.SerializeObject(new msg(Nachricht.RoomJoin,_client.id,0,Convert.ToString(_roomList[index].PlayerOneId))));
        }
    }

    private void _on_RumeAkt_pressed()
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

    private void GetWebtRTCTest(int id)
    {
        GetNode<Label>("Label").Text = "NAchricht von " + id +" erhalten"; 
    }

    private void SwitchToLevelSelectionMenu()
    {
        //int test = CustomMultiplayer.GetRpcSenderId();
        if(_server != null)
        {
            if(GetNode<CheckButton>("ServerOffenLassen").Pressed == false)
            {
                _server.StopServer();
                if(GlobalVariables.Instance.Lobby != null)
                    GlobalVariables.Instance.Lobby.QueueFree();
            }
            else
            {
                //lobalVariables.Instance.Lobby = this;
                RemoveChild(_server);
                GlobalVariables.Instance.AddChild(_server);
                _server.Hide();
            }
        }
        //RoomHost sendet Nachricht an Server das beide Clients sich vom Server trennen und er beide aus _ConnectedClients löschen kann
        if(_roomList.Exists(x => x.PlayerOneId == _client.id))
        {
            _client.SendData(JsonConvert.SerializeObject(new msg(Nachricht.PeerToPeerConnectionEstablished,_client.id,0,Convert.ToString(FindOtherRoomMate()))));
        }
        GlobalVariables.Instance.WebRTC = WebRTCMultiplayer;
        _client.QueueFree();
        GetTree().ChangeScene("res://Szenen/LevelSelectionMenu.tscn");
        QueueFree();
    }

    private void _on_PrintRTC_pressed()
    {
        GetNode<Label>("RTCVerbindungen").Text = WebRTCMultiplayer.GetPeers().ToString();
    }

    private void _on_SwitchToLevelSelection_pressed()
    {
        SwitchToLevelSelectionMenu();
    }
}
