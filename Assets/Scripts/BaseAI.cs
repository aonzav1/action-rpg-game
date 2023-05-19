using FishNet.Object;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SphereCollider))]
public class BaseAI : NetworkBehaviour
{
    [Header("Detection")]
    public Transform target;
    [SerializeField] private Transform detected;
    [SerializeField] private float eyeSight = 30f;
    [SerializeField] private float detectionRange = 20f;
    [SerializeField] private Vector3 eyePosition = new Vector3(0, 1.6f, 0);
    [SerializeField] private float deactivateTime = 5f;
    private float deactivatingTime = 0;
    [SerializeField] private float maxKnittingTime = 1f;
    private float knittingTime = 0;

    private bool isActive;
    private bool isCharging;

    [HideInInspector]
    public MoveUnit moveUnit;
    [HideInInspector]
    public CombatUnit combatUnit;
    [HideInInspector]
    public Entity entity;

    public virtual void Start()
    {
        entity = GetComponent<Entity>();
        moveUnit = GetComponent<MoveUnit>();
        combatUnit = GetComponent<CombatUnit>();
    }


    public void Update()
    {
        if (!base.IsServer)
            return;
        if (!isActive || entity.IsDie())
            return;

        UpdateState();
    }

    public virtual void UpdateState()
    {
        detected = DetectForward();

        UpdateKnittingTime();
        UpdateCooldowns();

        if (knittingTime <= 0)
        {
            target = SeekNewTarget();
            knittingTime = maxKnittingTime;
        }

        if (target == null)
        {
            if (detected)
                target = detected;
            else
                TryStopMove();
        }
        else
        {
            deactivatingTime = 0;
        }
    }

    private void FixedUpdate()
    {
        if (!base.IsServer)
            return;
        if (!isActive)
            return;

        if (target == null)
            return;

        MoveTowardTarget();
    }


    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            isActive = true;
            knittingTime = maxKnittingTime;
            deactivatingTime = 0;
            SeekNewTarget();
        }
    }

    private void UpdateKnittingTime()
    {
        if (detected == null || detected != target)
            knittingTime -= Time.deltaTime;
        else
            knittingTime = maxKnittingTime;
    }

    public Transform SeekNewTarget()
    {
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, detectionRange);
        Collider closestTarget = null;
        float distance = 999f;
        Vector3 dirToBestTarget = Vector3.zero;

        for (int i = 0; i < hitColliders.Length; i++)
        {
            if (!hitColliders[i].gameObject.CompareTag("Player"))
                continue;

            Vector3 dirToTarget = hitColliders[i].transform.position - transform.position;

            float curDistance = dirToTarget.sqrMagnitude;
            if (curDistance < distance)
            {
                closestTarget = hitColliders[i];
                distance = curDistance;
                dirToBestTarget = dirToTarget;
            }
        }
        if (closestTarget != null)
        {
            RaycastHit hit;
            Physics.Raycast(transform.position + eyePosition, dirToBestTarget, out hit);
            {
                if (hit.collider.gameObject.CompareTag("Player"))
                    return hit.collider.transform;
            }
        }
        return null;
    }
    private Transform DetectForward()
    {
        RaycastHit hit;

        if (Physics.Raycast(transform.position + eyePosition, transform.forward, out hit, eyeSight))
        {
            if (hit.collider.gameObject.CompareTag("Player"))
                return hit.collider.transform;
        }
        return null;
    }

    public virtual void UpdateCooldowns()
    {

    }
    public virtual void TryStopMove()
    {
        moveUnit.StopMove();
        deactivatingTime += Time.deltaTime;
        if (deactivatingTime >= deactivateTime)
        {
            Debug.Log("stop move");
            isActive = false;
        }
    }

    public virtual Vector3 MoveTowardTarget()
    {
        Vector3 dir = target.transform.position - transform.position;
        dir = dir.normalized;
        dir.y = 0;
        dir = Vector3.ClampMagnitude(dir, 1);

        if (!combatUnit.IsReadyToAttack())
        {
            if (isCharging)
            {
                moveUnit.DoRotate(dir);
            }
            return dir;
        }

        moveUnit.DoMove(dir);
        return dir;
    }

    public void SetCharging(bool newState)
    {
        isCharging = newState;
    }
}
