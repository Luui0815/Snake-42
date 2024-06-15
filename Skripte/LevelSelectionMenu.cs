using System;
using Godot;
using Godot.Collections;

public class LevelSelectionMenu : Control
{
    private LevelSelection _levelSelectionGegeneinander;
    private LevelSelection _levelSelectionMiteinander;
    private Sprite _Spieler1Pfeil;
    private Sprite _Spieler2Pfeil;
    public override void _Ready()
    {
        _levelSelectionGegeneinander = GetNode<LevelSelection>("LevelSelectionGegneinander");
        _levelSelectionGegeneinander.Connect(nameof(LevelSelection.LevelSelected), this, "LevelSelectionGegeneinanderPressed");
        _levelSelectionMiteinander = GetNode<LevelSelection>("LevelSelectionMiteinander");
        _levelSelectionMiteinander.Connect(nameof(LevelSelection.LevelSelected), this, "LevelSelectionMiteinanderPressed");

        _Spieler1Pfeil = GetNode<Sprite>("Spieler1Pfeil");
        _Spieler2Pfeil = GetNode<Sprite>("Spieler2Pfeil");

        //Standartmäßig Level1 gegeneinader anwählen
        _levelSelectionGegeneinander.CheckBoxes[0].Pressed = true;

        CustomMultiplayer = GlobalVariables.Instance.Multiplayer;
        CustomMultiplayer.Connect("network_peer_packet",this,"test");
    }

    private void LevelSelectionGegeneinanderPressed()
    {
        //Pfeil Spieler 1 (also du selbst) je nach Auswahl setzen
        Vector2 position = _levelSelectionGegeneinander.CheckBoxes[_levelSelectionGegeneinander.SelectedLevel - 1].RectGlobalPosition;
        position.x -= 50;
        position.y += 20;
        _Spieler1Pfeil.Position = position; 
        //Spieler 2 Pfeil auf Gegenspieler Computer setzen, dort ist man selber ja Spieler 2
        int[] test= CustomMultiplayer.GetNetworkConnectedPeers();
        int test2 = CustomMultiplayer.GetNetworkUniqueId();
        Rpc(nameof(SetzeSpieler2PfeilPosition),position);
        _levelSelectionMiteinander.UncheckLevelSelection();
    }

    private void LevelSelectionMiteinanderPressed()
    {
        //Pfeil Spieler 1 (also du selbst) je nach Auswahl setzen
        Vector2 position = _levelSelectionMiteinander.CheckBoxes[_levelSelectionMiteinander.SelectedLevel - 1].RectGlobalPosition;
        position.x -= 50;
        position.y += 20;
        _Spieler1Pfeil.Position = position; 
        //Spieler 2 Pfeil auf Gegenspieler Computer setzen, dort ist man selber ja Spieler 2
        RpcId(GlobalVariables.Instance.RPCRoomMateId,nameof(SetzeSpieler2PfeilPosition),position);

        _levelSelectionGegeneinander.UncheckLevelSelection();
    }

    [RemoteSync]
    private void SetzeSpieler2PfeilPosition(Vector2 position)
    {
        // es kann passieren das beide Pfeile auf das selbe zeigen, dann muss man sie hinteriander platzieren
        if(_Spieler1Pfeil.Position == position)
            position.x -= 200;

        _Spieler2Pfeil.Position = position;
    }

    private int TreffeAuswahl()
    {
        // Level Gegeneinander = 1,2,3
        //Level Miteinander = 4,5,6
        //ToDo: Schwierigkeitsgrad auch zufällig auswählen

        //Prüfen ob beide das selbe gewählt haben, dabei ist x komponenet um den Wert oben verschoben
        if(_Spieler1Pfeil.Position.y == _Spieler2Pfeil.Position.y && _Spieler1Pfeil.Position.x == _Spieler2Pfeil.Position.x + 200)
        {
            // Unterscheidung zwischen mit und Gegeneinader
            if(_levelSelectionGegeneinander.SelectedLevel != 0)
            {
                return _levelSelectionGegeneinander.SelectedLevel;
            }
            else
                return _levelSelectionMiteinander.SelectedLevel;
        }
        else
        {
            // wenn beide sich nicht einigen konnten entscheidet der Zufall
            Random r = new Random();
            if(r.Next(0,1) == 0)
                return 1;
            else
                return 1;
        }
        
    }

    private void _on_Button_pressed()
    {
        Rpc("Count");
        bool t = CustomMultiplayer.HasNetworkPeer();
        bool x = CustomMultiplayer.IsNetworkServer();
        CustomMultiplayer.SendBytes("Hallo".ToUTF8());
    }

    [Sync]
    private void Count()
    {
        int test = CustomMultiplayer.GetRpcSenderId();
        GD.Print(test);
        GetNode<Label>("Label").Text =  Convert.ToString(Convert.ToInt32(GetNode<Label>("Label").Text) + 1);
    }

    private void test(int id, byte[] packet )
    {
        string s = packet.GetStringFromUTF8();
        GD.Print(s);
    }
}
