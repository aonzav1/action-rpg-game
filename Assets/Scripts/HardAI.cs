using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FishNet.Object;

public class HardAI : BaseAI
{
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

    public override void UpdateState()
    {
        base.UpdateState();
        TryAttackPlayer();
    }

    public override Vector3 MoveTowardTarget()
    {
        Vector3 dir = base.MoveTowardTarget();

        TryDodge(dir);

        return dir;
    }

    private void TryDodge(Vector3 dir)
    {
        if (target == null || !combatUnit.IsReadyToAttack())
        {
            return;
        }
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
        if ( target == null || !combatUnit.IsReadyToAttack())
        {
            return;
        }
        SetCharging(false);

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
                        SetCharging(true);
                        skill_cur_CD = skill_2_CD;
                    }
                    return;
                case 3:
                    if (dist <= specialAttackRange)
                    {
                        Debug.Log("Skill 3");
                        combatUnit.RequestAttack(3);
                        SetCharging(true);
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

    public override void UpdateCooldowns()
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
}
