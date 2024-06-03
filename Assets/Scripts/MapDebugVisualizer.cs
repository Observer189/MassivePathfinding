using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using DefaultNamespace;
using MoreMountains.Tools;
using SimpleFileBrowser;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;

public class MapDebugVisualizer : MonoBehaviour
{ 
    [SerializeField]
    private bool ShowCostFieldValue;
    [SerializeField]
    private bool ShowPortals;
    [SerializeField]
    private bool ShowIntroTransitions;
    [SerializeField]
    private bool ShowClearanceField;
    [SerializeField]
    private bool ShowPaths;
    [SerializeField]
    private bool ShowIntegrationFields;
    [SerializeField]
    private int DebugRadius;
    [SerializeField]
    private TextMeshProUGUI xCoord;
    [SerializeField]
    private TextMeshProUGUI yCoord;
    [SerializeField]
    private TextMeshProUGUI calcTimeText;
    [SerializeField]
    private TextMeshProUGUI pathLengthText;
    [SerializeField]
    private Transform startPosIcon;
    [SerializeField]
    private Transform goalPosIcon;
    [SerializeField]
    private LineRenderer lineRenderer;
    [SerializeField]
    private TMP_Dropdown pathfindMethodDropdown;
    [SerializeField]
    private Transform selectionBox;
    [SerializeField]
    private GameObject[] unitsToSpawn;
    [SerializeField]
    private Color portalDebugColor;
    [SerializeField]
    private Color portalTransitionDebugColor;
    [SerializeField]
    private Color introTransitionsDebugColor;
    [SerializeField]
    private float debugLineThickness;
    [SerializeField]
    private float introTransitionsLineThickness;

    private Camera camera;

    private Vector2Int? startPos;
    
    private Vector2Int? goalPos;

    private Path<Vector2Int> currentPath;

    private Vector2Int cursorPos;

    private PathfindingMethod pathfindingMethod;

    private float calculationTime;

    private float pathLength;

    private byte agentSize = 1;

    private VisualizerMode mode = VisualizerMode.Pathfind;

    private Vector2? startSelectionPos;
    private Vector2 selectionPos;

    private List<Unit> selectedUnits;

    private void Awake()
    {
        camera = Camera.main;
        selectedUnits = new List<Unit>();
    }

    private void Start()
    {
        startPosIcon.gameObject.SetActive(false);
        goalPosIcon.gameObject.SetActive(false);
        lineRenderer.enabled = false;
        selectionBox.gameObject.SetActive(false);
        pathfindingMethod = GetPathfindingMethod(pathfindMethodDropdown.captionText.text);
    }

