using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GetTeamColor : MonoBehaviour
{
    [SerializeField] Material blueColor;
    [SerializeField] Material redColor;
    // Start is called before the first frame update
    void Start()
    {
        if(GetComponentInParent<Unit>().GetTeam() == ETeam.Red)
            GetComponent<MeshRenderer>().material = redColor;
        else
        {
            GetComponent<MeshRenderer>().material = blueColor;
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
