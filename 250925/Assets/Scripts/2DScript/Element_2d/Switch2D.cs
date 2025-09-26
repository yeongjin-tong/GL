using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Switch))]
public class UISwitchController : MonoBehaviour
{
    private Switch selfSwitch;
    private Image image;

    void Awake()
    {
        selfSwitch = GetComponent<Switch>();

        // selfSwitch�� ���°� �ٲ� ������ UpdateVisuals �Լ��� �ڵ����� ȣ��
        if (selfSwitch != null)
        {
            selfSwitch.OnStateChanged += ClickUIChange;
        }
        
        image = GetComponent<Image>();
    }

    private void OnMouseDown()
    {
        // ������ ����ġ�� Toggle �Լ��� ȣ��
        selfSwitch.Toggle();
    }

    private void OnMouseUp()
    {
        // ������ ����ġ�� Toggle �Լ��� ȣ��
        selfSwitch.Toggle();
    }

    private void ClickUIChange(bool isOn)
    {
        if(isOn)
        {
            image.color = new Color32(255, 223, 223, 255);
        }
        else
        {
            image.color = new Color32(255, 255, 255, 255);
        }
    }

    private void OnDestroy()
    {
        if (selfSwitch != null)
        {
            selfSwitch.OnStateChanged -= ClickUIChange;
        }
    }
    // (���¿� ���� �̹��� ���� ������ Switch.cs�� OnStateChanged �̺�Ʈ�� �����Ͽ� ���� ����)
}