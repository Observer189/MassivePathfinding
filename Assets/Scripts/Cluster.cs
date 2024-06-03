using System.Collections;
using System.Collections.Generic;
using DefaultNamespace;
using UnityEngine;

public class Cluster
{
    public uint minX;
    public uint minY;
    public List<Portal> portals;

    public Dictionary<int,Dictionary<Portal, ushort[,]>> PortalIntegrationFields => portalIntegrationFields;
    public Dictionary<int,Dictionary<Vector2Int, ushort[,]>> TargetIntegrationFields => targetIntegrationFields;

    public Dictionary<int, Dictionary<Portal, float[,]>> RealPortalIntegrationFields => realPortalIntegrationFields;
    public Dictionary<int, Dictionary<Vector2Int, float[,]>> RealTargetIntegrationFields => realTargetIntegrationFields;

    private Dictionary<int,Dictionary<Portal, ushort[,]>> portalIntegrationFields;
    private Dictionary<int,Dictionary<Vector2Int, ushort[,]>> targetIntegrationFields;

    private Dictionary<int, Dictionary<Portal, float[,]>> realPortalIntegrationFields;
    private Dictionary<int,Dictionary<Vector2Int, float[,]>> realTargetIntegrationFields;

    private Vector2Int[] directions = new[]
        { new Vector2Int(0, 1), new Vector2Int(0, -1), new Vector2Int(1, 0), new Vector2Int(-1, 0) };
    private Vector2Int[] diagonals = new[]
        { new Vector2Int(1, 1), new Vector2Int(1, -1), new Vector2Int(-1, 1), new Vector2Int(-1, -1) };

    private const float BorderRepulsionRate = 0.1f;

    public Cluster()
    {
        portals = new List<Portal>();
        portalIntegrationFields = new Dictionary<int,Dictionary<Portal, ushort[,]>>();
        targetIntegrationFields = new Dictionary<int,Dictionary<Vector2Int, ushort[,]>>();
        realTargetIntegrationFields = new Dictionary<int, Dictionary<Vector2Int, float[,]>>();
        realPortalIntegrationFields = new Dictionary<int, Dictionary<Portal, float[,]>>();
    }

    public void BuildPortalsOnSide(byte side, byte[,] costField,byte[,] clearanceField, Cluster[,] clusters, 
        int clusterWidth, int clusterHeight, int maxPortalSize, bool uniformPortalDistribution)
    {
        Vector2Int curPos = default;
        Vector2Int dir = default;
        Vector2Int dirToNeighbor = default;
        
        switch (side)
        {
            case 0: curPos.x = (int)minX;
                curPos.y = (int)minY;
                dir.x = 1;
                dir.y = 0;
                dirToNeighbor.x = 0;
                dirToNeighbor.y = -1;
                break;
            case 1: curPos.x = (int)(minX+clusterWidth-1);
                curPos.y = (int)minY;
                dir.x = 0;
                dir.y = 1;
                dirToNeighbor.x = 1;
                dirToNeighbor.y = 0;
                break;
            case 2: curPos.x = (int)minX;
                curPos.y = (int)(minY+clusterHeight-1);
                dir.x = 1;
                dir.y = 0;
                dirToNeighbor.x = 0;
                dirToNeighbor.y = 1;
                break;
            case 3: curPos.x = (int)minX;
                curPos.y = (int)minY;
                dir.x = 0;
                dir.y = 1;
                dirToNeighbor.x = -1;
                dirToNeighbor.y = 0;
                break;
        }
        
        Vector2Int sectorPos = new Vector2Int() { x = (int)(minX / clusterWidth), y = (int)(minY / clusterHeight) };
        Vector2Int neighborSectorPos = sectorPos + dirToNeighbor;
        //Log.Debug(curPos);
        List<Vector2Int> curPortalPositions = default;
        List<Vector2Int> curNeighborPortalPositions = default;
        bool prevIsPassable = false;

        for (; curPos.x < minX + clusterHeight && curPos.y < minY + clusterHeight; curPos += dir)
        {
            if (costField[curPos.x, curPos.y] != byte.MaxValue &&
                costField[curPos.x + dirToNeighbor.x, curPos.y + dirToNeighbor.y] != byte.MaxValue)
            {
                if (prevIsPassable)
                {
                    curPortalPositions.Add(curPos);
                    curNeighborPortalPositions.Add(curPos + dirToNeighbor);
                }
                else
                {
                    curPortalPositions = new List<Vector2Int>();
                    curNeighborPortalPositions = new List<Vector2Int>();
                    curPortalPositions.Add(curPos);
                    curNeighborPortalPositions.Add(curPos + dirToNeighbor);
                }

                prevIsPassable = true;
            }
            else
            {
                if (prevIsPassable)
                {
                    BuildPortalsOnGap(curPortalPositions,curNeighborPortalPositions,
                        neighborSectorPos,costField,clearanceField,clusters,maxPortalSize,uniformPortalDistribution);
                }

                prevIsPassable = false;
            }
        }

        if (prevIsPassable)
        {
            BuildPortalsOnGap(curPortalPositions,curNeighborPortalPositions,
                   neighborSectorPos,costField,clearanceField,clusters,maxPortalSize,uniformPortalDistribution);
        }
        
        
        
    }

