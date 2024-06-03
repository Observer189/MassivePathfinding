using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using DefaultNamespace;
using MoreMountains.Tools;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.Tilemaps;
using Debug = UnityEngine.Debug;

public class PathfindingMap : MMSingleton<PathfindingMap>
{
    [SerializeField]
    private int width;
    [SerializeField]
    private int height;
    [SerializeField]
    private int clusterWidth;
    [SerializeField]
    private int clusterHeight;
    [SerializeField]
    private int maxPortalSize;
    [SerializeField]
    private bool uniformPortalDistribution;
    [SerializeField]
    private int maxAgentSize = 20;
    [SerializeField]
    private bool useClearances;
    [SerializeField]
    private bool useEikonalEquations;
    [SerializeField]
    private TilemapNavigationCost[] tilemapCosts;
    [SerializeField]
    private Tilemap obstacleTilemap;
    [SerializeField]
    private TileBase defaultObstacleTile;
    [SerializeField]
    private byte costFieldDefaultValue = byte.MaxValue;
    [SerializeField]
    private DiagonalPassingType diagonalMovementType;

    private byte[,] costField;
    private byte[,] clearanceField;
    private Cluster[,] clusters;

    private AstarPathfinder astarPathfinder;
    private AbstractPathfinder abstractPathfinder;
    private FIMFLowFieldCalculator flowFieldCalculator;

    public byte[,] CostField => costField;

    public byte[,] ClearanceField => clearanceField;

    public Cluster[,] Clusters => clusters;

    public int Width => width;

    public int Height => height;

    public int ClusterHeight
    {
        get => clusterHeight;
        set => clusterHeight = value;
    }

    public int ClusterWidth
    {
        get => clusterWidth;
        set => clusterWidth = value;
    }

    public bool UseClearances => useClearances;

    public int MaxAgentSize => maxAgentSize;

    public bool UseEikonalEquations => useEikonalEquations;

    public FIMFLowFieldCalculator FlowFieldCalculator => flowFieldCalculator;

    public static float DiagonalTransitionCostModifier = 1.41421356237f;

    public DiagonalPassingType DiagonalMovementType => diagonalMovementType;

    protected override void Awake()
    {
        base.Awake();
        costField = new byte[width, height];
        clearanceField = new byte[width, height];
        clusters = new Cluster[width / clusterWidth, height / clusterHeight];
        astarPathfinder = new AstarPathfinder();
        abstractPathfinder = new AbstractPathfinder();
        flowFieldCalculator = new FIMFLowFieldCalculator();
    }

    private void Start()
    {
        ReadCostFieldFromTilemaps();
        FillClearanceField();
        astarPathfinder.Allocate(new Vector2Int(width, height));
        BuildClusters();
        abstractPathfinder.Allocate(CalculatePortalCount());
        flowFieldCalculator.CostField = costField;
        flowFieldCalculator.ClearanceField = clearanceField;
        CalculateIntegrationFieldForAllPortals();
    }

