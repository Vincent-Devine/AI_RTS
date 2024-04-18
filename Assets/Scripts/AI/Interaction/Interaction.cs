using System.Collections.Generic;
using UnityEngine;

public class Interaction : MonoBehaviour
{
    private static Interaction _Instance = null;
    public static Interaction Instance
    {
        get
        {
            if (_Instance == null)
                _Instance = FindObjectOfType<Interaction>();
            return _Instance;
        }
    }

    [SerializeField] private AIController aiController;
    [SerializeField] private int squadSize = 3;
    [SerializeField] private int maxIterationCreateFactory = 10;
    [SerializeField] private float radiusCreateFactory = 2f;

    private bool createFirstFactory = false;

    private void Update()
    {
        if(!createFirstFactory && aiController.GetFactoryList.Count > 0)
        {
            CreateFactory();
            CreateLightUnit(3, 0);
            createFirstFactory = true;
        }
    }

    public void CreateSquads(Goal p_goal)
    {
        List<Unit> lightAvailable, heavyAvailable;

        GetAllUnitAvailableByType(out lightAvailable, out heavyAvailable);

        if(p_goal == Goal.ATTACK)
        {
            if (heavyAvailable.Count < squadSize)
                Perception.Instance.GoalFinish(p_goal);
            CreateHeavySquad(heavyAvailable, p_goal);

        }
        else if (p_goal == Goal.EXPLORATION)
        { 
            if(lightAvailable.Count < squadSize)
                Perception.Instance.GoalFinish(p_goal);     

            CreateLightSquad(lightAvailable, p_goal);
        }
        else if (p_goal == Goal.UPGRADE)
        {
            CreateFactory();
            Perception.Instance.GoalFinish(p_goal);
        }
        else
        {
            if (lightAvailable.Count + heavyAvailable.Count < squadSize)
                Perception.Instance.GoalFinish(p_goal);
            
            List<Unit> availableUnit = new List<Unit>();
            availableUnit.AddRange(lightAvailable);
            availableUnit.AddRange(heavyAvailable);

            CreateMixSquad(availableUnit, p_goal);
        }
    }

    private void GetAllUnitAvailableByType(out List<Unit> p_lightAvailable, out List<Unit> p_heavyAvailable)
    {
        p_lightAvailable = new List<Unit>();
        p_heavyAvailable = new List<Unit>();

        if (aiController.UnitList.Count == 0)
            return;

        foreach(Unit unit in  aiController.UnitList)
        {
            if(!unit.inMission)
            {
                if(unit.GetUnitData.Caption == "Trooper" ||
                   unit.GetUnitData.Caption == "Quad" ||
                   unit.GetUnitData.Caption == "Trike")
                {
                    p_lightAvailable.Add(unit);
                }
                else
                {
                    p_heavyAvailable.Add(unit);
                }
            }
        }
    }

    private void CreateLightUnit(int p_number, int p_unitType = -1)
    {
        if (p_number == 0)
            return;

        if (p_unitType == -1)
            p_unitType = Random.Range(0, 3);

        foreach (Factory factory in aiController.GetFactoryList)
        {
            if(factory.GetFactoryData.TypeId != 0 || factory.NbUnitPossibleCreate() == 0)
                continue;

            aiController.SelectedFactory = factory;
            for(int i = 0; i < factory.NbUnitPossibleCreate(); i++)
            {
                if (aiController.RequestUnitBuild(p_unitType))
                    p_number--;

                if (p_number == 0)
                    return;
            }

            aiController.UnselectCurrentFactory();
            if (p_number == 0)
                return;
        }
    }

    private void CreateHeavyUnit(int p_number, int p_unitType = -1)
    {
        if (p_number == 0)
            return;

        if (p_unitType == -1)
            p_unitType = Random.Range(0, 3);

        foreach (Factory factory in aiController.GetFactoryList)
        {
            if (factory.GetFactoryData.TypeId != 1 || factory.NbUnitPossibleCreate() == 0)
                continue;

            aiController.SelectedFactory = factory;
            for (int i = 0; i < factory.NbUnitPossibleCreate(); i++)
            {
                if (aiController.RequestUnitBuild(p_unitType))
                    p_number--;

                if (p_number == 0)
                    return;
            }

            aiController.UnselectCurrentFactory();
            if (p_number == 0)
                return;
        }
    }

