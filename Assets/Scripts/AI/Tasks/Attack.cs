using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

public class Attack : Node
{
    NavMeshAgent agent;
    private Vector3 target;

    Rigidbody rb;
    Animator animator;
    Unit unit;

    bool killAllUnitInArea = false;

    public Attack(NavMeshAgent _agent, Vector3 _target, Rigidbody _rb, Animator _animator, float _speed, Unit _unit)
    {
        if (_agent != null)
            agent = _agent;
        if (target != null)
            target = _target;
        if (rb != null)
            rb = _rb;
        if (animator != null)
            animator = _animator;

        unit = _unit;
    }

    public override NODESTATE Evaluate()
    {
        object t = GetData("targetToAttack");

        if (unit != null && t != null)
        {
            if (unit.potentialTargets.Count > 0)
            {
                killAllUnitInArea = false;
                unit.StartAttacking(unit.potentialTargets[0]);
                if(unit.potentialTargets[0] == null)
                    unit.potentialTargets.RemoveAt(0);

                if (unit.potentialTargets.Count == 0)
                    killAllUnitInArea = true;

                state = NODESTATE.RUNNING;
            }
            else if(killAllUnitInArea)
            {
                if(unit.stance == Stance.AGGRESSIVE && unit.inMission == true)
                {
                    unit.inMission = false;
                    unit.stance = Stance.NEUTRAL;
                    Plan.Instance.SquadFinishGoal(unit);
                }
                ClearData("targetToAttack");
                state = NODESTATE.SUCCESS;
                killAllUnitInArea = false;
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
