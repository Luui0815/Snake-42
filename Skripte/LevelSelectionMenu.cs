using System;
using Godot;
using Godot.Collections;

public class LevelSelectionMenu : Control
{
    public override void _Ready()
    {
        Multiplayer.Connect("network_peer_packet",this,nameof(ReceivePacket));
    }
    private void ReceivePacket(int id, byte[] packet )
    {
        GetNode<Label>("EmpfangeneNachricht").Text = packet.GetStringFromUTF8();
    }

    private void _on_Senden_pressed()
    {
        Multiplayer.SendBytes(GetNode<TextEdit>("SendendeNachricht").Text.ToUTF8());
        // PeerInfo
        var d = GlobalVariables.Instance.WebRTC.GetPeers();
        GD.Print(d);
	    GetNode<TextEdit>("PeerInfo").Text = d.ToString();
    }
    
    private void _on_RPCTestButton_pressed()
    {
        Rpc(nameof(TestRpc));
    }

    [RemoteSync]
    private void TestRpc()
    {
        GetNode<Label>("RPCTest").Text += "1";
    }
}
