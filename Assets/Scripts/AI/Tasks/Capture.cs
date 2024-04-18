using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Capture : Node
{
    NavMeshAgent agent;
    private Vector3 target;

    Rigidbody rb;
    Animator animator;
    Unit unit;
    Vector3 targetBuilding;
    bool temp = false;
    public Capture(NavMeshAgent _agent, Vector3 _target, Rigidbody _rb,
        Animator _animator, float _speed, Unit _unit, Vector3 _buildingTarget)
    {
        if (_agent != null)
            agent = _agent;
        if (target != null)
            target = _target;
        if (rb != null)
            rb = _rb;
        if (animator != null)
            animator = _animator; 
        if (targetBuilding != null)
            targetBuilding = _buildingTarget;

        unit = _unit;
    }

    public override NODESTATE Evaluate()
    {
        //Pb with buildsing capture data temp fix collider exit
        object t = GetData("buildingToCapture");
        
        if (unit !=null && t != null && unit.closeBuilding != null)
        {
            if(unit.closeBuilding.GetTeam() != unit.GetTeam())
            {
                
                unit.StartCapture(unit.closeBuilding);
                temp = true;
                state = NODESTATE.RUNNING;
            }
            else if(unit.closeBuilding.GetTeam() == unit.GetTeam() )
            {
                temp = false;

                if(unit.inMission == true)
                {
                    unit.inMission = false;
                    unit.stance = Stance.NEUTRAL;
                    Plan.Instance.SquadFinishGoal(unit);
                }

                ClearData("buildingToCapture");
                state = NODESTATE.SUCCESS;
            }
            else
            {
                state = NODESTATE.FAILURE;
            }

        }
        else
        {
            state = NODESTATE.FAILURE;
        }

        return state;
    }
}