    // Update is called once per frame
    void Update()
    {
        WorldTextManager.ReturnAll();

        if (Input.GetKeyDown(KeyCode.Tab))
        {
            SwitchMode();
        }

        if (selectedUnits != null)
        {
            foreach (var unit in selectedUnits)
            {
                unit.ShowPath = ShowPaths;
            }
        }

        cursorPos = (Vector2Int)camera.ScreenToWorldPoint(Input.mousePosition).Floor();
        if (mode == VisualizerMode.Pathfind)
        {

            xCoord.text = cursorPos.x.ToString();
            yCoord.text = cursorPos.y.ToString();

            if (Input.GetKeyDown(KeyCode.Mouse0) && cursorPos.x >= 0 && cursorPos.x < PathfindingMap.Instance.Width
                && cursorPos.y >= 0 && cursorPos.y < PathfindingMap.Instance.Height
                && PathfindingMap.Instance.CostField[cursorPos.x, cursorPos.y] != 255)
            {
                startPos = cursorPos;
                startPosIcon.transform.localScale = Vector3.one * agentSize;
                startPosIcon.transform.position = startPos.Value.ToVector2() + Vector2.one * (agentSize / 2.0f);
                startPosIcon.gameObject.SetActive(true);
            }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                startPos = null;
                startPosIcon.gameObject.SetActive(false);
                goalPos = null;
                goalPosIcon.gameObject.SetActive(false);
                lineRenderer.enabled = false;
            }

            if (Input.GetKeyDown(KeyCode.KeypadPlus))
            {
                agentSize += 1;
                agentSize = (byte)Mathf.Clamp(agentSize, 1, PathfindingMap.Instance.MaxAgentSize);
                startPosIcon.transform.position = startPos.Value.ToVector2() + Vector2.one * (agentSize / 2.0f);
                startPosIcon.transform.localScale = Vector3.one * agentSize;
            }

            if (Input.GetKeyDown(KeyCode.KeypadMinus))
            {
                agentSize -= 1;
                agentSize = (byte)Mathf.Clamp(agentSize, 1, PathfindingMap.Instance.MaxAgentSize);
                startPosIcon.transform.position = startPos.Value.ToVector2() + Vector2.one * (agentSize / 2.0f);
                startPosIcon.transform.localScale = Vector3.one * agentSize;
            }

            if (startPos.HasValue && Input.GetKeyDown(KeyCode.Mouse1) &&
                PathfindingMap.Instance.CostField[cursorPos.x, cursorPos.y] != 255)
            {
                goalPos = cursorPos;
                goalPosIcon.transform.position = goalPos.Value.ToVector2() + Vector2.one * 0.5f;
                goalPosIcon.gameObject.SetActive(true);

                if (pathfindingMethod == PathfindingMethod.Astar)
                {
                    var sw = Stopwatch.StartNew();
                    currentPath =
                        PathfindingMap.Instance.PathfindAstar(startPos.Value, goalPos.Value, agentSize);
                    calculationTime = sw.ElapsedMilliseconds;
                    calcTimeText.text = $"{calculationTime} ms";
                }
                else if (pathfindingMethod == PathfindingMethod.HPA)
                {
                    var sw = Stopwatch.StartNew();
                    currentPath =
                        PathfindingMap.Instance.PathfindHPA(startPos.Value, goalPos.Value, agentSize);
                    calculationTime = sw.ElapsedMilliseconds;
                    calcTimeText.text = $"{calculationTime} ms";
                }

                if (currentPath != null)
                {
                    Vector3[] positions = new Vector3[currentPath.route.Count];
                    pathLength = currentPath.totalCost.RoundDown(1);
                    for (int i = 0; i < currentPath.route.Count; i++)
                    {
                        positions[i] = currentPath.route[i].ToVector2() + Vector2.one * 0.5f;
                        /*if(i>0)
                        pathLength += PathfindingMap.Instance.CostField[currentPath[i].x, currentPath[i].y];*/
                    }

                    pathLengthText.text = pathLength.ToString();

                    lineRenderer.positionCount = currentPath.route.Count;
                    lineRenderer.SetPositions(positions);
                    lineRenderer.enabled = true;
                }
            }
        }
        else
        {
            selectionPos = camera.ScreenToWorldPoint(Input.mousePosition);

            if (Input.GetKeyDown(KeyCode.Mouse0))
            {
                startSelectionPos = selectionPos;
                
                selectionBox.gameObject.SetActive(true);
            }
            else if(Input.GetKeyUp(KeyCode.Mouse0))
            {
                selectionBox.position = (Vector2)(startSelectionPos + selectionPos) / 2;
                selectionBox.localScale = ((Vector2)(selectionPos - startSelectionPos)).Absolute();
                selectionBox.gameObject.SetActive(false);
                var hits = Physics2D.OverlapBoxAll(selectionBox.position,selectionBox.localScale,0);

                foreach (var unit in selectedUnits)
                {
                    unit.Deselect();
                }
                selectedUnits.Clear();

                foreach (var col in hits)
                {
                    var unit = col.GetComponent<Unit>();
                    if (unit != null)
                    {
                        unit.Select();
                        selectedUnits.Add(unit);
                    }
                }
            }
            else if(Input.GetKey(KeyCode.Mouse0))
            {
                selectionBox.position = (Vector2)(startSelectionPos + selectionPos) / 2;
                selectionBox.localScale = (Vector2)(selectionPos - startSelectionPos);
            }

            if (selectedUnits.Count > 0 && Input.GetKeyDown(KeyCode.Mouse1) &&
                PathfindingMap.Instance.CostField[cursorPos.x, cursorPos.y] != 255)
            {
                goalPos = cursorPos;
                goalPosIcon.transform.position = goalPos.Value.ToVector2() + Vector2.one * 0.5f;
                goalPosIcon.gameObject.SetActive(true);
                 
                var sw = Stopwatch.StartNew();
                if (pathfindingMethod == PathfindingMethod.Astar)
                {
                    for (int i = 0; i < selectedUnits.Count; i++)
                    {
                        var path = PathfindingMap.Instance.PathfindAstar(selectedUnits[i].GridPosition,
                            goalPos.Value,selectedUnits[i].UnitSize);
                        if (path != null)
                        {
                            path.route.Reverse();
                        }

                        selectedUnits[i].RealPath = path;
                    }
                }
                else if(pathfindingMethod == PathfindingMethod.HPA)
                {
                    for (int i = 0; i < selectedUnits.Count; i++)
                    {
                        var path = PathfindingMap.Instance.PathfindHPA(selectedUnits[i].GridPosition,
                            goalPos.Value,selectedUnits[i].UnitSize);
                        selectedUnits[i].RealPath = path;
                    }
                }
                else if(pathfindingMethod == PathfindingMethod.FlowField)
                {
                    PathfindingMap.Instance.FlowFieldPathRequest(selectedUnits,goalPos.Value);
                }
                calculationTime = sw.ElapsedMilliseconds;
                calcTimeText.text = $"{calculationTime} ms";
            }
            
            HandleUnitSpawn();
        }

