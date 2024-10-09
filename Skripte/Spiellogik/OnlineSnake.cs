using Godot;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public class OnlineSnake : BaseSnake
{
    protected bool _isGameOver;
    protected bool _PointsOnClientUpdated;
    public override void _Ready()
    {
        base._Ready();
        _tween.Connect("tween_all_completed", this, nameof(_on_Tween_tween_all_completed));
    }

    public override void _Process(float delta)
    {
        /*
        if (_Interpolate == true)
        {
            for (int i = 0; i < _body.Points.Length; i++)
            {
                _body.SetPointPosition(i, _body.Points[i].LinearInterpolate(_points[i], 0.01f));// war 0.001f
            }
            if (Convert.ToInt32(_body.Points[0].x) == Convert.ToInt32(_points[0].x) && Convert.ToInt32(_body.Points[0].y) == Convert.ToInt32(_points[0].y))
                _Interpolate = false;
        }
        */
    }

    public override void SetPlayerSettings(bool isServer, bool isSnake1, BaseSnake otherSnake)
    {
        // Server hat beide Schlangen, steuert aktiv aber nur die 1.
        // Bei jeder Bewegungsaenderung sendet er es an den 2.Spieler
        // Der 2. Spieler sendet nur Richtungsaenderungen an den Server(Spieler 1!)
        _isServer = isServer;

        if (isSnake1 == false)
        {
            _body.DefaultColor = new Color(255, 255, 0, 1);
            for (int i = 0; i < _points.Length; i++)
            {
                _points[i] += new Vector2(0, 2 * _gridSize);
                _body.SetPointPosition(i, _points[i]);
            }
        }
        _otherSnake = otherSnake;
        _isSnake1 = isSnake1;
    }

    public override void _Input(InputEvent @event)
    {
        Vector2 direction = Vector2.Zero;
        if (Input.IsActionPressed("ui_up") && _direction != Vector2.Down) direction = Vector2.Up;
        if (Input.IsActionPressed("ui_right") && _direction != Vector2.Left) direction = Vector2.Right;
        if (Input.IsActionPressed("ui_left") && _direction != Vector2.Right) direction = Vector2.Left;
        if (Input.IsActionPressed("ui_down") && _direction != Vector2.Up) direction = Vector2.Down;
        // es wird auf alle Inputs reagiert
        // nur wenn direction != 0,0 wurde der richtige gedrueckt!
        if (direction != Vector2.Zero)
        {
            // Nur diejenige Schlange sendet Richtungsaenderung welche der Spiler auch wirklich steuert!
            if ((_isServer && _isSnake1) || (!_isServer && !_isSnake1))
                NetworkManager.NetMan.rpc(GetPath(), nameof(SetAktDirectionCache), false, true, true, Convert.ToInt32(direction.x), Convert.ToInt32(direction.y));
        }
        
    }

    protected virtual void SetAktDirectionCache(int X, int Y)
    {
        _directionCache.x = X;
        _directionCache.y = Y;
    }

    public override void MoveSnake()
    {
        _tween.InterpolateMethod(this, "MoveTween", 0, 1, moveDelay, Tween.TransitionType.Linear, Tween.EaseType.InOut);
        _direction = _directionCache;
        _tween.Start();
        _Merker = false;
    }

    protected override void MoveTween(float argv)
    {
        if (_Merker == false)
        {
            for (int i = 0; i < _body.GetPointCount(); i++)
            {
                Vector2 newPos, diff = Vector2.Zero;
                if (i == 0)
                    newPos = _points[i] + _direction * new Vector2(_gridSize * argv, _gridSize * argv);
                else
                {
                    if (!(_growing == true && i == _body.Points.Count() - 1))
                    {
                        diff = Vector2.Zero;
                        if (_points[i - 1].x - _points[i].x != 0)
                            diff.x = (_points[i - 1].x - _points[i].x) / _gridSize;
                        if (_points[i - 1].y - _points[i].y != 0)
                            diff.y = (_points[i - 1].y - _points[i].y) / _gridSize;

                        newPos = _points[i] + diff * new Vector2(_gridSize * argv, _gridSize * argv);
                    }
                    else
                    {
                        // letztes Koerperteil darf nicht bewegt werden!
                        newPos = _body.GetPointPosition(i);
                    }
                }
                _body.SetPointPosition(i, newPos);
            }

            _face.Position = _body.Points[0];
            _face.RotationDegrees = -Mathf.Rad2Deg(_direction.AngleTo(Vector2.Right));

            // wenn argv = 1 dann ist eine Schleife durch
            if (argv == 1)
            {
                _Merker = true;
            }
        }
    }

    protected void ResetGrowing()
    {
        _growing = false;
    }

    protected virtual void _on_Tween_tween_all_completed()
    {
        if(_isServer)
        {
            // Der Server aktualisiert, anchdem er einen Schritt gelaufen ist grundlegenden Daten
            // zuerst wird der evtl noch laufende Tweeen auf dem Client gestoppt
            NetworkManager.NetMan.rpc(_tween.GetPath(), nameof(_tween.StopAll), false, false, true);
            // growing wird bei beiden Zurückgesetzt
            NetworkManager.NetMan.rpc(GetPath(), nameof(ResetGrowing), false, true, true);
            // danch prüft der Server für beide ob einen Frucht gegessen wurde!
            CheckFruitCollision(); // => Diese Methode setzt bei, wenn eine Frucht gegessen wurde die Frucht bei beiden an die gleich Stelle neu!
            // danach prüfen beide ob sie gestorben 
            CheckIfGameOver();
            // Punkte auf _body.Points setzen, diese sind in diesem Zyklus noch nicht gewandert!
            _points = _body.Points;
            // PunkteUpdate an Client senden!
            TimeToSynchPoints();
            // Client sendet antwort wenn Punkte aktualisier werden an Server => dann Tween wieder starten
        }
    }

    protected void PointUpdateOnClientReceived()
    {
        // Punkteupdate erfolgreich => Tween bei beiden wieder starten!
        NetworkManager.NetMan.rpc(GetPath(), nameof(MoveSnake));
    }

    protected void TimeToSynchPoints()
    {
        int[] x = new int[_points.Length];
        int[] y = new int[_points.Length];
        for (int j = 0; j < _points.Length; j++)
        {
            x[j] = Convert.ToInt32(_points[j].x); // Hier kein Datenverlust da float hier Ganzzahlen sind
            y[j] = Convert.ToInt32(_points[j].y);
        }
        NetworkManager.NetMan.rpc(GetPath(), nameof(SynchPointsOnClient), false, false, false, JsonConvert.SerializeObject(x), JsonConvert.SerializeObject(y));
    }

    protected void SynchPointsOnClient(string Xjson, string Yjson)
    {

        int[] x = JsonConvert.DeserializeObject<int[]>(Xjson);
        int[] y = JsonConvert.DeserializeObject<int[]>(Yjson);
        
        for (int i = 0; i < x.Length; i++)
        {
            _points[i].x = x[i];
            _points[i].y = y[i];
        }
        // antwort an Server schicken das alle Punkte aktualisiert worden sind
        NetworkManager.NetMan.rpc(GetPath(), nameof(PointUpdateOnClientReceived), false, false, true);
    }

    protected override void CheckFruitCollision()
    {
        if (_body.Points[0] == _fruit.Position)
        {
            _audioPlayer.Play();
            //IncreaseSpeed();
            _controller.UpdateScore();
            GD.Print($"{Name} hat Frucht gefressen!");
            _body.AddPoint(_body.GetPointPosition(_body.Points.Count() - 1));
            _growing = true;
            _points = _body.Points;

            Vector2 newPos = _fruit.RandomizePosition();
            NetworkManager.NetMan.rpc(_fruit.GetPath(), nameof(_fruit.SetNewPosition), false, true, true, newPos.x, newPos.y);
            // Jetzte dem Client sagen das eine Frucht gegeseen wurde!
            NetworkManager.NetMan.rpc(GetPath(), nameof(SetEatingOnOtherPlayer), false, false, true);
        }
    }

    protected virtual void SetEatingOnOtherPlayer()
    {
        _audioPlayer.Play();
        //IncreaseSpeed();
        _controller.UpdateScore();
        GD.Print($"{Name} hat Frucht gefressen!");
        _body.AddPoint(_body.GetPointPosition(_body.Points.Count() - 1));
        _growing = true;
        _points = _body.Points;
    }

    protected virtual void CheckIfGameOver()
    {
        string LoseMsg = "";
        // Alte is GameOver Logik:
        foreach (var obstacle in _controller.Obstacles)
        {
            if (_body.Points[0] == obstacle.RectGlobalPosition)
            {
                LoseMsg = ($"Game Over fuer {Name}.\nHat ein Hindernis getroffen!");
            }
        }

        if (_otherSnake != null && IsInstanceValid(_otherSnake))
        {
            if (_otherSnake.Points.Contains(_body.Points[0]))
            {
                if (_body.Points[0] == _otherSnake.Points[0])
                {
                    LoseMsg = ($"Unentschieden.\n{Name} und {_otherSnake.Name} sind kollidiert.");
                }
                else
                {
                    LoseMsg = ($"Game Over fuer {Name}.\nIst mit {_otherSnake.Name} kollidiert!");
                }

            }
        }

        if (_points.Length >= 3)
        {
            for (int i = 1; i < _points.Length; i++)
            {
                if (_points[0] == _points[i])
                {
                    LoseMsg = $"Game Over fuer {Name}. Hat sich selbst gefressen!";
                }
            }
        }

        // wenn LoseMsg != "" dann ist Spiel vorbei => Lose MSG noch an Client senden!
        if(LoseMsg != "")
        {
            NetworkManager.NetMan.rpc(GetPath(), nameof(ShowGameOverScreenAndFinishGame), false, true, true, LoseMsg);
        }
    }

    protected void ShowGameOverScreenAndFinishGame(string LoseMsg)
    {
        _controller.LoseMessage = LoseMsg;
        _controller.OnGameFinished();
    }
}
