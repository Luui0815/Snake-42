using Godot;
using Godot.Collections;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

public class PeerToPeerMenu : Control
{
    private WebRTCPeerConnection Peer;
    private WebRTCMultiplayer MultiplayerPeer;

    // Speicherstruktur für die ICe Kandidaten
    private struct IceCandidate
    {
        public string Media {get;}
        public int Index {get;}
        public string Name {get;}

        public IceCandidate(string media, int index, string name)
        {
            Media = media;
            Index = index;
            Name = name;
        }
    }
    private List<IceCandidate> _IceList = new List<IceCandidate>();

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
        MultiplayerPeer.Initialize(1,false);
        MultiplayerPeer.AddPeer(Peer,1);

        // Signale verbinden!
        Peer.Connect("session_description_created", this, nameof(SDPCreated));
        Peer.Connect("ice_candidate_created", this, nameof(WebRTCPeerIceCandidateCreated));
    }

    // Schritt1: Partner A erzeugt seine SPD und ICe Kandidaten und gibt sie aus!
    private void _on_StartConnection_pressed()
    {
        if(Peer.CreateOffer() != Error.Ok)
        {
            GD.Print("Fehler bei Erzeugung SDP!");
            // Signal session_description_created => SDPCreated wird aufgerufen
        }
    }
    private void SDPCreated(string type, string sdp)
    {
        Peer.SetLocalDescription(type,sdp);
        // Daten kompakt in Label schreiben, damit User ihn austauschen kann!
        GetNode<TextEdit>("SelfSDPData").Text = type + "|" + sdp;
        // Signal ICE Candiadte Created wird emiittiert => WebRTCPeerIceCandidateCreated
    }

    private void WebRTCPeerIceCandidateCreated(string media, int index, string name) 
    {
        // es werden mehrere Ice Kandidaten erzeugt, d.h. die Methode wird öfers aufgerufen
        _IceList.Add(new IceCandidate(media, index, name));
        GetNode<TextEdit>("SelfIceCandidates").Text = JsonConvert.SerializeObject(_IceList);
    }

    // Schritt2: PartnerB bekommt die SDP Daten von Partner A und setzt sie als entfernte SDP
    private void _on_SetRemoteData_pressed()
    {
        string[] data = GetNode<TextEdit>("ForeignSDPData").Text.Split("|");
        if(data.Length != 2)
        {
            GD.Print("Falsche SDP Daten!");
            return;
        }

        if(Peer.SetRemoteDescription(data[0], data[1]) != Error.Ok)
        {
            GD.Print("Fehler bei Erzeugung Remote SDP!");
        }
        // Signal session_description_created => SDPCreated wird aufgerufen
    }
    // nachdem Partner B die RemoteSDP Daten gestzt hat, erstellt er seine ICE KAndidaten
    // das ist das gleiche wie wenn A sie erstellt desween sie Methode:WebRTCPeerIceCandidateCreated

    // danach fügt Partner B die IceKandidaten von Partner A seiner Verbindung hinzu
    private void _on_SetICEData_pressed()
    {
        // IceData welche von dem anderen Peer erzeugt wurden als Fremde Daten speichern!
        string data = GetNode<TextEdit>("ForeignIceCandidates").Text;
        List<IceCandidate> ForeignIceList = JsonConvert.DeserializeObject<List<IceCandidate>>(data);

        foreach(IceCandidate ice in ForeignIceList)
        {
            Peer.AddIceCandidate(ice.Media, ice.Index, ice.Name);
        }
    }

    // Nachdem Partner B die Ice Kandidaten an Partner A übermittelt der 
    // setzt auch wie Partner B die SPD remote und ICE von B
    // danach sollte die Verbindung stehen

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
