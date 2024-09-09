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
        private readonly Action<byte[]> _sendMethod;
        private WaveInEvent _WaveIn;
        private WaveOutEvent _waveOut;
        private BufferedWaveProvider _bufferedProvider;
        public bool IsRecording 
        {
            get
            {
                return IsRecording;
            }
            set
            {
                if(value == true)
                {
                    IsRecording = true;
                    _WaveIn.StartRecording();
                }
                else
                {
                    IsRecording = false;
                    _WaveIn.StopRecording();
                }
            }
        } // auf true wenn Audio aufgenommen und 
        public bool IsPlaying {get; set;} // auf true wenn Audio von anderen wiedergegeben wird, wenn auf false hört er keinen Sprachchat!
        public AudioStreaming(Action<byte[]> sendMethod)
        {
            // da es über jedes beliebige Netzwerk gesendet werden soll übergibt der Anwender eine Funktion welche die Daten konvertiert und sendet
            // Audioaufnahme initialisieren
            _WaveIn = new WaveInEvent();
            _WaveIn.WaveFormat = new WaveFormat(44100, 1);
            _WaveIn.DataAvailable += ConvertAudioData;
            _sendMethod = sendMethod;
            // Audioausgabe initialisieren
            _waveOut = new WaveOutEvent();
            _bufferedProvider = new BufferedWaveProvider(new WaveFormat(44100, 1));
            _waveOut.Init(_bufferedProvider);
            _waveOut.Play();
        }

        private void ConvertAudioData(object sender, WaveInEventArgs e)
        {
            _sendMethod(e.Buffer);
        }
        public Action<byte[]> GetPlayMethod()
        {
            return PlayAudioStream;
        }
        private void PlayAudioStream(byte[] data)
        {
            if (IsPlaying == true)
                _bufferedProvider.AddSamples(data, 0 ,data.Length);
            // nur wenn der Spieler will das er das von andren hören will hört er es auch!
        }
    }

    private AudioStreaming _audioStream;
    private Action<byte[]> _playAudioStream;

    // Die Klasse empfängt alle Nachrichten die über WebRTC gehen und macht auch RPCs
    private WebRTCMultiplayer _multiplayer = new WebRTCMultiplayer(); // geht auch mit WebRTC
    public static NetworkManager NetMan { get; private set; }
    public bool AudioIsRecording
    {
        get
        {
            return AudioIsRecording;
        }
        set
        {
            _audioStream.IsRecording = value;
        }
    }

    public bool AudioIsPlaying 
    {
        get
        {
            return AudioIsPlaying;
        }
        set
        {
            _audioStream.IsPlaying = value;
        }
    }

    public void Init(WebRTCMultiplayer multiplayer)
    {
        _multiplayer = multiplayer;
    }
    public override void _Ready()
    {
        NetMan = this;
        // Daten für das aufnehemn der Audio festlegen
        _audioStream =  new AudioStreaming(SendAudio);
        _playAudioStream = _audioStream.GetPlayMethod();
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
                        _playAudioStream(data.AudioStream);
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
}
