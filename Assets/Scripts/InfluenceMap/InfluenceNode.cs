using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InfluenceNode : GraphNode
{
    public ETeam faction;
    public float value = 0f;

    public bool SetValue(ETeam f, float v)
    {
        if (f == ETeam.Neutral)
        {
            faction = f; value = v;
            return true;
        }

        if (f == faction)
        {
            value += v;
            return true;
        }
        else if (v > value)
        {
            value = v;
            faction = f;
            return true;
        }
        return false;
    }
}