    public void LoadFromStrings(int width, int height, string[] mapData)
    {
        var dataWidth = width;
        var dataHeight = height;
        this.width = (int)(width + (clusterWidth - width % clusterWidth));
        this.height = (int)(height + (clusterHeight - height % clusterHeight));
        costField = new byte[this.width, this.height];
        clusters = new Cluster[this.width / clusterWidth, this.height / clusterHeight];

        for (int i = 0; i < tilemapCosts.Length; i++)
        {
            tilemapCosts[i].tilemap.ClearAllTiles();
        }

        for (int x = 0; x < this.width; x++)
        {
            for (int y = 0; y < this.height; y++)
            {
                if (x < dataWidth && y < dataHeight)
                {
                    var ch = mapData[x][y];

                    for (int i = 0; i < tilemapCosts.Length; i++)
                    {
                        if (tilemapCosts[i].aliases.Contains(ch))
                        {
                            tilemapCosts[i].tilemap.SetTile(new Vector3Int(x, y), tilemapCosts[i].defaultTile);
                            costField[x, y] = tilemapCosts[i].cost;
                        }
                    }
                }
                else
                {
                    obstacleTilemap.SetTile(new Vector3Int(x, y), defaultObstacleTile);
                    costField[x, y] = byte.MaxValue;
                }
            }
        }

        FillClearanceField();
        astarPathfinder.Allocate(new Vector2Int(this.width, this.height));
        BuildClusters();
        abstractPathfinder.Allocate(CalculatePortalCount());
        flowFieldCalculator.CostField = costField;
        //CalculateIntegrationFieldForAllPortals();

        var units = GameObject.FindGameObjectsWithTag("Unit");
        for (int i = 0; i < units.Length; i++)
        {
            Destroy(units[i]);
        }
    }
    
    
    public async Task<PerformanceTester.LoadTestData> LoadFromStringsTest(int width, int height, string[] mapData)
    {
        PerformanceTester.LoadTestData testData = new PerformanceTester.LoadTestData();

        var dataWidth = width;
        var dataHeight = height;
        this.width = (int)(width + (clusterWidth - width % clusterWidth));
        this.height = (int)(height + (clusterHeight - height % clusterHeight));
        costField = new byte[this.width, this.height];
        clusters = new Cluster[this.width / clusterWidth, this.height / clusterHeight];

        /*for (int i = 0; i < tilemapCosts.Length; i++)
        {
            tilemapCosts[i].tilemap.ClearAllTiles();
        }*/

        for (int x = 0; x < this.width; x++)
        {
            for (int y = 0; y < this.height; y++)
            {
                if (x < dataWidth && y < dataHeight)
                {
                    var ch = mapData[x][y];

                    for (int i = 0; i < tilemapCosts.Length; i++)
                    {
                        if (tilemapCosts[i].aliases.Contains(ch))
                        {
                            //tilemapCosts[i].tilemap.SetTile(new Vector3Int(x, y), tilemapCosts[i].defaultTile);
                            costField[x, y] = tilemapCosts[i].cost;
                        }
                    }
                }
                else
                {
                    //obstacleTilemap.SetTile(new Vector3Int(x, y), defaultObstacleTile);
                    costField[x, y] = byte.MaxValue;
                }
            }
        }
        

        var swClearance = Stopwatch.StartNew();
        await Task.Run(()=>FillClearanceField());
        swClearance.Stop();
        testData.FillClearanceFieldTime = swClearance.Elapsed;
        astarPathfinder.Allocate(new Vector2Int(this.width, this.height));
        Debug.Log("Start build clusters");
        var swClust = Stopwatch.StartNew();
        await Task.Run(()=>BuildClusters());
        swClust.Stop();
        Debug.Log($"End build clusters, time = {swClust.ElapsedMilliseconds}");
        testData.BuildClustersTime = swClust.Elapsed;
        
        abstractPathfinder.Allocate(CalculatePortalCount());
        flowFieldCalculator.CostField = costField;
        Debug.Log("Start load integration");
        var swInt = Stopwatch.StartNew();
        await Task.Run(()=>CalculateIntegrationFieldForAllPortals());
        swInt.Stop();
        Debug.Log($"End load integration, time = {swInt.ElapsedMilliseconds}");
        testData.CalculateIntegrationFieldTime = swInt.Elapsed;

        /*var units = GameObject.FindGameObjectsWithTag("Unit");
        for (int i = 0; i < units.Length; i++)
        {
            Destroy(units[i]);
        }*/

        return testData;
    }

    protected void FillClearanceField()
    {
        clearanceField = new byte[width, height];
        Parallel.For(0, width, (int x) =>
        {
            for (int y = 0; y < height; y++)
            {
                clearanceField[x, y] = CalculateClearanceForCell(x, y);
            }
        });
        /*for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                clearanceField[x, y] = CalculateClearanceForCell(x, y);
            }
        }*/

        FlowFieldCalculator.ClearanceField = clearanceField;
    }

