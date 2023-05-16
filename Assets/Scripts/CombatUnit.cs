using FishNet.Object;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct Attack
{
    public string attackName;
    public GameObject toggleObject;
    public float pre_delay;
    public float post_delay;
    public AttackTimeStep[] attackSteps;
}
[Serializable]
public struct AttackTimeStep
{
    public GameObject toggleObject;
    //public float damage;
    public float duration;
}

public class CombatUnit : NetworkBehaviour
{
    [SerializeField] private Attack normalAttack;
    [SerializeField] private Attack[] specialAttack;

    private Entity _entity;
    private bool isAttacking;


    private void Awake()
    {
        _entity = GetComponent<Entity>();
    }

    public bool IsAttacking()
    {
        return isAttacking;
    }

    [ObserversRpc]
    public virtual void NormalAttack()
    {
        _entity.GetAnim().Play("Attack");
        StartCoroutine(DoAttackSteps(normalAttack));
    }

    [ObserversRpc]
    public virtual void SpecialAttack(int num)
    {
        _entity.GetAnim().Play("Attack_"+num);
    }

    IEnumerator DoAttackSteps(Attack attack)
    {
        isAttacking = true;

        if (attack.toggleObject)
            attack.toggleObject.SetActive(true);

        yield return new WaitForSeconds(attack.pre_delay);

        for (int i = 0; i < attack.attackSteps.Length; i++)
        {
            GameObject toggleObject = attack.attackSteps[i].toggleObject;
            if (toggleObject)
                toggleObject.SetActive(true);

            yield return new WaitForSeconds(attack.attackSteps[i].duration);

            if(toggleObject)
                toggleObject.SetActive(false);
        }

        if (attack.toggleObject)
            attack.toggleObject.SetActive(false);

        yield return new WaitForSeconds(attack.post_delay);

        isAttacking = false;
    }
}
