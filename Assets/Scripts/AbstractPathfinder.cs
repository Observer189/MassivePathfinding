using System.Collections;
using System.Collections.Generic;
using Priority_Queue;
using UnityEngine;

public class AbstractPathfinder
{
    public byte SearchClearance { get; set; } = 1;
    
    private FastPriorityQueue<PortalNode> openSet;
    private Dictionary<Portal, PortalNode> visited;
    
    private List<PortalNeighborInfo> neighbors = new List<PortalNeighborInfo>();

    private delegate float HeuristicFunction(Portal startPortal, Portal targetPortal);

    private HeuristicFunction heuristic;

    public void Allocate(int graphSize)
    {
        openSet = new FastPriorityQueue<PortalNode>(graphSize/2+10);
        visited = new Dictionary<Portal, PortalNode>(graphSize/2+10);
    }
    
    public Path<Portal> Pathfind(Portal startPortal, Portal targetPortal)
        {
            openSet.Clear();
            visited.Clear();

            heuristic = EuclideanDistance;

            var startNode = new PortalNode {portal = startPortal, cost = 0, prev = null};
            openSet.Enqueue(startNode,0);
            visited[startPortal] = startNode;
           
            while (openSet.Count > 0)
            {
                var cur = openSet.Dequeue();
                //Debug.Log(cur.position);
                if (cur.portal == targetPortal)
                {
                    var resList = new List<Portal>();
                    float cost = cur.cost;
                    while (true)
                    {
                        resList.Add(cur.portal);
                        if (cur.prev == null)
                        {
                            return new Path<Portal>(resList,cost);
                        }
                        else
                        {
                            cur = cur.prev;
                        }
                    }
                }
                var neighbors = GetNeighbors(cur.portal);
                foreach (var neighbor in neighbors)
                {
                    if (visited.TryGetValue(neighbor.neighbor,out PortalNode n))
                    {
                        if (n.cost > cur.cost + neighbor.cost)
                        {
                            n.cost = cur.cost + neighbor.cost;
                            n.prev = cur;
                            var f = n.cost + heuristic(n.portal,targetPortal);
                            openSet.UpdatePriority(n,f);
                        }
                    }
                    else
                    {
                        var node = new PortalNode() {  portal = neighbor.neighbor, prev = cur,cost = (cur.cost+ neighbor.cost)};
                        visited[neighbor.neighbor] = node;
                        openSet.Enqueue(node, cur.cost + neighbor.cost + heuristic(neighbor.neighbor,targetPortal));
                    }
                }
            }
            return null;
        }
    
    List<PortalNeighborInfo> GetNeighbors(Portal portal)
       {
           neighbors.Clear();
           foreach (var transition in portal.introTransitions[SearchClearance])
           {
               if (PathfindingMap.Instance.UseClearances && transition.Key.clearance < SearchClearance)
               {
                   continue;
               }

               PortalNeighborInfo neighbor = new PortalNeighborInfo()
               {
                   neighbor = transition.Key,
                   cost = transition.Value.totalCost
               };
               neighbors.Add(neighbor);
           }

           if (portal.sibling != null)
           {
               PortalNeighborInfo sibling = new PortalNeighborInfo()
               {
                   neighbor = portal.sibling,
                   cost = portal.transitionToSiblingCost
               };
               neighbors.Add(sibling);
           }

           return neighbors;
       }
    
    private float EuclideanDistance(Portal startPortal, Portal targetPortal)
    {
        var startPos = startPortal.positions[startPortal.transitionNodeIndex];
        var targetPos = targetPortal.positions[targetPortal.transitionNodeIndex];
        var diff = (targetPos - startPos).Absolute();
        var squareEdge = Mathf.Min(diff.x, diff.y);

        return squareEdge * PathfindingMap.DiagonalTransitionCostModifier + (Mathf.Max(diff.x, diff.y) - squareEdge);
    }
    
    
}
public class PortalNode:FastPriorityQueueNode
{
    public Portal portal;
    public float cost;
    public PortalNode prev;
}

public struct PortalNeighborInfo
{
    public Portal neighbor;
    public float cost;
}

