using Godot;
using System;

public class PeerToPeerMenu : Control
{
    private WebRTCPeerConnection Peer;
    private WebRTCMultiplayer MultiplayerPeer;

    public override void _Ready()
    {
        Peer = new WebRTCPeerConnection();
        var iceServers = new Godot.Collections.Dictionary {
            {"iceServers", new Godot.Collections.Array {
                new Godot.Collections.Dictionary {
                    {"urls", "stun:stun.l.google.com:19302"}
                }
            }}
            //noch wietere Stun Server hinzufügen
            };
        Peer.Initialize(iceServers);

        MultiplayerPeer = new WebRTCMultiplayer();

        // Signale verbinden!
        Peer.Connect("session_description_created", this, nameof(SDPCreated));
        Peer.Connect("ice_candidate_created", this, nameof(WebRTCPeerIceCandidateCreated));
    }

    private void SDPCreated(string type, string sdp)
    {
        Peer.SetLocalDescription(type,sdp);
        // Daten in Label schreiben, damit User ihn austauschen kann!
        GetNode<TextEdit>("TextSelfType").Text = type;
        GetNode<TextEdit>("TextSelfSDPData").Text = sdp;
        // Signal ICE Candiadte Created wird emiittiert => WebRTCPeerIceCandidateCreated
    }

    private void WebRTCPeerIceCandidateCreated(string media, int index, string name) 
    {
        // wieder auf Labels ausgeben
        GetNode<TextEdit>("TextSelfMedia").Text += media + "\n";
        GetNode<TextEdit>("TextSelfIndex").Text += index.ToString() + "\n";
        GetNode<TextEdit>("TextSelfName").Text += name + "\n";
    }


    private void _on_StartConnection_pressed()
    {
        MultiplayerPeer.Initialize(GetNode<TextEdit>("id").Text.ToInt(),false); // eigene id angeben
        MultiplayerPeer.AddPeer(Peer,GetNode<TextEdit>("id").Text.ToInt()); // id des anderen angeben!
        if(Peer.CreateOffer() != Error.Ok)
        {
            GD.Print("Fehler bei Erzeugung SDP!");
            // Signal session_description_created => SDPCreated wird aufgerufen
        }
    }

    private void _on_SetRemoteData_pressed()
    {
        if(Peer.SetRemoteDescription(GetNode<TextEdit>("TextForeignType").Text, GetNode<TextEdit>("TextForeignSDPData").Text) != Error.Ok)
        {
            GD.Print("Fehler bei Erzeugung Remote SDP!");
        }
        // Signal session_description_created => SDPCreated wird aufgerufen
    }

    private void _on_SetICEData_pressed()
    {
        // IceData welche von dem anderen Peer erzeugt wurden als Fremde Daten speichern!
        Peer.AddIceCandidate(GetNode<TextEdit>("TextForeignMedia").Text, Convert.ToInt32(GetNode<TextEdit>("TextForeignIndex").Text), GetNode<TextEdit>("TextForeignName").Text);
    }

    public override void _Process(float delta)
    {
        MultiplayerPeer.Poll();
        if(WebRTCPeerConnection.ConnectionState.Connected == Peer.GetConnectionState())
        {
            GlobalVariables.Instance.WebRTC = MultiplayerPeer;
        }
    }

    private void _on_Button_pressed()
    {
        GetNode<Label>("RpcInfo").Text = MultiplayerPeer.GetPeers().ToString();
    }

    private void _on_weiter_pressed()
    {
        GetTree().ChangeScene("res://Szenen/LevelSelectionMenu.tscn");
        QueueFree();
    }
}
