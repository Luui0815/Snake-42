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
        MultiplayerPeer.Initialize(Convert.ToInt32(GetNode<TextEdit>("SelfPeerId").Text)); // eigene id angeben
        MultiplayerPeer.AddPeer(Peer,Convert.ToInt32(GetNode<TextEdit>("ForeignPeerId").Text)); // id des anderen angeben!

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
        GetNode<TextEdit>("TextSelfMedia").Text = media;
        GetNode<TextEdit>("TextSelfIndex").Text = index.ToString();
        GetNode<TextEdit>("TextSelfName").Text = name;
    }


    private void _on_StartConnection_pressed()
    {
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

    private void _on_SendHallo_pressed()
    {
        if (Multiplayer.NetworkPeer == null)
        {
            Multiplayer.NetworkPeer = MultiplayerPeer;
            Multiplayer.Connect("network_peer_packet",this,"receiveHallo");
        }
        
        Multiplayer.SendBytes("Hallo!".ToUTF8());
    }

    private void receiveHallo(int id, byte[] packet)
    {
        GD.Print("nachricht von " + id + " : " + packet.GetStringFromUTF8());
    }


    public override void _Process(float delta)
    {
        Peer.Poll();
        //MultiplayerPeer.Poll(); muss einfach nicht
    }
}
