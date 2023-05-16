using FishNet.Object;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CombatUnit : NetworkBehaviour
{
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
        StartCoroutine(AttackScenario(1));
    }

    [ObserversRpc]
    public virtual void SpecialAttack(int num)
    {
        _entity.GetAnim().Play("Attack_"+num);
    }

    IEnumerator AttackScenario(float delay)
    {
        isAttacking = true;
        yield return new WaitForSeconds(delay);
        isAttacking = false;
    }
}
