using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine.Events;

public class Entity : NetworkBehaviour
{
    public GameObject _character;

    [Header("Stats")]
    [SerializeField] private int maxHP = 100;
    [SerializeField] private int maxMP = 100;
    [SerializeField] private int mpRegenPerTick = 40;
    [SerializeField] private float mpRegenInterval = 5;
    [SerializeField] private int maxStamina = 100;
    [SerializeField] private int staminaRegenPerTick = 25;
    [SerializeField] private float staminaRegenInterval = 3;

    [SyncVar]
    private float hp;
    private float mp;
    private float stamina;

    private Animator _animator;

    private float staminaRegenTimer = 0;
    private float mpRegenTimer = 0;

    public UnityAction<float> OnHealthChanged;
    public UnityAction<float> OnManaChanged;
    public UnityAction<float> OnStaminaChanged;

    private void Awake()
    {
        _animator = _character.GetComponent<Animator>();
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        if (base.IsOwner)
        {
            UIManager.Get().AssignTracker(this);
            UpdateHealthGauge();
            UpdateManaGauge();
            UpdateStaminaGauge();

            ResetStaminaRegenTimer();
            ResetManaRegenTimer();
        }
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        SetupInitialStats();
    }

    private void SetupInitialStats()
    {
        hp = maxHP;
        mp = maxMP;
        stamina = maxStamina;

        UpdateHealthGauge();
        UpdateManaGauge();
        UpdateStaminaGauge();
    }

    private void Update()
    {
        _character.transform.localPosition = new Vector3(0, _character.transform.localPosition.y, 0);

        if (!base.IsOwner)
            return;

        staminaRegenTimer -= Time.deltaTime;
        mpRegenTimer -= Time.deltaTime;

        if (staminaRegenTimer <= 0)
        {
            RegenStamina();
        }
        if (mpRegenTimer <= 0)
        {
            RegenMana();
        }
    }

    public void RegenStamina()
    {
        ResetStaminaRegenTimer();
        stamina += staminaRegenPerTick;
        if (stamina > maxStamina)
            stamina = maxStamina;
        UpdateStaminaGauge();
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
        UpdateManaGauge();
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
        OnStaminaChanged?.Invoke(stamina / maxStamina);
    }
    private void UpdateManaGauge()
    {
        OnManaChanged?.Invoke(mp / maxMP);
    }
    private void UpdateHealthGauge()
    {
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
}