    public void CalculatePortalsIntroTransitions()
    {
        foreach (var portal in portals)
        {
            portal.introTransitions = new Dictionary<int, Dictionary<Portal, Path<Vector2Int>>>();
            for (int i = 1; i <= PathfindingMap.Instance.MaxAgentSize; i++)
            {
                portal.introTransitions[i] = new Dictionary<Portal, Path<Vector2Int>>();
                foreach (var otherPortal in portals)
                {
                    /*if (portal.positions[portal.transitionNodeIndex] == new Vector2Int(63, 127) && i==1)
                    {
                        Debug.Log($"other portal = {otherPortal.positions[otherPortal.transitionNodeIndex]}");
                    }*/

                    if (otherPortal == portal)
                    {
                        continue;
                    }

                    var path = PathfindingMap.Instance.PathfindAstar(portal.positions[portal.transitionNodeIndex],
                        otherPortal.positions[otherPortal.transitionNodeIndex], (byte)i,
                        (int)minX, (int)minY, (int)minX + PathfindingMap.Instance.ClusterWidth,
                        (int)minY + PathfindingMap.Instance.ClusterHeight);
                    if (path != null)
                    {
                        portal.introTransitions[i][otherPortal] = path;
                    }
                    /*else
                    {
                        if (portal.positions[portal.transitionNodeIndex] == new Vector2Int(63, 127) && i==1)
                        {
                            Debug.Log($"path not found = {otherPortal.positions[otherPortal.transitionNodeIndex]}");
                        }
                    }*/
                }

                if (!PathfindingMap.Instance.UseClearances)
                {
                    break;
                }
            }

            /*if (portal.positions[portal.transitionNodeIndex] == new Vector2Int(63, 127))
            {
                foreach (var kv in portal.introTransitions[1])
                {
                    Debug.Log(kv.Key.positions[kv.Key.transitionNodeIndex]);
                }
            }*/
        }
    }

    public Vector2? GetFlowDir(Vector2Int position, Portal portal, int unitSize)
    {
        if (!PathfindingMap.Instance.UseEikonalEquations)
        {
            Dictionary<Portal, ushort[,]> portalDict = null;
            if (portalIntegrationFields.TryGetValue(unitSize, out var dict))
            {
                portalDict = dict;
            }
            else
            {
                portalDict = new Dictionary<Portal, ushort[,]>();
                portalIntegrationFields[unitSize] = portalDict;
            }

            if (portalDict.TryGetValue(portal, out var field))
            {
                return GetFlowDirInternal(position, field);
            }
            else
            {
                var f = CalculateIntegrationField(portal.GetPositionsWithClearance(unitSize), unitSize);
                portalDict[portal] = f;
                return GetFlowDirInternal(position, f);
            }
        }
        else
        {
            Dictionary<Portal, float[,]> portalDict = null;
            if (realPortalIntegrationFields.TryGetValue(unitSize, out var dict))
            {
                portalDict = dict;
            }
            else
            {
                portalDict = new Dictionary<Portal, float[,]>();
                realPortalIntegrationFields[unitSize] = portalDict;
            }

            if (portalDict.TryGetValue(portal, out var field))
            {
                return GetFlowDirEikonal(position, field, unitSize);
            }
            else
            {
                var f = new float[PathfindingMap.Instance.ClusterWidth,PathfindingMap.Instance.ClusterHeight];
                PathfindingMap.Instance.FlowFieldCalculator.CalculateIntegrationField(f,
                    portal.GetPositionsWithClearance(unitSize), minX,minY, unitSize);
                portalDict[portal] = f;
                return GetFlowDirEikonal(position, f, unitSize);
            }
        }
    }

