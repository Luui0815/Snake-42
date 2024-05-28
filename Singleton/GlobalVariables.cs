using Godot;
using Snake42;
using System;
public partial class GlobalVariables : Node
{
    public static GlobalVariables Instance { get; private set; }

    public Lobby Lobby { get; set; }

    public override void _Ready()
    {
        Instance = this;
    }
}