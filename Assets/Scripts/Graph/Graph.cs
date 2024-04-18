using UnityEngine;
using System.Collections.Generic;
using System.Threading;
using System;

public class GraphNode
{
    // TODO : use an int id for nodes comparison
    public Vector3 Position = Vector3.zero;
    public List<GraphNode> Neighbours;
}

public class Connection
{
    public int Cost;
    public GraphNode FromGraphNode;
    public GraphNode ToGraphNode;
}

public class Graph : MonoBehaviour
{
    [SerializeField]
    protected int GridSizeH = 100;
    [SerializeField]
    protected int GridSizeV = 100;
    [SerializeField]
    protected int SquareSize = 1;
    [SerializeField]
    protected float MaxHeight = 10f;

    // enable / disable debug Gizmos
    [SerializeField]
    protected bool DrawGrid = false;
    [SerializeField]
    protected bool DrawNodes = false;
    [SerializeField]
    protected bool DrawConnections = false;

    // threading
    protected Thread GraphThread = null;

    // Grid parameters
    protected Vector3 GridStartPos = Vector3.zero;
    protected int NbTilesH = 0;
    protected int NbTilesV = 0;

    // Nodes
    public List<GraphNode> GraphNodeList { get; protected set; } = new List<GraphNode>();
    protected Dictionary<GraphNode, List<Connection>> ConnectionGraph = new Dictionary<GraphNode, List<Connection>>();

    public Action OnGraphCreated;
    
    public Dictionary<GraphNode, List<Connection>> GetConnectionGraph()
    {
        return ConnectionGraph;
    }

    private void Awake()
    {
        CreateTiledGrid();
    }

    private void Start()
    {
        // Generate graph in a new thread
        ThreadStart threadStart = new ThreadStart(CreateGraph);
        GraphThread = new Thread(threadStart);
        GraphThread.Start();
    }

    #region graph construction

    // GraphNode factory for class specific nodes
    protected virtual GraphNode CreateGraphNode()
    {
        return new GraphNode();
    }

    // Create all nodes for the tiled grid
    protected void CreateTiledGrid()
    {
        GraphNodeList.Clear();

        GridStartPos = transform.position + new Vector3(-GridSizeH / 2f, 0f, -GridSizeV / 2f);

        NbTilesH = GridSizeH / SquareSize;
        NbTilesV = GridSizeV / SquareSize;

        for (int i = 0; i < NbTilesV; i++)
        {
            for (int j = 0; j < NbTilesH; j++)
            {
                Vector3 nodePos = GridStartPos + new Vector3((j + 0.5f) * SquareSize, 0f, (i + 0.5f) * SquareSize);

                GraphNodeList.Add(CreateAndSetupGraphNode(nodePos));
            }
        }
    }

    virtual protected GraphNode CreateAndSetupGraphNode(Vector3 pos)
    {
        RaycastHit hitInfo = new RaycastHit();

        // Always compute node Y pos from floor collision
        if (Physics.Raycast(pos + Vector3.up * MaxHeight, Vector3.down, out hitInfo, MaxHeight + 1, 1 << LayerMask.NameToLayer("Floor")))
        {
            pos.y = hitInfo.point.y;
        }

        GraphNode node = CreateGraphNode();
        node.Position = pos;

        return node;
    }

    virtual protected Connection CreateConnection(GraphNode from, GraphNode to)
    {
        Connection connection = new Connection();
        connection.FromGraphNode = from;
        connection.ToGraphNode = to;

        return connection;
    }

    // Compute possible connections between each nodes
    virtual protected void CreateGraph()
    {
        foreach (GraphNode node in GraphNodeList)
        {
            if (IsGraphNodeValid(node))
            {
                ConnectionGraph.Add(node, new List<Connection>());
                node.Neighbours = GetNeighbours(node); // cache neighbours list
                foreach (GraphNode neighbour in node.Neighbours)
                {
                    ConnectionGraph[node].Add(CreateConnection(node, neighbour));
                }
            }
        }

        OnGraphCreated?.Invoke();
    }

    public bool IsPosValid(Vector3 pos)
    {
        if (GraphThread != null && GraphThread.ThreadState == ThreadState.Running)
            return false;

        if (pos.x > (-GridSizeH / 2) && pos.x < (GridSizeH / 2) && pos.z > (-GridSizeV / 2) && pos.z < (GridSizeV / 2))
            return true;
        return false;
    }