    protected byte CalculateClearanceForCell(int x, int y)
    {
        for (byte curClearance = 0; curClearance <= maxAgentSize; curClearance++)
        {
            for (int i = 0; i < curClearance + 1; i++)
            {
                if (CellIsOutOfMap(x + i, y + curClearance) || costField[x + i, y + curClearance] == byte.MaxValue)
                {
                    return curClearance;
                }
            }

            for (int i = 0; i < curClearance; i++)
            {
                if (CellIsOutOfMap(x + curClearance, y + i) || costField[x + curClearance, y + i] == byte.MaxValue)
                {
                    return curClearance;
                }
            }
        }

        return (byte)maxAgentSize;
    }

    public bool CellIsOutOfMap(int x, int y)
    {
        return x < 0 || y < 0 || x >= width || y >= height;
    }

    protected void BuildClusters()
    {
        for (int i = 0; i < width / clusterWidth; i++)
        {
            for (int j = 0; j < height / clusterHeight; j++)
            {
                clusters[i, j] = new Cluster() { minX = (uint)(i * ClusterWidth), minY = (uint)(j * ClusterHeight) };
            }
        }

        for (int i = 0; i < width / clusterWidth; i++)
        {
            for (int j = 0; j < height / clusterHeight; j++)
            {
                //Debug.Log($"cluster = ({i},{j}), minX = {clusters[i,j].minX}. minY = {clusters[i,j].minY}");
                if(i != width/clusterWidth - 1)
                clusters[i, j].BuildPortalsOnSide(1, costField, clearanceField, clusters, clusterWidth, clusterHeight,
                    maxPortalSize, uniformPortalDistribution);
                if(j != height/clusterHeight - 1)
                clusters[i, j].BuildPortalsOnSide(2, costField, clearanceField, clusters, clusterWidth, clusterHeight,
                    maxPortalSize, uniformPortalDistribution);
            }
        }

        for (int i = 0; i < width / clusterWidth; i++)
        {
            for (int j = 0; j < height / clusterHeight; j++)
            {
                Debug.Log($"Calculate transitions in cluster {i},{j}");
                clusters[i, j].CalculatePortalsIntroTransitions();
            }
        }
    }

    public Path<Vector2Int> PathfindAstar(Vector2Int startPos, Vector2Int targetPos,
        byte agentSize = 1, int minX = 0, int minY = 0, int maxX = -1, int maxY = -1, bool debug = false)
    {
        astarPathfinder.DiagonalPassingType = diagonalMovementType;
        astarPathfinder.SearchClearance = agentSize;
        return astarPathfinder.Pathfind(startPos, targetPos, costField, minX, minY, maxX, maxY, debug);
    }

    public Path<Portal> PathfindPortals(Vector2Int startPos, Vector2Int targetPos, byte agentSize = 1)
    {
        if (useClearances && clearanceField[targetPos.x, targetPos.y] < agentSize)
        {
            return null;
        }

        var startPortal = InsertFictivePortal(startPos,agentSize);
        var targetPortal = InsertFictivePortal(targetPos,agentSize);

        abstractPathfinder.SearchClearance = agentSize;
        var abstractPath = abstractPathfinder.Pathfind(startPortal, targetPortal);
        
        RemoveFictivePortal(startPortal);
        RemoveFictivePortal(targetPortal);

        return abstractPath;
    }

