using Godot;
using Newtonsoft.Json;
using System;
using System.CodeDom;
using System.Runtime.InteropServices;

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

        public _RtcMsg(_RtcMsgState state, string data)
        {
            MsgState = state;
            Data = data;
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
                        if (msg.Length > 3)
                        {           
                            rpc(msg[0], msg[1], true, JsonConvert.DeserializeObject(msg[3]));
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
            SendRawMessage(_RtcMsg.ConvertToJson(new _RtcMsg(_RtcMsgState.RPC,NodePath + "|" + Method + "|" + JsonConvert.SerializeObject(Args))));
        } 
    }

    public void SendMessage(string text)
    {
        SendRawMessage(_RtcMsg.ConvertToJson(new _RtcMsg(_RtcMsgState.CostumMsg, text)));
    }
    private void SendRawMessage(string message)
    {
        _multiplayer.PutPacket(message.ToUTF8());
    }


}
