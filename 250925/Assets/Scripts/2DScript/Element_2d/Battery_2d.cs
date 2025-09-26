using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Battery))]
public class Battery_2d : MonoBehaviour
{
    private Battery selfBattery;
    private Image image;
    // public Sprite onSprite, offSprite; // ��/�� ��������Ʈ

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
        // ����: ���� ����
        image.color = isOn ? Color.yellow : Color.white;
        // �Ǵ� ��������Ʈ ����
        // image.sprite = isOn ? onSprite : offSprite;
    }

    private void OnDestroy()
    {
        if (selfBattery != null)
            selfBattery.OnStateChanged -= UpdateVisuals;
    }
}