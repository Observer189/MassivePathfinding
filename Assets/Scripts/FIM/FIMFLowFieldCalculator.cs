using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FIMFLowFieldCalculator
{
    private const float ObstacleCost = 1000;
    private const float Epsilon = 0.0001f;
    private const float Infinity = 99999999999999999f;

    private LinkedList<Vector2Int> queue;
    
    private Vector2Int[] directions = new[]
        { new Vector2Int(0, 1), new Vector2Int(0, -1), new Vector2Int(1, 0), new Vector2Int(-1, 0) };

    private uint minX;
    private uint minY;
    private int width;
    private int height;

    private byte[,] costField;
    private byte[,] clearanceField;

    public byte[,] CostField
    {
        get => costField;
        set => costField = value;
    }

    public byte[,] ClearanceField
    {
        get => clearanceField;
        set => clearanceField = value;
    }

    public FIMFLowFieldCalculator()
    {
        queue = new LinkedList<Vector2Int>();
    }

    public void CalculateIntegrationField(float[,] integrationField, List<Vector2Int> destinations,
        uint minX, uint minY, int agentSize = 1)
    {
        queue.Clear();
        this.minX = minX;
        this.minY = minY;
        width = integrationField.GetLength(0);
        height = integrationField.GetLength(1);
        
        
        for (int i = 0; i < integrationField.GetLength(0); i++)
        {
            for (int j = 0; j < integrationField.GetLength(1); j++)
            {
                integrationField[i, j] = Infinity;
            }
        }

        foreach (var cur in destinations)
        {
            integrationField[cur.x - minX, cur.y - minY] = 0;
        }
        
        List<Vector2Int> neighbors = new List<Vector2Int>();

        foreach (var current in destinations)
        {
            foreach (var dir in directions)
            {
                var pos = current + dir;
                if (InBound(pos))
                {

                    /*if (PathfindingMap.Instance.CostField[pos.x, pos.y] != 255 &&
                        PathfindingMap.Instance.ClearanceField[pos.x, pos.y] >= agentSize)*/
                    {
                        neighbors.Add(pos);
                    }
                }
            }
        }

        foreach (var cur in neighbors)
        {
            if (!destinations.Contains(cur))
            {
                queue.AddLast(cur);
            }
        }

        int it = 0;
        
        while (queue.Count > 0)
        {
            it++;
            var currentNode = queue.First;
            while (currentNode != null)
            {
                //Debug.Log($"x = {currentNode.Value.x +1} y = {currentNode.Value.y + 1}, minX = {minX}, minY = {minY}, " +
                 //         $"width = {integrationField.GetLength(0)}, height = {integrationField.GetLength(1)}");
                var p = integrationField[currentNode.Value.x - minX,currentNode.Value.y - minY];
                var q = UpdatePoint(currentNode.Value, integrationField, agentSize);
                integrationField[currentNode.Value.x - this.minX, currentNode.Value.y - this.minY] = q;
                 
                //Debug.Log("current = "+currentNode.Value + "p-q = " + (p-q) + "q = " + q);
                if (Mathf.Abs(p-q) < Epsilon)
                {
                    neighbors.Clear();
                    foreach (var dir in directions)
                    {
                        var pos = currentNode.Value + dir;
                        if (InBound(pos))
                        {

                            /*if (PathfindingMap.Instance.CostField[pos.x, pos.y] != 255 &&
                                PathfindingMap.Instance.ClearanceField[pos.x, pos.y] >= agentSize)*/
                            {
                                neighbors.Add(pos);
                            }
                        }
                    }

                    foreach (var neighbor in neighbors)
                    {
                        if (!queue.Contains(neighbor))
                        {
                            p = integrationField[neighbor.x - this.minX, neighbor.y - this.minY];
                            q = UpdatePoint(neighbor, integrationField, agentSize);
                            if (p > q)
                            {
                                integrationField[neighbor.x - this.minX, neighbor.y - this.minY] = q;
                                queue.AddBefore(currentNode, neighbor);
                            }
                        }
                    }

                    var t = currentNode;
                    currentNode = currentNode.Next;
                    queue.Remove(t);
                    continue;
                }
                currentNode = currentNode.Next;
            }
        }
    }

    private float UpdatePoint(Vector2Int point, float[,] integrationField, int agentSize)
    {
        float leftA = ObstacleCost;
        float rightA = ObstacleCost;
        if (InBound(new Vector2Int(point.x - 1, point.y)) 
            && PathfindingMap.Instance.ClearanceField[point.x-1,point.y] >= agentSize)
        {
            leftA = integrationField[point.x - 1 - minX, point.y - minY];
        }
        
        if (InBound(new Vector2Int(point.x + 1, point.y)) 
            && PathfindingMap.Instance.ClearanceField[point.x+1,point.y] >= agentSize)
        {
            rightA = integrationField[point.x + 1 - minX, point.y - minY];
        }

        float a = Mathf.Max(0, Mathf.Min(leftA, rightA));
        
        float leftB = ObstacleCost;
        float rightB = ObstacleCost;
        if (InBound(new Vector2Int(point.x, point.y - 1)) 
            && PathfindingMap.Instance.ClearanceField[point.x,point.y - 1] >= agentSize)
        {
            leftB = integrationField[point.x - minX, point.y - 1 - minY];
        }
        
        if (InBound(new Vector2Int(point.x, point.y + 1))
            && PathfindingMap.Instance.ClearanceField[point.x,point.y+1] >= agentSize)
        {
            rightB = integrationField[point.x - minX, point.y + 1 - minY];
        }

        float b = Mathf.Max(0, Mathf.Min(leftB, rightB));

        var t = costField[point.x, point.y];
        if (clearanceField[point.x, point.y] < agentSize)
        {
            t = byte.MaxValue;
        }
        //Debug.Log($"{point}, leftA = {leftA}, rightA = {rightA}, a = {a}, leftB = {leftB}, rightB = {rightB}, b = {b}");
        
        return CalculateEikonalEquation(a,b,t);
    }

    private bool InBound(Vector2Int pos)
    {
        return pos.x >= minX && pos.x < minX + width
                             && pos.y >= minY && pos.y < minY + height;
    }

    private float CalculateEikonalEquation(float a, float b, byte t)
    {
        var min = Mathf.Min(a, b);
        var max = a + b - min;
        max = Mathf.Min(max,min + 1);

        var u = max + t;
        //Debug.Log($"min = {min}, max = {max}, t = {t}, sqrt = {-min * min - max * max + 2 * min * max + 2 * t * t}");
        if (u < min)
        {
            return u;
        }
        else
        {
            var discr = -min * min - max * max + 2 * min * max + 2 * t * t;
            if (discr < 0)
            {
                Debug.Log($"a = {a}, b = {b}, t = {t}, max = {max}, min = {min}");

            }

            return (min + max + Mathf.Sqrt(discr)) / 2;
            //return (min + max) + Mathf.Sqrt((min + max) * (min + max) - 2 * (min * min + max * max - t * t)) + ((t==byte.MaxValue)?ObstacleCost:0);
        }
    }
}
