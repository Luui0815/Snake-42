using Godot;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public class OnlineSnake : BaseSnake
{
    protected bool _Interpolate;
    protected Timer _updateTimer;

    public override void _Ready()
    {
        base._Ready();
    }

    protected void TimeToSynchBody()
    {
        float[] x = new float[_body.Points.Count()];
        float[] y = new float[_body.Points.Count()];
        for (int i = 0; i < _body.Points.Count(); i++)
        {
            x[i] = _body.Points[i].x;
            y[i] = _body.Points[i].y;
        }
        NetworkManager.NetMan.rpc(GetPath(), nameof(SynchBodyPointsOnClient), false, false, false, JsonConvert.SerializeObject(x), JsonConvert.SerializeObject(y));
    }

    public override void _Process(float delta)
    {
        if (_Interpolate == true)
        {
            for (int i = 0; i < _body.Points.Length; i++)
            {
                _body.SetPointPosition(i, _body.Points[i].LinearInterpolate(_points[i], 0.01f));// war 0.001f
            }
            if (Convert.ToInt32(_body.Points[0].x) == Convert.ToInt32(_points[0].x) && Convert.ToInt32(_body.Points[0].y) == Convert.ToInt32(_points[0].y))
                _Interpolate = false;
        }
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
        // Egal ob Spiler 1 oder 2 beide steuern mit wasd, also _IsSpiler1 = true
        _isPlayerOne = true;
        _otherSnake = otherSnake;
        _isSnake1 = isSnake1;

        // UpdateTimer stellen wenn Server
        if(_isServer == true)
        {
            _updateTimer = GetNode<Timer>("UpdateTimer");
            _updateTimer.WaitTime = 0.05f;
            _updateTimer.OneShot = false;
            _updateTimer.Connect("timeout", this, nameof(TimeToSynchBody));
        }
    }

    public override void _Input(InputEvent @event)
    {
        if (_isPlayerOne == false) // Prüfen ob das weg kann!
            return;

        if (@event.IsPressed()) // // Prüfen ob das weg kann!
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
                // Prüfen ob das weg kann!
                if (Input.IsActionPressed("move_right") && _direction != Vector2.Left) direction = Vector2.Right;
                if (Input.IsActionPressed("move_left") && _direction != Vector2.Right) direction = Vector2.Left;
                if (Input.IsActionPressed("move_up") && _direction != Vector2.Down) direction = Vector2.Up;
                if (Input.IsActionPressed("move_down") && _direction != Vector2.Up) direction = Vector2.Down;
            }
            // es wird auf alle Inputs reagiert
            // nur wenn direction != 0,0 wurde der richtige gedrueckt!


            // online
            if (direction != Vector2.Zero)
            {
                // Wenn Server, dann aktualisieren deinen Cache selbst, der Spieler2 muss das nicht wissen
                //if(_isServer == true)
                //{
                //    SetAktDirectionCache(Convert.ToInt32(direction.x), Convert.ToInt32(direction.y));
                //}
                //else
                //{
                // Wenn er Spiler 2 ist sollen seine Einagben an Spielr 1 geschickt werden!
                // da er 2 Schlangen hat, wuerde die Richtungseingabe 2 mal gesendet, daher nur RichtungsÄnderungen 
                // senden die von Schlange 2 kommen => dann stimmt der rpc Pfad gleich beim Spiler 1 überein!
                // mit folgendem rpc call wird der directioncache von Spiler1 von Schlange 2 durch Spieler2 Schlange 2 geändert
                //if(_isSnake1 == false)
                //{
                // Aus irgendeinem Grund kann Vector2 nicht gewnadelt werden!
                // Nur diejenige Schlange sendet Richtungsaenderung welche der Spiler auch wirklich steuert!
                if ((_isServer && _isSnake1) || (!_isServer && !_isSnake1))
                    NetworkManager.NetMan.rpc(GetPath(), nameof(SetAktDirectionCache), false, true, true, Convert.ToInt32(direction.x), Convert.ToInt32(direction.y));
                //}
                //}
            }
        }
    }

    protected virtual void SetAktDirectionCache(int X, int Y)
    {
        _directionCache.x = X;
        _directionCache.y = Y;
    }

    public override void MoveSnake()
    {
        _direction = _directionCache;
        _tween.InterpolateMethod(this, "RPCTween", 0, 1, moveDelay, Tween.TransitionType.Linear, Tween.EaseType.InOut);
        _tween.Start();
        if(_isServer == true)
            _updateTimer.Start();
    }

    public virtual void RPCTween(float argv)
    {
        // Wenn Server mache MoveTween
        // Wenn Client mache ClientMoveTween
        if (_isServer == true)
        {
            // Man hat das Problem das die Positionen der Schlangen bei beiden Spielern auseinander gehen, da argv nicht bei jedem genau uzr gleichen zeit
            // die gleichen Werte haben, daher muss man ingewissen Abstaenden den Cleint wieder mit dem Server synchronisieren!!! => Wie? Kein blassen Schimemr
            MoveTween(argv);
        }
        else
        {
            ClientMoveTween(argv);
        }
    }

    protected override void MoveTween(float argv)
    {
        if (_Merker == false)
        {
            int i = 0;
            foreach (Vector2 pos in _body.Points)
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
                i++;
            }

            if (_eating == true)
            {
                _body.AddPoint(_body.GetPointPosition(_body.Points.Count() - 1));
                _growing = true;
                _points = _body.Points;
                _eating = false;
            }

            _face.Position = _body.Points[0];
            _face.RotationDegrees = -Mathf.Rad2Deg(_direction.AngleTo(Vector2.Right));

            // wenn argv = 1 dann ist eine Schleife durch
            if (argv == 1)
            {
                _Merker = true;
                _points = _body.Points;
                CheckFruitCollision();

                if (_growing == true)
                    _growing = false;

                if (IsGameOver())
                {
                    NetworkManager.NetMan.rpc(_controller.GetPath(), nameof(_controller.OnGameFinished));
                }
                else
                {
                    _direction = _directionCache;
                }

                // Clientstand wieder mit Serverstand synchronisieren, jetzt sollten alle Pos. int sein
                /*
v
                */
                // int Array in Byte Array wandeln, da Json zu langsam wäre
                /*
                byte[] Xbyte = new byte[x.Length * sizeof(int)];
                Buffer.BlockCopy(x, 0, Xbyte, 0, Xbyte.Length);

                byte[] Ybyte = new byte[y.Length * sizeof(int)];
                Buffer.BlockCopy(y, 0, Ybyte, 0, Ybyte.Length);
                */
                // Nun _points aktualisieren!
                int[] x = new int[_points.Length];
                int[] y = new int[_points.Length];
                for (int j = 0; j < _points.Length; j++)
                {
                    x[j] = Convert.ToInt32(_points[j].x); // Hier kein Datenverlust da float hier Ganzzahlen sind
                    y[j] = Convert.ToInt32(_points[j].y);
                }
                NetworkManager.NetMan.rpc(GetPath(), nameof(SynchPointsOnClient), false, false, false, JsonConvert.SerializeObject(x), JsonConvert.SerializeObject(y));
            }
        }

        if (argv != 1)
        {
            _Merker = false;
        }
    }

    private void SynchPointsOnClient(string Xjson, string Yjson)
    {

        int[] x = JsonConvert.DeserializeObject<int[]>(Xjson);
        int[] y = JsonConvert.DeserializeObject<int[]>(Yjson);


        for (int i = 0; i < x.Length; i++)
        {
            _points[i].x = x[i];
            _points[i].y = y[i];
        }
    }

    protected virtual void ClientMoveTween(float argv)
    {
        if (_Merker == false)
        {
            //_Interpolate = false;
            int i = 0;
            foreach (Vector2 pos in _body.Points)
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
                i++;
            }

            if (_eating == true)
            {
                _body.AddPoint(_body.GetPointPosition(_body.Points.Count() - 1));
                _growing = true;
                _points = _body.Points;
                _eating = false;
            }

            _face.Position = _body.Points[0];
            _face.RotationDegrees = -Mathf.Rad2Deg(_direction.AngleTo(Vector2.Right));

            // wenn argv = 1 dann ist eine Schleife durch
            if (argv == 1)
            {
                _Merker = true;
                _points = _body.Points;

                if (_growing == true)
                    _growing = false;

                _direction = _directionCache;
            }
        }

        if (argv != 1)
        {
            _Merker = false;
        }
    }

    private void SynchBodyPointsOnClient(string Xjson, string Yjson)
    {

        float[] x = JsonConvert.DeserializeObject<float[]>(Xjson);
        float[] y = JsonConvert.DeserializeObject<float[]>(Yjson);


        for (int i = 0; i < x.Length; i++)
        {
            _body.SetPointPosition(i, new Vector2(x[i], y[i]));
        }
    }

    protected override void CheckFruitCollision()
    {
        if (_body.Points[0] == _fruit.Position)
        {
            _eating = true;
            _audioPlayer.Play();
            //IncreaseSpeed();
            _controller.UpdateScore();
            GD.Print($"{Name} hat Frucht gefressen!");

            Vector2 newPos = _fruit.RandomizePosition();
            NetworkManager.NetMan.rpc(_fruit.GetPath(), nameof(_fruit.SetNewPosition), false, true, true, newPos.x, newPos.y);
            // Jetzte dem Client sagen das eine Frucht gegeseen wurde!
            NetworkManager.NetMan.rpc(GetPath(), nameof(SetEatingOnOtherPlayer), false, false, true);
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

}
