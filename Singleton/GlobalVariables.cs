using Godot;
using Godot.Collections;
using Snake42;
using System;
using System.Linq;
public partial class GlobalVariables : Node
{
    public class RTCRoom:Node
    {
        // Die Klasse dient zur einfacheren Kommunikation zwischen den beiden Spielern welche Snake zusammenspielen
        public string MyName { get; set; }
        public string MatesName { get; set; }
        public bool IamPlayerOne { get; set; } = false;
        private ulong myTimestamp;
        private bool decisionMade = false;
        public RTCRoom(string myPlayerName)
        {
            MyName = myPlayerName;
        }

        // Methode zum Senden des Handshake mit Name und OS-Zeit
        public void SendHandshake()
        {
            myTimestamp = OS.GetSystemTimeMsecs(); // OS-Zeit in Millisekunden

            // Nachricht mit Name und Zeit senden
            NetworkManager.NetMan.rpc(GetPath(),nameof(ReceiveHandshake), false, MyName, myTimestamp);
        }

        // Diese Methode empfängt das Handshake von einem anderen Spieler
        public void ReceiveHandshake(string matesName, ulong matesTimestamp)
        {
            // Nur weitermachen, wenn die Entscheidung noch nicht getroffen wurde
            if (!decisionMade)
            {
                MatesName = matesName;

                // Vergleiche die Zeitstempel, um zu entscheiden, wer Spieler 1 ist
                if (myTimestamp < matesTimestamp)
                {
                    IamPlayerOne = true;  // Ich bin Spieler 1
                }
                else if (myTimestamp > matesTimestamp)
                {
                    IamPlayerOne = false; // Der andere Spieler ist Spieler 1
                }
                else
                {
                    // Falls die Timestamps gleich sind (sehr unwahrscheinlich), vergleiche den Namen
                    IamPlayerOne = String.Compare(MyName, matesName, StringComparison.Ordinal) < 0;
                }

                decisionMade = true;

                // Nachricht zurücksenden, um dem anderen Spieler das Ergebnis mitzuteilen
                NetworkManager.NetMan.rpc(GetPath(),nameof(SetPlayerRole), false, IamPlayerOne);
            }
        }
        // Synchronisiere die Entscheidung bei beiden Spielern
        public void SetPlayerRole(bool isPlayerOne)
        {
            IamPlayerOne = isPlayerOne;

            if (IamPlayerOne)
            {
                GD.Print("Ich bin Spieler 1!");
            }
            else
            {
                GD.Print("Ich bin Spieler 2!");
            }
        }
    }

    public static GlobalVariables Instance { get; private set; }
    public int LevelDifficulty {get;set;}=0; // 0= einfach, 1= mittel, 2=schwer, 3=Profi
    public int LevelMode {get;set;}=3;//0=miteinander,1=gegeneinader,2=solo

    public Lobby Lobby { get; set; }
    public PackedScene ConfirmationDialog { get; set; }

    public WebRTCMultiplayer WebRTC {get; set; } 
    public RTCRoom Room;
    public static string MyPlayerName;

    static public readonly Dictionary IceServers = new Dictionary 
    {
        {"iceServers", 
            new Godot.Collections.Array 
            {
                new Godot.Collections.Dictionary 
                {
                    {"urls", "stun:stun.l.google.com:19302"}
                }
            }
        }
        // Weiter Stun Server hinzufügen!
    };


    public override void _Ready()
    {
        Instance = this;
    }
    
    public void BackToMainMenuOrLobby()
    {
        if(Instance.Lobby == null)
            GetTree().ChangeScene("res://Szenen/MainMenu.tscn");
        else
        {
            GetTree().CurrentScene.QueueFree();
            GetTree().Root.AddChild(Lobby);
            Lobby.Show();
        }
    }
    public void ShowPopUp()
    {

    }
    public override void _Process(float delta)
    {
    }
}