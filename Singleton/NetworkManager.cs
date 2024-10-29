using Godot;
using Newtonsoft.Json;
using System;
using System.CodeDom;
using System.Runtime.InteropServices;
using NAudio.Wave;
using System.Collections.Generic;
using System.Net.NetworkInformation;

// die Klasse wird benutzt um eine Spielrverbindung zu managen
public class NetworkManager : Node
{
    [Signal]
    public delegate void MessageReceived(string msg);
    [Signal]
    public delegate void AudioError();

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
        KeepAlivePing,
    }

    private class AudioStreaming
    {
        // Diese Klasse dient dazu Audiodaten aufzuzeichen, über ein Netzwerk zu senden und bei dem anderen abzuspielen
        private readonly Action<byte[]> _sendMethod;
        private WaveInEvent _WaveIn;
        private WaveOutEvent _waveOut;
        private BufferedWaveProvider _bufferedProvider;
        private bool _Recording;
        public bool IsRecording 
        {
            get
            {
                return _Recording;
            }
            set
            {
                if(value == true)
                {
                    _Recording = true;
                    _WaveIn.StartRecording();
                }
                else
                {
                    _Recording = false;
                    _WaveIn.StopRecording();
                }
            }
        } // auf true wenn Audio aufgenommen und 
        public bool IsPlaying {get; set;} // auf true wenn Audio von anderen wiedergegeben wird, wenn auf false hört er keinen Sprachchat!
        public AudioStreaming(Action<byte[]> sendMethod)
        {
            // da es über jedes beliebige Netzwerk gesendet werden soll übergibt der Anwender eine Funktion welche die Daten konvertiert und sendet
            // Audioaufnahme initialisieren
            _WaveIn = new WaveInEvent
            {
                WaveFormat = new WaveFormat(44100, 1)
            };
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
    private WebRTCMultiplayer _multiplayer = new WebRTCMultiplayer(); 
    public static NetworkManager NetMan { get; private set; }
    public bool AudioIsRecording
    {
        get
        {
            return AudioIsRecording;
        }
        set
        {
            // wenn es kein Mikro am PC kommt, kommt es hier zum Absturz!
            try
            {
                _audioStream.IsRecording = value;
            }
            catch
            {
                ErrorMessage("Mikrofon","Es wurde kein Mikrofon gefunden!");
                EmitSignal(nameof(AudioError));
            }
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
            try
            {
                _audioStream.IsPlaying = value;
            }
            catch
            {
                ErrorMessage("Lautsprecher","Es wurde kein Lautsprecher gefunden!");
                EmitSignal(nameof(AudioError));
            }
        }
    }

    private float _PingIntervall; // nach 1 sek wird ein KeepAlivePing gesendet um zu überprüfen ob die Verbindung noch steht
    private float _LastPingTime;
    private bool _multiplayerIsActive;
    private bool _PingAnswerReceived;
    private int _CyclesWithoutPing = 0; // wenn mehr als 5 Zyklen kein Ping empfangen wird ist es als Verbindungsabbruch zu werten!
    private WebRTCPeerConnectionGDNative rtc = new WebRTCPeerConnectionGDNative();
    public int BufferCount
    {
        get
        {
            if(_multiplayerIsActive)
                return _multiplayer.GetAvailablePacketCount();
            else
                return 0;
        }
    }
    public UInt64 PingTime;
    public void Init(WebRTCMultiplayer multiplayer, float KeepAlivePingInterval = 1.0f)
    {
        _multiplayer = multiplayer;
        _PingIntervall = KeepAlivePingInterval;
        _multiplayerIsActive = true;
        _PingAnswerReceived = true;
    }
    public override void _Ready()
    {
        NetMan = this;
        // Daten für das aufnehemn der Audio festlegen
        _audioStream =  new AudioStreaming(SendAudio);
        _playAudioStream = _audioStream.GetPlayMethod();
        _multiplayerIsActive = false;
        // Die Node muss um jeden Preis laufen sonst Verbindungsabbruch und totales Chaos
        PauseMode = PauseModeEnum.Process;
    }

    public override void _Process(float delta)
    {
        // nur sinvoll auf Nachrichten zu warten wenn der _multiplayer initialisiert wird, bzw. wenn eine richtige Vwebindung steht
        if(_multiplayerIsActive == true)
        {
            // Auf Nachrichten hören und diese interpretieren!
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
                                // egal ob relaible oder nicht, wird eh nicht versendet, da schon angekommener rpc vom gegenüber
                                rpc(msg[0], msg[1], true, true, true, JsonConvert.DeserializeObject<object[]>(msg[2]));
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
                        case _RtcMsgState.KeepAlivePing:
                        {
                            _PingAnswerReceived = true;
                            _CyclesWithoutPing = 0;
                            PingTime = Convert.ToUInt64((DateTime.Now - JsonConvert.DeserializeObject<DateTime>(data.Data)).TotalMilliseconds);
                            break;
                        }
                    }
                }
            }
            // Keep Alive Mechanismus:
            _LastPingTime += delta;
            if(_LastPingTime >= _PingIntervall)
            {
                // prüfen ob im vorhergehenden Intervall eine Antwort zurückkam
                if(_PingAnswerReceived == false)
                {
                    _CyclesWithoutPing += 1;
                    if(_CyclesWithoutPing > 5)
                    {
                        // keine Antwort => Verbindungsabbruch!
                        ErrorMessage("Verbindungsabbruch", "Die Peer To Peer Verbindung wurde abgebrochen.").Connect("popup_hide", GlobalVariables.Instance, nameof(GlobalVariables.Instance.BackToMainMenuOrLobby));
                        // eigene Verbindung schließen, da nur 2 Spieler miteinander verbunden sind und es wenig sinn macht den anderen im Raum zu lassen!
                        CloseConnection();
                    }
                }
                // Zeit einen neuen Ping zu senden!
                _multiplayer.TransferMode = WebRTCMultiplayer.TransferModeEnum.Reliable;
                SendRawMessage(JsonConvert.SerializeObject(new _RtcMsg(_RtcMsgState.KeepAlivePing, JsonConvert.SerializeObject(DateTime.Now))).ToUTF8());
                _PingAnswerReceived = false;
                _LastPingTime = 0.0f;
            }
        }
    }

    private List<_RtcMsg> RPCPuffer = new List<_RtcMsg>();
     private List<Exception> RPCFehler= new List<Exception>();

    public void rpc(string NodePath, string Method, bool remoterpc = false, bool dolocal = true, bool relaible = true, params object[] Args)
    {
        // remoterpc wird benutzt wenn man den rpc vom anderen empfangen hat
        // der der ihn auslöst setzt in standartmäßig auf false

        if(dolocal == true)
        {
            // lokal den Rpc vollführen
            try
            {
                GetNode(NodePath).Call(Method,Args);
            }
            catch(Exception e)
            {
                // RPCFehler.Add(e);
                // Juckt? throw new Exception("Der Pfad: " + NodePath + " oder die Methode: " + Method + " existiert nicht!",e);
            }
        }

        // dann Nachricht an den anderen senden, dieser soll ihn auch machen!
        // wenn remote, hat der andere ihn schon ausgeführt!
        // nur rpc an anderen senden wenn es eine echte Verbindung gibt, d.h. Init() aufgerufen wurde
        // wenn nicht mache den rpc nur lokal
        if(remoterpc == false && _multiplayerIsActive == true)
        {
            var settings = new JsonSerializerSettings
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            };
            if(relaible == true)
                _multiplayer.TransferMode = WebRTCMultiplayer.TransferModeEnum.Reliable;
            else
                _multiplayer.TransferMode = WebRTCMultiplayer.TransferModeEnum.UnreliableOrdered;
            
            _RtcMsg msg = new _RtcMsg(_RtcMsgState.RPC,NodePath + "|" + Method + "|" + JsonConvert.SerializeObject(Args,settings));
            // RPCPuffer.Add(msg);
            SendRawMessage(_RtcMsg.ConvertToJson(msg).ToUTF8());
        } 
    }

    public void SendMessage(string text)
    {
        _multiplayer.TransferMode = WebRTCMultiplayer.TransferModeEnum.Reliable;
        SendRawMessage(_RtcMsg.ConvertToJson(new _RtcMsg(_RtcMsgState.CostumMsg, text)).ToUTF8());
    }

    public void SendAudio(byte[] stream)
    {
        _multiplayer.TransferMode = WebRTCMultiplayer.TransferModeEnum.UnreliableOrdered;
        SendRawMessage(_RtcMsg.ConvertToJson(new _RtcMsg(_RtcMsgState.AudioStream, null, stream)).ToUTF8());
    }

    private void SendRawMessage(byte[] message)
    {
        _multiplayer.PutPacket(message);
    }

    private ConfirmationDialog ErrorMessage(string titel, string description)
    {
        ConfirmationDialog ErrorPopup = (ConfirmationDialog)GlobalVariables.Instance.ConfirmationDialog.Instance();
        ErrorPopup.Init(titel,description);
        GetTree().Root.AddChild(ErrorPopup);
        ErrorPopup.PopupCentered();
        ErrorPopup.Show();
        return ErrorPopup;
    }

    public void CloseConnection()
    {
        _multiplayerIsActive = false;
        _multiplayer.Close();
    }
}