    public Path<Vector2Int> PathfindHPA(Vector2Int startPos, Vector2Int targetPos, byte agentSize = 1)
    {
        if (useClearances && clearanceField[targetPos.x, targetPos.y] < agentSize)
        {
            return null;
        }

        var startPortal = InsertFictivePortal(startPos,agentSize);
        var targetPortal = InsertFictivePortal(targetPos,agentSize);

        abstractPathfinder.SearchClearance = agentSize;
        var abstractPath = abstractPathfinder.Pathfind(startPortal, targetPortal);
        if (abstractPath != null)
        {
            var realPath = new List<Vector2Int>();
            var cost = 0f;
            var prevPortal = abstractPath.route[^1];
            for (int i = abstractPath.route.Count - 2; i >= 0; i--)
            {
                var curPortal = abstractPath.route[i];
                if (curPortal.cluster == prevPortal.cluster)
                {
                    Path<Vector2Int> path = null;
                    //Debug.Log($" prev = {prevPortal.positions[prevPortal.transitionNodeIndex]}, cur = {curPortal.positions[curPortal.transitionNodeIndex]}");
                    try
                    {
                        path = curPortal.introTransitions[agentSize][prevPortal];

                    }
                    catch (Exception e)
                    {
                        Debug.Log($" prev = {prevPortal.positions[prevPortal.transitionNodeIndex]}, cur = {curPortal.positions[curPortal.transitionNodeIndex]}");
                        foreach (var kv in curPortal.introTransitions[1])
                        {
                            Debug.Log(kv.Key.positions[kv.Key.transitionNodeIndex]);
                        }
                        throw;
                    }
                    realPath.AddRange(path.route);
                    cost += path.totalCost;
                }
                else
                {
                    realPath.Add(curPortal.positions[curPortal.transitionNodeIndex]);
                    cost += prevPortal.transitionToSiblingCost;
                }

                prevPortal = curPortal;
            }

            RemoveFictivePortal(startPortal);
            RemoveFictivePortal(targetPortal);

            return new Path<Vector2Int>(realPath, cost);
        }

        RemoveFictivePortal(startPortal);
        RemoveFictivePortal(targetPortal);

        return null;
    }

    public void FlowFieldPathRequest(List<Unit> units, Vector2Int targetPos)
    {
        Dictionary<Cluster, Dictionary<byte, List<Unit>>> partition = new Dictionary<Cluster, Dictionary<byte,List<Unit>>>();

        for (int i = 0; i < units.Count; i++)
        {
            var unitPos = units[i].GridPosition;
            var cluster = clusters[unitPos.x / clusterWidth,unitPos.y/ clusterHeight];

            Dictionary<byte, List<Unit>> sizeDict = null;
            if(partition.TryGetValue(cluster, out var dict))
            {
                sizeDict = dict;
            }
            else
            {
                sizeDict = new Dictionary<byte, List<Unit>>();
                partition[cluster] = sizeDict;
            }

            List<Unit> unitList = null;
            if (sizeDict.TryGetValue(units[i].UnitSize, out var list))
            {
                unitList = list;
            }
            else
            {
                unitList = new List<Unit>();
                sizeDict[units[i].UnitSize] = unitList;
            }
            
            unitList.Add(units[i]);
        }

        foreach (var unitsInCluster in partition)
        {
            foreach (var oneSizedUnits in unitsInCluster.Value)
            {
                List<Unit> linkedParts = new List<Unit>();
                var curUnit = oneSizedUnits.Value[0];

                var abstractPath = PathfindPortals(curUnit.GridPosition, targetPos, curUnit.UnitSize);
                curUnit.AbstractPath = abstractPath;
                linkedParts.Add(curUnit);

                for (int i = 1; i < oneSizedUnits.Value.Count; i++)
                {
                    curUnit = oneSizedUnits.Value[i];
                    for (int j = 0; j < linkedParts.Count; j++)
                    {
                        var realPath = PathfindAstar(curUnit.GridPosition, linkedParts[j].GridPosition,
                            oneSizedUnits.Key,(int)unitsInCluster.Key.minX,(int)unitsInCluster.Key.minY,
                            (int)unitsInCluster.Key.minX + clusterWidth, (int)unitsInCluster.Key.minY + clusterHeight);

                        if (realPath != null)
                        {
                            curUnit.AbstractPath = linkedParts[j].AbstractPath;
                        }
                        else
                        {
                            curUnit.AbstractPath = PathfindPortals(curUnit.GridPosition, targetPos, curUnit.UnitSize);
                            linkedParts.Add(curUnit);
                        }
                    }
                }
                
            }
        }
    }

