using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [SerializeField] private Image healthBar;
    [SerializeField] private Image manaBar;
    [SerializeField] private Image staminaBar;

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
}
