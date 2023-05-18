using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FishNet.Object;

public class Projectile : NetworkBehaviour
{
    [SerializeField] private float damage;
    [SerializeField] private float maxTime = 5f;
    [SerializeField]
    private float initialForce;
    [SerializeField]
    private bool destroyOnHit = false;

    Rigidbody _rigidBody;
    private Entity ownerEntity;
    private float currentTimer = 0;

    List<GameObject> hits = new List<GameObject>();

    private void Awake()
    {
        _rigidBody = GetComponent<Rigidbody>();
    }

    public void SetOwner(Entity shooter)
    {
        ownerEntity = shooter;
    }

    private void Start()
    {
        currentTimer = 0;
        Fire();
    }

    private void Update()
    {
        currentTimer += Time.deltaTime;
        if (currentTimer >= maxTime) { DeactiveThis(); }
    }

    void Fire()
    {
        if (_rigidBody != null)
        {
            _rigidBody.AddForce(transform.forward * initialForce);
        }
    }

    //For once-hit target (IsTrigger = false)
    private void OnCollisionEnter(Collision collision)
    {
        if (!base.IsServer)
            return;

        Entity en = collision.gameObject.GetComponent<Entity>();

        if (en != null)
        {
            if (en == ownerEntity)
                return;
            en.GetHit(damage, Vector3.zero, ownerEntity);
        }
        if (destroyOnHit)
        {
            DeactiveThis();
        }
    }

    //Support multitarget (IsTrigger = true)
    private void OnTriggerStay(Collider other)
    {
        if (!base.IsServer)
            return;

        Entity en = other.GetComponent<Entity>();

        if (en != null)
        {
            if (en == ownerEntity)
                return;
            if (hits.Contains(other.gameObject))
                return;
            hits.Add(other.gameObject);
            en.GetHit(damage, Vector3.zero, ownerEntity);
        }
        if (destroyOnHit)
        {
            DeactiveThis();
        }
    }

    private void DeactiveThis()
    {
        if (!base.IsDeinitializing)
        {
            Debug.Log("Despawn");
            base.Despawn(gameObject);
        }
    }
}