    // Converts world 3d pos to tile 2d pos
    public Vector2Int GetTileCoordFromPos(Vector3 pos)
    {
        Vector3 realPos = pos - GridStartPos;
        Vector2Int tileCoords = Vector2Int.zero;
        tileCoords.x = Mathf.FloorToInt(realPos.x / SquareSize);
        tileCoords.y = Mathf.FloorToInt(realPos.z / SquareSize);
        return tileCoords;
    }

    public GraphNode GetGraphNode(Vector3 pos)
    {
        return GetGraphNode(GetTileCoordFromPos(pos));
    }

    public GraphNode GetGraphNode(Vector2Int pos)
    {
        return GetGraphNode(pos.x, pos.y);
    }

    protected GraphNode GetGraphNode(int x, int y)
    {
        int index = y * NbTilesH + x;
        if (index >= GraphNodeList.Count || index < 0)
            return null;

        return GraphNodeList[index];
    }

    virtual protected bool IsGraphNodeValid(GraphNode node)
    {
        return node != null;
    }

    private void AddGraphNode(List<GraphNode> list, GraphNode node)
    {
        if (IsGraphNodeValid(node))
            list.Add(node);
    }
    #endregion

    virtual protected List<GraphNode> GetNeighbours(GraphNode node)
    {
        Vector2Int tileCoord = GetTileCoordFromPos(node.Position);
        int x = tileCoord.x;
        int y = tileCoord.y;

        List<GraphNode> nodes = new List<GraphNode>();

        if (x > 0)
        {
            if (y > 0)
            {
                AddGraphNode(nodes, GetGraphNode(x - 1, y - 1));
            }
            AddGraphNode(nodes, GraphNodeList[(x - 1) + y * NbTilesH]);
            if (y < NbTilesV - 1)
            {
                AddGraphNode(nodes, GraphNodeList[(x - 1) + (y + 1) * NbTilesH]);
            }
        }

        if (y > 0)
        {
            AddGraphNode(nodes, GraphNodeList[x + (y - 1) * NbTilesH]);
        }
        if (y < NbTilesV - 1)
        {
            AddGraphNode(nodes, GraphNodeList[x + (y + 1) * NbTilesH]);
        }

        if (x < NbTilesH - 1)
        {
            if (y > 0)
            {
                AddGraphNode(nodes, GraphNodeList[(x + 1) + (y - 1) * NbTilesH]);
            }
            AddGraphNode(nodes, GraphNodeList[(x + 1) + y * NbTilesH]);
            if (y < NbTilesV - 1)
            {
                AddGraphNode(nodes, GraphNodeList[(x + 1) + (y + 1) * NbTilesH]);
            }
        }

        return nodes;
    }

    #region Gizmos

    protected virtual void DrawGridGizmo()
    {
        float gridHeight = 0.01f;
        Gizmos.color = Color.yellow;
        for (int i = 0; i < NbTilesV + 1; i++)
        {
            Vector3 startPos = transform.position + new Vector3(-GridSizeH / 2f, gridHeight, -GridSizeV / 2f + i * SquareSize);
            Gizmos.DrawLine(startPos, startPos + Vector3.right * GridSizeV);

            for (int j = 0; j < NbTilesH + 1; j++)
            {
                startPos = transform.position +new Vector3(-GridSizeH / 2f + j * SquareSize, gridHeight, -GridSizeV / 2f);
                Gizmos.DrawLine(startPos, startPos + Vector3.forward * GridSizeV);
            }
        }
    }
    protected virtual void DrawConnectionsGizmo()
    {
        foreach (GraphNode crtGraphNode in GraphNodeList)
        {
            if (ConnectionGraph.ContainsKey(crtGraphNode))
            {
                foreach (Connection c in ConnectionGraph[crtGraphNode])
                {
                    Gizmos.color = Color.green;
                    Gizmos.DrawLine(c.FromGraphNode.Position, c.ToGraphNode.Position);
                }
            }
        }
    }
    protected virtual void DrawGraphNodesGizmo()
    {
        for (int i = 0; i < GraphNodeList.Count; i++)
        {
            GraphNode node = GraphNodeList[i];
            Gizmos.color = Color.white;
            Gizmos.DrawCube(node.Position, Vector3.one * SquareSize * 0.5f);
        }
    }

    private void OnDrawGizmos()
    {
        if (DrawGrid)
        {
            DrawGridGizmo();
        }
        if (DrawNodes)
        {
            DrawGraphNodesGizmo();
        }
        if (DrawConnections)
        {
            DrawConnectionsGizmo();
        }
    }
    #endregion
}