        if(ShowCostFieldValue)
            DebugCostFieldValue();
        if(ShowPortals)
            DebugPortals();
        if(ShowIntroTransitions)
            DebugIntroTransitions();
        if(ShowClearanceField && ! ShowCostFieldValue)
            DebugClearanceField();
        if(ShowIntegrationFields && !ShowClearanceField && !ShowCostFieldValue)
            DebugIntegrationFields();
    }

    public void OnChangePathfindingMethod()
    {
        pathfindingMethod = GetPathfindingMethod(pathfindMethodDropdown.captionText.text);
    }

    public void LoadMapClick()
    {
        FileBrowser.SetFilters(false , new FileBrowser.Filter( "Maps", ".map" ));
        FileBrowser.SetDefaultFilter(".map");
        FileBrowser.ShowLoadDialog(OnSelectLoadMap, OnCancelLoadMap, FileBrowser.PickMode.Files,false,
            Application.streamingAssetsPath);
    }

    private void OnSelectLoadMap(string[] filepath)
    {
        Debug.Log(filepath[0]);
        var data = File.ReadAllLines(filepath[0]);
        var width = int.Parse(data[1].Split(' ', StringSplitOptions.RemoveEmptyEntries)[1]);
        var height = int.Parse(data[2].Split(' ', StringSplitOptions.RemoveEmptyEntries)[1]);

        data = data.Skip(4).ToArray();
        
        PathfindingMap.Instance.LoadFromStrings(width,height,data);
        Debug.Log(width+ " " + height);
    }

    private void OnCancelLoadMap()
    {
        
    }

    public void Test()
    {
        var perfTester = PathfindingMap.Instance.GetComponent<PerformanceTester>();
        StartCoroutine(perfTester.TestAstar2());
    }

    private void SwitchMode()
    {
        if (mode == VisualizerMode.Pathfind)
        {
            startPos = null;
            startPosIcon.gameObject.SetActive(false);
            goalPos = null;
            goalPosIcon.gameObject.SetActive(false);
            lineRenderer.enabled = false;

            mode = VisualizerMode.RTS;
        }
        else
        {
            startPosIcon.gameObject.SetActive(true);
            goalPosIcon.gameObject.SetActive(true);
            lineRenderer.enabled = true;

            mode = VisualizerMode.Pathfind;
        }
    }

    private PathfindingMethod GetPathfindingMethod(string methodName)
    {
        switch (methodName)
        {
            case "A*": return PathfindingMethod.Astar;
            case "HPA*": return PathfindingMethod.HPA;
            case "FlowField": return PathfindingMethod.FlowField;
        }

        return PathfindingMethod.HPA;
    }

    private string GetPathfindingMethodName(PathfindingMethod method)
    {
        switch (method)
        {
            case PathfindingMethod.Astar:
                return "A*";
                break;
            case PathfindingMethod.HPA:
                return "HPA*";
                break;
            case PathfindingMethod.FlowField:
                return "FlowField";
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(method), method, null);
        }
    }

    private void DebugCostFieldValue()
    {
        var costField = PathfindingMap.Instance.CostField;
        for (int x = cursorPos.x - DebugRadius; x <= cursorPos.x + DebugRadius; x++)
        {
            for (int y = cursorPos.y - DebugRadius; y <= cursorPos.y + DebugRadius; y++)
            {
                if (x >= 0 && x < costField.GetLength(0) && y >= 0 && y < costField.GetLength(1))
                {
                    WorldTextManager.DisplayText(costField[x,y].ToString(),
                        new Vector2(x,y)+new Vector2(0.5f,0.5f));

                }
            }
        }
    }

    private void DebugPortals()
    {
        for (int i = 0; i < PathfindingMap.Instance.Width / PathfindingMap.Instance.ClusterWidth; i++)
        {
            for (int j = 0; j < PathfindingMap.Instance.Height / PathfindingMap.Instance.ClusterHeight; j++)
            {
                var cluster = PathfindingMap.Instance.Clusters[i, j];
                foreach (var portal in cluster.portals)
                {
                    var neighbor = portal.sibling;
                    
                    DebugDraw.DrawLine(portal.positions[0].ToVector2()+new Vector2(0.5f,0.5f), 
                        portal.positions[^1].ToVector2() + new Vector2(0.5f,0.5f),debugLineThickness,portalDebugColor);
                    
                    DebugDraw.DrawLine(portal.positions[portal.transitionNodeIndex].ToVector2()+new Vector2(0.5f,0.5f),
                        neighbor.positions[neighbor.transitionNodeIndex].ToVector2()+new Vector2(0.5f,0.5f),debugLineThickness, portalTransitionDebugColor);
                    
                    /*Debug.DrawLine(portal.positions[portal.transitionNodeIndex].ToVector2()+new Vector2(0.5f,0.5f),
                        neighbor.positions[neighbor.transitionNodeIndex].ToVector2()+new Vector2(0.5f,0.5f), portalTransitionDebugColor);
                    
                    Debug.DrawLine(portal.positions[0].ToVector2()+new Vector2(0.5f,0.5f), 
                        portal.positions[^1].ToVector2() + new Vector2(0.5f,0.5f),portalDebugColor);*/
                }
            }
        }
    }

    private void DebugIntroTransitions()
    {
        foreach (var cluster in PathfindingMap.Instance.Clusters)
        {
            foreach (var portal in cluster.portals)
            {
                if (portal.positions[portal.transitionNodeIndex] == new Vector2Int(63, 127))
                {
                    foreach (var transition in portal.introTransitions[agentSize])
                    {
                        for (int i = 1; i < transition.Value.route.Count; i++)
                        {
                            DebugDraw.DrawLine(transition.Value.route[i - 1] + new Vector2(0.5f, 0.5f),
                                transition.Value.route[i] + new Vector2(0.5f, 0.5f), introTransitionsLineThickness,
                                introTransitionsDebugColor);

                        }
                    }
                }
            }
        }
    }

    private void DebugClearanceField()
    {
        var costField = PathfindingMap.Instance.ClearanceField;
        for (int x = cursorPos.x - DebugRadius; x <= cursorPos.x + DebugRadius; x++)
        {
            for (int y = cursorPos.y - DebugRadius; y <= cursorPos.y + DebugRadius; y++)
            {
                if (x >= 0 && x < costField.GetLength(0) && y >= 0 && y < costField.GetLength(1))
                {
                    WorldTextManager.DisplayText(costField[x,y].ToString(),
                        new Vector2(x,y)+new Vector2(0.5f,0.5f));

                }
            }
        }
    }

    private void DebugIntegrationFields()
    {
        if (cursorPos.x < 0 || cursorPos.y < 0 || cursorPos.x >= PathfindingMap.Instance.Width
            || cursorPos.y >= PathfindingMap.Instance.Height)
        {
            return;
        }

        var sectorPos = new Vector2Int(cursorPos.x / PathfindingMap.Instance.ClusterWidth,
            cursorPos.y / PathfindingMap.Instance.ClusterHeight);
        var cluster = PathfindingMap.Instance.Clusters[sectorPos.x,sectorPos.y];
        foreach (var portal in cluster.portals)
        {
            if (!PathfindingMap.Instance.UseEikonalEquations)
            {
                if (cluster.PortalIntegrationFields.TryGetValue((int)agentSize, out var dict))
                {
                    if (dict.ContainsKey(portal))
                    {
                        if (portal.positions.Contains(cursorPos))
                        {
                            var intField = dict[portal];
                            for (int i = 0; i < intField.GetLength(0); i++)
                            {
                                for (int j = 0; j < intField.GetLength(1); j++)
                                {
                                    var gridPos = new Vector2Int((int)(cluster.minX + i), (int)(cluster.minY + j));
                                    if (intField[i, j] != ushort.MaxValue)
                                        WorldTextManager.DisplayText(intField[i, j].ToString(),
                                            new Vector2(gridPos.x, gridPos.y) + new Vector2(0.5f, 0.5f));
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                if (cluster.RealPortalIntegrationFields.TryGetValue((int)agentSize, out var dict))
                {
                    if (dict.ContainsKey(portal))
                    {
                        if (portal.positions.Contains(cursorPos))
                        {
                            var intField = dict[portal];
                            for (int i = 0; i < intField.GetLength(0); i++)
                            {
                                for (int j = 0; j < intField.GetLength(1); j++)
                                {
                                    var gridPos = new Vector2Int((int)(cluster.minX + i), (int)(cluster.minY + j));
                                    if (intField[i, j] != ushort.MaxValue)
                                        WorldTextManager.DisplayText(intField[i, j].RoundDown(1).ToString(),
                                            new Vector2(gridPos.x, gridPos.y) + new Vector2(0.5f, 0.5f));
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    private void HandleUnitSpawn()
    {
        var pos = (Vector2)camera.ScreenToWorldPoint(Input.mousePosition);
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            Instantiate(unitsToSpawn[0],pos,Quaternion.identity);
        }
        else if(Input.GetKeyDown(KeyCode.Alpha2))
        {
            if(unitsToSpawn.Length > 1)
            Instantiate(unitsToSpawn[1],pos,Quaternion.identity);
        }
        else if(Input.GetKeyDown(KeyCode.Alpha3))
        {
            if(unitsToSpawn.Length > 2)
                Instantiate(unitsToSpawn[2],pos,Quaternion.identity);
        }
        else if(Input.GetKeyDown(KeyCode.Alpha4))
        {
            if(unitsToSpawn.Length > 3)
                Instantiate(unitsToSpawn[3],pos,Quaternion.identity);
        }
        else if(Input.GetKeyDown(KeyCode.Alpha5))
        {
            if(unitsToSpawn.Length > 4)
                Instantiate(unitsToSpawn[4],pos,Quaternion.identity);
        }
    }

    public enum VisualizerMode
    {
        Pathfind, RTS
    }

}