    private void CreateFactory()
    {
        int lightFactoryNb = 0;
        int heavyFactoryNb = 0;
        foreach (Factory factory in aiController.GetFactoryList)
        {
            if (factory.GetFactoryData.Caption == "Light Factory")
                lightFactoryNb++;
            else
                heavyFactoryNb++;
        }

        if (lightFactoryNb > heavyFactoryNb)
        {
            int nbIteration = 0;
            if (nbIteration + 1 >= maxIterationCreateFactory * aiController.GetFactoryList.Count)
                return;

            while (TryCreateFactory(1, nbIteration) == false)
            {
                nbIteration++;
                if (nbIteration + 1 >= maxIterationCreateFactory * aiController.GetFactoryList.Count)
                    return;
            }
        }
        else
        {
            int nbIteration = 0;
            if (nbIteration >= maxIterationCreateFactory * aiController.GetFactoryList.Count + 1)
                return;

            while (TryCreateFactory(0, nbIteration) == false)
            {
                nbIteration++;
                if (nbIteration + 1 >= maxIterationCreateFactory * aiController.GetFactoryList.Count)
                    return;
            }
        }
    }

    private bool TryCreateFactory(int p_factoryIndex, int p_currentIteration)
    {
        if (aiController.GetFactoryList.Count == 0)
            return false;

        int indexFactory = (int)(p_currentIteration / maxIterationCreateFactory) + 1;
        Vector3 positionLastFactory = aiController.GetFactoryList[aiController.GetFactoryList.Count - indexFactory].transform.position;
        Vector3 randomPosition = GetRandomPositionAroundPoint(positionLastFactory, p_currentIteration);
        aiController.SelectedFactory = aiController.GetFactoryList[0];

        return aiController.RequestFactoryBuild(p_factoryIndex, randomPosition);
    }

    private Vector3 GetRandomPositionAroundPoint(Vector3 p_position, int p_currentIteration)
    {
        float rad = 2f * Mathf.PI / maxIterationCreateFactory * p_currentIteration;
        return new Vector3(
            p_position.x + radiusCreateFactory * Mathf.Cos(rad),
            p_position.y,
            p_position.z + radiusCreateFactory * Mathf.Sin(rad));
    }

    private void CreateLightSquad(List<Unit> p_lightAvailable, Goal p_goal)
    {
        if (p_lightAvailable.Count >= squadSize)
        {
            List<Unit> squad = new List<Unit>();
            for (int j = 0; j < squadSize; j++)
            {
                squad.Add(p_lightAvailable[0]);
                p_lightAvailable.RemoveAt(0);
                Debug.Log("Add light unit to squad");
            }
            Plan.Instance.SetBestAction(squad, p_goal);
        }
        else
        {
            int lightUnitNeededToCreate = squadSize - p_lightAvailable.Count;
            CreateLightUnit(lightUnitNeededToCreate);
        }
    }

    private void CreateHeavySquad(List<Unit> p_heavyAvailable, Goal p_goal)
    {
        if (p_heavyAvailable.Count >= squadSize)
        {
            List<Unit> squad = new List<Unit>();
            for (int j = 0; j < squadSize; j++)
            {
                squad.Add(p_heavyAvailable[0]);
                p_heavyAvailable.RemoveAt(0);
                Debug.Log("Add heavy unit to squad");
            }
            Plan.Instance.SetBestAction(squad, p_goal);
        }
        else
        {
            int heavyUnitNeededToCreate = squadSize - p_heavyAvailable.Count;
            CreateHeavyUnit(heavyUnitNeededToCreate);
        }
    }

    private void CreateMixSquad(List<Unit> p_unitAvailable, Goal p_goal)
    {
        if (p_unitAvailable.Count >= squadSize)
        {
            List<Unit> squad = new List<Unit>();
            for (int j = 0; j < squadSize; j++)
            {
                squad.Add(p_unitAvailable[0]);
                p_unitAvailable.RemoveAt(0);
            }
            Plan.Instance.SetBestAction(squad, p_goal);
        }
        else
        {
            int unitNeededToCreate = squadSize - p_unitAvailable.Count;
            CreateHeavyUnit(unitNeededToCreate / 2);
            CreateLightUnit(unitNeededToCreate / 2);
        }
    }

    // private void FixedUpadte()
    // {
    //     if (aiController.TotalBuildPoints > 5 && Perception.Instance.currentGoals.Count > 0)
    //     {
    //         if (Perception.Instance.currentGoals[0] == Goal.ATTACK ||
    //             Perception.Instance.currentGoals[0] == Goal.DEFENSE)
    //         {
    //             CreateHeavyUnit(1);
    //         }
    //         else if (Perception.Instance.currentGoals[0] == Goal.ATTACK ||
    //             Perception.Instance.currentGoals[0] == Goal.DEFENSE)
    //         {
    //             CreateLightUnit(1);
    //         }
    //     }
    // }
}
