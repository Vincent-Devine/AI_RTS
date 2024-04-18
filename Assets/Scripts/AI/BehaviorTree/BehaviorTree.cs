using UnityEngine;
using UnityEngine.AI;

public abstract class BehaviorTree : MonoBehaviour
{
    protected Node root;
    public Rigidbody rb;
    public Animator animator;
    public NavMeshAgent agent;

    public Vector3 target;
    public float speed;
    public Vector3 buildingToCapture;
    public Vector3 targetToAttack ;
    public Vector3 targetToDefend;
    public bool stopTask ;

    public Unit unit;
    //Start is called before the first frame update
    void Start()
    {
        if (GetComponent<Rigidbody>())
            rb = GetComponent<Rigidbody>();
        if (GetComponent<Animator>())
            animator = GetComponent<Animator>();
        if (GetComponent<NavMeshAgent>())
            agent = GetComponent<NavMeshAgent>();
        if(GetComponent<Unit>())
        {
            GetComponent<Unit>().enabled = true;
            unit = GetComponent<Unit>();
        }
        root = SetUpTree();

    }

    // Update is called once per frame
    void Update()
    {
        if (root != null)
        {
            root.SetData("target", target);
            root.SetData("buildingToCapture", buildingToCapture);
            root.SetData("targetToAttack", targetToAttack);
            root.SetData("targetToDefend", targetToDefend);
            root.SetData("stop", stopTask);
            root.Evaluate();
        }
    }

    protected abstract Node SetUpTree();
}
