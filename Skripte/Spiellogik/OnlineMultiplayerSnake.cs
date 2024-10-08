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

    // public override void MoveSnake() => kann so bleiben
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
                Vector2 newPos = new Vector2();

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

            _face1.Position = _body.Points[0];
            _face2.Position = _body.Points[_body.Points.Count() - 1];

            _face1.RotationDegrees = -Mathf.Rad2Deg(_directionCachePlayer1.AngleTo(Vector2.Right));
            _face2.RotationDegrees = -Mathf.Rad2Deg(_directionCachePlayer2.AngleTo(Vector2.Left));

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
            NetworkManager.NetMan.rpc(GetPath(), nameof(ResetGrowing), false, true, true);
            // danch prüft der Server für beide ob einen Frucht gegessen wurde!
            CheckFruitCollision(); // => Diese Methode setzt bei, wenn eine Frucht gegessen wurde die Frucht bei beiden an die gleich Stelle neu!
            // danach prüfen beide ob sie gestorben 
            NetworkManager.NetMan.rpc(GetPath(), nameof(IsGameOver), false, true, true);
            // Swap Control ausführen wenn gegessen wurde
            if(_growing)
            {
                NetworkManager.NetMan.rpc(GetPath(), nameof(SwapControl));
            }
            // Punkte auf _body.Points setzen, diese sind in diesem Zyklus noch nicht gewandert!
            _points = _body.Points;
            // PunkteUpdate an Client senden!
            TimeToSynchPoints();
            // Client sendet antwort wenn Punkte aktualisier werden an Server => dann Tween wieder starten
        }
    }

    // PointUpdateOnClientReceived() => kann so bleiben
    // TimeToSynchPoints() => kann so bleiben
    // SynchPointsOnClient() => kann so bleiben
    // CheckFruitCollision() => kann so bleiben
    // SetEatingOnOtherPlayer() => kann so bleiben
    // IsGameOver() => kann so bleiben

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
    }
}