    public Vector2? GetFlowDir(Vector2Int position, Vector2Int target, int unitSize)
    {
        if (!PathfindingMap.Instance.UseEikonalEquations)
        {
            Dictionary<Vector2Int, ushort[,]> targetDict = null;
            if (targetIntegrationFields.TryGetValue(unitSize, out var dict))
            {
                targetDict = dict;
            }
            else
            {
                targetDict = new Dictionary<Vector2Int, ushort[,]>();
                targetIntegrationFields[unitSize] = targetDict;
            }

            if (targetDict.TryGetValue(target, out var f))
            {
                return GetFlowDirInternal(position, f);
            }
            else
            {
                var field = CalculateIntegrationField(new List<Vector2Int>() { target }, unitSize);
                targetDict[target] = field;
                return GetFlowDirInternal(position, field);
            }
        }
        else
        {
            Dictionary<Vector2Int, float[,]> targetDict = null;
            if (realTargetIntegrationFields.TryGetValue(unitSize, out var dict))
            {
                targetDict = dict;
            }
            else
            {
                targetDict = new Dictionary<Vector2Int, float[,]>();
                realTargetIntegrationFields[unitSize] = targetDict;
            }

            if (targetDict.TryGetValue(target, out var f))
            {
                return GetFlowDirEikonal(position, f,unitSize);
            }
            else
            {
                var field = new float[PathfindingMap.Instance.ClusterWidth,PathfindingMap.Instance.ClusterHeight];
                PathfindingMap.Instance.FlowFieldCalculator.CalculateIntegrationField(field,
                    new List<Vector2Int>() { target },minX,minY, unitSize);
                targetDict[target] = field;
                return GetFlowDirEikonal(position, field,unitSize);
            }
        }
    }

    private Vector2? GetFlowDirInternal(Vector2Int position, ushort[,] field)
    {
        var pos = position - new Vector2Int((int)minX,(int)minY);
        if (field[pos.x, pos.y] == 0)
        {
            return Vector2.zero;
        }

            Vector2Int minNeighbor = Vector2Int.zero;
            ushort minIntegration = ushort.MaxValue;
            foreach (var dir in directions)
            {
                var neighborPos = pos + dir;
                if (neighborPos.x < 0 || neighborPos.y < 0 ||
                    neighborPos.x >= field.GetLength(0) || neighborPos.y >= field.GetLength(1))
                {
                    continue;
                }

                var integration = field[neighborPos.x, neighborPos.y];
                if (integration < minIntegration)
                {
                    minIntegration = integration;
                    minNeighbor = pos + dir;
                }
            }
            
            if (PathfindingMap.Instance.DiagonalMovementType != DiagonalPassingType.NoPassing)
            {
                foreach (var dir in diagonals)
                {
                    var neighborPos = pos + dir;
                  
                    if (neighborPos.x > 0 && neighborPos.y > 0 &&
                        neighborPos.x < field.GetLength(0) && neighborPos.y < field.GetLength(1) &&
                        field[neighborPos.x, neighborPos.y] != ushort.MaxValue)
                    {
                        if (PathfindingMap.Instance.DiagonalMovementType == DiagonalPassingType.PassWhenNoObstacles)
                        {
                            if (field[pos.x, pos.y + dir.y] == ushort.MaxValue || field[pos.x + dir.x, pos.y] == ushort.MaxValue)
                            {
                                continue;
                            }
                        }
                        else if(PathfindingMap.Instance.DiagonalMovementType == DiagonalPassingType.PassWhenLessThanTwoObstacles)
                        {
                            if (field[pos.x, pos.y + dir.y] == ushort.MaxValue && field[pos.x + dir.x, pos.y] == ushort.MaxValue)
                            {
                                continue;
                            }
                        }

                        var integration = field[neighborPos.x, neighborPos.y];
                        if (integration < minIntegration)
                        {
                            minIntegration = integration;
                            minNeighbor = pos + dir;
                        }
                    }
                }
            }

           

            return minNeighbor - pos;
    }

