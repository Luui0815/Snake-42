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
    public Lobby Lobby { get; set; }
    public PackedScene ConfirmationDialog { get; set; }

    public WebRTCMultiplayer WebRTC {get; set; } 
    public RTCRoom Room = new RTCRoom();
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
            Lobby.InitRTCConnection();
            Lobby.Server.AddForeignClient(Lobby.Client.id, Lobby.Client.Name); // weil der Server den eigenen Lobbyclient vergisst sobald er eine RTC Verbindung hat
            Lobby._on_RumeAkt_pressed();
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