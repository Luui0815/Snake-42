using Godot;
using System;
using System.Linq;
using System.Xml.Serialization;

public class SnakeMiteinander : Snake
{
    private Node2D _face1;
    private Node2D _face2;

    private Vector2 _directionCachePlayer1;
    private Vector2 _directionCachePlayer2;
    private Vector2 _directionPlayer1;
    private Vector2 _directionPlayer2;
    private int _playersTurn; // 1 => Spieler1 ist am Zug, 2 => Spieler2 ist am Zug
    
    public override void _Ready()
    {
        _fruit = GetParent().GetNode<Fruit>("Fruit");
        _controller = GetParent<GameController>();

        _body = GetNode<Line2D>("Body");
        _points = _body.Points;
        _face1 = GetNode<Node2D>("Face1");
        _face2 = GetNode<Node2D>("Face2");
        _tween = GetNode<Tween>("Tween");
        _playersTurn = 1;
        _directionCachePlayer1 = Vector2.Right;
        _directionCachePlayer2 = Vector2.Left;
        _isPlayerOne = true;
        MoveSnake();
        _face =  _face1;
    }

    public override void _Input(InputEvent @event)
    {
        // Player1 input
        if (Input.IsActionPressed("ui_up") && _directionPlayer1 != Vector2.Down) _directionCachePlayer1 = Vector2.Up;
        if (Input.IsActionPressed("ui_right") && _directionPlayer1 != Vector2.Left) _directionCachePlayer1 = Vector2.Right;
        if (Input.IsActionPressed("ui_left") && _directionPlayer1 != Vector2.Right) _directionCachePlayer1 = Vector2.Left;
        if (Input.IsActionPressed("ui_down") && _directionPlayer1 != Vector2.Up) _directionCachePlayer1 = Vector2.Down;

        // Player2 input
        if (Input.IsActionPressed("move_right") && _directionPlayer2 != Vector2.Left) _directionCachePlayer2 = Vector2.Right;
        if (Input.IsActionPressed("move_left") && _directionPlayer2 != Vector2.Right) _directionCachePlayer2 = Vector2.Left;
        if (Input.IsActionPressed("move_up") && _directionPlayer2 != Vector2.Down) _directionCachePlayer2 = Vector2.Up;
        if (Input.IsActionPressed("move_down") && _directionPlayer2 != Vector2.Up) _directionCachePlayer2 = Vector2.Down;
    }

    public override void MoveSnake()
    {
        _directionPlayer1 = _directionCachePlayer1;
        _directionPlayer2 = _directionCachePlayer2;
        _tween.InterpolateMethod(this, "MoveTween", 0, 1, moveDelay, Tween.TransitionType.Linear, Tween.EaseType.InOut);
        _tween.Start();
    }

    protected override void MoveTween(float argv)
    {
        Vector2 currentDirection;
        // aktuelle Richtung in die gelaufen werden soll ermitteln

        if (_playersTurn == 1)
           currentDirection = _directionCachePlayer1;
        else
           currentDirection = _directionCachePlayer2;

        if (_Merker == false)
        {
            int i = 0;
            foreach(Vector2 pos in _body.Points)
            {
                Vector2 newPos,diff=Vector2.Zero;
                if(i == 0)
                    newPos =  _points[i] + currentDirection * new Vector2(_gridSize * argv, _gridSize * argv);
                else
                {
                    // Beim wachsen wird das Extrakörperteil genu in der Mitte des Körpers hinzugefügt
                    if(!(_growing == true && i == _body.Points.Count() / 2))
                    {
                        diff = Vector2.Zero;
                        if(_points[i-1].x - _points[i].x != 0)
                            diff.x = (_points[i-1].x - _points[i].x) / _gridSize;
                        if(_points[i-1].y - _points[i].y != 0)
                            diff.y = (_points[i-1].y - _points[i].y) / _gridSize;
                    
                        newPos =  _points[i] + diff * new Vector2(_gridSize * argv, _gridSize * argv);
                    }
                    else
                    {
                        // mittlerstes Körperteil darf nicht bewegt werden!
                        newPos = _body.GetPointPosition(i);
                    }
                }
                _body.SetPointPosition(i, newPos);
                i++;
            }

            if(_eating == true)
            {
                _body.AddPoint(_body.GetPointPosition(_body.Points.Count() / 2));
                _growing = true;
                _points = _body.Points;
                _eating = false;
            }

            _face1.Position = _body.Points[0];
            _face1.RotationDegrees = -Mathf.Rad2Deg(_directionCachePlayer1.AngleTo(Vector2.Right));

            _face2.Position = _body.Points[_body.Points.Count() - 1];
            _face2.RotationDegrees = -Mathf.Rad2Deg(_directionCachePlayer2.AngleTo(Vector2.Right));

            // wenn argv = 1 dann ist eine Schleife durch
            if(argv == 1)
            {
                _Merker = true;
                _points = _body.Points;
                CheckFruitCollision();

                if(_growing == true)
                {
                    _growing = false;
                    // nachdem die Schlange gewachsen ist wird die Stuerung an den anderen abgegeben
                    if(_playersTurn == 1)
                        _playersTurn = 2;
                    else
                        _playersTurn = 1;
                }

                if (IsGameOver())
                {
                   _controller.OnGameFinished();
                }
                else
                {
                    _directionPlayer1 = _directionCachePlayer1;
                    _directionPlayer2 = _directionCachePlayer2;
                } 
            }
        }
        
        if(argv != 1)
            _Merker = false;
    }


}
