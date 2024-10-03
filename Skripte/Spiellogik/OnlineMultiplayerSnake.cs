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
    }

    // falls es kracht BaseSnake otherSnake in Übergabeparameter!
    public void SetPlayerSettings(bool isServer, bool isSnake1)
    {
        // Server hat beide Schlangen, steuert aktiv aber nur die 1.
        // Bei jeder Bewegungsaenderung sendet er es an den 2.Spieler
        // Der 2. Spieler sendet nur Richtungsaenderungen an den Server(Spieler 1!)
        _isServer = isServer;
        _isPlayerOneTurn = true;

        if(_isServer == true)
        {
            _updateTimer = GetNode<Timer>("UpdateTimer");
            _updateTimer.WaitTime = 0.1f;
            _updateTimer.OneShot = false;
            _updateTimer.Connect("timeout", this, nameof(TimeToSynchBody));
        }
    }

    public override void _Input(InputEvent @event)
    {
        if (_isPlayerOne == false) // Prüfen ob das weg kann!
            return;
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
            NetworkManager.NetMan.rpc(GetPath(), nameof(SetAktDirectionCache), false, true, true,Convert.ToInt32(direction.x), Convert.ToInt32(direction.y), _isServer);
            
        }
    }

    protected void SetAktDirectionCache(int X, int Y, bool AktCache1)
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
    /* kann so von Online Snake übernommen werden:
    public override void MoveSnake()
    {
        //_currentDirection = _isPlayerOneTurn ? _directionCachePlayer1 : _directionCachePlayer2;
        _tween.InterpolateMethod(this, "MoveTween", 0, 1, moveDelay, Tween.TransitionType.Linear, Tween.EaseType.InOut);
        _tween.Start();
    }
    */

    public override void RPCTween(float argv)
    {
        MoveTween(argv);
    }

    protected override void MoveTween(float argv)
    {
        if (!_Merker)
        {
            _currentDirection = _isPlayerOneTurn ? _directionCachePlayer1 : _directionCachePlayer2;

            int headIndex = _isPlayerOneTurn ? 0 : _body.Points.Count() - 1;
            int direction = _isPlayerOneTurn ? 1 : -1;

            for (int i = _isPlayerOneTurn ? 0 : _body.Points.Count() - 1;
                 _isPlayerOneTurn ? i < _body.Points.Count() : i >= 0;
                 i += direction)
            {
                Vector2 newPos;

                if (i == headIndex)
                {
                    newPos = _points[i] + _currentDirection * _gridSize * argv;
                }
                else
                {
                    int prevIndex = i - direction;
                    Vector2 diff = (_points[prevIndex] - _points[i]) / _gridSize;
                    newPos = _points[i] + diff * _gridSize * argv;
                }

                _body.SetPointPosition(i, newPos);
            }

            if (_eating)
            {
                _body.AddPoint(_body.GetPointPosition(_body.Points.Count() / 2));
                _growing = true;
                _points = _body.Points;
                _eating = false;
            }

            _face1.Position = _body.Points[0];
            _face2.Position = _body.Points[_body.Points.Count() - 1];

            _face1.RotationDegrees = -Mathf.Rad2Deg(_directionCachePlayer1.AngleTo(Vector2.Right));
            _face2.RotationDegrees = -Mathf.Rad2Deg(_directionCachePlayer2.AngleTo(Vector2.Left));

            if (argv == 1)
            {
                _Merker = true;
                _points = _body.Points;

                CheckFruitCollision();

                if (_growing)
                    _growing = false;

                if (IsGameOver())
                {
                    _controller.OnGameFinished();
                }
                else if (_eating)
                {
                    SwapControl();
                }
            }
        }

        if (argv != 1)
        {
            _Merker = false;
        }
    }


    protected override void CheckFruitCollision()
    {
        if (_body.Points[_isPlayerOneTurn ? 0 : _body.Points.Count() - 1] == _fruit.Position)
        {
            _audioPlayer.Play();
            _tween.StopAll();
            _eating = true;
            _fruit.RandomizePosition();
            //IncreaseSpeed();
            _controller.UpdateScore();
            GD.Print($"{Name} hat Frucht gefressen!");
            MoveSnake();
        }
    }

    protected override bool IsGameOver()
    {
        foreach (var obstacle in _controller.Obstacles)
        {
            if (_body.Points[_isPlayerOneTurn ? 0 : _body.Points.Count() - 1] == obstacle.RectGlobalPosition)
            {
                _controller.LoseMessage = ($"Game Over fuer {Name}.\nHat ein Hindernis getroffen!");
                return true;
            }
        }

        if (_otherSnake != null && IsInstanceValid(_otherSnake))
        {
            if (_otherSnake.Points.Contains(_body.Points[0]))
            {
                if (_body.Points[0] == _otherSnake.Points[0])
                {
                    _controller.LoseMessage = ($"Unentschieden.\n{Name} und {_otherSnake.Name} sind kollidiert.");
                    return true;
                }
                else
                {
                    _controller.LoseMessage = ($"Game Over fuer {Name}.\nIst mit {_otherSnake.Name} kollidiert!");
                    return true;
                }

            }
        }

        if (_points.Length >= 3)
        {
            int startIndex = _isPlayerOneTurn ? 1 : _points.Length - 2;
            int endIndex = _isPlayerOneTurn ? _points.Length : 0;
            int step = _isPlayerOneTurn ? 1 : -1;

            for (int i = startIndex; (step == 1 ? i < endIndex : i >= endIndex); i += step)
            {
                if (_body.Points[_isPlayerOneTurn ? 0 : _body.Points.Count() - 1] == _points[i])
                {
                    GD.Print($"Game Over fuer {Name}. Hat sich selbst gefressen!");
                    return true;
                }
            }
        }
        return false;
    }
    /*
    private Vector2[] SetPoints(Vector2[] points)
    {
        Vector2[] newPoints = new Vector2[points.Length];
        for (int i = 0; i < points.Length; i++)
        {
            newPoints[i] = points[i];
        }
        return newPoints;
    }
    */
    private void SwapControl()
    {
        _isPlayerOneTurn = !_isPlayerOneTurn;
        if (_isPlayerOneTurn)
        {
            _directionCachePlayer1 = _directionCachePlayer2 * -1;
        }
        else
        {
            _directionCachePlayer2 = _directionCachePlayer1 * -1;
        }

        _face1.RotationDegrees = -Mathf.Rad2Deg(_directionCachePlayer1.AngleTo(Vector2.Right));
        _face2.RotationDegrees = -Mathf.Rad2Deg(_directionCachePlayer2.AngleTo(Vector2.Left));

        _eating = false;
    }
}
