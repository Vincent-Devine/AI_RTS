using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

enum UnitAction : uint
{
    MOVE_TO,
    SHOOT,
    PROTECT,
    EXPLORE,
    CAPTURE_ORE
}

public class Plan : MonoBehaviour
{
    private static Plan _Instance = null;
    public static Plan Instance
    {
        get
        {
            if (_Instance == null)
                _Instance = FindObjectOfType<Plan>();
            return _Instance;
        }
    }

    private Dictionary<int, List<Unit>> squads = new Dictionary<int, List<Unit>>();
    private Dictionary<int, bool> squadsChangePlan = new Dictionary<int, bool>();
    private Dictionary<int, Goal> goalBySquads = new Dictionary<int, Goal>();
    [SerializeField] private int idSquad = 0;

    [SerializeField] float timeSquadCanChangePlan = 1f;
    [SerializeField] Vector3 basePosition = Vector3.zero;
    [SerializeField] Unity.AI.Navigation.NavMeshSurface navMeshTerrain;

    [SerializeField] private int timeDefenseMax = 15;

    //TEST DEBUG TO REMOVE
    [SerializeField] List<Unit> UnitList = new List<Unit>();
    [SerializeField] FogOfWarSystem fogOfWarSystem;
    Vector3 temp = Vector3.zero;

    private Dictionary<Goal, List<UnitAction>> predefinedAction = new Dictionary<Goal, List<UnitAction>>()
    {
        { Goal.ATTACK, new List<UnitAction>(){ UnitAction.MOVE_TO, UnitAction.SHOOT} },
        { Goal.DEFENSE, new List<UnitAction>(){ UnitAction.MOVE_TO, UnitAction.PROTECT} },
        { Goal.EXPLORATION, new List<UnitAction>(){ UnitAction.MOVE_TO, UnitAction.EXPLORE } },
        { Goal.CAPTURE, new List<UnitAction>(){ UnitAction.MOVE_TO, UnitAction.CAPTURE_ORE} }
    };

    private Dictionary<Goal, Stance> predefinedStance = new Dictionary<Goal, Stance>()
    {
        { Goal.ATTACK, Stance.AGGRESSIVE },
        { Goal.CAPTURE, Stance.EXPLOITING },
        { Goal.DEFENSE, Stance.DEFENSIVE },
        { Goal.EXPLORATION, Stance.NEUTRAL }
    };

    private void Start()
    {
        SetBestAction(UnitList, Goal.EXPLORATION);
    }
    public void SetBestAction(List<Unit> p_squad, Goal p_goal)
    {
        int squadID = AddSquad(p_squad, p_goal);

        // If new action create, use switch instead of for loop
        Vector3 bestTraget = GetBestTarget(p_goal, squadID);
        Stance stance = predefinedStance[p_goal];

        for (int i = 0; i < p_squad.Count; i++)
        {

            p_squad[i].stance = stance;
            p_squad[i].behaviorTree.target = GetPositionFormationSquad(bestTraget, i, p_squad.Count, p_squad[i].transform.position);
            p_squad[i].inMission = true;

            switch (p_goal)
            {
                case Goal.ATTACK:
                    p_squad[i].behaviorTree.targetToAttack = p_squad[0].behaviorTree.target;
                    break;
                case Goal.CAPTURE:
                    p_squad[i].behaviorTree.buildingToCapture = p_squad[0].behaviorTree.target;
                    break;
                case Goal.DEFENSE:
                    p_squad[i].behaviorTree.targetToDefend = p_squad[0].behaviorTree.target;
                    break;
                default:
                    break;
            }
        }
    }

    public void ActionCantBeExecuted(Unit p_unit)
    {
        int squadID = GetSquad(p_unit);
        if (!CheckSquadCanChangePlan(squadID))
            return;

        StartTimerSquadCantChangePlan(squadID);

        List<InfluenceNode> neighboursWith = GetNeighboursWithEnemy(p_unit);
        Vector3 newTarget = GetBasePosition();
        float higherInfluence = float.MinValue;
        int id = -1;
        for (int i = 0; i < neighboursWith.Count; i++)
        {
            if(higherInfluence < neighboursWith[i].value)
            {
                higherInfluence = neighboursWith[i].value;
                id = i;
            }
        }

        if (higherInfluence > GetSquadPower(squadID))
            GiveNewActions(newTarget, Stance.DEFENSIVE, squadID);
        else
            GiveNewActions(neighboursWith[id].Position, Stance.AGGRESSIVE, squadID);
    }

    private int AddSquad(List<Unit> p_squad, Goal p_goal)
    {
        squads.Add(idSquad, p_squad);
        squadsChangePlan.Add(idSquad, true);
        goalBySquads.Add(idSquad, p_goal);
        idSquad++;
        return idSquad - 1;
    }

