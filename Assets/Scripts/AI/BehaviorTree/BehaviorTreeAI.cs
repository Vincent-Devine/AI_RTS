using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class BehaviorTreeAI : BehaviorTree
{
    protected override Node SetUpTree()
    {
        Node root = new Selector(new List<Node>()
        {
            new Sequence(new List<Node>()
            {
                new MoveTo(agent, target, rb, animator, speed),
                new Attack(agent, target, rb, animator, speed, unit),
            }),
            new Sequence(new List<Node>()
            {
                new MoveTo(agent, target, rb, animator, speed),
                new Capture(agent, target, rb, animator, speed, unit, buildingToCapture),
            }),
            new Sequence(new List<Node>()
            {
                new MoveTo(agent, target, rb, animator, speed),
                new Defense(agent, target, rb, animator, speed, unit),
            }),
        });

        return root;
    }
}
