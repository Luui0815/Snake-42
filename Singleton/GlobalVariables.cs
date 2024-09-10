using Godot;
using Godot.Collections;
using Snake42;
using System;
using System.Linq;
public partial class GlobalVariables : Node
{
    public static GlobalVariables Instance { get; private set; }
    public int LevelDifficulty {get;set;}=0; // 0= einfach, 1= mittel, 2=schwer, 3=Profi
    public int LevelMode {get;set;}=3;//0=miteinander,1=gegeneinader,2=solo

    public Lobby Lobby { get; set; }
    public PackedScene ConfirmationDialog { get; set; }

    public WebRTCMultiplayer WebRTC {get; set; } 

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
        GetTree().Root.QueueFree(); // alle Szenen löschen
        if(Instance.Lobby == null)
            GetTree().ChangeScene("res://Szenen/Hauptmenü.tscn");
        else
        {
            GetTree().Root.AddChild(Lobby);
            Lobby.Show();
        }
    }
    public override void _Process(float delta)
    {
    }
}