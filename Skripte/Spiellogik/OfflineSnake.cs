using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public class OfflineSnake : BaseSnake
{
    public override void _Ready()
    {
        base._Ready();
    }

    public void SetOfflinePlayerSettings(bool isPlayerOne, Snake otherSnake = null)
    {
        _isPlayerOne = isPlayerOne;
        Snake s = null;
        if (!isPlayerOne)
        {
            if (GetParent() != null)
                s = GetParent().GetNodeOrNull<Snake>("Snake1");
            if (s != null)
                _otherSnake = s;
            else
                _otherSnake = otherSnake;

            _body.DefaultColor = new Color(255, 255, 0, 1);
            for (int i = 0; i < _points.Length; i++)
            {
                _points[i] += new Vector2(0, 2 * _gridSize);
                _body.SetPointPosition(i, _points[i]);
            }
        }
        else
        {
            _otherSnake = GetParent().GetNodeOrNull<Snake>("Snake2");
        }
    }
}