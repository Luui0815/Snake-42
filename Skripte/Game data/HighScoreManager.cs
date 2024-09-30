using System;
using System.Collections.Generic;
using System.IO;
using Godot;
using Newtonsoft.Json;

public class HighScores
{
    public Dictionary<string, int> HighScoreDict = new Dictionary<string, int>();
}

public class HighScoreManager
{
    private string _filePath = ProjectSettings.GlobalizePath("user://highscores.json");
    private HighScores _highscores;

    public HighScoreManager()
    {
        LoadHighScores();
    }

    private void LoadHighScores()
    {
        if (System.IO.File.Exists(_filePath))
        {  
            var json = System.IO.File.ReadAllText(_filePath);
            _highscores = JsonConvert.DeserializeObject<HighScores>(json);
            GD.Print("HighScores geladen");
        }
        else
        {
            _highscores= new HighScores();
            SaveHighScores();
            GD.Print("HighScore Datei neu erstellt");
        }
    }

    private void SaveHighScores()
    {
        var json = JsonConvert.SerializeObject(_highscores);
        System.IO.File.WriteAllText(_filePath, json);
        GD.Print("HighScores gespeichert");
    }

    public void SetHighScore(string level, int score)
    {
        if(_highscores.HighScoreDict.ContainsKey(level))
        {
            if(score > _highscores.HighScoreDict[level])
            {
                _highscores.HighScoreDict[level] = score;
                SaveHighScores();
                GD.Print("Neuer HighScore gesetzt");
            }
        }
        else
        {
            _highscores.HighScoreDict[level] = score;
            SaveHighScores();
        }
    }

    public int GetHighScore(string level)
    {
        if (_highscores.HighScoreDict.ContainsKey(level))
        {
            return _highscores.HighScoreDict[level];
        }
        else
        {
            return 0;
        }
    }
}
