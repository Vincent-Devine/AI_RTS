using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Selector : Node
{
    public Selector() : base() { }
    public Selector(List<Node> children) : base(children) { }
    public override NODESTATE Evaluate()
    {
        bool anyChildRunning = false;

        foreach (Node item in children)
        {
            switch (item.Evaluate())
            {
                case NODESTATE.FAILURE:
                    {
                        continue;
                    }
                case NODESTATE.SUCCESS:
                    {
                        state = NODESTATE.SUCCESS;
                        return state;
                    }
                case NODESTATE.RUNNING:
                    {
                        state = NODESTATE.RUNNING;
                        return state;
                    }
                default:
                    {
                        continue;
                    }
            }
        }

        state = NODESTATE.FAILURE;
        return state;
    }
}

