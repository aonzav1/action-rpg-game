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

    private void Awake()
    {
        _rigidBody = GetComponent<Rigidbody>();
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        ownerEntity = Entity.controllingEntity;
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

    private void OnCollisionEnter(Collision collision)
    {
        if (!base.IsOwner)
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

    private void DeactiveThis()
    {
        Destroy(gameObject);
    }
}
