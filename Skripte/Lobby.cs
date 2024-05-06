using Godot;
using Newtonsoft.Json;
using Snake42;
using System;
using System.Collections.Generic;
using System.Linq;

public class Lobby : Control
{
    private class Raum
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

    private RichTextLabel _ChatLog;
    private Label _PlayerNameLabel;
    private LineEdit _MSGInput;
    private Client _client;
    private List<Raum> _RaumList=new List<Raum>();
    private Button _Raum;
    private Label _Raumbeschreibung;
    private Label _SpielerAnzahl;
    private MultiplayerAPI multiplayer;
    public override void _Ready()
    {
        _ChatLog= GetNode<RichTextLabel>("ChatMSGBox/ChatLog");
        _PlayerNameLabel= GetNode<Label>("ChatMSGBox/HBoxContainer/PlayerNameLabel");
        _MSGInput = GetNode<LineEdit>("ChatMSGBox/HBoxContainer/MessageInput");
        _Raum = GetNode<Button>("RaumListe/Raum");
        _Raumbeschreibung = GetNode<Label>("RaumListe/Raum/Raumbeschreibung");
        _SpielerAnzahl = GetNode<Label>("RaumListe/Raum/Spieleranzahl");

        // alle Knoten werden nicht gefunden
        _PlayerNameLabel.Text = _client.PlayerName;
    }
    
    public void Init(Client c)
    {
        _client = c;
        _client.Connect(nameof(Client.MSGReceived), this, "CLientReceivedMSG" );
    }


    public override void _Process(float delta)
    {
      
    }

    private void CLientReceivedMSG(Nachricht state, string msg)
    {
        if(state == Nachricht.chatMSG)
        {
            updateChatLog(msg);
        }
        else if(state == Nachricht.RoomCreate)
        {
            CreateNewRoom(msg.Split("|"));
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

    private void CreateNewRoom(string[] text)
    {
        //text[0] -> Raumersteller Id
        //text[1] -> Raum von XXX
        //text[2] -> Spieler 1/2
        _RaumList.Add(new Raum(Convert.ToInt32(text[0])));
        

        if(_RaumList.Count==1)
        {
            _Raumbeschreibung.Text = text[1];
            _SpielerAnzahl.Text = text[2];
            _Raum.Visible = true;
            _Raum.Connect("pressed", this, "JoinRoom");
        }
        else 
        {
            Button btn = new Button();
            btn = _Raum; // neuer Speicherplatz für neuen Raum, keine Zeiger werden weitergegeben
            btn.GetNode<Label>("RaumListe/Raum/Raumbeschreibung").Text = "Raum von: " + _client.PlayerName;
            btn.GetNode<Label>("RaumListe/Raum/Spieleranzahl").Text = "Spieler 1/2";
            Vector2 position = new Vector2(_Raum.RectSize.x * _RaumList.Count,(_Raum.RectSize.y + 10) * _RaumList.Count);
            btn.SetPosition(position);
            _Raum.Connect("pressed", this, "JoinRoom");
        }

    }

    private void JoinRoom()
    {

    }
}
