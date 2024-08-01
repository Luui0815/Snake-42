using Godot;
using System;

public class OptionSelection : Control
{
    private class CustomCheckBox : CheckBox
    {
        [Signal]
        public delegate void CheckBoxPressed(int index);

        public int Index { get; set; } = -1;

        public override void _Toggled(bool buttonPressed)
        {
            base._Toggled(buttonPressed);
            if (buttonPressed)
            {
                EmitSignal(nameof(CheckBoxPressed), Index);
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

    private CustomCheckBox[] _CheckBoxes;

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
    }

}
