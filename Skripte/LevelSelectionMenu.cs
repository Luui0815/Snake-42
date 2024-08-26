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
        Multiplayer.SendBytes(GetNode<Label>("SendendeNachricht").Text.ToUTF8());
    }
    
    private void _on_RPCTestButton_pressed()
    {
        Rpc(nameof(TestRpc));
    }

    [Remote]
    private void TestRpc()
    {
        GetNode<Label>("RPCTest").Text += "1";
    }
}
