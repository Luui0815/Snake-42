using Godot;
using Godot.Collections;
using Newtonsoft.Json;
using Snake42;
using System;
using System.Collections.Generic;
using System.Data.Common;
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
    private ItemList _RaumListe;
    private WebRTCPeerConnection WebRTCPeer = new WebRTCPeerConnection();
    private WebRTCMultiplayer WebRTCMultiplayer = new WebRTCMultiplayer();

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

        _client.Connect(nameof(Client.MSGReceived), this, "CLientReceivedMSG" );

        _PlayerNameLabel.Text = _client.PlayerName;

        //jeder Client muss die Liste der Räume vom Server zu beginn anfordern
        _client.SendData(JsonConvert.SerializeObject(new msg(Nachricht.OfferRoomData,_client.id,0,"")));
    }


    public override void _Process(float delta)
    {
      WebRTCPeer.Poll();
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
            WebRTCPeer.SetLocalDescription(data[0],data[1]);
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
            _RaumListe.AddItem(room.Raumname + "      " + (room.PlayerTwoId == 0 ? 1 : 2) + "/2 Spieler" + (room.PlayerTwoId == 0 ? "       Beitretbar": ""));
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
        else
        {
            // ToDo: Popup Fehlermeldung
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
                // Raumeigenschaften werden vom Server grade gebogen
                _client.SendData(JsonConvert.SerializeObject(new msg(Nachricht.RoomLeft,_client.id,0,JsonConvert.SerializeObject(room))));
            }
        }
    }

    private void _on_SpielStarten_pressed()
    {
        // Raumhost fragt Spieler 2 ob er eine WebRTCPeer Verbindung aufbauen möchte
                var iceServers = new Godot.Collections.Dictionary {
            {"iceServers", new Godot.Collections.Array {
                new Godot.Collections.Dictionary {
                    {"urls", "stun:stun.l.google.com:19302"}
                }
            }}
            //noch wietere Stun Server hinzufügen
        };
        Error error;

        error=WebRTCPeer.Initialize(iceServers);

        //SDP Session Description machen
        WebRTCPeer.Connect("session_description_created", this, nameof(WebRTCPeerSDPCreated));
        //ICE Kandidat wurde erstellt
        WebRTCPeer.Connect("ice_candidate_created", this, nameof(WebRTCPeerIceCandidateCreated));
        error=WebRTCMultiplayer.AddPeer(WebRTCPeer, _client.id);
        // hier noc ne Bedingung hin
        error = WebRTCPeer.CreateOffer();
    }

    private void WebRTCPeerSDPCreated(string type, string sdp)
    {
        WebRTCPeer.SetLocalDescription(type,sdp);
        //Senden der Beschreibung an den anderen Client
        msg m = new msg(Nachricht.SDPData,_client.id, _roomList.First(x => x.PlayerOneId == _client.id).PlayerTwoId,type + "|" + sdp);
        _client.SendData(JsonConvert.SerializeObject(m));

    }

    private void WebRTCPeerIceCandidateCreated(string media, int index, string name)
    {
        
    }
}