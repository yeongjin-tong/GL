using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Battery))]
public class Battery_2d : MonoBehaviour
{
    private Battery selfBattery;
    private Image image;
    // public Sprite onSprite, offSprite; // 켬/끔 스프라이트

    void Awake()
    {
        selfBattery = GetComponent<Battery>();
        if (selfBattery != null)
        {
            selfBattery.OnStateChanged += UpdateVisuals;
        }
        image = GetComponent<Image>();
    }

    private void OnMouseDown() 
    {
        selfBattery.ToggleEvent();
    }

    private void UpdateVisuals(bool isOn)
    {
        // 예시: 색상 변경
        image.color = isOn ? Color.yellow : Color.white;
        // 또는 스프라이트 변경
        // image.sprite = isOn ? onSprite : offSprite;
    }

    private void OnDestroy()
    {
        if (selfBattery != null)
            selfBattery.OnStateChanged -= UpdateVisuals;
    }
}