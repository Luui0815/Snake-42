using Godot;
using Newtonsoft.Json;
using Snake42;
using System;

public class Lobby : Control
{
    private RichTextLabel _ChatLog;
    private Label _PlayerNameLabel;
    private LineEdit _MSGInput;
    private Client _client;
    public override void _Ready()
    {
        _ChatLog= GetNode<RichTextLabel>("ChatMSGBox/ChatLog");
        _PlayerNameLabel= GetNode<Label>("ChatMSGBox/HBoxContainer/PlayerNameLabel");
        _MSGInput = GetNode<LineEdit>("ChatMSGBox/HBoxContainer/MessageInput");
        // alle Knoten werden nicht gefunden
        _PlayerNameLabel.Text = _client.PlayerName;
    }
    
    public void Init(Client c)
    {
        _client = c;
        _client.Connect(nameof(Client.MSGReceived), this, "CLientReceivedMSG" );
        c._Process(1);
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
}
