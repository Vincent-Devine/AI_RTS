using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Defense : Node
{
    NavMeshAgent agent;
    private Vector3 target;

    Rigidbody rb;
    Animator animator;
    Unit unit;
    float timer = 5;
    float time = 0f;
    bool waitOver = false;
    bool temp = false;
    public Defense(NavMeshAgent _agent, Vector3 _target, Rigidbody _rb, Animator _animator, float _speed, Unit _unit)
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
        object t = GetData("targetToDefend");

        if (t != null && unit != null)
        {
            if (time < timer)
            {
                time += Time.deltaTime;

                temp = true;
                state = NODESTATE.RUNNING;
            }
            else if (time >= timer)
            {
                unit.inMission = false;
                unit.stance = Stance.NEUTRAL;
                Plan.Instance.SquadFinishGoal(unit);
                temp = false;
                time = 0f;
                state = NODESTATE.SUCCESS;
                ClearData("targetToDefend");

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

    IEnumerator WaitForDefense(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        temp = true;
    }
}
