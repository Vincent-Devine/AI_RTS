using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Sequence : Node
{
    public Sequence() : base() { }
    public Sequence(List<Node> children) : base(children) { }
    public override NODESTATE Evaluate()
    {
        bool anyChildRunning = false;

        foreach (Node item in children)
        {
            switch (item.Evaluate())
            {
                case NODESTATE.FAILURE:
                    {
                        state = NODESTATE.FAILURE;
                        return state;
                    }
                case NODESTATE.SUCCESS:
                    {
                        continue;
                    }
                case NODESTATE.RUNNING:
                    {
                        anyChildRunning = true;
                        continue;
                    }
                default:
                    {
                        state = NODESTATE.RUNNING;
                        return state;
                    }
            }
        }

        state = anyChildRunning ? NODESTATE.RUNNING : NODESTATE.SUCCESS;
        return state;
    }
}