using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FishNet.Object;
using FishNet.Object.Synchronizing;

public class Entity : NetworkBehaviour
{
    [Header("Stats")]
    [SerializeField] private int maxHP;
    [SerializeField] private int maxMP;
    [SerializeField] private float speed;
    [SerializeField] private float turnRate;

    [SyncVar]
    private int HP;
    [SyncVar]
    private int MP;

    public override void OnStartServer()
    {
        base.OnStartServer();
        SetupInitialStats();
    }

    private void SetupInitialStats()
    {
        HP = maxHP;
        MP = maxMP;
    }

    public float GetSpeed()
    {
        return speed;
    }

    public float GetTurnRate()
    {
        return turnRate;
    }
}
