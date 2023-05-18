using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FishNet.Object;

public class FallGuard : NetworkBehaviour
{
    [SerializeField] private bool clientAuthority = true;
    [SerializeField] private float fallTheshold = -5f;
    private Vector3 spawnPoint;

    private void Awake()
    {
        spawnPoint = transform.position;
    }

    void Update()
    {
        if (clientAuthority && !base.IsOwner)
            return;
        if (!clientAuthority && !base.IsServer)
            return;

        if (transform.position.y < fallTheshold)
        {
            ReturnToSpawnPoint();
        }
    }

    void ReturnToSpawnPoint()
    {
        transform.position = spawnPoint;
    }
}
