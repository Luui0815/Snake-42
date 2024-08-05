using Godot;
using System;
using System.Linq;
using System.Xml.Serialization;

public class SnakeMiteinander : Snake
{
    private Node2D _face1;
    private Node2D _face2;
    
    public override void _Ready()
    {
        _fruit = GetParent().GetNode<Fruit>("Fruit");
        _controller = GetParent<GameController>();

        _body = GetNode<Line2D>("Body");
        _points = _body.Points;
        _face1 = GetNode<Node2D>("Face1");
        _face2 = GetNode<Node2D>("Face2");
        _tween = GetNode<Tween>("Tween");

        _directionCache = _direction;
        _isPlayerOne = true;
        MoveSnake();
        _face =  _face1;
    }

    protected override void MoveTween(float argv)
    {
        base.MoveTween(argv); 
        _face2.Position = _body.Points[_body.Points.Count() - 1];
        _face2.RotationDegrees = -Mathf.Rad2Deg(_direction.AngleTo(Vector2.Right));
        if(argv == 1 && _Merker == true)
            _isPlayerOne = !_isPlayerOne;
    }


}
