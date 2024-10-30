using Godot;
using System;

public class Einstellungen : Control
{
    private OptionSelection _SelectDifficulty;
    private OptionSelection _SelectLevel;
    private OptionSelection _SelectMode;
    private AudioStreamPlayer2D _audioplayer;
    private Button _voicechatButton;

    //für online:
    private bool _OtherPlayerIsReady;
    private bool _IamReady;

    public override void _Ready()
    {
        // Für SelectDificullty:
        // Suche den ursprünglichen Node
        CheckBox originalSelectDifficulty = GetNode<CheckBox>("SelectDifficulty");

        // Erstelle eine neue Instanz von OptionSelection
        _SelectDifficulty = new OptionSelection(4, new string[] { "Leicht", "Mittel", "Schwer", "Profi" });
        _SelectDifficulty.Name = originalSelectDifficulty.Name; // macht es einfacher
        _SelectDifficulty.AddColorOverride("Color1", new Color(255, 255 ,255));

        // Setze die Position von _SelectDifficulty auf die Position des ursprünglichen Nodes
        _SelectDifficulty.RectPosition = originalSelectDifficulty.RectPosition;

        // Entferne den ursprünglichen Node und füge den neuen hinzu
        RemoveChild(originalSelectDifficulty);
        originalSelectDifficulty.QueueFree();
        AddChild(_SelectDifficulty);

        // Signal hinzufügen bei Online Multiplayer
        if(GlobalVariables.Instance.OnlineGame == true)
        {
            _SelectDifficulty.Connect(nameof(OptionSelection.SelectionChanged), this, nameof(ChangeOnlineSelection), new Godot.Collections.Array{"SelectDifficulty"});
            _SelectDifficulty.EnableOtherPlayerSelection(0); // da der gegenüber nur seine Auswahl schickt wenn er was ändert muss
            // sein Ausgangszustand so abbgebildet werden, das ist auch deiner!
        }

        //Für SelectLevel-------------------------------------------------------------------------
        // Suche den ursprünglichen Node
        CheckBox originalSelectLevel = GetNode<CheckBox>("SelectLevel");

        // Erstelle eine neue Instanz von OptionSelection
        _SelectLevel = new OptionSelection(3, new string[] { "Level 1", "Level 2", "Level 3"});
        _SelectLevel.Name = originalSelectLevel.Name; // macht es einfacher
        _SelectLevel.AddColorOverride("Color1", new Color(255, 255, 255));

        // Setze die Position von _SelectDifficulty auf die Position des ursprünglichen Nodes
        _SelectLevel.RectPosition = originalSelectLevel.RectPosition;

        // Entferne den ursprünglichen Node und füge den neuen hinzu
        RemoveChild(originalSelectLevel);
        originalSelectLevel.QueueFree();
        AddChild(_SelectLevel);

        // Signal hinzufügen bei Online Multiplayer
        if(GlobalVariables.Instance.OnlineGame == true)
        {
            _SelectLevel.Connect(nameof(OptionSelection.SelectionChanged), this, nameof(ChangeOnlineSelection), new Godot.Collections.Array{"SelectLevel"});
            _SelectLevel.EnableOtherPlayerSelection(0); // da der gegenüber nur seine Auswahl schickt wenn er was ändert muss
            // sein Ausgangszustand so abbgebildet werden, das ist auch deiner!
        }

        //Für SelectMode-------------------------------------------------------------------------
        // Suche den ursprünglichen Node
        CheckBox originalSelectMode = GetNode<CheckBox>("SelectMode");

        // Erstelle eine neue Instanz von OptionSelection
        // im OnlineMultiplayer gibt es keinen Einzelspieler!
        if(GlobalVariables.Instance.OnlineGame == true)
        {
            _SelectMode = new OptionSelection(2, new string[] { "Miteinander", "Gegeneiander"});
        }
        else
        {
            _SelectMode = new OptionSelection(3, new string[] { "Miteinander", "Gegeneiander", "Einzelspieler"}, 2);
        }
        _SelectMode.Name = originalSelectMode.Name; // macht es einfacher
        _SelectMode.AddColorOverride("Color1", new Color(255, 255, 255));

        // Setze die Position von _SelectDifficulty auf die Position des ursprünglichen Nodes
        _SelectMode.RectPosition = originalSelectMode.RectPosition;

        // Entferne den ursprünglichen Node und füge den neuen hinzu
        RemoveChild(originalSelectMode);
        originalSelectMode.QueueFree();
        AddChild(_SelectMode);

        // Signal hinzufügen bei Online Multiplayer
        if(GlobalVariables.Instance.OnlineGame == true)
        {
            _SelectMode.Connect(nameof(OptionSelection.SelectionChanged), this, nameof(ChangeOnlineSelection), new Godot.Collections.Array{"SelectMode"});
            _SelectMode.EnableOtherPlayerSelection(0); // da der gegenüber nur seine Auswahl schickt wenn er was ändert muss
            // sein Ausgangszustand so abbgebildet werden, das ist auch deiner!
        }

        _OtherPlayerIsReady = false;
        _IamReady = false;
        if(GlobalVariables.Instance.OnlineGame == false)
        {
            GetNode<Label>("LabelName").Hide();
            GetNode<TextEdit>("PlayerName").Hide();
        }
        // wenn ein Name schon vorhanden ist den Voreintragen!
        GetNode<TextEdit>("PlayerName").Text = GlobalVariables.Instance.Room.MyName;

        //Audioplayer abspielen
        _audioplayer = GetNode<AudioStreamPlayer2D>("LobbyTheme");
        _audioplayer.Play();

        InitializeVoiceChatButtons();
    }

