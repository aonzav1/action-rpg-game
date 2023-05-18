using FishNet;
using FishNet.Managing;
using FishNet.Object;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
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
    public GameObject projectile;

    public float duration;
}

public class CombatUnit : NetworkBehaviour
{
    [SerializeField] private Transform attackTransform;
    [SerializeField] private Transform fireTransform;
    [SerializeField] private Attack normalAttack;
    [SerializeField] private Attack[] specialAttack;

    private Entity _entity;
    private bool isAttacking;
    private NetworkManager _networkManager;


    private void Awake()
    {
        _entity = GetComponent<Entity>();
        _networkManager = InstanceFinder.NetworkManager;
    }

    public bool IsReadyToAttack()
    {
        return !isAttacking && _entity.IsControllable();
    }
    [ServerRpc(RequireOwnership =false)]
    public void RequestAttack(int num)
    {
        isAttacking = true; 
        if (num == 0)
        {
            NormalAttack();
        }
        else
        {
            SpecialAttack(num);
        }
    }


    [ObserversRpc(RunLocally = true)]
    public virtual void NormalAttack()
    {
        _entity.GetAnim().Play("Attack");
        attackTransform.transform.rotation = _entity.GetCharacterTransform().rotation;
        StartCoroutine(DoAttackSteps(normalAttack));
    }

    [ObserversRpc(RunLocally = true)]
    public virtual void SpecialAttack(int num)
    {
        _entity.GetAnim().Play("Attack_"+num);
        UpdateAttackTransformRotation();
        StartCoroutine(DoAttackSteps(specialAttack[num-1]));
    }

    IEnumerator DoAttackSteps(Attack attack)
    {
        isAttacking = true;

        if (attack.toggleObject)
            attack.toggleObject.SetActive(true);

        yield return new WaitForSeconds(attack.pre_delay);

        for (int i = 0; i < attack.attackSteps.Length; i++)
        {
            UpdateAttackTransformRotation();
            GameObject toggleObject = attack.attackSteps[i].toggleObject;
            if (toggleObject)
                toggleObject.SetActive(true);

            if (base.IsServer && attack.attackSteps[i].projectile)
                SpawnProjectile(attack.attackSteps[i].projectile);

            yield return new WaitForSeconds(attack.attackSteps[i].duration);

            if(toggleObject)
                toggleObject.SetActive(false);
        }

        if (attack.toggleObject)
            attack.toggleObject.SetActive(false);

        yield return new WaitForSeconds(attack.post_delay);

        isAttacking = false;
    }

    private void UpdateAttackTransformRotation()
    {
        attackTransform.transform.rotation = _entity.GetCharacterTransform().rotation;
    }

    [Server]
    public void SpawnProjectile(GameObject target)
    {
        Debug.Log("Received spawn projectile");
        NetworkObject nob = _networkManager.GetPooledInstantiated(target, true);
        nob.transform.SetPositionAndRotation(fireTransform.position, fireTransform.rotation);
        nob.GetComponent<Projectile>().SetOwner(_entity);
        _networkManager.ServerManager.Spawn(nob,base.Owner);
    }

    public bool IsAttacking()
    {
        return isAttacking;
    }
}
