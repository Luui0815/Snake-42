using Godot;
using Snake42;
using System;
using System.Linq;

namespace Snake42
{
    class WebRTCRoom
    {
        public readonly int PlayerOneId;
        public readonly int PlayerTwoId;
        public readonly int SelfId;

        public WebRTCRoom(int PlayerOneId, int PlayerTwoId, int SelfId)
        {
            this.PlayerOneId = PlayerOneId;
            this.PlayerTwoId = PlayerTwoId;
            this.SelfId = SelfId;
        }
    }
}

public class LevelSelectionMenu : Control
{
    private LevelSelection _levelSelectionGegeneinander;
    private LevelSelection _levelSelectionMiteinander;
    private Sprite _Spieler1Pfeil;
    private Sprite _Spieler2Pfeil;
    private WebRTCRoom _room;
    public override void _Ready()
    {
        _levelSelectionGegeneinander = GetNode<LevelSelection>("LevelSelectionGegneinander");
        _levelSelectionGegeneinander.Connect(nameof(LevelSelection.LevelSelected), this, "LevelSelectionGegeneinanderPressed");
        _levelSelectionMiteinander = GetNode<LevelSelection>("LevelSelectionMiteinander");
        _levelSelectionMiteinander.Connect(nameof(LevelSelection.LevelSelected), this, "LevelSelectionMiteinanderPressed");

        _Spieler1Pfeil = GetNode<Sprite>("_Spieler1Pfeil");
        _Spieler2Pfeil = GetNode<Sprite>("_Spieler2Pfeil");

        // bestimmen des WebRTCRooms
        int[] Teilnehmer = Multiplayer.GetNetworkConnectedPeers();
        if(Teilnehmer.Length != 2)
        {
            GD.Print("Zu viele Teilnehmer im WebRTC Raum!");
            PauseMode = PauseModeEnum.Stop;
            //ToDo: Fehlermeldung
        }
        else if(Teilnehmer[0] != Multiplayer.GetNetworkUniqueId() && Teilnehmer[1] != Multiplayer.GetNetworkUniqueId())
        {
            GD.Print("Im WebRTC Raum ist SelfId nicht in PlayerOneId und PlayerTwoId enthalten");
            PauseMode = PauseModeEnum.Stop;
            //ToDo: Fehlermeldung
        }
        else
            _room = new WebRTCRoom(Teilnehmer[0],Teilnehmer[1],Multiplayer.GetNetworkUniqueId());

    }

    private void LevelSelectionGegeneinanderPressed()
    {
        _levelSelectionMiteinander.UncheckLevelSelection();
    }

    private void LevelSelectionMiteinanderPressed()
    {
        _levelSelectionGegeneinander.UncheckLevelSelection();
    }



}
