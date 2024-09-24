using Godot;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public class Snake : Node2D
{
    private Vector2 _direction = Vector2.Right;
    private Vector2 _directionCache;
    protected AudioStreamPlayer2D _audioPlayer;
    protected Vector2[] _points;
    protected Line2D _body;
    protected Node2D _face;
    protected Tween _tween;
    protected Fruit _fruit;
    protected GameController _controller;
    protected Snake _otherSnake;

    protected int _gridSize = 32;
    public float moveDelay;
    protected bool _eating = false;
    protected bool _growing = false;
    protected bool _isPlayerOne;
    protected bool _Merker = false;

    public Vector2[] Points { get { return _points; } }

    protected bool _isServer;
    protected bool _isSnake1;

    public override void _Ready()
    {
        _fruit = GetParent().GetNode<Fruit>("Fruit");
        _controller = GetParent<GameController>();
        _audioPlayer = GetNode<AudioStreamPlayer2D>("Eating");
        _body = GetNode<Line2D>("Body");
        _points = _body.Points;
        _face = GetNode<Node2D>("Face");
        _tween = GetNode<Tween>("Tween");

        _directionCache = _direction;
    }

    public void SetOfflinePlayerSettings(bool isPlayerOne, Snake otherSnake = null)
    {
        _isPlayerOne = isPlayerOne;
        Snake s = null;
        if (!isPlayerOne)
        {
            if(GetParent() != null)
                s = GetParent().GetNodeOrNull<Snake>("Snake1");
            if (s != null)
                _otherSnake = s;
            else
                _otherSnake = otherSnake;

            _body.DefaultColor = new Color(255, 255, 0, 1);
            for(int i = 0; i < _points.Length; i++)
            {
                _points[i] += new Vector2(0, 2*_gridSize);
                _body.SetPointPosition(i, _points[i]);
            }
        }
        else
        {
            _otherSnake = GetParent().GetNodeOrNull<Snake>("Snake2");
        }
        Console.WriteLine($"Schalange 1, Schalange 2: {_otherSnake}");
    }

    public void SetOnlinePlayerSettings(bool isServer, bool isSnake1, Snake otherSnake)
    {
        // Server hat beide Schlangen, steuert aktiv aber nur die 1.
        // Bei jeder Bewegungsänderung sendet er es an den 2.Spieler
        // Der 2. Spieler sendet nur Richtungsänderungen an den Server(Spieler 1!)
        _isServer = isServer;

        if(isSnake1 == false)
        {
            _body.DefaultColor = new Color(255,255,0,1);
            for(int i = 0; i < _points.Length; i++)
            {
                _points[i] += new Vector2(0, 2*_gridSize);
                _body.SetPointPosition(i, _points[i]);
            }
        }
        // Egal ob Spiler 1 oder 2 beide steuern mit wasd, also _IsSpiler1 = true
        _isPlayerOne = true;
        _otherSnake = otherSnake;
        _isSnake1 = isSnake1;
    }

    public override void _Input(InputEvent @event)
    {
        if(GlobalVariables.Instance.OnlineGame == true && _isPlayerOne == false)
            return;

        if (@event.IsPressed())
        {
            Vector2 direction = Vector2.Zero;

            if (_isPlayerOne)
            {

                if (Input.IsActionPressed("ui_up") && _direction != Vector2.Down) direction = Vector2.Up;
                if (Input.IsActionPressed("ui_right") && _direction != Vector2.Left) direction = Vector2.Right;
                if (Input.IsActionPressed("ui_left") && _direction != Vector2.Right) direction = Vector2.Left;
                if (Input.IsActionPressed("ui_down") && _direction != Vector2.Up) direction = Vector2.Down;       
            }
            else
            {
                if (Input.IsActionPressed("move_right") && _direction != Vector2.Left) direction = Vector2.Right;
                if (Input.IsActionPressed("move_left") && _direction != Vector2.Right) direction = Vector2.Left;
                if (Input.IsActionPressed("move_up") && _direction != Vector2.Down) direction= Vector2.Up;
                if (Input.IsActionPressed("move_down") && _direction != Vector2.Up) direction = Vector2.Down;
            }
            // es wird auf alle Inputs reagiert
            // nur wenn direction != 0,0 wurde der richtige gedrückt!

            // Offline:
            if(GlobalVariables.Instance.OnlineGame == false)
            {
                if(direction != Vector2.Zero)
                    SetAktDirectionCache(Convert.ToInt32(direction.x), Convert.ToInt32(direction.y));
            }
            // online
            else
            {
                if(direction != Vector2.Zero)
                {
                    // Wenn Server, dann aktualisieren deinen Cache selbst, der Spieler2 muss das nicht wissen
                    //if(_isServer == true)
                    //{
                    //    SetAktDirectionCache(Convert.ToInt32(direction.x), Convert.ToInt32(direction.y));
                    //}
                    //else
                    //{
                        // Wenn er Spiler 2 ist sollen seine Einagben an Spielr 1 geschickt werden!
                        // da er 2 Schlangen hat, würde die Richtungseingabe 2 mal gesendet, daher nur Richtungsänderungen 
                        // senden die von Schlange 2 kommen => dann stimmt der rpc Pfad gleich beim Spiler 1 überein!
                        // mit folgendem rpc call wird der directioncache von Spiler1 von Schlange 2 durch Spieler2 Schlange 2 geändert
                        //if(_isSnake1 == false)
                        //{
                            // Aus irgendeinem Grund kann Vector2 nicht gewnadelt werden!
                            // Nur diejenige Schlange sendet Richtungsänderung welche der Spiler auch wirklich steuert!
                            if((_isServer && _isSnake1) || (!_isServer && !_isSnake1))
                                NetworkManager.NetMan.rpc(GetPath(), nameof(SetAktDirectionCache), false, true, true, Convert.ToInt32(direction.x), Convert.ToInt32(direction.y));
                        //}
                    //}
                }
                
            }
        }
    }

    public virtual void SetAktDirectionCache(int X, int Y)
    {
        _directionCache.x = X;
        _directionCache.y = Y;
    }

    public virtual void MoveSnake()
    {
        _direction = _directionCache;
        _tween.InterpolateMethod(this, "RPCTween", 0, 1, moveDelay, Tween.TransitionType.Linear, Tween.EaseType.InOut);
        _tween.Start();
    }

    public virtual void RPCTween(float argv)
    {
        if(GlobalVariables.Instance.OnlineGame == true)
        {
            // Wenn Server mache MoveTween
            // Wenn Client mache ClientMoveTween
            if(_isServer == true)
            {
                // Man hat das Problem das die Positionen der Schlangen bei beiden Spielern auseinander gehen, da argv nicht bei jedem genau uzr gleichen zeit
                // die gleichen Werte haben, daher muss man ingewissen Abständen den Cleint wieder mit dem Server synchronisieren!!! => Wie? Kein blassen Schimemr
                MoveTween(argv);
            }
            else
            {
                ClientMoveTween(argv);
            }
        }
        else
        {
            MoveTween(argv);
        }


    }

    protected virtual void ClientMoveTween(float argv)
    {
        if(_Merker == false)
        {
            //_Interpolate = false;
            int i = 0;
            foreach(Vector2 pos in _body.Points)
            {
                Vector2 newPos,diff=Vector2.Zero;
                if(i == 0)
                    newPos =  _points[i] + _direction * new Vector2(_gridSize * argv, _gridSize * argv);
                else
                {
                    if(!(_growing == true && i == _body.Points.Count() -1))
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
                        // letztes Körperteil darf nicht bewegt werden!
                        newPos = _body.GetPointPosition(i);
                    }
                }
                _body.SetPointPosition(i, newPos);
                i++;
            }

            if(_eating == true)
            {
                _body.AddPoint(_body.GetPointPosition(_body.Points.Count()-1));
                _growing = true;
                _points = _body.Points;
                _eating = false;
            }

            _face.Position = _body.Points[0];
            _face.RotationDegrees = -Mathf.Rad2Deg(_direction.AngleTo(Vector2.Right));

            // wenn argv = 1 dann ist eine Schleife durch
            if(argv == 1)
            {
                _Merker = true;
                _points = _body.Points;

                if(_growing == true)
                    _growing = false;

                _direction = _directionCache;
            }
        }

        if(argv != 1)
        {
            _Merker = false;
        }
    }

