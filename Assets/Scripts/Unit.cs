using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Unit : MonoBehaviour
{
   public float destinationDistance = 0.1f;
   public byte UnitSize = 1;
   public SpriteRenderer selection;
   public LineRenderer lineRenderer;
   public float separatingRadius;

   public float pathfindingWeight;
   public float separatingWeight;

   public Vector2Int GridPosition
   {
      get
      {
         var pos = (Vector2)transform.position - Vector2.one * (0.5f * (UnitSize - 1));
         return pos.Floor();
      }
   }

   public Path<Vector2Int> RealPath
   {
      get => realPath;
      set
      {
         if(value != null)
         currentPathPoint = 1;
         abstractPath = null;
         realPath = value;
      }
   }

   public Path<Portal> AbstractPath
   {
      get => abstractPath;
      set
      {
         if(value != null)
         currentPathPoint = value.route.Count - 2;
         realPath = null;
         abstractPath = value;
      }
   }

   public bool ShowPath
   {
      get => showPath;
      set => showPath = value;
   }

   private bool showPath;

   private Path<Vector2Int> realPath;

   private Path<Portal> abstractPath;

   private Locomotion locomotion;

   private int currentPathPoint = 0;

   private Vector2 pathfindingTargetVelocity;

   private void Awake()
   {
      selection.enabled = false;
      locomotion = GetComponent<Locomotion>();
   }

   private void Update()
   {
      if (showPath)
      {
         if (realPath != null)
         {
            List<Vector3> positions = new List<Vector3>();
            positions.Add(transform.position);
            for (int i = currentPathPoint; i < realPath.route.Count; i++)
            {
               positions.Add(realPath.route[i].ToVector2() + Vector2.one * 0.5f);
               /*if(i>0)
               pathLength += PathfindingMap.Instance.CostField[currentPath[i].x, currentPath[i].y];*/
            }
            lineRenderer.transform.position = Vector2.zero;
            lineRenderer.transform.rotation = Quaternion.identity;
            lineRenderer.positionCount = positions.Count;
            lineRenderer.SetPositions(positions.ToArray());
            lineRenderer.enabled = true;
         }
         else if(abstractPath!=null)
         {
            List<Vector3> positions = new List<Vector3>();
            positions.Add(transform.position);
            for (int i = currentPathPoint; i >= 0; i--)
            {
               positions.Add(abstractPath.route[i].positions[abstractPath.route[i].transitionNodeIndex].ToVector2() + Vector2.one * 0.5f);
               /*if(i>0)
               pathLength += PathfindingMap.Instance.CostField[currentPath[i].x, currentPath[i].y];*/
            }
            lineRenderer.transform.position = Vector2.zero;
            lineRenderer.transform.rotation = Quaternion.identity;
            lineRenderer.positionCount = positions.Count;
            lineRenderer.SetPositions(positions.ToArray());
            lineRenderer.enabled = true;
         }
      }
      else
      {
         lineRenderer.enabled = false;
      }

      if (realPath != null && realPath.route.Count > 1)
      {
         var pos = realPath.route[currentPathPoint] + Vector2.one * 0.5f* UnitSize;

         pathfindingTargetVelocity = (pos - (Vector2)transform.position).normalized * locomotion.maxSpeed;
         //locomotion.SetTargetSpeed((pos - (Vector2)transform.position).normalized * locomotion.maxSpeed);

         if (Vector2.Distance(transform.position, pos) < destinationDistance)
         {
            currentPathPoint++;
            if (currentPathPoint == realPath.route.Count)
            {
               //locomotion.Stop();
               pathfindingTargetVelocity = Vector2.zero;
               realPath = null;
            }
         }
      }
      else if (abstractPath != null && abstractPath.route.Count > 1)
      {
         HandleAbstractPathfind();
      }
      else
      {
         pathfindingTargetVelocity = Vector2.zero;
      }

      Vector2 accumulatedDesireVelocity = Vector2.zero;

      accumulatedDesireVelocity = pathfindingTargetVelocity * pathfindingWeight + Separating() * separatingWeight;

      accumulatedDesireVelocity = accumulatedDesireVelocity.normalized *
                                     Mathf.Min(accumulatedDesireVelocity.magnitude,
                                        locomotion.maxSpeed);
      locomotion.SetTargetSpeed(accumulatedDesireVelocity);
   }

   protected void HandleAbstractPathfind()
   {
      var cluster = PathfindingMap.Instance.Clusters
            [GridPosition.x/PathfindingMap.Instance.ClusterWidth,GridPosition.y/PathfindingMap.Instance.ClusterHeight];
         var portal = abstractPath.route[currentPathPoint];
         //Debug.Log(portal.positions[portal.transitionNodeIndex]);
      
         if (cluster != portal.cluster)
         {
            if(currentPathPoint != 0 && cluster == portal.sibling.cluster)
            {
               currentPathPoint-=2;
            }
            else
            {
               AbstractPath = PathfindingMap.Instance.PathfindPortals(GridPosition,
                  abstractPath.route[0].positions[0],UnitSize);
               return;
            }
         }
         
         portal = abstractPath.route[currentPathPoint];
         Vector2? dir = null;

         if (currentPathPoint == 0)
         {
            if (GridPosition == portal.positions[0])
            {
               dir = Vector2.zero;
               abstractPath = null;
            }
            else
            {
               dir = cluster.GetFlowDir(GridPosition, portal.positions[0],UnitSize);  
            }
         }
         else if (portal.GetPositionsWithClearance(UnitSize).Contains(GridPosition))
         {
            dir = portal.sibling.positions[portal.sibling.transitionNodeIndex] -
                  portal.positions[portal.transitionNodeIndex];
         }
         else
         {
            dir = cluster.GetFlowDir(GridPosition, portal,UnitSize);
         }

         if (dir.HasValue)
         {
            //Debug.DrawRay(transform.position,dir.Value*5,Color.red);
            pathfindingTargetVelocity = dir.Value.normalized * locomotion.maxSpeed;
         }
         else
         {
            pathfindingTargetVelocity = Vector2.zero;
            Debug.LogWarning($"Unit {gameObject.name} can't find path to portal " +
                             $"({abstractPath.route[currentPathPoint].positions[abstractPath.route[currentPathPoint].transitionNodeIndex]})" +
                             $"in cluster ({cluster.minX},{cluster.minY})!");
         }
   }

   protected Vector2 Separating()
   {
      Vector2 desireVelocity = Vector2.zero;
      List<Unit> neighbors = new List<Unit>();

      var hits = Physics2D.OverlapCircleAll(transform.position, separatingRadius);
      foreach (var hit in hits)
      {
         var unit = hit.GetComponent<Unit>();
         if (unit != null && unit.UnitSize >= UnitSize)
         {
            neighbors.Add(unit);
         }
      }

      foreach (var neighbor in neighbors)
      {
         if (neighbor != this)
         {
            var toAgent = (Vector2)transform.position - (Vector2)neighbor.transform.position;
            if(toAgent != Vector2.zero)
               desireVelocity += toAgent.normalized / toAgent.magnitude;
         }
      }

      return desireVelocity;
   }

   public void Select()
   {
      selection.enabled = true;
   }

   public void Deselect()
   {
      selection.enabled = false;
   }
}
