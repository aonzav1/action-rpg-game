using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [SerializeField] private Image healthBar;
    [SerializeField] private Image manaBar;
    [SerializeField] private Image staminaBar;

    [SerializeField] private TextMeshProUGUI expTxt;
    [SerializeField] private TextMeshProUGUI moneyTxt;

    private CanvasGroup _canvasGroup;
    private Entity _targetEntity;

    public static UIManager instance;

    private void Awake()
    {
        instance = this;
        _canvasGroup = GetComponent<CanvasGroup>();
        _canvasGroup.alpha = 0;
    }
    public static UIManager Get()
    {
        return instance;
    }

    public void AssignTracker(Entity target)
    {
        _targetEntity = target;
        _canvasGroup.alpha = 1;

        target.OnHealthChanged += UpdateHPFill;
        target.OnManaChanged += UpdateMPFill;
        target.OnStaminaChanged += UpdateStaminaFill;

        target.OnExpChanged += UpdateExp;
        target.OnMoneyChanged += UpdateMoney;
    }

    public void UpdateHPFill(float value)
    {
        healthBar.fillAmount = value;
    }
    public void UpdateMPFill(float value)
    {
        manaBar.fillAmount = value;
    }
    public void UpdateStaminaFill(float value)
    {
        staminaBar.fillAmount = value;
    }
    public void UpdateExp(int value)
    {
        expTxt.text = value.ToString();
    }
    public void UpdateMoney(int value)
    {
        moneyTxt.text = value.ToString();
    }
}
