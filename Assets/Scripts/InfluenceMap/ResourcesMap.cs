using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ResourcesMap : Graph
{
    // Singleton access
    static ResourcesMap _Instance = null;
    static public ResourcesMap Instance
    {
        get
        {
            if (_Instance == null)
                _Instance = FindObjectOfType<ResourcesMap>();
            return _Instance;
        }
    }

    public float UpdateFrequency = 0.5f;
    private float LastUpdateTime = float.MinValue;

    private bool IsGraphCreated = false;
    private bool IsInitialized = false;

    [SerializeField] float updateMapTimer = 2f;
    [SerializeField] float unitRadiusInfluence = 20f;
    [SerializeField] float unitStrenghtInfluence = 0.25f;

    public List<TargetBuilding> knownResources;
    public List<TargetBuilding> ownedResources;
    [SerializeField] List<TargetBuilding> enemyResources;

    float timer = 0f;

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
            //knownResources.Clear();
            //knownResources.AddRange(FindObjectsOfType<TargetBuilding>());
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

    public void ComputeInfluence()
    {
        // Reset all influence nodes
        //foreach (InfluenceNode node in GraphNodeList)
        //    node.SetValue(ETeam.Neutral, 0f);

        foreach (TargetBuilding unit in knownResources)
        {
            foreach (InfluenceNode node in GraphNodeList)
            {
                if (unit == null)
                    return;

                float distance = Vector2.Distance(
                    new Vector2(unit.transform.position.x, unit.transform.position.z), 
                    new Vector2(node.Position.x, node.Position.z));
                if (distance < unitRadiusInfluence)
                {
                    node.SetValue(ETeam.Neutral, 1);      
                }
            }
        }
    }
    protected override GraphNode CreateGraphNode()
    {
        return new InfluenceNode();
    }

    public void AddUnit(TargetBuilding u)
    {
        if (knownResources.Contains(u))
            return;
        knownResources.Add(u);
    }

    public void RemoveUnit(TargetBuilding u)
    {
        knownResources.Remove(u);
    }
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

    public void AddToCapturedRessources(ETeam capturingTeam, TargetBuilding capturedBuilding)
    {
        foreach (InfluenceNode node in GraphNodeList)
        {
            if (capturedBuilding == null)
                return;

            float distance = Vector2.Distance(
                new Vector2(capturedBuilding.transform.position.x, capturedBuilding.transform.position.z),
                new Vector2(node.Position.x, node.Position.z));

            if(knownResources.Contains(capturedBuilding))
            {
                knownResources.Remove(capturedBuilding);
            }

            if (distance < capturedBuilding.influenceRadius)
            {
                node.SetValue(ETeam.Neutral, 0);
                node.SetValue(ETeam.Blue, 0);
                node.SetValue(ETeam.Red, 0);

                node.SetValue(capturingTeam, capturedBuilding.influenceStrenght);
            }

        }       
            ownedResources.Add(capturedBuilding);
    }
}