    private Vector2? GetFlowDirEikonal(Vector2Int position, float[,] field, int agentSize)
    {
        var pos = position - new Vector2Int((int)minX,(int)minY);
        if (field[pos.x, pos.y] == 0)
        {
            return Vector2.zero;
        }

        var leftPos = pos + new Vector2Int(-1, 0);
        var left = field[pos.x, pos.y] + BorderRepulsionRate;
        //var left = 0f;
        if (leftPos.x >= 0 && leftPos.x < field.GetLength(0) 
                           && leftPos.y >= 0 && leftPos.y < field.GetLength(1) 
                           && PathfindingMap.Instance.CostField[leftPos.x+minX,leftPos.y+minY] != byte.MaxValue
                           && PathfindingMap.Instance.ClearanceField[leftPos.x + minX,leftPos.y+minY] >= agentSize)
        {
            left = field[leftPos.x, leftPos.y];
        }
        
        var rightPos = pos + new Vector2Int(1, 0);
        var right = field[pos.x, pos.y] + BorderRepulsionRate;
        //var right = 0f;
        if (rightPos.x >= 0 && rightPos.x < field.GetLength(0) &&
            rightPos.y >= 0 && rightPos.y < field.GetLength(1)
            && PathfindingMap.Instance.CostField[rightPos.x+minX,rightPos.y+minY] != byte.MaxValue
            && PathfindingMap.Instance.ClearanceField[rightPos.x+minX,rightPos.y+minY] >= agentSize)
        {
            right = field[rightPos.x, rightPos.y];
        }
        
        var hor = 0f;
        if (left == 0f && right == 0f)
        {
            hor = 0f;
        }
        else if (left == 0f)
        {
            hor = right - field[pos.x,pos.y];
        }
        else if(right == 0f)
        {
            hor = field[pos.x,pos.y] - left;
        }
        else
        {
            hor = ((field[pos.x,pos.y] - left) + (right - field[pos.x,pos.y])) / 2;
        }
        
        
        var topPos = pos + new Vector2Int(0, 1);
        
        var top = field[pos.x, pos.y] + BorderRepulsionRate;
        //var top = 0f;
        if (topPos.x >= 0 && topPos.x < field.GetLength(0) && topPos.y >= 0 
            && topPos.y < field.GetLength(1)
            && PathfindingMap.Instance.CostField[topPos.x + minX,topPos.y + minY] != byte.MaxValue
            && PathfindingMap.Instance.ClearanceField[topPos.x + minX,topPos.y + minY] >= agentSize)
        {
            top = field[topPos.x, topPos.y];
        }
        
        var downPos = pos + new Vector2Int(0, -1);
        var down = field[pos.x, pos.y] + BorderRepulsionRate;
        //var down = 0f;
        if (downPos.x >= 0 && downPos.x < field.GetLength(0) && downPos.y >= 0
            && downPos.y < field.GetLength(1)
            && PathfindingMap.Instance.CostField[downPos.x + minX,downPos.y + minY] != byte.MaxValue
            && PathfindingMap.Instance.ClearanceField[downPos.x + minX,downPos.y + minY] >= agentSize)
        {
            down = field[downPos.x, downPos.y];
        }
        
        var ver = 0f;
        if (top == 0f && down == 0f)
        {
            ver = 0f;
        }
        else if (top == 0f)
        {
            ver = field[pos.x,pos.y] - down;
        }
        else if(down == 0f)
        {
            ver = top - field[pos.x,pos.y];
        }
        else
        {
            ver = ((field[pos.x,pos.y] - down) + (top - field[pos.x,pos.y])) / 2;
        }
        /*Debug.Log($"pos = {pos}, leftPos = {leftPos}, rightPos = {rightPos}, topPos = {topPos}, downPos = {downPos}");
        Debug.Log($"top = {top}, down = {down}, ver = {ver}, right = {right}, left = {left}, hor = {hor}" +
                  $"pos = {position}, cost = {field[pos.x,pos.y]}");*/
        return (new Vector2(-hor,-ver)).normalized;
    }