private bool _Interpolate;

private void SynchClient(string Xjson, string Yjson)
{

    int[] x = JsonConvert.DeserializeObject<int[]>(Xjson);
    int[] y = JsonConvert.DeserializeObject<int[]>(Yjson);


    for (int i = 0; i < x.Length; i++)
    {
        _points[i] = new Vector2(x[i], y[i]);
    }
    _Interpolate = true;
    
}

public override void _Process(float delta)
{
    if(_Interpolate == true)
    {
        for (int i = 0; i < _body.Points.Length; i++)
        {
            _body.SetPointPosition(i, _body.Points[i].LinearInterpolate(_points[i], 0.01f));// war 0.001f
        }
        if(Convert.ToInt32(_body.Points[0].x) == Convert.ToInt32(_points[0].x) && Convert.ToInt32(_body.Points[0].y) == Convert.ToInt32(_points[0].y))
            _Interpolate = false;
    }
    


}


    protected virtual void MoveTween(float argv)
    {
        if(_Merker == false)
        {
            int i = 0;
            foreach(Vector2 pos in _body.Points)
            {
                Vector2 newPos,diff=Vector2.Zero;
                if(i == 0)
                    newPos =  _points[i] + _direction * new Vector2(_gridSize * argv, _gridSize * argv);
                else
                {
                    if(!(_growing == true && i == _body.Points.Count() -1))
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
                        // letztes Körperteil darf nicht bewegt werden!
                        newPos = _body.GetPointPosition(i);
                    }
                }
                _body.SetPointPosition(i, newPos);
                i++;
            }

            if(_eating == true)
            {
                _body.AddPoint(_body.GetPointPosition(_body.Points.Count()-1));
                _growing = true;
                _points = _body.Points;
                _eating = false;
            }

            _face.Position = _body.Points[0];
            _face.RotationDegrees = -Mathf.Rad2Deg(_direction.AngleTo(Vector2.Right));

            // wenn argv = 1 dann ist eine Schleife durch
            if(argv == 1)
            {
                _Merker = true;
                _points = _body.Points;
                CheckFruitCollision();

                if(_growing == true)
                    _growing = false;
                
                if (IsGameOver())
                {
                    if(GlobalVariables.Instance.OnlineGame)
                        NetworkManager.NetMan.rpc(_controller.GetPath(), nameof(_controller.OnGameFinished));
                    else
                        _controller.OnGameFinished();
                }
                else
                {
                    _direction = _directionCache;
                }

                                // Clientstand wieder mit Serverstand synchronisieren, jetzt sollten alle Pos. int sein
                int[] x = new int[_body.Points.Count()];
                int[] y = new int[_body.Points.Count()];
                for(int j = 0; j < _body.Points.Count(); j++)
                {
                    x[j] = Convert.ToInt32(_body.Points[j].x);
                    y[j] = Convert.ToInt32(_body.Points[j].y);
                }

                // int Array in Byte Array wandeln, da Json zu langsam wäre
                /*
                byte[] Xbyte = new byte[x.Length * sizeof(int)];
                Buffer.BlockCopy(x, 0, Xbyte, 0, Xbyte.Length);

                byte[] Ybyte = new byte[y.Length * sizeof(int)];
                Buffer.BlockCopy(y, 0, Ybyte, 0, Ybyte.Length);
                */
                NetworkManager.NetMan.rpc(GetPath(), nameof(SynchClient), false, false, false, JsonConvert.SerializeObject(x), JsonConvert.SerializeObject(y));
            }
        }

        if(argv != 1)
        {
            _Merker = false;
        }
    }



    protected virtual void CheckFruitCollision()
    {
        if (_body.Points[0] == _fruit.Position)
        {
            _eating = true;
            _audioPlayer.Play();
            //IncreaseSpeed();
            _controller.UpdateScore();
            GD.Print($"{Name} hat Frucht gefressen!");

            if(GlobalVariables.Instance.OnlineGame == false)
		        _fruit.SetNewPosition(_fruit.RandomizePosition());
            else
            {
                Vector2 newPos = _fruit.RandomizePosition();
                NetworkManager.NetMan.rpc(_fruit.GetPath(), nameof(_fruit.SetNewPosition), false, true, true, newPos.x , newPos.y);
                // Jetzte dem Client sagen das eine Frucht gegeseen wurde!
                NetworkManager.NetMan.rpc(GetPath(), nameof(SetEatingOnOtherPlayer), false, false, true);
            }
        }
    }

    protected virtual void SetEatingOnOtherPlayer()
    {
        _eating = true;
        _audioPlayer.Play();
        //IncreaseSpeed();
        _controller.UpdateScore();
        GD.Print($"{Name} hat Frucht gefressen!");
    }

    protected void IncreaseSpeed()
    {
        //moveDelay = Math.Max(0.06f, moveDelay - 0.04f);
        moveDelay *= 0.95f;
        GD.Print(moveDelay.ToString());
    }

    protected virtual bool IsGameOver()
    {
        foreach (var obstacle in _controller.Obstacles)
        {
            if (_body.Points[0] == obstacle.RectGlobalPosition)
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
            for (int i = 1; i < _points.Length; i++)
            {
                if (_points[0] == _points[i])
                {
                    GD.Print($"Game Over fuer {Name}. Hat sich selbst gefressen!");
                    return true;
                }
            }
        }
        return false;
    }
}
