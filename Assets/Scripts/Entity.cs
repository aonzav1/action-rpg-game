using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FishNet.Object;
using FishNet.Object.Synchronizing;

public class Entity : NetworkBehaviour
{
    public GameObject _character;

    [Header("Stats")]
    [SerializeField] private int maxHP;
    [SerializeField] private int maxMP;

    [SyncVar]
    private int HP;
    [SyncVar]
    private int MP;

    private Animator _animator;

    private void Awake()
    {
        _animator = _character.GetComponent<Animator>();
    }

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

    private void Update()
    {
        _character.transform.localPosition = new Vector3(0, _character.transform.localPosition.y, 0);
    }


    public Animator GetAnim()
    {
        return _animator;
    }

    public Transform GetCharacterTransform()
    {
        return _character.transform;
    }
}
