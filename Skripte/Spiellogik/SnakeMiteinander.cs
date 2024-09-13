using Godot;
using System.Linq;

public class SnakeMiteinander : Snake
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

    public override void _Input(InputEvent @event)
    {
        if (_isPlayerOneTurn)
        {
            if (Input.IsActionPressed("ui_up") && _currentDirection != Vector2.Down) _directionCachePlayer1 = Vector2.Up;
            if (Input.IsActionPressed("ui_right") && _currentDirection != Vector2.Left) _directionCachePlayer1 = Vector2.Right;
            if (Input.IsActionPressed("ui_left") && _currentDirection != Vector2.Right) _directionCachePlayer1 = Vector2.Left;
            if (Input.IsActionPressed("ui_down") && _currentDirection != Vector2.Up) _directionCachePlayer1 = Vector2.Down;
        }
        else
        {
            if (Input.IsActionPressed("move_up") && _currentDirection != Vector2.Down) _directionCachePlayer2 = Vector2.Up;
            if (Input.IsActionPressed("move_right") && _currentDirection != Vector2.Left) _directionCachePlayer2 = Vector2.Right;
            if (Input.IsActionPressed("move_left") && _currentDirection != Vector2.Right) _directionCachePlayer2 = Vector2.Left;
            if (Input.IsActionPressed("move_down") && _currentDirection != Vector2.Up) _directionCachePlayer2 = Vector2.Down;
        }

        GD.Print($"Cache 1{_directionCachePlayer1} Cache 2{_directionCachePlayer2}\nRichtung: {_currentDirection}");
    }

    public override void MoveSnake()
    {
        //_currentDirection = _isPlayerOneTurn ? _directionCachePlayer1 : _directionCachePlayer2;
        _tween.InterpolateMethod(this, "MoveTween", 0, 1, moveDelay, Tween.TransitionType.Linear, Tween.EaseType.InOut);
        _tween.Start();
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
                _tween.StopAll();
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

                MoveSnake();
            }
        }

        if (argv != 1)
        {
            _Merker = false;
        }
    }


    protected override void CheckFruitCollision()
    {
        if (_body.Points[_isPlayerOneTurn ? 0 : _body.Points.Count()-1] == _fruit.Position)
        {
            _tween.StopAll();
            _eating = true;
            _fruit.RandomizePosition();
            //IncreaseSpeed();
            _controller.UpdateScore();
            GD.Print($"{Name} hat Frucht gefressen!");
            MoveSnake();
        }
    }

    private Vector2[]SetPoints(Vector2[] points)
    {
        Vector2[] newPoints = new Vector2[points.Length];
        for(int i=0;i<points.Length; i++)
        {
            newPoints[i] = points[i];
        }
        return newPoints;
    }

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

        //Vector2 tempPosition = _face1.Position;
        //_face1.Position = _face2.Position;
        //_face2.Position = tempPosition;

        ////Array.Reverse(_body.Points);
        //Array.Reverse(_points);

        _face1.RotationDegrees = -Mathf.Rad2Deg(_directionCachePlayer1.AngleTo(Vector2.Right));
        _face2.RotationDegrees = -Mathf.Rad2Deg(_directionCachePlayer2.AngleTo(Vector2.Left));

        _eating = false;
    }
}
