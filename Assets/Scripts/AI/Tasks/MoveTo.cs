using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class MoveTo : Node
{
    NavMeshAgent agent;
    private Vector3 target;

    Rigidbody rb;
    Animator animator;

    Unit unit;
    bool setDestination = false;

    public MoveTo(NavMeshAgent _agent, Vector3 _target, Rigidbody _rb, Animator _animator, float _speed)
    {
        if (_agent != null)
            agent = _agent;
        if(target != null)
            target = _target;
        if(rb != null)
            rb = _rb;
        if(animator != null)
            animator = _animator;

        unit = agent.GetComponent<Unit>();
    }
    public override NODESTATE Evaluate()
    {
        object t = GetData("target");
        object s = GetData("stop");

        if (t != null && (Vector3)GetData("target") != Vector3.zero)
        {
            if(!setDestination)
            {
                agent.SetDestination((Vector3) GetData("target"));
                setDestination = true;
            }

            if (s != null && (bool)s)
            {
                ClearData("stop");
                if (unit.stance == Stance.NEUTRAL && unit.inMission == true)
                {
                    unit.inMission = false;
                    Plan.Instance.SquadFinishGoal(unit);
                }

                setDestination = false;
                state = NODESTATE.SUCCESS;
            }

            if (agent.remainingDistance > 0f && agent.remainingDistance <= agent.stoppingDistance )
            {

                //Debug.Log("Task sucessful");
                ClearData("target");
                if(unit.stance == Stance.NEUTRAL && unit.inMission == true)
                {
                    unit.inMission = false;
                    Plan.Instance.SquadFinishGoal(unit);
                }

                setDestination = false;
                state = NODESTATE.SUCCESS;
            }
            else
            {
                //Debug.Log("Task running");
                state = NODESTATE.RUNNING;
            }

        }
        else
        {
            //Debug.Log("Task failed");
            state = NODESTATE.FAILURE;
        }

        return state;
    }

}