    private Portal InsertFictivePortal(Vector2Int position, byte agentSize = 1)
    {
        Vector2Int sectorPos = new Vector2Int() { x = (position.x / clusterHeight), y = (position.y / clusterWidth) };
        Cluster cluster = null;
        try
        {
            cluster = clusters[sectorPos.x, sectorPos.y];
        }
        catch (Exception e)
        {
            Debug.Log($"sector pos = {sectorPos}, sectors width = {clusters.GetLength(0)} sectros height = {clusters.GetLength(1)}");
            throw;
        }

        var portal = new Portal();
        portal.positions = new List<Vector2Int>();
        portal.positions.Add(position);
        portal.transitionNodeIndex = 0;
        portal.cluster = cluster;
        portal.introTransitions = new Dictionary<int, Dictionary<Portal, Path<Vector2Int>>>();
        portal.clearance = maxAgentSize;

        portal.introTransitions[agentSize] = new Dictionary<Portal, Path<Vector2Int>>();
        foreach (var otherPortal in cluster.portals)
        {
            var path = PathfindAstar(portal.positions[portal.transitionNodeIndex],
                otherPortal.positions[otherPortal.transitionNodeIndex], agentSize,
                (int)cluster.minX, (int)cluster.minY, (int)cluster.minX + ClusterWidth,
                (int)cluster.minY + ClusterHeight);
            if (path != null)
            {
                var otherRoute = new List<Vector2Int>(path.route);
                otherRoute.Reverse();
                var otherPath = new Path<Vector2Int>(otherRoute, path.totalCost);

                portal.introTransitions[agentSize][otherPortal] = path;
                otherPortal.introTransitions[agentSize][portal] = otherPath;
            }
        }


        cluster.portals.Add(portal);

        return portal;
    }

    private void RemoveFictivePortal(Portal portal)
    {
        var position = portal.positions[portal.transitionNodeIndex];
        Vector2Int sectorPos = new Vector2Int() { x = (position.x / clusterHeight), y = (position.y / clusterWidth) };
        var cluster = clusters[sectorPos.x, sectorPos.y];

        cluster.portals.Remove(portal);
        foreach (var otherPortal in cluster.portals)
        {
            foreach (var transitions in otherPortal.introTransitions)
            {
                transitions.Value.Remove(portal);
            }
            //otherPortal.introTransitions.Remove(portal);
        }
    }

    public int CalculatePortalCount()
    {
        var sum = 0;
        foreach (var cluster in clusters)
        {
            sum += cluster.portals.Count;
        }

        return sum;
    }

    private void CalculateIntegrationFieldForAllPortals()
    {
        /*Parallel.For(0, clusters.GetLength(0), (int i) =>
        {
            for (int j = 0; j < clusters.GetLength(1); j++)
            {
                clusters[i,j].CalculateIntegrationFieldForAllPortals();
            }
        });*/

        for (int i = 0; i < clusters.GetLength(0); i++)
        {
            for (int j = 0; j < clusters.GetLength(1); j++)
            {
                Debug.Log($"Cluster = {i+1}/{clusters.GetLength(0)},{j+1}/{clusters.GetLength(1)}");
                clusters[i,j].CalculateIntegrationFieldForAllPortals();
            }
        }
    }

    protected void ReadCostFieldFromTilemaps()
    {
        for (int i = 0; i < tilemapCosts.Length; i++)
        {
            var tilemap = tilemapCosts[i];
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (tilemap.tilemap.HasTile(new Vector3Int(x, y)))
                    {
                        costField[x, y] = tilemap.cost;
                    }
                }
            }
        }

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (costField[x, y] == 0)
                {
                    costField[x, y] = costFieldDefaultValue;
                }
            }
        }
    }
}

[Serializable]
public class TilemapNavigationCost
{
    public Tilemap tilemap;
    public byte cost;
    public char[] aliases;
    public TileBase defaultTile;
}

public enum PathfindingMethod
{
    Astar,
    HPA,
    FlowField
}