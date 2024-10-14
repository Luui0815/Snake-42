using Godot;
using System;
using System.Linq;

public class OnlineMultiplayerSnake : OnlineSnake
{
    private Node2D _face1;
    private Node2D _face2;

    private Vector2 _directionCachePlayer1;
    private Vector2 _directionCachePlayer2;
    private Vector2 _currentDirection;
    private bool _isPlayerOneTurn;

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
        _isPlayerOneTurn = true;

        Error e = _tween.Connect("tween_all_completed", this, nameof(_on_Tween_tween_all_completed));
    }

    // falls es kracht BaseSnake otherSnake in Übergabeparameter!
    public override void SetPlayerSettings(bool isServer)
    {
        // Server hat beide Schlangen, steuert aktiv aber nur die 1.
        // Bei jeder Bewegungsaenderung sendet er es an den 2.Spieler
        // Der 2. Spieler sendet nur Richtungsaenderungen an den Server(Spieler 1!)
        _isServer = isServer;
        _isPlayerOneTurn = true;
    }

    public override void _Input(InputEvent @event)
    {
        Vector2 direction = Vector2.Zero;
        if (Input.IsActionPressed("ui_up") && _currentDirection != Vector2.Down) direction = Vector2.Up;
        if (Input.IsActionPressed("ui_right") && _currentDirection != Vector2.Left) direction = Vector2.Right;
        if (Input.IsActionPressed("ui_left") && _currentDirection != Vector2.Right) direction = Vector2.Left;
        if (Input.IsActionPressed("ui_down") && _currentDirection != Vector2.Up) direction = Vector2.Down;

        if (direction != Vector2.Zero)
        {
            // Prüfen ob man Spieler 1 oder Spieler 2 ist, je nachdem muss das in den anderen Cache
            // Player1 ist automatisch Server => SetAktDirectionCache muss übergeben werden welcher akt. werden soll
            // auf beiden Geräten!
            NetworkManager.NetMan.rpc(GetPath(), nameof(SetAktMiteinanderDirectionCache), false, true, true,Convert.ToInt32(direction.x), Convert.ToInt32(direction.y), _isServer);
            
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

    public override void MoveSnake()
    {
        _tween.InterpolateMethod(this, "MoveTween", 0, 1, moveDelay, Tween.TransitionType.Linear, Tween.EaseType.InOut);
        _direction = _isPlayerOneTurn ? _directionCachePlayer1 : _directionCachePlayer2;
        _tween.Start();
        _Merker = false;
    }
    protected override void MoveTween(float argv)
    {
        if (!_Merker)
        {
            if(_isPlayerOneTurn)
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
            }
            else
            {
                for (int i = _body.Points.Length - 1; i >= 0; i--)
                {
                    Vector2 newPos, diff = Vector2.Zero;
                    if (i == _body.Points.Length - 1)
                        newPos = _points[i] + _direction * new Vector2(_gridSize * argv, _gridSize * argv);
                    else
                    {
                        if (!(_growing == true && i == _body.Points.Length - 2))
                        {
                            diff = Vector2.Zero;
                            if (_points[i + 1].x - _points[i].x != 0)
                                diff.x = (_points[i + 1].x - _points[i].x) / _gridSize;
                            if (_points[i + 1].y - _points[i].y != 0)
                                diff.y = (_points[i + 1].y - _points[i].y) / _gridSize;

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
            }
            

            _face1.Position = _body.Points[0];
            _face2.Position = _body.Points[_body.Points.Count() - 1];

            Vector2 lookDirection;
            if (_isPlayerOneTurn)
            {
                // genau in die entgegengesetzte Richtung des an ihm befindlichen Körperteil setzten
                lookDirection = ((_points[_points.Count() - 1] - _points[_points.Count() - 2]) / _gridSize) * -1;
                _face1.RotationDegrees = -Mathf.Rad2Deg(_directionCachePlayer1.AngleTo(Vector2.Right));
                _face2.RotationDegrees = -Mathf.Rad2Deg(lookDirection.AngleTo(Vector2.Left));
            }
            else
            {
                // -1 mehr da durch CheckFruit Collision der letzte Punkte dupliziert wurde. d.h. 2 mal gleiche Punktkoordinaten!
                lookDirection = ((_points[1] - _points[0]) / _gridSize) * -1;
                _face2.RotationDegrees = -Mathf.Rad2Deg(_directionCachePlayer2.AngleTo(Vector2.Left));
                _face1.RotationDegrees = -Mathf.Rad2Deg(lookDirection.AngleTo(Vector2.Right));
            }

            if (argv == 1)
            {
                _Merker = true;
            }
        }
    }

    // ResetGrowing() => kann so bleiben

    // _OnTween_tween_all_completed() => muss wegen Swap Control überschreiben werden
    protected override void _on_Tween_tween_all_completed()
    {
        if(_isServer)
        {
            // Der Server aktualisiert, nachdem er einen Schritt gelaufen ist grundlegenden Daten
            // zuerst wird der evtl noch laufende Tweeen auf dem Client gestoppt
            NetworkManager.NetMan.rpc(_tween.GetPath(), nameof(_tween.StopAll), false, false, true);
            // growing wird bei beiden Zurückgesetzt
            // NetworkManager.NetMan.rpc(GetPath(), nameof(ResetGrowing), false, true, true);
            // Punkte auf _body.Points setzen, diese sind in diesem Zyklus noch nicht gewandert!
            _points = _body.Points;
            // danach prüfen beide ob sie gestorben 
            CheckIfGameOver();
            // danch prüft der Server für beide ob einen Frucht gegessen wurde!
            CheckFruitCollision(); // => Diese Methode setzt bei, wenn eine Frucht gegessen wurde die Frucht bei beiden an die gleich Stelle neu!
            // Swap Control ausführen wenn gegessen wurde
            if(_growing)
            {
                NetworkManager.NetMan.rpc(GetPath(), nameof(SwapControl));
            }
            // PunkteUpdate an Client senden!
            // TimeToSynchPoints();
            // Client sendet antwort wenn Punkte aktualisier werden an Server => dann Tween wieder starten
        }
    }

    // PointUpdateOnClientReceived() => kann so bleiben
    // TimeToSynchPoints() => kann so bleiben
    // SynchPointsOnClient() => kann so bleiben
    protected override void CheckFruitCollision()
    {
        if (_body.Points[_isPlayerOneTurn ? 0 : _body.Points.Count() - 1] == _fruit.Position)
        {
            _audioPlayer.Play();
            _controller.UpdateScore();
            GD.Print($"{Name} hat Frucht gefressen!");
            _body.AddPoint(_body.GetPointPosition(_body.Points.Count() - 1));
            _growing = true;
            //IncreseSpeed();
            _points = _body.Points;

            _fruit.SetNewPosition(_fruit.RandomizePosition());
            Vector2 newPos = _fruit.RandomizePosition();
            NetworkManager.NetMan.rpc(_fruit.GetPath(), nameof(_fruit.SetNewPosition), false, true, true, newPos.x, newPos.y);
            // Jetzte dem Client sagen das eine Frucht gegeseen wurde!
            NetworkManager.NetMan.rpc(GetPath(), nameof(SetEatingOnOtherPlayer), false, false, true);
        }
    }
    // SetEatingOnOtherPlayer() => kann so bleiben

    protected override void CheckIfGameOver()
    {
        string LoseMsg = "";
        // Angepasste GameOVer Logik von OfflineMultiplayerSnake:
        foreach (var obstacle in _controller.Obstacles)
        {
            if (_body.Points[_isPlayerOneTurn ? 0 : _body.Points.Count() - 1] == obstacle.RectGlobalPosition)
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
        for(int i = (_isPlayerOneTurn ? 1 : 0); i < (_isPlayerOneTurn ? _points.Count() : _points.Count() - 1); i++)
        {
            if(_points[_isPlayerOneTurn ? 0 : _points.Count() - 1].x == _points[i].x && _points[_isPlayerOneTurn ? 0 : _points.Count() - 1].y == _points[i].y)
            {
                LoseMsg = ($"Game Over fuer {Name}. Hat sich selbst gefressen!");
                break;
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
            // genau in die entgegengesetzte Richtung des an ihm befindlichen Körperteil setzten
            _directionCachePlayer1 = ((_points[1] - _points[0]) / _gridSize) * -1;
        }
        else
        {
            // -1 mehr da durch CheckFruit Collision der letzte Punkte dupliziert wurde. d.h. 2 mal gleiche Punktkoordinaten!
            _directionCachePlayer2 = ((_points[_points.Count() - 1] - _points[_points.Count() - 2]) / _gridSize) * -1;
        }

        _face1.RotationDegrees = -Mathf.Rad2Deg(_directionCachePlayer1.AngleTo(Vector2.Right));
        _face2.RotationDegrees = -Mathf.Rad2Deg(_directionCachePlayer2.AngleTo(Vector2.Left));
    }
}