using System;
using System.Data;
using Godot;
using Godot.Collections;
using NAudio.Wave;

public class RTCTest : Control
{
    public override void _Ready()
    {
        //Multiplayer.NetworkPeer = GlobalVariables.Instance.WebRTC;
        //Multiplayer.Connect("network_peer_packet",this,nameof(ReceivePacket));
        // Selbstegamchte Klasse für RPC aufrufen
        NetworkManager.NetMan.Init(GlobalVariables.Instance.WebRTC);
        NetworkManager.NetMan.Connect("MessageReceived", this, nameof(ReceiveMsg));
        // Room aufbauen
        int t = GlobalVariables.Instance.WebRTC.GetUniqueId();
    }

    public override void _Process(float delta)
    {
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
        NetworkManager.NetMan.rpc(GetPath(),nameof(TestRpc1));
    }

    int i = 4;
    private void _on_RPCTest2_pressed()
    {
        NetworkManager.NetMan.rpc(GetPath(),nameof(TestRpc2));
    }

    private void _on_RPCTest3_pressed()
    {
        NetworkManager.NetMan.rpc(GetPath(),nameof(TestRpc3),false, true, GetNode<TextEdit>("fürTest2").Text.ToInt());
    }

    private void _on_RPCTest4_pressed()
    {
        Sprite S = new Sprite();
        S.Position = new Vector2(1056,607);
        S.Texture =  ResourceLoader.Load<Texture>("res://.import/icon.png-ec880de02d5dab0aa15458af9d6c53ed.stex");

        NetworkManager.NetMan.rpc(GetPath(), nameof(TestRpc4),false);
    }
    
    private void TestRpc1()
    {
        GetNode<Label>("RPCTest").Text += "\nHallo hier RPC";
    }

    private void TestRpc2()
    {
        i++;
        GetNode<Label>("RPCTest").Text += "\nTest2: i=" + i;
    }

    private void TestRpc3(int Zahl)
    {
        i = Zahl;
        GetNode<Label>("RPCTest").Text += "\nTest2: i=" + i;
    }

    private void TestRpc4(object AS)
    {
        AddChild(AS as Sprite);
    }

    private void _on_AudioAktiv_pressed()
    {
        if(GetNode<Button>("AudioAktiv").Pressed == true)
        {
            NetworkManager.NetMan.AudioIsRecording = true;
            NetworkManager.NetMan.AudioIsPlaying = true;
        }
        else
        {
            NetworkManager.NetMan.AudioIsRecording = false;
            NetworkManager.NetMan.AudioIsPlaying = false;
        }
    }
}
