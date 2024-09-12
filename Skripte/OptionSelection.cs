using Godot;
using System;

public class OptionSelection : Control
{
    [Signal]
    public delegate void SelectionChanged(int index);
    private class CustomCheckBox : CheckBox
    {
        [Signal]
        public delegate void CheckBoxPressed(int index);
        public Sprite OtherPlayerCross;
        public int Index { get; set; } = -1;
        private bool isLocked;

        public new bool Pressed
        {
            get
            {
                return base.Pressed;
            }
            set
            {
                // man muss isLocked zurücksetzten!
                if(value == false)
                    isLocked = false;
                base.Pressed = value;
            }
        }

        public override void _Toggled(bool buttonPressed)
        {
            // Falls die Checkbox bereits angehakt ist, wird das Umschalten verhindert
            if (isLocked)
            {
                // Checkbox-Zustand beibehalten
                Pressed = true;
                return;
            }

            base._Toggled(buttonPressed);

            // Wenn die Checkbox aktiviert wird, sperren wir sie und senden das Signal
            if (buttonPressed)
            {
                isLocked = true;
                EmitSignal(nameof(CheckBoxPressed), Index);
            }
        }
        public void SetOtherPlayerSelection(bool selected)
        {
            if (OtherPlayerCross != null)  // Sicherstellen, dass das Sprite existiert
            {
                if (selected)
                    OtherPlayerCross.Show();  // Sprite anzeigen
                else
                    OtherPlayerCross.Hide();  // Sprite verstecken
            }
        }
    }


    [Export]
    public int SelectedOption; // hierrüber frägt man ab welche OPtion ausgewählt ist
    [Export]
    public readonly int PreselectedOption; // hierrüber legt man fest welche OPtion zu beginn festgelegt wird
    [Export]
    public readonly int OptionNumbers; // hierrüber legt m,an fest wie viele OPtionen es geben soll
    [Export]
    public readonly int DistanceRight; 
    [Export]
    public readonly int DistanceLeft; 
    [Export]
    public readonly int DistanceTop; 
    [Export]
    public readonly int DistanceButtom; 
    [Export]
    public readonly int Height; 
    [Export]
    public readonly int Width; 
    [Export]
    public readonly string OtherPlayerSelectionCrossPath = "res://Assets/EinstellungenMultiplayerKreuz.png";

    private CustomCheckBox[] _CheckBoxes;
    private Sprite OtherPlayerSelection;

    public OptionSelection (int _OptionNumbers, string[] _CheckboxesText, int _PreselectedOption = 0, int _DistanceRight = 10, int _DistanceLeft = 10, int _DistanceTop = 10, int _DistanceButtom = 10, int _Height = 50, int _Width = 100) 
    {
        PreselectedOption = SelectedOption = _PreselectedOption;
        DistanceRight = _DistanceRight;
        DistanceLeft = _DistanceLeft;
        DistanceTop = _DistanceTop;
        DistanceButtom = _DistanceButtom;
        Height = _Height;
        Width = _Width;

        // größe der Node einstellen
        Width = _DistanceLeft + _DistanceRight +_Width;
        Height = _DistanceTop + _DistanceTop + (_Height * _OptionNumbers);

        // Checkboxes erzeugen
        Vector2 Pos = new Vector2(_DistanceLeft, _DistanceTop);
        Vector2 SpaceBetween = new Vector2(_DistanceLeft, _DistanceTop);
        Vector2 size = new Vector2(_Width, _Height);

        _CheckBoxes = new CustomCheckBox[_OptionNumbers];

        for(int i = 0; i < _OptionNumbers; i++)
        {
            _CheckBoxes[i] = new CustomCheckBox();
            _CheckBoxes[i].SetPosition(Pos);
            _CheckBoxes[i].SetSize(size);
            _CheckBoxes[i].Text = _CheckboxesText[i];
            _CheckBoxes[i].Index = i;
            _CheckBoxes[i].Connect("CheckBoxPressed", this, nameof(CBPressed));
            // Sprite für MultiplayerSelection setzen---------------------------------------------------------------------------------------
            _CheckBoxes[i].OtherPlayerCross = new Sprite();
            _CheckBoxes[i].OtherPlayerCross.Texture = (Texture)ResourceLoader.Load(OtherPlayerSelectionCrossPath);
            // Größe des Sprites anpassen (z.B. mit einer festen Skalierung oder relativ zur Checkbox-Größe)
            _CheckBoxes[i].OtherPlayerCross.Scale = new Vector2(0.3f * (size.y / _CheckBoxes[i].OtherPlayerCross.Texture.GetSize().x), // größe anpassen!
                                                                0.3f * (size.y / _CheckBoxes[i].OtherPlayerCross.Texture.GetSize().y));
            // Position rechtsbündig in der Checkbox setzen
            float crossWidth = _CheckBoxes[i].OtherPlayerCross.Texture.GetSize().x * _CheckBoxes[i].OtherPlayerCross.Scale.x;
            float crossHeight = _CheckBoxes[i].OtherPlayerCross.Texture.GetSize().y * _CheckBoxes[i].OtherPlayerCross.Scale.y;
            // Berechne die Position, sodass das Sprite rechtsbündig ist
            _CheckBoxes[i].OtherPlayerCross.Position = new Vector2((Pos.x + size.x) - crossWidth, 15); // 15 bestimmt wie weit unten das Kreuz ist
            // Das Sprite darf nicht zentriert sein, damit die Position korrekt ist
            _CheckBoxes[i].OtherPlayerCross.Centered = false;
            _CheckBoxes[i].AddChild(_CheckBoxes[i].OtherPlayerCross);
            _CheckBoxes[i].OtherPlayerCross.Hide();
            // ENDE-------------------------------------------------------------------------------------------------------------------------
            Pos.y += size.y +SpaceBetween.y;
            AddChild(_CheckBoxes[i]);
        }

        _CheckBoxes[_PreselectedOption].Pressed = true;
    }

    private void CBPressed(int index)
    {
        SelectedOption = index;
        // alle anderen auf false setzten
        foreach(CustomCheckBox cb in _CheckBoxes)
        {
            if(cb.Index != index)
                cb.Pressed = false;
            else
                cb.Pressed = true;
        }
        EmitSignal(nameof(SelectionChanged), index);
    }
    // nur für Multiplayer:
    public void EnableOtherPlayerSelection(int CheckboxIndex)
    {
        // bei allen Checkboxen den Hacken entfernen außer bei dem mit dem Index
        foreach(CustomCheckBox cb in _CheckBoxes)
        {
            if(cb.Index == CheckboxIndex)
                cb.SetOtherPlayerSelection(true);
            else
                cb.SetOtherPlayerSelection(false);
        }
    }
}
