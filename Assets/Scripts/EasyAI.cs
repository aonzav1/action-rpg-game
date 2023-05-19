using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FishNet.Object;
using System.Linq;

public class EasyAI : BaseAI
{
    [Header("Attack")]
    [SerializeField] private float normalAttackRange;
    [SerializeField] private float normalAttackCD;
    [SerializeField] private float specialAttackRange;
    [SerializeField] private float skill_1_CD;
    [SerializeField] private float skill_2_CD;
    private float attack_cur_CD;
    private float skill_cur_CD;

    private bool isRunningAround;
    private int reachedPoint;
    private int pointsToReach;

    [Header("For running around mechanic")]
    [SerializeField] private float runTriggerHPPercent = 0.2f;
    [SerializeField] private List<Transform> runPoints;

    [Header("For 50% heal mechanic")]
    [SerializeField] private float healTriggerHPPercent = 0.5f;
    private bool isHealed;

    public override void Start()
    {
        base.Start();
        entity.OnHealthChanged += OnHealthChanged;
        entity.OnDead += OnDead;
        pointsToReach = runPoints.Count();
    }

    public override void UpdateState()
    {
        if (isRunningAround && reachedPoint != pointsToReach)
        {
            HandleReachPoint();
            return;
        }
        base.UpdateState();
        TryAttackPlayer();
    }

    private void TryAttackPlayer()
    {
        if (target == null || !combatUnit.IsReadyToAttack())
        {
            return;
        }
        SetCharging(false);

        float dist = Vector3.Distance(transform.position, target.position);

        if(skill_cur_CD <= 0)
        {
            int r = Random.Range(0, 3);
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
                        Debug.Log(combatUnit.IsReadyToAttack());
                        Debug.Log("Skill 2");
                        combatUnit.RequestAttack(1);
                        SetCharging(true);
                        skill_cur_CD = skill_2_CD;
                    }
                    return;
            }
        }
        if (dist <= normalAttackRange && attack_cur_CD <= 0)
        {
            Debug.Log(combatUnit.IsReadyToAttack());
            Debug.Log("Normal attack");
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
        attack_cur_CD = normalAttackCD;
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

    private Transform RandomRunPoint()
    {
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
                target = RandomRunPoint();
        }
    }
    private void OnDead()
    {
        reachedPoint = 0;
        isRunningAround = false;
        isHealed = false;
    }

}
