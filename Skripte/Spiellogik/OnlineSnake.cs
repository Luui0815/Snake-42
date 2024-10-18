using Godot;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public class OnlineSnake : BaseSnake
{
    protected UInt64 _updateInterval = 85000; // evtl. Anpassung falls Puffer überläuft, das man die Zeit erhöht!
    protected UInt64 _TimeSinceLastUpdate;
    protected UInt64 _ClientTimeAtBodyPointUpdate;
    public UInt64 _ClientTimeDiffBodyUpdate;
    protected List<Vector2> _TargetPoints = new List<Vector2>();
    private float latencyFactor{get; set;}
    protected bool _CalculateNewLatenyFactor;

    public override void _Ready()
    {
        base._Ready();
        // _tween.Connect("tween_all_completed", this, nameof(_on_Tween_tween_all_completed));
        for(int i = 0; i < _body.GetPointCount(); i++)
            _TargetPoints.Add(new Vector2(_body.GetPointPosition(i)));
    }

    // _Process hat nur 30 fps und syncht nicht die Bewegungen! => Bewegungsprozess des Clients muss in _PhysicsProcess, das läuft auf 60 fps, je nach Einstellungen
    public override void _Process(float delta)
    {
        if(_isServer)
        {
            UInt64 test = Time.GetTicksUsec();
            if(Time.GetTicksUsec() - _TimeSinceLastUpdate > _updateInterval)
            {
                _TimeSinceLastUpdate = Time.GetTicksUsec();
                TimeToSynchBodyPoints();
            }
        }
    }
    protected List<Vector2> _SavedTargetPoints = new List<Vector2>(); // wird benötigt da TargetPoints unabhängig von dem Animationszyklus geändert werden kann
    protected List<UInt64> _ListMeassuredPingTimes = new List<UInt64>();
    protected double _AveragePingTime;
    // Läuft auf 60 fps:
    public override void _PhysicsProcess(float delta)
    {
        if(!_isServer)
        {
            float diff = 0f;

            if(_CalculateNewLatenyFactor)
            {
                if(_TargetPoints[0].x - _body.GetPointPosition(0).x != 0f)
                    diff = _TargetPoints[0].x - _body.GetPointPosition(0).x;
                else
                    diff = _TargetPoints[0].y - _body.GetPointPosition(0).y;

                float distanceeffortpercycle;
                if(_AveragePingTime != 0f)
                    distanceeffortpercycle = (diff * delta) / ((float)(_AveragePingTime / 1000f)); 
                else
                    distanceeffortpercycle = 0.5f;

                if(diff != 0f)
                {
                    latencyFactor = Mathf.Abs(distanceeffortpercycle / diff); 
                    if(latencyFactor < 0f)
                        latencyFactor = 0f;
                    if(latencyFactor > 1f)
                        latencyFactor = 1f;
                }
                else
                    latencyFactor = 0;
                
                MakeAverageLatenyFactor();

                _CalculateNewLatenyFactor = false;
                
                // die jetzigen TargetPoints speichern da sie asynchron aktualisiertwe dren können, das führt zu rucklern!
                _SavedTargetPoints.Clear();
                for(int i = 0; i < _TargetPoints.Count(); i++)
                    _SavedTargetPoints.Add(new Vector2(_TargetPoints[i]));
                _points = _body.Points;
            }
            
            for(int i = 0; i < _SavedTargetPoints.Count(); i++)
            {
                Vector2 DiffVec = _SavedTargetPoints[i] - _points[i];
                DiffVec *= latencyFactor;
                Vector2 newPos = _body.Points[i] + DiffVec;
                _body.SetPointPosition(i, newPos);
            }

            // Gesicht nachsetzen
            _face.Position = _body.Points[0];
            _face.RotationDegrees = -Mathf.Rad2Deg(_direction.AngleTo(Vector2.Right));

            if(Name == "Snake1")
            {
                GlobalVariables.Instance.PingTimeSnake1 = (float)_AveragePingTime;
                GlobalVariables.Instance.Snake1diff = diff;
                GlobalVariables.Instance.Snake1LatencyFactor = latencyFactor;
            }
            if(Name == "Snake2")
            {
                GlobalVariables.Instance.PingTimeSnake2 = (float)_AveragePingTime;
                GlobalVariables.Instance.Snake2diff = diff;
                GlobalVariables.Instance.Snake2LatencyFactor = latencyFactor;
            }
        }
    }
    protected List<float> ListLatencyFactorHistory = new List<float>();
    protected void MakeAverageLatenyFactor()
    {
        if(ListLatencyFactorHistory.Count() > 45)
            ListLatencyFactorHistory.RemoveAt(0);
        ListLatencyFactorHistory.Add(latencyFactor);
        
        latencyFactor = ListLatencyFactorHistory.Average();
    }

    protected void CalculatePingTimeStatistic()
    {
        // Mir scheint es so das ohne diese Satistik die Schlange trotzdem flüssiger läuft
        // Auf Basis der letzten 5 Pingzeiten wird ein durchschnittlicher Ping berechnet
        if(_ListMeassuredPingTimes.Count() > 15)
            _ListMeassuredPingTimes.RemoveAt(0);
        _ListMeassuredPingTimes.Add(_ClientTimeDiffBodyUpdate);

        // jetzt den durchschnitt berechnen!
        _AveragePingTime = _ListMeassuredPingTimes.Average(ping => (double)ping);;
        
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
                for(int j = 0; j < _body.GetPointCount(); j++)
                    _TargetPoints[j] = new Vector2(_body.GetPointPosition(j));
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
            // Nur diejenige Schlange sendet Richtungsaenderung welche der Spieler auch wirklich steuert!
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
        if(_isServer)
        {
            _tween.InterpolateMethod(this, "MoveTween", 0, 1, moveDelay, Tween.TransitionType.Linear, Tween.EaseType.InOut);
            _direction = _directionCache;
            _tween.Start();
            _Merker = false;
        }
        else
        {
            _ClientTimeAtBodyPointUpdate = OS.GetTicksMsec();
            _CalculateNewLatenyFactor = true;
        }
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
                _on_Tween_tween_all_completed();
            }
        }
        if(argv != 1)
            _Merker = false;
    }

    protected void SetAktDirection()
    {
        // bracuht auch der Client, dadurch schickt er in der input Methode nur gültige Richtungsangaben an den Server und dieser muss weniger machen
        _direction = new Vector2(_directionCache);
    }

    protected virtual void _on_Tween_tween_all_completed()
    {
        if(_isServer)
        {
            // Der Server aktualisiert, anchdem er einen Schritt gelaufen ist grundlegenden Daten
            // zuerst wird der evtl noch laufende Tweeen auf dem Client gestoppt
            // NetworkManager.NetMan.rpc(_tween.GetPath(), nameof(_tween.StopAll), false, false, true);
            // growing wird bei beiden Zurückgesetzt
            // NetworkManager.NetMan.rpc(GetPath(), nameof(ResetGrowing), false, true, true);
            _growing = false;
            // danch prüft der Server für beide ob einen Frucht gegessen wurde!
            CheckFruitCollision(); // => Diese Methode setzt bei, wenn eine Frucht gegessen wurde die Frucht bei beiden an die gleich Stelle neu!
            // danach prüfen beide ob sie gestorben 
            CheckIfGameOver();
            // Punkte auf _body.Points setzen, diese sind in diesem Zyklus noch nicht gewandert!
            _points = _body.Points;
            // PunkteUpdate an Client senden! => unnötig
            // TimeToSynchPoints(); => unnötig
            // Client und Server akt. ihre Richtung
            NetworkManager.NetMan.rpc(GetPath(), nameof(SetAktDirection));
        }
    }

    /*
    protected void PointUpdateOnClientReceived()
    {
        // Punkteupdate erfolgreich => Tween bei beiden wieder starten!
        NetworkManager.NetMan.rpc(GetPath(), nameof(MoveSnake));
    }
    */

    /*
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
        // etworkManager.NetMan.rpc(GetPath(), nameof(PointUpdateOnClientReceived), false, false, true);
    }
    */

    protected void TimeToSynchBodyPoints()
    {
        float[] x = new float[_body.GetPointCount()];
        float[] y = new float[_body.GetPointCount()];
        for (int j = 0; j < _body.GetPointCount(); j++)
        {
            x[j] = _body.GetPointPosition(j).x;
            y[j] = _body.GetPointPosition(j).y;
        }
        NetworkManager.NetMan.rpc(GetPath(), nameof(SynchBodyPointsOnClient), false, false, false, JsonConvert.SerializeObject(x), JsonConvert.SerializeObject(y), Time.GetTicksUsec());
    }
    protected void SynchBodyPointsOnClient(string Xjson, string Yjson, UInt64 SendTime)
    {
        float[] x = JsonConvert.DeserializeObject<float[]>(Xjson);
        float[] y = JsonConvert.DeserializeObject<float[]>(Yjson);
        _TargetPoints.Clear();
        
        for (int i = 0; i < x.Length; i++)
        {
            _TargetPoints.Add(new Vector2(x[i], y[i]));
        }
        _ClientTimeDiffBodyUpdate = SendTime - _ClientTimeAtBodyPointUpdate;
        _ClientTimeAtBodyPointUpdate = SendTime;
        if(_ClientTimeDiffBodyUpdate < _updateInterval)
            _ClientTimeDiffBodyUpdate = _updateInterval;

        // Umwandlung von Micro in Millisek
        _ClientTimeDiffBodyUpdate /= 1000;

        _CalculateNewLatenyFactor = true;
        // um Netzwerkschwankungen auszugelcihen wird für folgende Berechnungen ein Mittelwert beerchnet
        CalculatePingTimeStatistic();
        // antwort an Server schicken das alle Punkte aktualisiert worden sind
        // etworkManager.NetMan.rpc(GetPath(), nameof(PointUpdateOnClientReceived), false, false, true);
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
        _TargetPoints.Add(_body.GetPointPosition(_body.Points.Count() - 1)); // hoffen das es funzt
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
