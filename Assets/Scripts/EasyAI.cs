using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FishNet.Object;
using System.Linq;

[RequireComponent(typeof(SphereCollider))]
public class EasyAI : NetworkBehaviour
{
    [Header("Detection")]
    [SerializeField] private Transform target;
    [SerializeField] private Transform detected;
    [SerializeField] private float eyeSight = 30f;
    [SerializeField] private float detectionRange = 15f;
    [SerializeField] private Vector3 eyePosition = new Vector3(0,1.5f,0);

    [SerializeField] private float maxKnittingTime = 5;
    private float knittingTime = 0;

    [Header("Attack")]
    [SerializeField] private float normalAttackRange;
    [SerializeField] private float normalAttackCD;
    [SerializeField] private float specialAttackRange;
    [SerializeField] private float skill_1_CD;
    [SerializeField] private float skill_2_CD;
    private float attack_cur_CD;
    private float skill_cur_CD;

    private bool isActive;
    private bool isCharging;
    private bool isRunningAround;
    private int reachedPoint;
    private int pointsToReach;

    [Header("For running around mechanic")]
    [SerializeField] private float runTriggerHPPercent = 0.2f;
    [SerializeField] private List<Transform> runPoints;

    [Header("For 50% heal mechanic")]
    [SerializeField] private float healTriggerHPPercent = 0.5f;
    private bool isHealed;

    MoveUnit moveUnit;
    CombatUnit combatUnit;
    Entity entity;

    private void Start()
    {
        entity = GetComponent<Entity>();
        moveUnit = GetComponent<MoveUnit>();
        combatUnit = GetComponent<CombatUnit>();
        entity.OnHealthChanged += OnHealthChanged;
        pointsToReach = runPoints.Count();
    }

    private void Update()
    {
        if (!base.IsServer)
            return;
        if (!isActive)
            return;
        if (isRunningAround && reachedPoint != pointsToReach)
        {
            HandleReachPoint();
            return;
        }

        detected = DetectForward();

        UpdateKnittingTime();
        UpdateCooldowns();

        if (knittingTime <= 0)
        {
            target = SeekNewTarget();
            if(target == null && detected == null)
            {
                StopMove();
                return;
            }
            knittingTime = maxKnittingTime;
        }

        if (target == null)
        {
            if (detected)
                target = detected;
            return;
        }
        TryAttackPlayer();
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

    private Transform SeekNewTarget()
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

    private void MoveTowardTarget()
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
            return;
        }
   
        moveUnit.DoMove(dir);
    }


    private void TryAttackPlayer()
    {
        if (!combatUnit.IsReadyToAttack())
        {
            return;
        }
        isCharging = false;

        float dist = Vector3.Distance(transform.position, target.position);

        if(skill_cur_CD <= 0)
        {
            int r = Random.Range(0, 5);
            skill_cur_CD = 1;
            switch (r)
            {
                case 1:
                    if (dist <= normalAttackRange)
                    {
                        Debug.Log("Skill 1");
                        combatUnit.OnAttackDone += OnAttackDone;
                        combatUnit.RequestAttack(0);
                        skill_cur_CD = skill_1_CD;
                    }
                    return;
                case 2:
                    if (dist <= specialAttackRange)
                    {
                        Debug.Log("Skill 2");
                        combatUnit.RequestAttack(1);
                        isCharging = true; 
                        skill_cur_CD = skill_2_CD;
                    }
                    return;
            }
        }
        if (dist <= normalAttackRange && attack_cur_CD <= 0)
        {
            combatUnit.RequestAttack(0);
            attack_cur_CD = normalAttackCD;
        }
    }

    private void UpdateCooldowns()
    {
        if(skill_cur_CD > 0)
        {
            skill_cur_CD -= Time.deltaTime;
        }
        if(attack_cur_CD > 0)
        {
            attack_cur_CD -= Time.deltaTime;
        }
    }

    private void OnAttackDone(bool isSuccess)
    {
        Debug.Log("OnAttackdone");
        combatUnit.OnAttackDone -= OnAttackDone;
        if (!isSuccess)
            return;
        JumpBackward();
    }

    private void JumpBackward()
    {
        Debug.Log("Jumpback");
        if (target == null)
            return;
        Vector3 dir = target.transform.position - transform.position;
        dir = dir.normalized;
        dir.y = 0;
        dir = Vector3.ClampMagnitude(dir, 1);

        moveUnit.DoDodge(-dir);
    }

    private void StopMove()
    {
        Debug.Log("stop move");
        isActive = false;
        moveUnit.StopMove();
    }

    private void OnHealthChanged(float percent)
    {
        if(percent <= healTriggerHPPercent)
        {
            SecondLife();
        }
        if (percent <= runTriggerHPPercent)
        {
            RunAround();
        }
    }

    private void RunAround()
    {
        if (isRunningAround)
            return;
        isRunningAround = true;
        target = RandomRunPoint();
    }

    private Transform RandomRunPoint(Transform remove=null)
    {
        runPoints.Remove(remove);
        return runPoints[Random.Range(0, runPoints.Count)];
    }

    private void SecondLife()
    {
        if (isHealed)
            return;
        Debug.Log("SECOND LIFE");
        isHealed = true;
        moveUnit.DoJump();
        entity.RestoreHealth();
    }

    private void HandleReachPoint()
    {
        float dist = Vector3.Distance(transform.position, target.position);
        if (dist < 0.1f)
        {
            Debug.Log("Point reach");
            reachedPoint += 1;
            if (reachedPoint == pointsToReach)
                target = SeekNewTarget();
            else
                target = RandomRunPoint(target);
        }
    }
}
