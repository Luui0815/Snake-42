using Godot;

public class LevelSelectionMenu : Control
{
    private LevelSelection _levelSelectionGegeneinander;
    private LevelSelection _levelSelectionMiteinander;
    private Sprite _Spieler1Pfeil;
    private Sprite _Spieler2Pfeil;
    public override void _Ready()
    {
        _levelSelectionGegeneinander = GetNode<LevelSelection>("LevelSelectionGegneinander");
        _levelSelectionGegeneinander.Connect(nameof(LevelSelection.LevelSelected), this, "LevelSelectionGegeneinanderPressed");
        _levelSelectionMiteinander = GetNode<LevelSelection>("LevelSelectionMiteinander");
        _levelSelectionMiteinander.Connect(nameof(LevelSelection.LevelSelected), this, "LevelSelectionMiteinanderPressed");

        _Spieler1Pfeil = GetNode<Sprite>("Spieler1Pfeil");
        _Spieler2Pfeil = GetNode<Sprite>("Spieler2Pfeil");
    }

    private void LevelSelectionGegeneinanderPressed()
    {
        _levelSelectionMiteinander.UncheckLevelSelection();
    }

    private void LevelSelectionMiteinanderPressed()
    {
        _levelSelectionGegeneinander.UncheckLevelSelection();
    }
}
