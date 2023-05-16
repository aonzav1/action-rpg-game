using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FishNet.Object;

public class FallGuard : NetworkBehaviour
{
    [SerializeField] private float fallTheshold = -5f;
    private Vector3 spawnPoint;

    private void Awake()
    {
        spawnPoint = transform.position;
    }


    void Update()
    {
        if (!base.IsOwner)
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
