using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CheckDiscovery : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<Unit>() != null)
        {
            //CapturingTeam = other.GetComponent<Unit>().GetTeam();
            ResourcesMap.Instance.knownResources.Add(this.GetComponentInParent<TargetBuilding>());
            other.GetComponent<Unit>().closeBuilding = this.GetComponentInParent<TargetBuilding>();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.GetComponent<Unit>().closeBuilding == this)
        {
            other.GetComponent<Unit>().closeBuilding = null;
        }
    }
}
