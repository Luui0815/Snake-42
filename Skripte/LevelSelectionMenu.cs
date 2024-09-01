using System;
using Godot;
using Godot.Collections;

public class LevelSelectionMenu : Control
{
    public override void _Ready()
    {
        //Multiplayer.NetworkPeer = GlobalVariables.Instance.WebRTC;
        //Multiplayer.Connect("network_peer_packet",this,nameof(ReceivePacket));
        // Selbstegamchte Klasse für RPC aufrufen
        NetworkManager.NetMan.Init(GlobalVariables.Instance.WebRTC);
        NetworkManager.NetMan.Connect("MessageReceived", this, nameof(ReceiveMsg));
    }

    public override void _Process(float delta)
    {
        /*
        GlobalVariables.Instance.WebRTC.Poll();
        if(GlobalVariables.Instance.WebRTC.GetAvailablePacketCount() > 0)
        {
            GetNode<Label>("EmpfangeneNachricht").Text ="NAchticht von WebRTC Verbindung" + GlobalVariables.Instance.WebRTC.GetPacket().GetStringFromUTF8();
        }
        */
    }

    private void ReceivePacket(int id, byte[] packet )
    {
        GetNode<Label>("EmpfangeneNachricht").Text = "MSG von " + id + ": " + packet.GetStringFromUTF8();
    }
    
    private void ReceiveMsg(string msg)
    {
        GetNode<Label>("EmpfangeneNachricht").Text = msg;
    }
    
    private void _on_Senden_pressed()
    {
        //string msg = GetNode<TextEdit>("SendendeNachricht").Text + " From MultiplayerAPI";
        //Multiplayer.SendBytes(msg.ToUTF8());
        //GlobalVariables.Instance.WebRTC.PutPacket(GetNode<TextEdit>("SendendeNachricht").Text.ToUTF8());
        NetworkManager.NetMan.SendMessage(GetNode<TextEdit>("SendendeNachricht").Text);
        // PeerInfo
        var d = GlobalVariables.Instance.WebRTC.GetPeers();
        GD.Print(d);
	    GetNode<TextEdit>("PeerInfo").Text = d.ToString();
    }
    private void _on_RPCTestButton_pressed()
    {
        NetworkManager.NetMan.rpc(GetPath(),nameof(TestRpc));
    }
    
    private void TestRpc()
    {
        GetNode<Label>("RPCTest").Text += "\nHallo hier RPC";
    }
}
