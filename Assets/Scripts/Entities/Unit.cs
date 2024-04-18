using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UIElements;
using static UnityEngine.UI.CanvasScaler;

public class Unit : BaseEntity
{
    [SerializeField]
    UnitDataScriptable UnitData = null;
    [SerializeField]
    float detectionRadius = 2f;

    Transform BulletSlot;
    float LastActionDate = 0f;
    BaseEntity EntityTarget = null;
    TargetBuilding CaptureTarget = null;
    NavMeshAgent NavMeshAgent;

    public BehaviorTreeAI behaviorTree;
    public Stance stance = Stance.NEUTRAL;
    public TargetBuilding closeBuilding;
    public bool inMission = false;

    public List<BaseEntity> potentialTargets;
    public UnitDataScriptable GetUnitData { get { return UnitData; } }
    public int Cost { get { return UnitData.Cost; } }
    public int GetTypeId { get { return UnitData.TypeId; } }
    override public void Init(ETeam _team)
    {
        if (IsInitialized)
            return;

        base.Init(_team);

        HP = UnitData.MaxHP;
        OnDeadEvent += Unit_OnDead;
    }
    void Unit_OnDead()
    {
        if (IsCapturing())
            StopCapture();

        if (GetUnitData.DeathFXPrefab)
        {
            GameObject fx = Instantiate(GetUnitData.DeathFXPrefab, transform);
            fx.transform.parent = null;
        }

        Destroy(gameObject);
    }
    #region MonoBehaviour methods
    override protected void Awake()
    {
        base.Awake();

        NavMeshAgent = GetComponent<NavMeshAgent>();
        BulletSlot = transform.Find("BulletSlot");
        behaviorTree = GetComponent<BehaviorTreeAI>();

        // fill NavMeshAgent parameters
        NavMeshAgent.speed = GetUnitData.Speed;
        NavMeshAgent.angularSpeed = GetUnitData.AngularSpeed;
        NavMeshAgent.acceleration = GetUnitData.Acceleration;
    }
    override protected void Start()
    {
        // Needed for non factory spawned units (debug)
        if (!IsInitialized)
            Init(Team);

        base.Start();

    }
    override protected void Update()
    {
        // Attack / repair task debug test $$$ to be removed for AI implementation
        if (EntityTarget != null)
        {
            if (EntityTarget.GetTeam() != GetTeam())
                behaviorTree.targetToAttack = EntityTarget.gameObject.transform.position;
            //ComputeAttack();
            else
                ComputeRepairing();
        }

	}
    #endregion

    #region IRepairable
    override public bool NeedsRepairing()
    {
        return HP < GetUnitData.MaxHP;
    }
    override public void Repair(int amount)
    {
        HP = Mathf.Min(HP + amount, GetUnitData.MaxHP);
        base.Repair(amount);
    }
    override public void FullRepair()
    {
        Repair(GetUnitData.MaxHP);
    }
    #endregion

    #region Tasks methods : Moving, Capturing, Targeting, Attacking, Repairing ...

    // $$$ To be updated for AI implementation $$$

    // Moving Task
    public void SetTargetPos(Vector3 pos)
    {
        if (EntityTarget != null)
            EntityTarget = null;

        if (CaptureTarget != null)
            StopCapture();

        if (NavMeshAgent)
        {
            GetComponent<BehaviorTreeAI>().target = pos;
            //NavMeshAgent.isStopped = false;
            //NavMeshAgent.SetDestination(pos);
        }
    }

    // Targetting Task - attack
    public void SetAttackTarget(BaseEntity target)
    {
        GetComponent<BehaviorTreeAI>().target = target.transform.position;

        if (CanAttack(target) == false)
            return;

        if (CaptureTarget != null)
            StopCapture();

        if (target.GetTeam() != GetTeam()) 
        {
            GetComponent<BehaviorTreeAI>().targetToAttack = target.transform.position;
        }
    }

    // Targetting Task - capture
    public void SetCaptureTarget(TargetBuilding target)
    {
        GetComponent<BehaviorTreeAI>().target = target.transform.position;

        if (CanCapture(target) == false)
            return;

        if (EntityTarget != null)
            EntityTarget = null;

        if (IsCapturing())
            StopCapture();

        if (target.GetTeam() != GetTeam())
        {
            GetComponent<BehaviorTreeAI>().buildingToCapture = target.transform.position;
            //StartCapture(target);
        }
    }

