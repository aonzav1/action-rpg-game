using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine.Events;
using FishNet.Connection;

public enum EntityState { live, dead}

public class Entity : NetworkBehaviour
{
    public GameObject _character;
    public bool serverAuth;

    [Header("Stats")]
    [SerializeField] private int maxHP = 100;
    [SerializeField] private int maxMP = 100;
    [SerializeField] private int mpRegenPerTick = 40;
    [SerializeField] private float mpRegenInterval = 5;
    [SerializeField] private int maxStamina = 100;
    [SerializeField] private int staminaRegenPerTick = 25;
    [SerializeField] private float staminaRegenInterval = 3;
    [SerializeField] private int expDrop;
    [SerializeField] private int moneyDrop;

    [SyncVar]
    private float hp;
    [SyncVar]
    private float mp;
    [SyncVar]
    private float stamina;

    private int exp;
    private int money;

    private float _prevHp;
    private float _prevMp;
    private float _prevStamina;

    private Animator _animator;
    private Rigidbody _rigidbody;

    private float staminaRegenTimer = 0;
    private float mpRegenTimer = 0;
    private float stunDuration = 0;

    [SyncVar]
    private EntityState _state;

    public UnityAction<float> OnHealthChanged;
    public UnityAction<float> OnManaChanged;
    public UnityAction<float> OnStaminaChanged;
    public UnityAction<int> OnExpChanged;
    public UnityAction<int> OnMoneyChanged;
    public UnityAction<float> OnTakeDamage;
    public UnityAction OnDead;

    public static Entity controllingEntity;

    private void Awake()
    {
        _animator = _character.GetComponent<Animator>();
        _rigidbody = GetComponent<Rigidbody>();
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        if (base.IsOwner)
        {
            UIManager.Get().AssignTracker(this);

            ResetStaminaRegenTimer();
            ResetManaRegenTimer();

            controllingEntity = this;
        }
    }
    public override void OnStartServer()
    {
        base.OnStartServer();
        SetupInitialStats();
        if (serverAuth)
        {
            ResetStaminaRegenTimer();
            ResetManaRegenTimer();
        }
    }

    private void SetupInitialStats()
    {
        hp = maxHP;
        mp = maxMP;
        stamina = maxStamina;
        exp = 0;
        money = 0;
    }

    public void RestoreHealth()
    {
        hp = maxHP;
    }

    private void Update()
    {
        _character.transform.localPosition = new Vector3(0, _character.transform.localPosition.y, 0);

        UpdateHealthGauge();

        if (stunDuration > 0)
        {
            stunDuration -= Time.deltaTime;
        }

        if (base.IsServer)
        {
            mpRegenTimer -= Time.deltaTime;
            if (mpRegenTimer <= 0)
            {
                RegenMana();
            }
        }

        if (!base.IsOwner)
            return;

        staminaRegenTimer -= Time.deltaTime;

        if (staminaRegenTimer <= 0)
        {
            RegenStamina();
        }
        UpdateStaminaGauge();
        UpdateManaGauge();

    }


    public void RegenStamina()
    {
        ResetStaminaRegenTimer();
        stamina += staminaRegenPerTick;
        if (stamina > maxStamina)
            stamina = maxStamina;
    }
    private void ResetStaminaRegenTimer()
    {
        staminaRegenTimer = staminaRegenInterval;
    }
    public void RegenMana()
    {
        ResetManaRegenTimer();
        mp += mpRegenPerTick;
        if (mp > maxMP)
            mp = maxMP;
    }
    private void ResetManaRegenTimer()
    {
        mpRegenTimer = mpRegenInterval;
    }

    public bool ConsumeStamina(float amount)
    {
        if (stamina < amount)
            return false;

        ResetStaminaRegenTimer();
        stamina -= amount;
        UpdateStaminaGauge();

        return true;
    }
    public bool ConsumeMana(float amount)
    {
        if (mp < amount)
            return false;

        mp -= amount;
        UpdateManaGauge();

        return true;
    }


    private void UpdateStaminaGauge()
    {
        if (stamina == _prevStamina)
            return;
        _prevStamina = stamina;
        OnStaminaChanged?.Invoke(stamina / maxStamina);
    }
    private void UpdateManaGauge()
    {
        if (mp == _prevMp)
            return;
        _prevMp = mp;
        OnManaChanged?.Invoke(mp / maxMP);
    }
    private void UpdateHealthGauge()
    {
        if (hp == _prevHp)
            return;
        _prevHp = hp;
        OnHealthChanged?.Invoke(hp / maxHP);
    }


    public Animator GetAnim()
    {
        return _animator;
    }
    public Transform GetCharacterTransform()
    {
        return _character.transform;
    }

    [Server]
    public void GetHit(float damage, Vector3 force,Entity attacker, bool isHeavy =false)
    {
        Debug.Log("Get hit: " + damage + " damage by "+attacker.gameObject.name);
        DisplayHitEffect(isHeavy);

        if (serverAuth)
        {
            _rigidbody.AddForce(force);
            if (isHeavy)
                stunDuration = 2.5f;
            else
                stunDuration = 0.5f;
        }
        else
        {
            DealForceRpc(base.Owner, force, isHeavy);
        }

        OnTakeDamage?.Invoke(damage);

        hp -= damage;

        if (hp <= 0)
        {
            Die(attacker);
        }
    }
    [TargetRpc]
    private void DealForceRpc(NetworkConnection conn,Vector3 force, bool isHeavy)
    {
        _rigidbody.AddForce(force);
        if(isHeavy)
            stunDuration = 2.5f;
        else
            stunDuration = 0.5f;
    }

    [ObserversRpc(RunLocally =true)]
    private void DisplayHitEffect(bool isHeavy)
    {
        if (isHeavy)
        {
            _animator.Play("GetHeavyHit");
        }
        else
        {
            _animator.Play("GetHit");
        }
    }

    [Server]
    private void Die(Entity attacker)
    {
        if (serverAuth)
            _animator.SetBool("isDie", true);
        else
            ShowDeadAnimation(base.Owner);

        _state = EntityState.dead;
        OnDead?.Invoke();
        
        if(!attacker.serverAuth)
            attacker.GiveExpAndMoney(attacker.Owner, expDrop, moneyDrop);

        StartCoroutine(Respawn());
    }
    [TargetRpc]
    private void ShowDeadAnimation(NetworkConnection conn)
    {
        _animator.SetBool("isDie", true);
        _state = EntityState.dead;
    }

    [TargetRpc]
    private void GiveExpAndMoney(NetworkConnection conn, int addedExp, int addedMoney)
    {
        exp += addedExp;
        money += addedMoney;

        OnExpChanged?.Invoke(exp);
        OnMoneyChanged?.Invoke(money);
    }

    private IEnumerator Respawn()
    {
        yield return new WaitForSeconds(5);
        _state = EntityState.live;
        SetupInitialStats();

        if (serverAuth)
        {
            _animator.SetBool("isDie", false);
            GetComponent<FallGuard>()?.Relocate();
        }
        else
            ForceRelocate(Owner);
    }
    [TargetRpc]
    private void ForceRelocate(NetworkConnection conn)
    {
        _animator.SetBool("isDie", false);
        GetComponent<FallGuard>()?.Relocate();
    }

    public bool IsDie()
    {
        return _state == EntityState.dead;
    }
    public bool IsControllable()
    {
        return _state == EntityState.live && stunDuration <= 0;
    }
}
