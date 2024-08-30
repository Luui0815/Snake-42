using System;
using Godot;
using Godot.Collections;

public class LevelSelectionMenu : Control
{
    private WebRTCDataChannel[] dc;

    public override void _Ready()
    {
        Multiplayer.Clear();
        Multiplayer.NetworkPeer = GlobalVariables.Instance.WebRTC;
        Multiplayer.Connect("network_peer_packet",this,nameof(ReceivePacket));
        // Selbstegamchter RPC
    }

    public override void _Process(float delta)
    {
        /*
        GlobalVariables.Instance.WebRTC.Poll();
        if(GlobalVariables.Instance.WebRTC.GetAvailablePacketCount() > 0)
        {
            GetNode<Label>("EmpfangeneNachricht").Text = GlobalVariables.Instance.WebRTC.GetPacket().GetStringFromUTF8();
        }
        */
    }
    
    private void ReceivePacket(int id, byte[] packet )
    {
        GetNode<Label>("EmpfangeneNachricht").Text = packet.GetStringFromUTF8();
    }
    
    private void _on_Senden_pressed()
    {
        Multiplayer.SendBytes(GetNode<TextEdit>("SendendeNachricht").Text.ToUTF8());
        //GlobalVariables.Instance.WebRTC.PutPacket(GetNode<TextEdit>("SendendeNachricht").Text.ToUTF8());
        // PeerInfo
        var d = GlobalVariables.Instance.WebRTC.GetPeers();
        GD.Print(d);
	    GetNode<TextEdit>("PeerInfo").Text = d.ToString();
    }
    private void _on_RPCTestButton_pressed()
    {
        //RPC.MyRPC.rpc(GetPath(),nameof(TestRpc));
    }
    
    private void TestRpc()
    {
        GetNode<Label>("RPCTest").Text += "\nHallo hier RPC Meine ID: " + Multiplayer.GetNetworkUniqueId();
        GD.Print("RPC wurd ausgelöst von: " + GlobalVariables.Instance.WebRTC.GetPeers().ToString());
    }
}
