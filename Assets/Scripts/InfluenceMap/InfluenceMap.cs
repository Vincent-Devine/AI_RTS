using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class InfluenceMap : Graph
{
    // Singleton access
    static InfluenceMap _Instance = null;
    static public InfluenceMap Instance
    {
        get
        {
            if (_Instance == null)
                _Instance = FindObjectOfType<InfluenceMap>();
            return _Instance;
        }
    }

    public float UpdateFrequency = 0.5f;
    private float LastUpdateTime = float.MinValue;

    // Units used to generate influence map
    public List<BaseEntity> UnitList;

    private bool IsGraphCreated = false;
    private bool IsInitialized = false;

    [SerializeField] float updateMapTimer = 2f;
    [SerializeField] float unitRadiusInfluence = 20f;
    [SerializeField] float unitStrenghtInfluence = 0.25f;

    private float timer = 0f;

    private void Awake()
    {
        CreateTiledGrid();
        OnGraphCreated += () => { IsGraphCreated = true; };
    }

    private void FixedUpdate()
    {
        if (!IsGraphCreated)
            return;

        if (!IsInitialized)
        {
            UnitList.Clear();
            UnitList.AddRange(FindObjectsOfType<BaseEntity>());
            IsInitialized = true;
            StartCoroutine(WaitFor(updateMapTimer));
        }

        // TODO : don't update influence map if no Unit has moved
        timer += Time.deltaTime;
        if (timer >= UpdateFrequency)
        {
            ComputeInfluence();
            timer = 0f;
        }
    }
    protected override GraphNode CreateGraphNode()
    {
        return new InfluenceNode();
    }

#region Influence Map
    public void AddUnit(Unit u)
    {
        if (UnitList.Contains(u))
            return;
        UnitList.Add(u);
    }

    public void RemoveUnit(Unit u)
    {
        UnitList.Remove(u);
    }

    public void ComputeInfluence()
    {
        // Reset all influence nodes
        foreach (InfluenceNode node in GraphNodeList)
            node.SetValue(ETeam.Neutral, 0f);

        foreach (BaseEntity unit in UnitList)
        {
            foreach(InfluenceNode node in GraphNodeList)
            {
                if (unit == null)
                    return;

                float distance = Vector2.Distance(
                    new Vector2(unit.transform.position.x, unit.transform.position.z), 
                    new Vector2(node.Position.x, node.Position.z));
                if (distance < unitRadiusInfluence)
                {
                    if(unit.GetTeam() == ETeam.Red)
                    {
                        node.SetValue(ETeam.Red, 1f / distance);

                        foreach (GraphNode neighbours in node.Neighbours)
                        {
                            node.SetValue(ETeam.Red, unitStrenghtInfluence / distance);
                        }
                    }
                    else if(unit.GetTeam() == ETeam.Blue)
                    {
                        node.SetValue(ETeam.Blue, 1f / distance);

                        foreach (GraphNode neighbours in node.Neighbours)
                        {
                            node.SetValue(ETeam.Blue,unitStrenghtInfluence / distance);
                        }
                    }

                }
            }   

        }
        // TODO : explore unit neighbor tiles and add influence value according to custom attenuation rules
        // hint => see breadth first search parcoural on influence graph

    }

#endregion

#region Gizmos

    // Draw influence map result as colored cubes using Gizmos
    protected override void DrawGraphNodesGizmo()
    {
        for (int i = 0; i < GraphNodeList.Count; i++)
        {
            InfluenceNode node = GraphNodeList[i] as InfluenceNode;
            if (node != null)
            {
                Color nodeColor = node.faction switch
                {
                    ETeam.Red => Color.red,
                    ETeam.Blue => Color.blue,
                    ETeam.Neutral => Color.black,
                    _ => throw new System.NotImplementedException()
                };

                nodeColor.a = Mathf.Max(node.value, 0.1f);
                Gizmos.color = nodeColor;
                Gizmos.DrawCube(node.Position, Vector3.one * SquareSize * 0.95f);
            }
        }
    }
#endregion

    IEnumerator WaitFor(float time) 
    {
        yield return new WaitForSeconds(time);  
        IsInitialized = false;
    }

    public void AddToCapturedResourcesList(ETeam capturingTeam)
    {
        foreach (InfluenceNode node in GraphNodeList)
        {

            node.SetValue(capturingTeam, 1);
        }
    }
}
