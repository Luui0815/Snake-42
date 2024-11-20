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
    }

    public static GlobalVariables Instance { get; private set; }
    public int LevelDifficulty {get;set;}=0; // 0= einfach, 1= mittel, 2=schwer, 3=Profi
    public int LevelMode {get;set;}=3;//0=miteinander,1=gegeneinader,2=solo
    public bool OnlineGame = false; // Wichtig für das Einstellungsmenu, je nachdem ob true oder false zur Aufrufzeit kommen die grünen hacken oder nicht
                                    // und die Signale werden verknüpft!

    // Für Verbindungen
    public Lobby Lobby { get; set; } = null;
    public PackedScene ConfirmationDialog { get; set; }

    public WebRTCMultiplayer WebRTC {get; set; } 
    public RTCRoom Room = new RTCRoom();
    public string WebSocketServerPort;
    static public readonly Dictionary IceServers = new Dictionary 
{
    {"iceServers", 
        new Godot.Collections.Array 
        {
            // Google STUN-Server
            new Dictionary 
            {
                {"urls", "stun:stun.l.google.com:19302"}
            },

            // Mozilla STUN-Server
            new Dictionary 
            {
                {"urls", "stun:stun.services.mozilla.com"}
            },

            // Coturn TURN-Server (öffentlich verfügbar)
            new Godot.Collections.Dictionary 
            {
                {"urls", "turn:turnServer1.org:3478"},
                {"username", "user"},
                {"credential", "password"}
            },
            new Dictionary 
            {
                {"urls", "turn:turnServer2.org:3478"},
                {"username", "user"},
                {"credential", "password"}
            },

            // openProject Turn Server
            new Dictionary
            {
                {"urls", "turn:openrelay.metered.ca:80"},
                {"username", "openrelayproject"},
                {"credentials", "openrelayproject"}
            },

            // Free TURN-Server von `turnserver.org` (für Testzwecke)
            new Dictionary 
            {
                {"urls", "turn:turnserver.org:3478"},
                {"username", "test"},
                {"credential", "test"}
            },

            // Stun-Server von `stun.l.google.com`
            new Dictionary 
            {
                {"urls", "stun:stun.l.google.com:19302"}
            },

            // `stun:stun4.l.google.com` (weiterer Google STUN-Server)
            new Dictionary 
            {
                {"urls", "stun:stun4.l.google.com:19302"}
            },

            // `stun:stun.sipgate.net` (STUN-Server von Sipgate)
            new Dictionary 
            {
                {"urls", "stun:stun.sipgate.net"}
            },

            // STUN-Server von `stun.ideasip.com`
            new Dictionary 
            {
                {"urls", "stun:stun.ideasip.com"}
            },

            // TURN-Server von `turn.anyfirewall.com` (Testserver, öffentlich zugänglich)
            new Dictionary 
            {
                {"urls", "turn:turn.anyfirewall.com:443"},
                {"username", "test"},
                {"credential", "test"}
            },
            // Weitere STUN-Server
            new Dictionary 
            {
                {"urls", "stun:stun1.l.google.com:19302"}
            },
            new Dictionary 
            {
                {"urls", "stun:stun2.l.google.com:19302"}
            },
            new Dictionary 
            {
                {"urls", "stun:stun3.l.google.com:19302"}
            },
            new Dictionary 
            {
                {"urls", "stun:stun4.l.google.com:19302"}
            },
        }
    }
};


    public float PingTimeSnake1;
    public float Snake1diff;
    public float Snake1LatencyFactor;
    public float PingTimeSnake2;
    public float Snake2diff;
    public float Snake2LatencyFactor;
    public Vector2[] Snake1Body = new Vector2[0];
    public Vector2[] Snake2Body = new Vector2[0];

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
            Lobby.InitRTCConnection();
            // Lobby.Server.AddForeignClient(Lobby.Client.id, Lobby.Client.PlayerName); // weil der Server den eigenen Lobbyclient vergisst sobald er eine RTC Verbindung hat
            Lobby._on_RumeAkt_pressed();
            Lobby.Show();
        }
        GetTree().Paused = false;
    }
}