using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Portal
{
    public List<Vector2Int> positions;
    public byte transitionNodeIndex;
    public Portal sibling;
    public float transitionToSiblingCost;
    public int clearance;
    public Cluster cluster;
    public Dictionary<int,Dictionary<Portal, Path<Vector2Int>>> introTransitions;

    public void CalculateTransitionToSiblingCost(byte[,] costField)
    {
        var pos = sibling.positions[sibling.transitionNodeIndex];
        transitionToSiblingCost = costField[pos.x,pos.y];
    }

    public byte GetPositionTrueClearance(int posInd)
    {
        return (byte)Mathf.Min(PathfindingMap.Instance.ClearanceField[positions[posInd].x,positions[posInd].y],
            PathfindingMap.Instance.ClearanceField[sibling.positions[posInd].x,sibling.positions[posInd].y]);
    }

    public List<Vector2Int> GetPositionsWithClearance(int clearance)
    {
        List<Vector2Int> res = new List<Vector2Int>();
        for (int i = 0; i < positions.Count; i++)
        {
            if (GetPositionTrueClearance(i) >= clearance)
            {
                res.Add(positions[i]);
            }
        }

        return res;
    }

}