    public void CalculateIntegrationFieldForAllPortals()
    {
        if (!PathfindingMap.Instance.UseEikonalEquations)
        {
            for (int i = 1; i < PathfindingMap.Instance.MaxAgentSize; i++)
            {
                var dict = new Dictionary<Portal, ushort[,]>();
                portalIntegrationFields[i] = dict;
                foreach (var portal in portals)
                {
                    dict[portal] = CalculateIntegrationField(portal.GetPositionsWithClearance(i), i);
                }
            }
        }
        else
        {
            for (int i = 1; i < PathfindingMap.Instance.MaxAgentSize; i++)
            {
                var dict = new Dictionary<Portal, float[,]>();
                realPortalIntegrationFields[i] = dict;
                foreach (var portal in portals)
                {
                    //Debug.Log($"Portal = {portal.positions[portal.transitionNodeIndex]}");
                    var f = new float[PathfindingMap.Instance.ClusterWidth,PathfindingMap.Instance.ClusterHeight];
                    PathfindingMap.Instance.FlowFieldCalculator.CalculateIntegrationField(f,
                        portal.GetPositionsWithClearance(i), minX,minY, i);
                    dict[portal] = f;
                }
            }
        }
    }


    private void BuildPortalsOnGap(List<Vector2Int> curPortalPositions, 
        List<Vector2Int> curNeighborPortalPositions,Vector2Int neighborSectorPos,byte[,] costField,byte[,] clearanceField, 
        Cluster[,] clusters, int maxPortalSize,bool uniformPortalDistribution)
    {
        int portalCount = curPortalPositions.Count / maxPortalSize;
        if (curPortalPositions.Count % maxPortalSize != 0) portalCount++;
        int it = 0;
        for (int i = 0; i < portalCount; i++)
        {
            List<Vector2Int> portalPositions = new List<Vector2Int>();
            List<Vector2Int> neighborPortalPositions = new List<Vector2Int>();
            if (uniformPortalDistribution)
            {
                int basePortalSize = curPortalPositions.Count / portalCount;
                int curPortalSize = (curPortalPositions.Count % portalCount > i) ? basePortalSize + 1 : basePortalSize;

                for (int j = 0; j < curPortalSize; j++)
                {
                    portalPositions.Add(curPortalPositions[it]);
                    neighborPortalPositions.Add(curNeighborPortalPositions[it]);
                    ++it;
                }
            }
            else
            {
                int curPortalSize = Mathf.Min(maxPortalSize, curPortalPositions.Count - maxPortalSize * i);

                for (int j = 0; j < curPortalSize; j++)
                {
                    portalPositions.Add(curPortalPositions[i*maxPortalSize + j]);
                    neighborPortalPositions.Add(curNeighborPortalPositions[i*maxPortalSize + j]);
                }
            }


            var transitionIndex = (byte)(portalPositions.Count / 2);
            int maxClearance = 0;
            if (PathfindingMap.Instance.UseClearances)
            {
                List<byte> maxPositions = new List<byte>();
                for (byte j = 0; j < portalPositions.Count; j++)
                {
                    //Debug.Log($"j = {j}, portal positions = {portalPositions.Count}");
                    var pos = portalPositions[j];
                    var neighborPos = neighborPortalPositions[j];
                    var clearance = Mathf.Min(clearanceField[pos.x, pos.y],
                        clearanceField[neighborPos.x, neighborPos.y]);
                    if (clearance >= maxClearance)
                    {
                        if (clearance != maxClearance)
                        {
                            maxPositions.Clear();
                        }

                        maxPositions.Add(j);
                        maxClearance = clearance;
                    }
                }

                byte bestInd = 0;
                float minDist = float.MaxValue;
                for (int j = 0; j < maxPositions.Count; j++)
                {
                    var dist = Mathf.Abs(portalPositions.Count/2.0f-maxPositions[j]);
                    if (dist < minDist)
                    {
                        bestInd = maxPositions[j];
                        minDist = dist;
                    }
                }

                transitionIndex = bestInd;
            }

            var portal = new Portal
            {
                positions = portalPositions,
                transitionNodeIndex = transitionIndex,
                cluster = this,
                clearance = maxClearance
            };
                    
            var neighborPortal = new Portal
            {
                positions = neighborPortalPositions,
                transitionNodeIndex = transitionIndex,
                cluster = clusters[neighborSectorPos.x, neighborSectorPos.y],
                clearance = maxClearance
            };

            portal.sibling = neighborPortal;
            neighborPortal.sibling = portal;
                        
            portal.CalculateTransitionToSiblingCost(costField);
            neighborPortal.CalculateTransitionToSiblingCost(costField);
                    
            portals.Add(portal);
            clusters[neighborSectorPos.x, neighborSectorPos.y].portals.Add(neighborPortal);
        }
    }