    private Vector3 GetBestTarget(Goal p_goal, int p_squadID)
    {
        switch (p_goal)
        {
            case Goal.ATTACK:
                return GetAttackArea(p_squadID);

            case Goal.DEFENSE:
                return GetDefenseArea(p_squadID);

            case Goal.EXPLORATION:
                return GetExlorationArea();

            case Goal.CAPTURE:
                return GetCaptureArea(p_squadID);

            default:
                return GetBasePosition();
        }
    }

    private Vector3 GetAttackArea(int p_squadID)
    {
        ETeam squadTeam = GetSquadTeam(p_squadID);
        Vector3 squadPosition = GetSquadPosition(p_squadID);
        float squadPower = GetSquadPower(p_squadID);

        float closetEnemyDistance = float.MaxValue;
        InfluenceNode nodeToAttack = null;

        foreach (InfluenceNode node in InfluenceMap.Instance.GraphNodeList)
        {
            if (node.faction != squadTeam && node.value > 0f && node.value < squadPower)
            {
                float temp = Vector3.Distance(node.Position, squadPosition);
                if(temp < closetEnemyDistance)
                {
                    closetEnemyDistance = temp;
                    nodeToAttack = node;
                }
            }
        }

        if (nodeToAttack == null)
            return GetBasePosition();

        return nodeToAttack.Position;
    }

    private Vector3 GetDefenseArea(int p_squadID)
    {
        ETeam squadTeam = GetSquadTeam(p_squadID);

        // Check if enemy near allies area
        List<Factory> factoryList = GameServices.GetControllerByTeam(squadTeam).GetFactoryList;
        foreach(Factory factory in factoryList)
        {
            InfluenceNode factoryNode = (InfluenceNode)InfluenceMap.Instance.GetGraphNode(factory.transform.position);
            if (factoryNode.faction != squadTeam && factoryNode.value > 0f)
                return factory.transform.position;

            List<InfluenceNode> factoryNeighboorsWithEnemy = GetNeighboursWithEnemy(factoryNode, squadTeam);
            if (factoryNeighboorsWithEnemy.Count > 0)
                return factory.transform.position;
        }

        List<TargetBuilding> ownedResources = ResourcesMap.Instance.ownedResources;
        foreach (TargetBuilding ownedResource in ownedResources)
        {
            // Check resource
            InfluenceNode resourceNode = (InfluenceNode)InfluenceMap.Instance.GetGraphNode(ownedResource.transform.position);
            if (resourceNode.faction != squadTeam && resourceNode.value > 0f)
                return ownedResource.transform.position;

            // Check neighboors
            List<InfluenceNode> resourceNeighboorsWithEnemy = GetNeighboursWithEnemy(resourceNode, squadTeam);
            if(resourceNeighboorsWithEnemy.Count > 0)
                return ownedResource.transform.position;
        }

        // Check if ressource need protection (but not in attack)
        TargetBuilding resourceNeedToBeDefended = null;
        float closetResourceDistance = float.MaxValue;

        foreach (TargetBuilding ownedResource in ownedResources)
        {
            InfluenceNode resourceNode = (InfluenceNode)InfluenceMap.Instance.GetGraphNode(ownedResource.transform.position);
            if (resourceNode.value == 0 && Vector3.Distance(GetSquadPosition(p_squadID), resourceNode.Position) < closetResourceDistance)
            {
                resourceNeedToBeDefended = ownedResource;
                closetResourceDistance = Vector3.Distance(GetSquadPosition(p_squadID), resourceNode.Position);
            }
        }

        if(resourceNeedToBeDefended != null)
            return resourceNeedToBeDefended.transform.position;
            
        return GetBasePosition();
    }

    private Vector3 GetExlorationArea()
    {
        Vector3 explorationArea = Vector3.zero;

        while(explorationArea == Vector3.zero)
        {
            explorationArea = GetRandomPointInNavMesh(explorationArea);
        }

        return explorationArea;
    }

    private Vector3 GetCaptureArea(int p_squadID)
    {
        List<TargetBuilding> neutralResources = ResourcesMap.Instance.knownResources;
        TargetBuilding closetNeutralResource = null;
        float closetNeutralResourceDistance = float.MaxValue;

        if (neutralResources.Count == 0)
            return GetBasePosition();

        foreach (TargetBuilding neutralResource in neutralResources)
        {
            float distance = Vector3.Distance(GetSquadPosition(p_squadID), neutralResource.transform.position);
            if(distance < closetNeutralResourceDistance)
            {
                closetNeutralResourceDistance = distance;
                closetNeutralResource = neutralResource;
            }
        }

        return closetNeutralResource.transform.position;
    }

    private Vector3 GetBasePosition()
    {
        return basePosition;
    }

