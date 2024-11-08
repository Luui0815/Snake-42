using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public class OnlineMultiplayerSnake : OnlineSnake
{
    private Node2D _face1;
    private Node2D _face2;
    private Vector2 _directionCachePlayer1;
    private Vector2 _directionCachePlayer2;
    private Vector2 _currentDirection;
    private bool _isPlayerOneTurn;
    protected new UInt64 _updateInterval = 100000; // evtl. Anpassen falls Pufferüberlauf
    protected int _NewTailIndex;

    public override void _Ready()
    {
        _fruit = GetParent().GetNode<Fruit>("Fruit");
        _controller = GetParent<GameController>();
        _audioPlayer = GetNode<AudioStreamPlayer2D>("Eating");
        _body = GetNode<Line2D>("Body");
        _points = _body.Points;
        _face1 = GetNode<Node2D>("Face1");
        _face2 = GetNode<Node2D>("Face2");
        _tween = GetNode<Tween>("Tween");

        _directionCachePlayer1 = Vector2.Right;
        _directionCachePlayer2 = Vector2.Left;
        _currentDirection = _directionCachePlayer1;

        for(int i = 0; i < _body.GetPointCount(); i++)
        {
            _TargetPoints.Add(new Vector2(_body.GetPointPosition(i)));
            _SavedTargetPoints.Add(new Vector2(_body.GetPointPosition(i)));
        }
    }

    // _Process hat nur 30 fps und syncht nicht die Bewegungen! => Bewegungsprozess des Clients muss in _PhysicsProcess, das läuft auf 60 fps, je nach Einstellungen
    public override void _Process(float delta)
    {
        if(_isServer)
        {
            if(Time.GetTicksUsec() - _TimeSinceLastUpdate > _updateInterval)
            {
                _TimeSinceLastUpdate = Time.GetTicksUsec();
                TimeToSynchBodyPoints();
            }
        }
        GlobalVariables.Instance.Snake1Body = _body.Points;
    }
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

                if(_AveragePingTime != 0f && diff != 0f)
                {
                    latencyFactor = delta / (float)(_AveragePingTime / 1000f);

                    if(latencyFactor < 0f)
                        latencyFactor = 0f;
                    if(latencyFactor > 1f)
                        latencyFactor = 1f;
                } 
                else
                    latencyFactor = 0.167f;
                
                MakeAverageLatenyFactor();

                _CalculateNewLatenyFactor = false;
                
                // Die aktuellen TargetPoints speichern, da sie asynchron aktualisiert werden können, was zu Rucklern führt.
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
        }
        
        RotateAndMoveFace();
    }
    // protected override void MakeAverageLatenyFactor() => kann man so lassen
    // protected override void CalculatePingTimeStatistic() => kann man so lassen

    public override void SetPlayerSettings(bool isServer)
    {
        // Server hat beide Schlangen, steuert aktiv aber nur die 1.
        // Bei jeder Bewegungsaenderung sendet er es an den 2.Spieler
        // Der 2. Spieler sendet nur Richtungsaenderungen an den Server(Spieler 1!)
        _isServer = isServer;
        _isPlayerOneTurn = true;
        for (int i = 0; i < _points.Length; i++)
        {
            _points[i] += new Vector2(0, 2 * _gridSize);
            _body.SetPointPosition(i, _points[i]);
            _TargetPoints[i] = new Vector2(_body.GetPointPosition(i));
            _SavedTargetPoints[i] = new Vector2(_body.GetPointPosition(i));
            // for(int j = 0; j < _body.GetPointCount(); j++)
            //    _TargetPoints[j] = new Vector2(_body.GetPointPosition(j));
        }
    }
    public override void _Input(InputEvent @event)
    {
        Vector2 direction = Vector2.Zero;

        if (Input.IsActionPressed("ui_up")) direction = Vector2.Up;
        if (Input.IsActionPressed("ui_right")) direction = Vector2.Right;
        if (Input.IsActionPressed("ui_left")) direction = Vector2.Left;
        if (Input.IsActionPressed("ui_down")) direction = Vector2.Down;

        if (direction != Vector2.Zero)
        {
            // Prüfen ob man Spieler 1 oder Spieler 2 ist, je nachdem muss das in den anderen Cache
            // Player1 ist automatisch Server => SetAktDirectionCache muss übergeben werden welcher akt. werden soll
            // eientlich nur auf Server, so erspart man sich if Abfrage
            NetworkManager.NetMan.rpc(GetPath(), nameof(SendServerDirectionCache), false, true, true,Convert.ToInt32(direction.x), Convert.ToInt32(direction.y), _isServer);  
        }
    }

    private void SendServerDirectionCache(int X,int Y, bool AktCache1)
    {
        // Server und Client senden jede Richtungsänderung an diese Methode
        // Diese muss nun prüfen ob die Richtungseingaben in den Chace übernommen werden können
        // Grundlegen kann man nur die Richtung ändern wenn man dran ist
        // dann ist noch zu beachten das die Schlange keine 180° Drehung machen kann
        Vector2 direction = new Vector2(X, Y);
        if(AktCache1)
        {
            if((_directionCachePlayer1 * -1) != direction)
                _directionCachePlayer1 = direction;
        }
        if(!AktCache1)
        {
            if((_directionCachePlayer2 * -1) != direction)
                _directionCachePlayer2 = direction;
        }
    }
    private void SetAktMiteinanderDirectionCache(int X, int Y, bool AktCache1)
    {
        if(AktCache1 == true)
        {
            // Cache des 1.Spielers aktualisieren!
            _directionCachePlayer1.x = X;
            _directionCachePlayer1.y = Y;
        }
        else
        {
            // Cache des 2.Spielers aktualisieren
            _directionCachePlayer2.x = X;
            _directionCachePlayer2.y = Y;
        }
    }

    // public override void MoveSnake() => kann so bleiben
    protected override void MoveTween(float argv)
    {
        if (!_merker)
        {
            // _currentDirection = _isPlayerOneTurn ? _directionCachePlayer1 : _directionCachePlayer2;
            Vector2 newPos = Vector2.Zero;

            if(_isPlayerOneTurn)
            {
                for (int i = 0; i < _body.GetPointCount(); i++)
                {
                    if (i == 0)
                    {
                        newPos = _points[i] + _currentDirection * _gridSize * argv;
                    }
                    else if(i == _NewTailIndex && _growing)
                    {
                        // wenn das Vieh wächst darf das mittlere Element nicht bewegt werden!
                        newPos = _points[i];
                    }
                    else 
                    {
                        Vector2 diff = (_points[i - 1] - _points[i]) / _gridSize;
                        newPos = _points[i] + diff * _gridSize * argv;
                    }

                    _body.SetPointPosition(i, newPos);
                }
            }
            else
            {
                for (int i = _body.GetPointCount() - 1; i >= 0; i--)
                {
                    if (i == _body.GetPointCount() - 1)
                    {
                        newPos = _points[i] + _currentDirection * _gridSize * argv;
                    }
                    else if(i == _NewTailIndex && _growing)
                    {
                        // wenn das Vieh wächst darf das mittlere Element nicht bewegt werden!
                        newPos = _points[i];
                    }
                    else 
                    {
                        Vector2 diff = (_points[i + 1] - _points[i]) / _gridSize;
                        newPos = _points[i] + diff * _gridSize * argv;
                    }

                    _body.SetPointPosition(i, newPos);
                }
            }
            
            RotateAndMoveFace();
            
            if (argv == 1)
            {
                _merker = true;
                _on_Tween_tween_all_completed();
            }
        }
        if(argv != 1)
            _merker = false;
    }

    protected void RotateAndMoveFace()
    {
        _face1.Position = _body.Points[0];
        _face2.Position = _body.Points[_body.Points.Count() - 1];

        Vector2 lookDirection;
        if (_isPlayerOneTurn)
        {
            // genau in die entgegengesetzte Richtung des an ihm befindlichen Körperteil setzten
            lookDirection = ((_points[_points.Count() - 1] - _points[_points.Count() - 2]) / _gridSize) * -1;
            _face1.RotationDegrees = -Mathf.Rad2Deg(_currentDirection.AngleTo(Vector2.Right));
            _face2.RotationDegrees = -Mathf.Rad2Deg(lookDirection.AngleTo(Vector2.Left));
        }
        else
        {
            // -1 mehr da durch CheckFruit Collision der letzte Punkte dupliziert wurde. d.h. 2 mal gleiche Punktkoordinaten!
            lookDirection = ((_points[1] - _points[0]) / _gridSize) * -1;
            _face2.RotationDegrees = -Mathf.Rad2Deg(_currentDirection.AngleTo(Vector2.Left));
            _face1.RotationDegrees = -Mathf.Rad2Deg(lookDirection.AngleTo(Vector2.Right));
        }
    }

    protected override void SetAktDirection()
    {
        // auch prüfen das die Vektoren nicht 180° entgegengesetzt sind
        if(_isPlayerOneTurn)
        {
            if(_currentDirection != (_directionCachePlayer1 * -1))
                _currentDirection = _directionCachePlayer1;
        }
        else
        {
            if(_currentDirection != (_directionCachePlayer2 * -1))
                _currentDirection = _directionCachePlayer2;
        }
    }

    
    protected override void _on_Tween_tween_all_completed()
    {
        if(_isServer)
        {
            _growing = false;
            CheckIfGameOver();
            CheckFruitCollision();
            _points = _body.Points;
            NetworkManager.NetMan.rpc(GetPath(), nameof(SetAktDirection));
        }
    }
    

    // protected override void TimeToSynchBodyPoints() => kann so bleiben
    // protected override void SynchBodyPointsOnClient(string Xjson, string Yjson, UInt64 SendTime) => kann so bleiben

    protected override void CheckFruitCollision()
    {
        if (_body.Points[_isPlayerOneTurn ? 0 : _body.Points.Count() - 1] == _fruit.Position)
        {
            Vector2[] tempPoints = _body.Points;
            _audioPlayer.Play();
            _controller.UpdateScore();
            GD.Print($"{Name} hat Frucht gefressen!");
            
            SwapControl();

            AddNewSnakePoint();

            _points = _body.Points; // Punkte auf das neue Array aktualisieren
            _growing = true; // Setze Wachstum auf aktiv

            _fruit.SetNewPosition(_fruit.RandomizePosition());
            Vector2 newPos = _fruit.RandomizePosition();
            NetworkManager.NetMan.rpc(_fruit.GetPath(), nameof(_fruit.SetNewPosition), false, true, true, newPos.x, newPos.y);
            // Jetzte dem Client sagen das eine Frucht gegeseen wurde!
            NetworkManager.NetMan.rpc(GetPath(), nameof(SetEatingOnOtherPlayer), false, false, true);
        }
    }

    protected override void SetEatingOnOtherPlayer()
    {
        _audioPlayer.Play();
        //IncreaseSpeed();
        _controller.UpdateScore();
        GD.Print($"{Name} hat Frucht gefressen!");
        SwapControl();
        // _points = _body.Points;
        //_body.AddPoint(_body.GetPointPosition(_body.Points.Count() / 2));
        //_body.AddPoint(_body.GetPointPosition(!_isPlayerOneTurn ? _body.Points.Count() - 1 : 0));
        //_TargetPoints.Add(_body.GetPointPosition(_body.Points.Count() - 1)); // hoffen das es funzt
        AddNewSnakePoint();
    }

    protected void AddNewSnakePoint()
    {
        List<Vector2> newPoints = _body.Points.ToList();;
        if(!_isPlayerOneTurn)
        {
            newPoints.Insert(0, newPoints[0]);
            _NewTailIndex = 0;
        }
        else
        {
            _NewTailIndex = _body.Points.Length;
            newPoints.Insert(_body.Points.Length - 1, newPoints[_body.Points.Length - 1]);
        }
        _body.Points = newPoints.ToArray();
    }

    protected override void CheckIfGameOver()
    {
        string LoseMsg = "";
        // Angepasste GameOVer Logik von OfflineMultiplayerSnake:
        foreach (var obstacle in _controller.Obstacles)
        {
            if (_body.Points[_isPlayerOneTurn ? 0 : _body.Points.Count() - 1] == obstacle)
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

        // Prüfen ob sie sich selbst gefressen hat
        if (_points.Length >= 3)
        {
            int startIndex = _isPlayerOneTurn ? 1 : _points.Length - 2;
            int endIndex = _isPlayerOneTurn ? _points.Length : 0;
            int step = _isPlayerOneTurn ? 1 : -1;

            for (int i = startIndex; (step == 1 ? i < endIndex : i >= endIndex); i += step)
            {
                if (_body.Points[_isPlayerOneTurn ? 0 : _body.Points.Count() - 1] == _points[i])
                {
                    LoseMsg = $"Game Over fuer {Name}. Hat sich selbst gefressen!";
                }
            }
        }

        if(LoseMsg != "")
        {
            NetworkManager.NetMan.rpc(GetPath(), nameof(ShowGameOverScreenAndFinishGame), false, true, true, LoseMsg);
        }
    }

    // ShowGameOverScreenAndFinishGame() => kann so bleiben

    private void SwapControl()
    {
        _isPlayerOneTurn = !_isPlayerOneTurn;

        if (_isPlayerOneTurn)
        {
            _directionCachePlayer1 = GetLastSegmentDirection(_isPlayerOneTurn);
        }
        else
        {
            _directionCachePlayer2 = GetLastSegmentDirection(_isPlayerOneTurn);
        }

        // jetzt _current direction so setzten das die Schlange nicht gleich in den eigenen Körper läuft
        Vector2 direction;
        if(_isPlayerOneTurn)
        {
            direction = _body.Points[0] - _body.Points[1];
        }
        else
        {
            direction = _body.Points[_body.Points.Length - 1] - _body.Points[_body.Points.Length - 2];
        }
        direction = direction.Normalized();
        _currentDirection *= -1; 

        // _face1.RotationDegrees = -Mathf.Rad2Deg(_directionCachePlayer1.AngleTo(Vector2.Right));
        // _face2.RotationDegrees = -Mathf.Rad2Deg(_directionCachePlayer2.AngleTo(Vector2.Left));
    }

    private Vector2 GetLastSegmentDirection(bool isPlayerOneTurn)
    {
        var points = _body.Points;
        Vector2 lastSegmentDirection;

        if (!isPlayerOneTurn)
        {
            lastSegmentDirection = points[points.Length - 1] - points[points.Length - 2];
        }
        else
        {
            lastSegmentDirection = points[0] - points[1];
        }

        lastSegmentDirection = lastSegmentDirection.Normalized();

        if (Mathf.Abs(lastSegmentDirection.x) > Mathf.Abs(lastSegmentDirection.y))
        {
            lastSegmentDirection = new Vector2(Mathf.Sign(lastSegmentDirection.x), 0);
        }
        else
        {
            lastSegmentDirection = new Vector2(0, Mathf.Sign(lastSegmentDirection.y));
        }

        GD.Print(lastSegmentDirection);
        return lastSegmentDirection;
    }
}