using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Represents a single continuous stroke of ink left by the player.
/// Stores 2D positions in world space and the run index when it was drawn.
/// </summary>
[Serializable]
public class InkStroke
{
    public List<Vector2> points = new List<Vector2>();
    public int runIndex = 1;

    public InkStroke()
    {
        points = new List<Vector2>();
        runIndex = 1;
    }

    public InkStroke(int runIndex)
    {
        points = new List<Vector2>();
        this.runIndex = runIndex;
    }

    public void AddPoint(Vector2 point)
    {
        points.Add(point);
    }
}
