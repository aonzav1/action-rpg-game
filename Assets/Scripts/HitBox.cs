using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using FishNet.Object;

public class HitBox : NetworkBehaviour
{
    [SerializeField] private float damage;
    [SerializeField] private bool isHeavy;
    [SerializeField] private Entity entity;

    [SerializeField] private float hitForce;
    [SerializeField]
    private bool calculateForceFromBoxOrigin = true;

    List<GameObject> hits = new List<GameObject>();

    public void OnTriggerStay(Collider other)
    {
        if (other.isTrigger)
            return;

        Entity en = other.GetComponent<Entity>();

        if (en == null)
            return;

        if (en == entity)
            return;

        if (hits.Contains(other.gameObject))
            return;

        hits.Add(other.gameObject);

        if (!base.IsServer)
            return;
        
        en.GetHit(damage, CalculateHitForce(other.transform.position), entity,isHeavy);
    }

    private Vector3 CalculateHitForce(Vector3 targetPos)
    {
        if(calculateForceFromBoxOrigin)
            return (targetPos - transform.position).normalized * hitForce;
        else
            return (targetPos - entity.transform.position).normalized * hitForce;
    }

    private void OnDisable()
    {
        hits.Clear();
    }
}
