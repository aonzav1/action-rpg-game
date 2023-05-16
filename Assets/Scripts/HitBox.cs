using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HitBox : MonoBehaviour
{
    [SerializeField] private float damage;
    [SerializeField] private Entity entity;

    List<GameObject> hits = new List<GameObject>();

    public void OnTriggerStay(Collider other)
    {
        Entity en = other.GetComponent<Entity>();

        if (en == null)
            return;

        if (hits.Contains(other.gameObject))
            return;

        hits.Add(other.gameObject);
        en.GetHit(damage, entity);
    }

    private void OnDisable()
    {
        hits.Clear();
    }
}
