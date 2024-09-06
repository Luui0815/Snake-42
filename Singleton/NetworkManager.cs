using Godot;
using Newtonsoft.Json;
using System;
using System.CodeDom;
using System.Runtime.InteropServices;
using NAudio.Wave;

// die Klasse wird benutzt um eine Spielrverbindung zu managen
public class NetworkManager : Node
{
    [Signal]
    public delegate void MessageReceived(string msg);
    [Signal]
    public delegate void AudioStreamReceived(byte[] data);

    // Klasse welche benutzt wird um Nachrichten zu versenden, nur für NetworkManager interresant
    private class _RtcMsg
    {
        public _RtcMsgState MsgState;
        public string Data;
        public byte[] AudioStream;

        public _RtcMsg(_RtcMsgState state, string data = null, byte[] audioStream = null)
        {
            MsgState = state;
            Data = data;
            AudioStream = audioStream;
        }

        public static string ConvertToJson(_RtcMsg msg)
        {
            return JsonConvert.SerializeObject(msg);
        }

        public static _RtcMsg ConvertTo_RtcMsg(string msg)
        {
            try
            {
                return JsonConvert.DeserializeObject<_RtcMsg>(msg);

            }
            catch
            {
                return null;
            }
        }
    }

    enum _RtcMsgState
    {
        RPC, // Benutzt um RPC Aufrufe zu kennzeichnen
        CostumMsg, // da NetworkManager nur als eine API benutzt werden soll muss, sich der Benutzer wenn er mehr NNachrichten zu unterschidlichen Zwecken 
        // versenden will eigene Nachrichtenstati ausdenken => Signal wird emittiert
        AudioStream,
    }

    private class AudioStreaming
    {
        // Diese Klasse dient dazu Audiodaten aufzuzeichen, über ein Netzwerk zu senden und bei dem anderen abzuspielen
        private readonly EventHandler<WaveInEventArgs> _sendMethod;
        private WaveInEvent _WaveIn;
        private WaveOutEvent _waveOut;
        public bool IsRecording {get; private set;}
        public AudioStreaming(EventHandler<WaveInEventArgs> sendMethod)
        {
            // da es über jedes beliebige Netzwerk gesendet werden soll übergibt der Anwender eine Funktion welche die Daten konvertiert und sendet
            _WaveIn = new WaveInEvent();
            _WaveIn.WaveFormat = new WaveFormat(44100, 1);
            _WaveIn.DataAvailable += sendMethod;
        }

        public void StartAudioRecording()
        {
            IsRecording = true;
            _WaveIn.StartRecording();
        }

        public void StopAudioRecording()
        {
            IsRecording = false;
            _WaveIn.StartRecording();
        }
    }

    // Die Klasse empfängt alle Nachrichten die über WebRTC gehen und macht auch RPCs
    private WebRTCMultiplayer _multiplayer = new WebRTCMultiplayer(); // geht auch mit WebRTC
    public static NetworkManager NetMan { get; private set; }

    public void Init(WebRTCMultiplayer multiplayer)
    {
        _multiplayer = multiplayer;
    }
    public override void _Ready()
    {
        NetMan = this;
        // Daten für das aufnehemn der Audio festlegen
    }

    public override void _Process(float delta)
    {
        _multiplayer.Poll();
        // nach neuen RTC nachrichten ausschau halten und diese dann KAtegoreien einorden
        if(_multiplayer.GetAvailablePacketCount() > 0)
        {
            _RtcMsg data = _RtcMsg.ConvertTo_RtcMsg(_multiplayer.GetPacket().GetStringFromUTF8());
            if(data != null)
            {
                // gültige Nachricht kam an => gucken was sie bedeutet
                switch(data.MsgState)
                {
                    case _RtcMsgState.RPC:
                    {
                        string[] msg = data.Data.Split("|"); //0 = NodePath, 1 = Method, wenn mehr = Args
                        if (msg.Length >= 3)
                        {           
                            rpc(msg[0], msg[1], true, JsonConvert.DeserializeObject<object[]>(msg[2]));
                        }
                        else
                        {
                            rpc(msg[0], msg[1], true);
                        }
                        break;
                    }
                    case _RtcMsgState.CostumMsg:
                    {
                        EmitSignal(nameof(MessageReceived), data.Data);
                        // Der API Nuter muss sich, wenn er mehrere verschiedene Stati austauschen will drüber klar werden wie er das machen will
                        break;
                    }
                    case _RtcMsgState.AudioStream:
                    {
                        EmitSignal(nameof(AudioStreamReceived), data.AudioStream);
                        // wenn Breakpoint in Übertragung während Audio Streams gesetzt ist dann kommt es hier zum Buffer überlauf
                        // Lösung: keine Breakpoint setzen! wenn audio aktiv
                        break;
                    }
                }
            }
        }
    }
    public void rpc(string NodePath, string Method, bool remoterpc = false, params object[] Args)
    {
        // remoterpc wird benutzt wenn man den rpc vom anderen empfangen hat
        // der der ihn auslöst setzt in standartmäßig auf false

        // lokal den Rpc vollführen
        try
        {
            GetNode(NodePath).Call(Method,Args);
        }
        catch(Exception e)
        {
            throw new Exception("Der Pfad: " + NodePath + " oder die Methode: " + Method + " existiert nicht!",e);
        }

        // dann Nachricht an den anderen senden, dieser soll ihn auch machen!
        // wenn remote, hat der andere ihn schon ausgeführt!
        if(remoterpc == false)
        {
            var settings = new JsonSerializerSettings
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            };
            SendRawMessage(_RtcMsg.ConvertToJson(new _RtcMsg(_RtcMsgState.RPC,NodePath + "|" + Method + "|" + JsonConvert.SerializeObject(Args,settings))).ToUTF8());
        } 
    }

    public void SendMessage(string text)
    {
        SendRawMessage(_RtcMsg.ConvertToJson(new _RtcMsg(_RtcMsgState.CostumMsg, text)).ToUTF8());
    }

    public void SendAudio(byte[] stream)
    {
        SendRawMessage(_RtcMsg.ConvertToJson(new _RtcMsg(_RtcMsgState.AudioStream, null, stream)).ToUTF8());
    }

    
    private void SendRawMessage(byte[] message)
    {
        _multiplayer.PutPacket(message);
    }
    
    public void StartAudioStream()
    {
        // Diese Methode startet den Audiostream und nimmt alle geräusche auf und sendet diese an den Gegenüber
        // erst wenn StopAudioStream aufgerufen wird, wird die Übertragung der Audio beendet!
    }

}
