using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TargetEnemies : MonoBehaviour
{
    List<BaseEntity> enemiesInRange;
    private void Start()
    {
        enemiesInRange = GetComponentInParent<Unit>().potentialTargets;
    }
    private void FixedUpdate()
    {
        if (enemiesInRange.Count <= 0)
            return;

        for (int i =0; i <= enemiesInRange.Count; i++)
        {
            if (i > enemiesInRange.Count && enemiesInRange[i] == null)
            {
                enemiesInRange.Remove(enemiesInRange[i]);
            }

        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.GetComponent<BaseEntity>())
            return;

        if(other.GetComponent<BaseEntity>().GetTeam() != GetComponentInParent<BaseEntity>().GetTeam())
        {
            enemiesInRange.Add(other.GetComponent<BaseEntity>());
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.GetComponent<BaseEntity>())
            return;

        if (enemiesInRange.Contains(other.GetComponent<BaseEntity>()))
        {
            enemiesInRange.Remove(other.GetComponent<BaseEntity>());
        }
    }
}