    private void InitializeVoiceChatButtons()
    {
        _voicechatButton = GetNode<Button>("ToggleVoiceChat");
        if (!GlobalVariables.Instance.OnlineGame)
        {
            _voicechatButton.Hide();
        }
    }

    private void _on_ToggleVoiceChat_pressed()
    {
        GD.Print($"ToggleVoiceSound pressed:");
        if (_voicechatButton.Pressed == true)
        {
            NetworkManager.NetMan.AudioIsRecording = true;
            NetworkManager.NetMan.AudioIsPlaying = true;
            _voicechatButton.Text = "Sprachchat ausschalten";
            GD.Print("Sprachchat ist an");
        }
        else
        {
            NetworkManager.NetMan.AudioIsRecording = false;
            NetworkManager.NetMan.AudioIsPlaying = false;
            _voicechatButton.Text = "Sprachchat anschalten";
            GD.Print("Sprachchat ist aus");
        }
    }

    private void _on_Start_pressed()
    {
        GlobalVariables.Instance.LevelDifficulty = _SelectDifficulty.SelectedOption;
        GlobalVariables.Instance.LevelMode = _SelectMode.SelectedOption;
        if(GlobalVariables.Instance.OnlineGame == false)
        {
            GetTree().ChangeScene($"res://Szenen/Levels/Level{_SelectLevel.SelectedOption + 1}.tscn");
            return;
        }
        // wenn online:
        _IamReady = true;
        // warten auf anderen Spieler, er muss auch bereit sein! => diesem Spiler mitteilen das er warten muss, anderm Spieler sagen das man wartet!
        GetNode<Label>("InfoBereit").Text = "Warten auf anderen Spieler!";
        // evtl. Input, also Änderungen verbieten
        ChangePlayerInputPossibility(true);
        // anderen Spieler sagen das er hinne machen soll! => mit rpc!
        NetworkManager.NetMan.rpc(GetPath(),nameof(SayOtherPlayerIsReady), false, false, true, _SelectDifficulty.SelectedOption, _SelectLevel.SelectedOption, _SelectMode.SelectedOption, GetNode<TextEdit>("PlayerName").Text);
        // damit sagt man dem anderen gleichzeitig das man bereit ist
        // im RPC aufruf merkt man dann ob beide bereit sind und trifft die Auswahl!
        // der Gegenüber muss dafür aber die Auswahl von dir kennen daher sende deien Auswahl mit! Auch wenn nur einer sie auswertet!
    }

    private void ChangePlayerInputPossibility(bool disable)
    {
        _SelectDifficulty.Disable(disable);
        _SelectLevel.Disable(disable);
        _SelectMode.Disable(disable);
        GetNode<TextEdit>("PlayerName").Readonly = disable;
    }

    private void _on_Back_pressed()
    {
        if(_IamReady == false)
        {
            if(GlobalVariables.Instance.OnlineGame == false)
            {
                GetTree().ChangeScene("res://Szenen/MainMenu.tscn");
                return;
            }
            // Wenn Online Game muss man Verbindung schließen!
            // Wenn man ne Lobby hat dann die Lobby öffnen!
            ErrorMessage("Bestätigung", "Wenn du auf Ok drückst wird die Verbindung zum anderen Spieler unterbrochen!\nWillst du es nicht klicke auf das Kreuz!").Connect("confirmed", this, nameof(ConfirmConnectionClose));
        }
        else
        {
            // über RPC sagen das man nicht mehr bereit ist
            NetworkManager.NetMan.rpc(GetPath(),nameof(SayOtherPlayerIsNotReay), false, false);
            _IamReady = false;
            GetNode<Label>("InfoBereit").Text = "";
            ChangePlayerInputPossibility(false);
        }
    }

