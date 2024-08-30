using Godot;
using System;
using System.CodeDom;
using System.Runtime.InteropServices;

public class NetworkManager : Node
{
    [Signal]
    public delegate void ChatMessageReceived(string msg);

    // Die Klasse empfängt alle Nachrichten die über WebRTC gehen und macht auch RPCs
    private WebRTCMultiplayer _multiplayer = new WebRTCMultiplayer(); // geht auch mit WebRTC
    
    public static NetworkManager NetMan;

    public void Init(WebRTCMultiplayer multiplayer)
    {
        _multiplayer = multiplayer;
    }
    public override void _Ready()
    {
        NetMan = this;
    }

    public override void _Process(float delta)
    {
        // nach neuen RTC nachrichten ausschau halten und diese dann KAtegoreien einorden
    }
    public void rpc(string NodePath, string Method, params object[] Args)
    {
        GetNode(NodePath).Call(Method,Args);
    }


}