    private ushort[,] CalculateIntegrationField(List<Vector2Int> startingPoints, int agentSize = 1)
    {
        int maxY = (int)(minY+PathfindingMap.Instance.ClusterHeight-1); 
        int maxX = (int)(minX + PathfindingMap.Instance.ClusterWidth-1);
        
        var integrationField = new ushort[PathfindingMap.Instance.ClusterWidth,PathfindingMap.Instance.ClusterHeight];

        for (int i = 0; i < integrationField.GetLength(0); i++)
        {
            for (int j = 0; j < integrationField.GetLength(1); j++)
            {
                integrationField[i, j] = ushort.MaxValue;
            }
        }

        Queue<Vector2Int> openSet = new Queue<Vector2Int>();
        for (int i = 0; i < startingPoints.Count; i++)
        {
            //Debug.Log(startingPoints[i]);
            if (PathfindingMap.Instance.ClearanceField[startingPoints[i].x, startingPoints[i].y] >= agentSize)
            {
                openSet.Enqueue(startingPoints[i]);
                integrationField[startingPoints[i].x - minX, startingPoints[i].y - minY] = 0;
            }
            else
            {
                integrationField[startingPoints[i].x - minX, startingPoints[i].y - minY] = ushort.MaxValue;
            }
        }
        
        while (openSet.Count>0)
        {
            var current = openSet.Dequeue();
            
            List<Vector2Int> neighbors = new List<Vector2Int>();
            foreach (var dir in directions)
            {
                var pos = current + dir;
                if (pos.x >= minX && pos.x <= maxX && pos.y >= minY && pos.y <= maxY)
                {
                    
                    if (PathfindingMap.Instance.CostField[pos.x,pos.y] != 255 && 
                        PathfindingMap.Instance.ClearanceField[pos.x,pos.y] >= agentSize)
                    {
                        neighbors.Add(pos);
                    }
                }
            }
            
            foreach (var neighbor in neighbors)
            {
                /*Debug.Log($"current = {current}, min = ({minX},{minY}), " +
                          $"fieldSize = ({integrationField.GetLength(0)},{integrationField.GetLength(1)})" +
                          $"neighbor = {neighbor}");
                Debug.Log(integrationField[current.x-minX,current.y-minY]);
                Debug.Log(PathfindingMap.Instance.CostField[neighbor.x,neighbor.y]);*/
                ushort cost = (ushort)(integrationField[current.x-minX,current.y-minY] +
                                       PathfindingMap.Instance.CostField[neighbor.x,neighbor.y]);
                if (cost < integrationField[neighbor.x-minX,neighbor.y-minY])
                {
                    if (!openSet.Contains(neighbor))
                    {
                        openSet.Enqueue(neighbor);
                    }
                    integrationField[neighbor.x-minX,neighbor.y-minY] = cost;
                }
            }
        }

        return integrationField;

    }
}