    private void ConfirmConnectionClose()
    {
        GlobalVariables.Instance.BackToMainMenuOrLobby();
        NetworkManager.NetMan.CloseConnection();
    }

    // nur für Online Multiplayer!
    private void ChangeOnlineSelection(int index, string OptionSelectionName)
    {
        // da für alle 3 OPtion selection das geleiche und die Objekte auf der anderen Seute eine andere sind, die Namen aber diesselben kann man es so am einfachsten machen
        // alternativ: 3 mal das gleiche für die 3 unterschiedlichen Option Selection menus
        NetworkManager.NetMan.rpc(GetPath(),nameof(ChangeSelectionOnOtherPlayer), false, false, true, OptionSelectionName, index); // remote rpc, da 2.false
    }
    // remote RPC
    private void ChangeSelectionOnOtherPlayer(string OptionSelectionName, int index)
    {
        // da es ein remote rpc (das 2. false) ist, wird auf der senderseite diese Methode nicht ausgeführt nur auf der anderen!
        GetNode<OptionSelection>(OptionSelectionName).EnableOtherPlayerSelection(index);
    }
    // remote RPC
    private void SayOtherPlayerIsNotReay()
    {
        _OtherPlayerIsReady = false;
        GetNode<Label>("InfoBereit").Text = "";
    }
    // remote RPC
    private void SayOtherPlayerIsReady(int DifficultIndex, int LevelIndex, int ModeIndex, string PlayerName)
    {
        GetNode<Label>("InfoBereit").Text = "Der andere Spieler wartet!";
        _OtherPlayerIsReady = true;
        if(_IamReady == true)
        {
            int DifficultDecision;
            int LevelDecision;
            int ModeDecision;
            Random r = new Random();
            // Derjenige welcher zuerst bereit war merkt hier das beide bereit sind
            // er trifft bei Unstimmigkeiten für beide die Entscheidungen!
            // dafür vergleicht er seine mit die des anderen!
            // Schwierigkeitsentscheidung:---------------------------------------
            if(DifficultIndex == _SelectDifficulty.SelectedOption)
            {
                DifficultDecision = DifficultIndex;
            }
            else
            {
                // Zufall
                if(r.Next(0,2) == 0)
                {
                    DifficultDecision = DifficultIndex;
                }
                else
                {
                    DifficultDecision = _SelectDifficulty.SelectedOption;
                }
            }
            // Levelentscheidung:-------------------------------------------------
            if(LevelIndex == _SelectLevel.SelectedOption)
            {
                LevelDecision = LevelIndex;
            }
            else
            {
                // Zufall
                if(r.Next(0,2) == 0)
                {
                    LevelDecision = LevelIndex;
                }
                else
                {
                    LevelDecision = _SelectLevel.SelectedOption;
                }
            }
            // Modeentscheidung:-------------------------------------------------
            if(ModeIndex == _SelectMode.SelectedOption)
            {
                ModeDecision = ModeIndex;
            }
            else
            {
                // Zufall
                if(r.Next(0,2) == 0)
                {
                    ModeDecision = ModeIndex;
                }
                else
                {
                    ModeDecision = _SelectMode.SelectedOption;
                }
            }
            // Jetzte bei beiden das Spiel starten!
            // RTC raum beim anderen setzen!
            // GlobalVariable, also die getroffenen Entscheidungen beim anderen setzten
            // danach umgedreht das bei dir
            NetworkManager.NetMan.rpc(GetPath(), nameof(SetRoomPlayer), false, false, true, PlayerName, GetNode<TextEdit>("PlayerName").Text, false);
            SetRoomPlayer(GetNode<TextEdit>("PlayerName").Text, PlayerName, true);
            // Entscheidungen setzten!
            NetworkManager.NetMan.rpc(GetPath(), nameof(SetDecisions), false, true, true, DifficultDecision, ModeDecision);
            // endlich das Spiel starten!
            NetworkManager.NetMan.rpc(GetPath(), nameof(StartGame), false, true, true, LevelDecision);
        }
    }
    // remote rpc
    private void SetRoomPlayer(string MyName, string MatesName, bool IsPlayerOne)
    {
        GlobalVariables.Instance.Room.MyName = MyName;
        GlobalVariables.Instance.Room.MatesName = MatesName;
        GlobalVariables.Instance.Room.IamPlayerOne = IsPlayerOne;
    }
    // rpc bei beiden
    private void SetDecisions(int DifficultDecision, int ModeDecision)
    {
        GlobalVariables.Instance.LevelDifficulty = DifficultDecision;
        GlobalVariables.Instance.LevelMode = ModeDecision;
    }
    //rpc bei beiden
    private void StartGame(int Level)
    {
        GetTree().ChangeScene($"res://Szenen/Levels/Level{Level + 1}.tscn");
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
}