    private Vector3 GetPositionFormationSquad(Vector3 p_initPosition, int p_unitID, int p_squadCount, Vector3 squadLeaderPos)
    {
        if (p_unitID == 0)
            return p_initPosition;
        else
        {
            Vector3 randomPoint = p_initPosition + Random.insideUnitSphere * 10;
            NavMeshHit hit;
            if (NavMesh.SamplePosition(randomPoint, out hit, 1.0f, NavMesh.AllAreas))
            {
                return hit.position;
            }
            return randomPoint;
        }

    }

    private int GetSquad(Unit p_unit)
    {
        foreach(KeyValuePair<int, List<Unit>> squad in squads)
        {
            foreach(Unit unit in squad.Value)
            {
                if (unit == p_unit)
                    return squad.Key;
            }
        }
        Debug.Log("No squad found for this unit: " +  p_unit.name);
        return -1;
    }

    private bool CheckSquadCanChangePlan(int p_squadID)
    {
        return squadsChangePlan[p_squadID];
    }

    private void StartTimerSquadCantChangePlan(int p_squadID)
    {
        StartCoroutine(TimerSquadCantChangePlan(p_squadID));
    }

    private IEnumerator TimerSquadCantChangePlan(int p_squadID)
    {
        squadsChangePlan[p_squadID] = false;
        yield return new WaitForSeconds(timeSquadCanChangePlan);
        squadsChangePlan[p_squadID] = true;
    }

    private float GetSquadPower(int p_squadID)
    {
        // TODO: Compute with real squad power
        return 0.25f * squads[p_squadID].Count;
    }

    private ETeam GetSquadTeam(int p_squadID)
    {
        return squads[p_squadID][0].Team;
    }

    private Vector3 GetSquadPosition(int p_squadID)
    {
        return squads[p_squadID][0].transform.position;
    }

    private void GiveNewActions(Vector3 p_newTarget, Stance p_newStance, int p_squadID)
    {
        for (int i = 0; i < squads[p_squadID].Count; i++)
        {
            squads[p_squadID][i].behaviorTree.target = GetPositionFormationSquad(p_newTarget, i, squads[p_squadID].Count, squads[p_squadID][i].gameObject.transform.position);
            squads[p_squadID][i].stance = p_newStance;
        }
    }

    private List<InfluenceNode> GetNeighboursWithEnemy(Unit p_unit)
    {
        List<InfluenceNode> neighboursWith = new List<InfluenceNode>();
        foreach (InfluenceNode node in InfluenceMap.Instance.GetGraphNode(p_unit.transform.position).Neighbours)
        {
            if (node.faction != p_unit.Team && node.value > 0f)
                neighboursWith.Add(node);
        }
        return neighboursWith;
    }

    private List<InfluenceNode> GetNeighboursWithEnemy(GraphNode p_node, ETeam p_team)
    {
        List<InfluenceNode> neighboursWith = new List<InfluenceNode>();
        foreach (InfluenceNode node in p_node.Neighbours)
        {
            if (node.faction != p_team && node.value > 0f)
                neighboursWith.Add(node);
        }
        return neighboursWith;
    }

    private Vector3 GetRandomPointInNavMesh( Vector3 p_randomPoint)
    {
        NavMeshTriangulation navMeshData = NavMesh.CalculateTriangulation();

        // Pick the first indice of a random triangle in the nav mesh
        int t = Random.Range(0, navMeshData.indices.Length - 3);

        // Select a random point on it
        Vector3 point = Vector3.Lerp(navMeshData.vertices[navMeshData.indices[t]], navMeshData.vertices[navMeshData.indices[t + 1]], Random.value);
        Vector3.Lerp(point, navMeshData.vertices[navMeshData.indices[t + 2]], Random.value);

        p_randomPoint = point;
        return p_randomPoint;
    }

    public void SquadFinishGoal(Unit p_unit)
    {
        int squadID = GetSquad(p_unit);

        if (squadID == -1)
            return;

        Debug.Log("Remove squad: " +  squadID + ", goal: " + goalBySquads[squadID]);
        Perception.Instance.GoalFinish(goalBySquads[squadID]);
        squads.Remove(squadID);
        squadsChangePlan.Remove(squadID);
        goalBySquads.Remove(squadID);
    }

    public void SquadFinishGoal(int p_squadID)
    {
        Debug.Log("Remove squad: " + p_squadID + ", goal: " + goalBySquads[p_squadID]);
        Perception.Instance.GoalFinish(goalBySquads[p_squadID]);
        squads.Remove(p_squadID);
        squadsChangePlan.Remove(p_squadID);
        goalBySquads.Remove(p_squadID);
    }

    public void UnitDead(Unit unit)
    {
        int squadID = GetSquad(unit);
        if (squadID == -1)
            return;

        squads[squadID].Remove(unit);
        if (squads[squadID].Count == 0)
            SquadFinishGoal(squadID);
    }
}