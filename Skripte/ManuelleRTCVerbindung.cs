using Godot;
using Godot.Collections;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

public class ManuelleRTCVerbindung : Control
{
    private WebRTCPeerConnection Peer;
    private WebRTCMultiplayer MultiplayerPeer;

    // um es dem benutzer einfacher zu machen werden die SDB und ICe Kandidaten zu einer großen Speicherstrucktur
    // zusammengeafsst und einmalig als Sting in Json übertragen, nicht SDP und ICE einzeln
    private class WebRTCData
    {
        // Daten für SDP
        public struct SDPData
        {
            public string Type {get;}
            public string SDP {get;}

            public SDPData(string type, string sdp)
            {
                Type = type;
                SDP = sdp;
            }
        }
        public SDPData SDP_Data {get; set;}
    
        // Daten für ICE Kandidaten
        public struct IceCandidateData
        {
            public string Media {get;}
            public int Index {get;}
            public string Name {get;}

            public IceCandidateData(string media, int index, string name)
            {
                Media = media;
                Index = index;
                Name = name;
            }
        }
        // da es im Normalfall deutlich mehr als 1 ICEKAndidaten gibt alle Kandidaten in einer Liste sammeln
        public List<IceCandidateData> ListIceCandidates {get;}
        // Konstuktor und co.
        public WebRTCData(string type, string sdp)
        {
            // zuerst muss man nur die SDP Daten haben, danach werden die einzelnen ICE Kandidaten eingtragen
            SDP_Data = new SDPData(type, sdp);
            ListIceCandidates = new List<IceCandidateData>();
        }

        public void AddIce(string media, int index, string name)
        {
            ListIceCandidates.Add(new IceCandidateData(media, index, name));
        }
    }

    private WebRTCData _LocalRtcData;
    private WebRTCData _RemoteRtcData;
    private bool WebRTCInitialized = false;
    public override void _Ready()
    {
        Peer = new WebRTCPeerConnection();
        Peer.Initialize(GlobalVariables.IceServers);

        MultiplayerPeer = new WebRTCMultiplayer();

        // Signale verbinden!
        Peer.Connect("session_description_created", this, nameof(SDPCreated));
        Peer.Connect("ice_candidate_created", this, nameof(WebRTCPeerIceCandidateCreated));

        WebRTCInitialized = false;
    }

    // Schritt1: Partner A erzeugt seine SPD und ICe Kandidaten und gibt sie aus!
    private void _on_StartConnection_pressed()
    {
        MultiplayerPeer.Initialize(1,false);
        MultiplayerPeer.AddPeer(Peer,1);
        WebRTCInitialized = true;
        // Es wird zuerst WebRTcMultiplayer mit id 2 initialisiert
        // Drückt ein Spieler aber auf SpielStarten wird er neu mit id = 1 initialisiiert
        // d.h. derjenige welche die verbindung initialisiert hat id 1, ist dann Spieler 1
        // der andere in der Verbindung ist dann id2 = Spieler 2!

        if(Peer.CreateOffer() != Error.Ok)
        {
            GD.Print("Fehler bei Erzeugung SDP!");
            // Signal session_description_created => SDPCreated wird aufgerufen
        }
    }
    private void SDPCreated(string type, string sdp)
    {
        Peer.SetLocalDescription(type,sdp);
        // SDP Daten WebRTCDAta speichern
        _LocalRtcData = new WebRTCData(type, sdp);
    }

    private void WebRTCPeerIceCandidateCreated(string media, int index, string name) 
    {
        // es werden mehrere Ice Kandidaten erzeugt, d.h. die Methode wird öfers aufgerufen
        _LocalRtcData.AddIce(media, index, name);
        // da man nichr die genaue Anzahl der Kandidaten und damit die aufrufe der Methode weiß muss man jedesmal
        // _localRtcdata jedesmal in Json konvertieren und ausgeben
        GetNode<TextEdit>("SelfRtcData").Text = JsonConvert.SerializeObject(_LocalRtcData);
    }

    // Schritt2: PartnerB bekommt die SDP und ICe Daten von Partner A und drückt auf speichern
    private void _on_SetRemoteData_pressed()
    {
        if(WebRTCInitialized == false)
        {
            MultiplayerPeer.Initialize(2,false);
            MultiplayerPeer.AddPeer(Peer,2);
            WebRTCInitialized = true;
        }
        
        // json.string in RTCData konvertieren
        try
        {
            _RemoteRtcData = JsonConvert.DeserializeObject<WebRTCData>(GetNode<TextEdit>("ForeignRtcData").Text);
        }
        catch
        {
            // Todo: Fehlerpop erscheinen lassen
            GD.Print("In den übertragenen RTC Daten liegt ein Fehler vor! Versuche es erneut");
        }
        // SDP von Partner A als remote SDp setzen
        if(Peer.SetRemoteDescription(_RemoteRtcData.SDP_Data.Type, _RemoteRtcData.SDP_Data.SDP) != Error.Ok)
        {
            GD.Print("Fehler bei Erzeugung Remote SDP!");
        }
        // Signal session_description_created => SDPCreated wird aufgerufen

        //ICe Kandidaten von Partner A hinzufügen
        foreach(WebRTCData.IceCandidateData ice in _RemoteRtcData.ListIceCandidates)
        {
            Peer.AddIceCandidate(ice.Media, ice.Index, ice.Name);
        }
    }
    // ab hier werden RTCDaten von Partner B erzeugt, erst SDp dann Ice
    // Nachdem Partner B die Ice Kandidaten an Partner A übermittelt der 
    // setzt auch wie Partner B die SPD remote und ICE von B
    // danach sollte die Verbindung stehen

    public override void _Process(float delta)
    {
        MultiplayerPeer.Poll();
        if(WebRTCPeerConnection.ConnectionState.Connected == Peer.GetConnectionState())
        {
            GlobalVariables.Instance.WebRTC = MultiplayerPeer;
            NetworkManager.NetMan.Init(MultiplayerPeer);
            GetTree().ChangeScene("res://Szenen/RTCTest.tscn");
            QueueFree();
        }
    }

    private void _on_Button_pressed()
    {
        GetNode<Label>("RpcInfo").Text = MultiplayerPeer.GetPeers().ToString();
    }

    private void _on_Zurck_pressed()
    {
        GetTree().ChangeScene("res://Szenen/Verbindungseinstellungen.tscn");
        QueueFree();
    }

    private void _on_ResetConnectionData_pressed()
    {
        GetNode<TextEdit>("ForeignRtcData").Text = "";
        GetNode<TextEdit>("SelfRtcData").Text = "";
        _Ready();
    }
}
