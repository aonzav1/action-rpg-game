using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FishNet.Object;
using Unity.VisualScripting;
using static UnityEditor.ShaderGraph.Internal.KeywordDependentCollection;

[RequireComponent(typeof(SphereCollider))]
public class SimpleAI : NetworkBehaviour
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
    [SerializeField] private float skill_3_CD;
    [SerializeField] private float dodge_CD;
    private float attack_cur_CD;
    private float skill_cur_CD;
    private float dodge_cur_CD;

    private bool isActive;
    private bool isCharging;

    MoveUnit moveUnit;
    CombatUnit combatUnit;

    private void Start()
    {
        moveUnit = GetComponent<MoveUnit>();
        combatUnit = GetComponent<CombatUnit>();
    }

    private void Update()
    {
        if (!base.IsServer)
            return;
        if (!isActive)
            return;

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

        if (!combatUnit.IsReadyToAttack())
        {
            if (isCharging)
            {
                moveUnit.DoRotate(dir);
            }
            return;
        }
   
        moveUnit.DoMove(dir);
        TryDodge(dir);
    }

    private void TryDodge(Vector3 dir)
    {
        if (dodge_cur_CD <= 0)
        {
            float dist = Vector3.Distance(transform.position, target.position);
            dodge_cur_CD = 1f;
            int r = Random.Range(0, 4);
            if (r == 0 && dist >= normalAttackRange && skill_cur_CD < 1)
            {
                Debug.Log("Dodge forward");
                moveUnit.DoDodge(dir);
                dodge_cur_CD = dodge_CD;
            }
            else if (target.GetComponent<CombatUnit>().IsAttacking() && dist <= specialAttackRange)
            {
                if (r == 1)
                {
                    Debug.Log("Dodge backward");
                    moveUnit.DoDodge(-dir);
                }else if (r == 2)
                {
                    Debug.Log("Dodge left");
                    Vector3 rotatedVector = Quaternion.AngleAxis(90, Vector3.left) * dir;
                    moveUnit.DoDodge(rotatedVector);
                }
                else if (r == 3)
                {
                    Debug.Log("Dodge right");
                    Vector3 rotatedVector = Quaternion.AngleAxis(-90, Vector3.left) * dir;
                    moveUnit.DoDodge(rotatedVector);
                }
                dodge_cur_CD = dodge_CD;
            }
        }
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
                    if (dist <= specialAttackRange)
                    {
                        Debug.Log("Skill 1");
                        combatUnit.RequestAttack(1);
                        skill_cur_CD = skill_1_CD;
                    }
                    return;
                case 2:
                    if (dist > normalAttackRange)
                    {
                        Debug.Log("Skill 2");
                        combatUnit.RequestAttack(2);
                        isCharging = true; 
                        skill_cur_CD = skill_2_CD;
                    }
                    return;
                case 3:
                    if (dist <= specialAttackRange)
                    {
                        Debug.Log("Skill 3");
                        combatUnit.RequestAttack(3);
                        isCharging = true;
                        skill_cur_CD = skill_3_CD;
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
        if(dodge_cur_CD > 0)
        {
            dodge_cur_CD -= Time.deltaTime;
        }
        if(attack_cur_CD > 0)
        {
            attack_cur_CD -= Time.deltaTime;
        }
    }

    private void StopMove()
    {
        Debug.Log("stop move");
        isActive = false;
        moveUnit.StopMove();
    }
}
