using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public static class ExtensionMethods
{
    public static ETeam GetOpponent(this ETeam team)
    {
        return team == ETeam.Blue ? ETeam.Red : ETeam.Blue;
    }
}
