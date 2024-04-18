using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Perception : MonoBehaviour
{
    private static Perception _Instance = null;
    public static Perception Instance
    {
        get
        {
            if (_Instance == null)
                _Instance = FindObjectOfType<Perception>();
            return _Instance;
        }
    }

    [SerializeField] private AIController aiController;
    [SerializeField] private int maxGoalAtSameTime = 2;
    [SerializeField] private float timerReComputeGoal = 5f;
    [SerializeField] private float timeRecallSameGame = 15f;
    [SerializeField] private float timeForNewExploration = 60f;
    [SerializeField] private int maxFactory = 10;

    public List<Goal> currentGoals = new List<Goal>();

    private Dictionary<Goal, bool> goalTryToMake = new Dictionary<Goal, bool>()
    {
        { Goal.ATTACK, false },
        { Goal.DEFENSE, false },
        { Goal.CAPTURE, false },
        { Goal.EXPLORATION, false },
        { Goal.UPGRADE, false },
    };

    private bool canExplore = true;
    
    private float timer = 0f;

    private void Start()
    {
        timer = timerReComputeGoal;
    }

    private void Update()
    {
        timer += Time.deltaTime;
        if (timerReComputeGoal > timer)
            return;

        ComputeGoal();
        timer = 0f;
    }

    public void ComputeGoal()
    {
        if (CheckMaximumGoal())
            return;

        for(int i = currentGoals.Count; i < maxGoalAtSameTime; i++)
        {
            if (CheckDefense())
                AddGoal(Goal.DEFENSE);
            else if (CheckAttack())
                AddGoal(Goal.ATTACK);
            else if (CheckCapture())
                AddGoal(Goal.CAPTURE);
            else if (CheckExploration())
                AddGoal(Goal.EXPLORATION);
            else if (CheckUpgrade())
                AddGoal(Goal.UPGRADE);
        }
    }

    #region CheckGoal
    private bool CheckDefense()
    {
        if (goalTryToMake[Goal.DEFENSE] || currentGoals.Contains(Goal.DEFENSE))
            return false;

        StartCoroutine(goalTry(Goal.DEFENSE));

        foreach (Unit unit in aiController.UnitList)
        {
            ETeam squadTeam = unit.Team;

            // Check if enemy near allies area
            List<Factory> factoryList = GameServices.GetControllerByTeam(squadTeam).GetFactoryList;
            foreach (Factory factory in factoryList)
            {
                InfluenceNode factoryNode = (InfluenceNode)InfluenceMap.Instance.GetGraphNode(factory.transform.position);
                if (factoryNode != null && factoryNode.faction != squadTeam && factoryNode.value > 0f)
                    return true;

                List<InfluenceNode> factoryNeighboorsWithEnemy = GetNeighboursWithEnemy(factoryNode, squadTeam);
                if (factoryNeighboorsWithEnemy.Count > 0)
                    return true;
            }

            List<TargetBuilding> ownedResources = ResourcesMap.Instance.ownedResources;
            foreach (TargetBuilding ownedResource in ownedResources)
            {
                // Check resource
                InfluenceNode resourceNode = (InfluenceNode)InfluenceMap.Instance.GetGraphNode(ownedResource.transform.position);
                if (resourceNode.faction != squadTeam && resourceNode.value > 0f)
                    return true;

                // Check neighboors
                List<InfluenceNode> resourceNeighboorsWithEnemy = GetNeighboursWithEnemy(resourceNode, squadTeam);
                if (resourceNeighboorsWithEnemy.Count > 0)
                    return true;
            }

            // Check if ressource need protection (but not in attack)
            TargetBuilding resourceNeedToBeDefended = null;
            float closetResourceDistance = float.MaxValue;

            foreach (TargetBuilding ownedResource in ownedResources)
            {
                InfluenceNode resourceNode = (InfluenceNode)InfluenceMap.Instance.GetGraphNode(ownedResource.transform.position);
                if (resourceNode.value == 0 && Vector3.Distance(unit.transform.position, resourceNode.Position) < closetResourceDistance)
                {
                    resourceNeedToBeDefended = ownedResource;
                    closetResourceDistance = Vector3.Distance(unit.transform.position, resourceNode.Position);
                }
            }

            if (resourceNeedToBeDefended != null)
                return true;
        }
        return false;
    }

    private bool CheckAttack()
    {
        if (goalTryToMake[Goal.ATTACK] || currentGoals.Contains(Goal.ATTACK))
            return false;

        StartCoroutine(goalTry(Goal.ATTACK));

        int unitPower = 0;
        ETeam team = ETeam.Red;

        // Get all available unit power
        foreach (Unit unit in aiController.UnitList)
        {
            if (!unit.inMission)
                unitPower += unit.GetUnitData.DPS;
        }

        foreach(InfluenceNode node in  InfluenceMap.Instance.GraphNodeList)
        {
            if(node.faction != team && node.value > 0 && node.value < unitPower)
                return true;
        }

        return false;
    }
    
    private bool CheckCapture()
    {
        if (goalTryToMake[Goal.CAPTURE] || currentGoals.Contains(Goal.CAPTURE))
            return false;

        StartCoroutine(goalTry(Goal.CAPTURE));

        List<TargetBuilding> neutralResources = ResourcesMap.Instance.knownResources;
        return neutralResources.Count != 0;
    }

    private bool CheckExploration()
    {
        if (!canExplore)
            return false;
        if (goalTryToMake[Goal.EXPLORATION])
            return false;
        if (currentGoals.Contains(Goal.EXPLORATION))
            return false;

        StartCoroutine(goalTry(Goal.EXPLORATION));

        return true;
    }

    private bool CheckUpgrade()
    {
        if (aiController.GetFactoryList.Count > maxFactory)
            return false;

        if (goalTryToMake[Goal.UPGRADE] || currentGoals.Contains(Goal.UPGRADE))
            return false;

        StartCoroutine(goalTry(Goal.UPGRADE));

        if(aiController == null)
        {
            Debug.Log("Controller not found");
            return false;
        }
        return aiController.TotalBuildPoints >= 15;
    }
    #endregion

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

    private IEnumerator goalTry(Goal goal)
    {
        goalTryToMake[goal] = true;
        yield return new WaitForSeconds(timeRecallSameGame);
        goalTryToMake[goal] = false;
    }

    private bool CheckMaximumGoal()
    {
        return currentGoals.Count >= maxGoalAtSameTime;
    }

    private void AddGoal(Goal p_goal)
    {
        currentGoals.Add(p_goal);
        Interaction.Instance.CreateSquads(p_goal);

        if (p_goal == Goal.EXPLORATION)
            StartCoroutine(StartExploration());
    }

    private IEnumerator StartExploration()
    {
        canExplore = false;
        yield return new WaitForSeconds(timeForNewExploration);
        canExplore = true;
    }

    public void GoalFinish(Goal p_goal)
    {
        Debug.Log("Finish goal: " +  p_goal);
        currentGoals.Remove(p_goal);
    }
}
