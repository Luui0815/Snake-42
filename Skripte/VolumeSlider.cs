using Godot;
using System;

public class VolumeSlider : HSlider
{
    private string busName = "Master"; 
    private string settingKey = "audio/volume"; // Schluessel zum Speichern in ProjectSettings
    private HSlider _volumeSlider;

    public override void _Ready()
    {
        _volumeSlider = GetParent().GetNode<HSlider>("VolumeSlider");

        // Lade gespeicherten Lautstaerkewert, falls vorhanden, oder setze Standardwert auf 50
        float savedVolume = ProjectSettings.HasSetting(settingKey) ? 
                            (float)ProjectSettings.GetSetting(settingKey) : 50.0f;

        // Setze Slider-Wert und die Lautstaerke des Audiobusses
        _volumeSlider.Value = savedVolume;
        SetVolume((float)_volumeSlider.Value);

        _volumeSlider.Connect("value_changed", this, nameof(OnVolumeChanged));
    }

    private void OnVolumeChanged(float value)
    {
        SetVolume(value);

        // Speichere Lautstaerkewert in den ProjectSettings
        ProjectSettings.SetSetting(settingKey, value);
        ProjectSettings.Save(); 
    }

    private void SetVolume(float value)
    {
        // Konvertiere Slider-Wert (0-100) in Dezibel
        float db = Mathf.Lerp(-40, 0, value / 100.0f);
        AudioServer.SetBusVolumeDb(AudioServer.GetBusIndex(busName), db);
    }
}