    // Targetting Task - repairing
    public void SetRepairTarget(BaseEntity entity)
    {
        if (CanRepair(entity) == false)
            return;

        if (CaptureTarget != null)
            StopCapture();

        if (entity.GetTeam() == GetTeam())
            StartRepairing(entity);
    }
    public bool CanAttack(BaseEntity target)
    {
        if (target == null)
            return false;

        // distance check
        if ((target.transform.position - transform.position).sqrMagnitude > GetUnitData.AttackDistanceMax * GetUnitData.AttackDistanceMax)
            return false;

        return true;
    }

    // Attack Task
    public void StartAttacking(BaseEntity target)
    {
        EntityTarget = target;
        ComputeAttack();
    }
    public void ComputeAttack()
    {
        if (CanAttack(EntityTarget) == false)
            return;

        //if (NavMeshAgent)
        //    NavMeshAgent.isStopped = true;

        transform.LookAt(EntityTarget.transform);
        // only keep Y axis
        Vector3 eulerRotation = transform.eulerAngles;
        eulerRotation.x = 0f;
        eulerRotation.z = 0f;
        transform.eulerAngles = eulerRotation;

        if ((Time.time - LastActionDate) > UnitData.AttackFrequency)
        {
            LastActionDate = Time.time;
            // visual only ?
            if (UnitData.BulletPrefab)
            {
                GameObject newBullet = Instantiate(UnitData.BulletPrefab, BulletSlot);
                newBullet.transform.parent = null;
                newBullet.GetComponent<Bullet>().ShootToward(EntityTarget.transform.position - transform.position, this);
            }
            // apply damages
            int damages = Mathf.FloorToInt(UnitData.DPS * UnitData.AttackFrequency);
            EntityTarget.AddDamage(damages);
        }
    }
    public bool CanCapture(TargetBuilding target)
    {
        if (target == null)
            return false;

        // distance check
        if ((target.transform.position - transform.position).sqrMagnitude > GetUnitData.CaptureDistanceMax * GetUnitData.CaptureDistanceMax)
            return false;

        return true;
    }

    // Capture Task
    public void StartCapture(TargetBuilding target)
    {
        if (CanCapture(target) == false)
            return;

        CaptureTarget = target;
        CaptureTarget.StartCapture(this);
    }

    public void StartCaptureClose()
    {
        if (CanCapture(closeBuilding) == false)
            return;

        CaptureTarget = closeBuilding;
        CaptureTarget.StartCapture(this);
    }

    public void StopCapture()
    {
        if (CaptureTarget == null)
            return;

        CaptureTarget.StopCapture(this);
        CaptureTarget = null;
    }

    public bool IsCapturing()
    {
        return CaptureTarget != null;
    }

    // Repairing Task
    public bool CanRepair(BaseEntity target)
    {
        if (GetUnitData.CanRepair == false || target == null)
            return false;

        // distance check
        if ((target.transform.position - transform.position).sqrMagnitude > GetUnitData.RepairDistanceMax * GetUnitData.RepairDistanceMax)
            return false;

        return true;
    }
    public void StartRepairing(BaseEntity entity)
    {
        if (GetUnitData.CanRepair)
        {
            EntityTarget = entity;
        }
    }

    // $$$ TODO : add repairing visual feedback
    public void ComputeRepairing()
    {
        if (CanRepair(EntityTarget) == false)
            return;

        //if (NavMeshAgent)
        //    NavMeshAgent.isStopped = true;

        transform.LookAt(EntityTarget.transform);
        // only keep Y axis
        Vector3 eulerRotation = transform.eulerAngles;
        eulerRotation.x = 0f;
        eulerRotation.z = 0f;
        transform.eulerAngles = eulerRotation;

        if ((Time.time - LastActionDate) > UnitData.RepairFrequency)
        {
            LastActionDate = Time.time;

            // apply reparing
            int amount = Mathf.FloorToInt(UnitData.RPS * UnitData.RepairFrequency);
            EntityTarget.Repair(amount);
        }
    }
    #endregion
}
