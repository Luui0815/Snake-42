using Godot;
using Newtonsoft.Json;
using Snake42;
using System;
using System.Collections.Generic;
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
    private Button _Raum;
    private List<Raum> _RaumList; // Liste der Räume welcher der Client hat
    private Label _Raumbeschreibung;
    private Label _SpielerAnzahl;
    private PackedScene _RaumButton;


    public override void _Ready()
    {
        _ChatLog= GetNode<RichTextLabel>("ChatMSGBox/ChatLog");
        _PlayerNameLabel= GetNode<Label>("ChatMSGBox/HBoxContainer/PlayerNameLabel");
        _MSGInput = GetNode<LineEdit>("ChatMSGBox/HBoxContainer/MessageInput");
        _Raum = GetNode<Button>("RaumListe/Raum");
        _Raumbeschreibung = GetNode<Label>("RaumListe/Raum/Raumbeschreibung");
        _SpielerAnzahl = GetNode<Label>("RaumListe/Raum/Spieleranzahl");
        _RaumButton = (PackedScene)ResourceLoader.Load("res://Szenen/RaumButton.tscn");

        _PlayerNameLabel.Text = _client.PlayerName;
        //jeder Client muss die Liste der Räume vom Server zu beginn anfordern
        _client.SendData(JsonConvert.SerializeObject(new msg(Nachricht.OfferRoomData,_client.id,0,"")));
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
        else if(state == Nachricht.AnswerRoomData)
        {
            _RaumList=JsonConvert.DeserializeObject<List<Raum>>(msg);
            foreach (Raum r in _RaumList)
            {
                CreateNewRoom(r);
            }
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
        foreach(Raum room in _RaumList)
        {
            
        }





        if(_RaumList.Count==1)
        {
            _Raumbeschreibung.Text = room.Raumname;
            if(room.PlayerTwoId==0)
                _SpielerAnzahl.Text = "Spieler 1/2";
            else
                _SpielerAnzahl.Text = "Spieler 2/2";
            _Raum.Visible = true;
            _Raum.Connect("pressed", this, "JoinRoom");
        }
        else 
        {
            Button btn = new Button();
            btn = _Raum; // neuer Speicherplatz für neuen Raum, keine Zeiger werden weitergegeben
            btn.GetNode<Label>("Raumbeschreibung").Text = room.Raumname;
            if(room.PlayerTwoId==0)
                _SpielerAnzahl.Text = "Spieler 1/2";
            else
                _SpielerAnzahl.Text = "Spieler 2/2";
            Vector2 position = new Vector2(_Raum.RectSize.x * _RaumList.Count,(_Raum.RectSize.y + 10) * _RaumList.Count);
            btn.SetPosition(position);
            AddChild(btn);
        }

    }

    public List<Raum> RoomList
    {
        get
        {
            return _RaumList;
        }
    }

    private void JoinRoom()
    {

    }

    private void _on_RumeAkt_pressed()
    {
        //fordert Liste der Räume vom Server an
        _client.SendData(JsonConvert.SerializeObject(new msg(Nachricht.OfferRoomData,_client.id,0,"")));
    }
}
