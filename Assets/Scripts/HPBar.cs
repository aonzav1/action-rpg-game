using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HPBar : MonoBehaviour
{
    [SerializeField] private Image fillImage;
    [SerializeField]
    private Entity _entity;

    private void Start()
    {
        _entity.OnHealthChanged += UpdateBar;
    }
    public void UpdateBar(float percent)
    {
        fillImage.fillAmount = percent;
        fillImage.transform.parent.gameObject.SetActive(percent > 0);
    }
    private void OnDestroy()
    {
        _entity.OnHealthChanged -= UpdateBar;
    }
}
