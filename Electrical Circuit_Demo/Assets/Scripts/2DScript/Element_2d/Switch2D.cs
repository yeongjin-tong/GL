using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Switch))]
public class UISwitchController : MonoBehaviour
{
    public Sprite[] state;
    private Switch selfSwitch;
    private Image image;

    void Awake()
    {
        selfSwitch = GetComponent<Switch>();

        // selfSwitch의 상태가 바뀔 때마다 UpdateVisuals 함수가 자동으로 호출
        if (selfSwitch != null)
        {
            selfSwitch.OnStateChanged += ClickUIChange;
        }
        
        image = GetComponent<Image>();
    }

    private void OnMouseDown()
    {
        // 마스터 스위치의 Toggle 함수를 호출
        selfSwitch.Toggle();
    }

    private void OnMouseUp()
    {
        // 마스터 스위치의 Toggle 함수를 호출
        selfSwitch.Toggle();
    }

    private void ClickUIChange(bool isOn)
    {
        if(state != null)
        {
            if (isOn)
            {
                image.sprite = state[1];
            }
            else
            {
                image.sprite = state[0];
            }
        }
    }

    private void OnDestroy()
    {
        if (selfSwitch != null)
        {
            selfSwitch.OnStateChanged -= ClickUIChange;
        }
    }
    // (상태에 따른 이미지 변경 로직은 Switch.cs의 OnStateChanged 이벤트를 구독하여 구현 가능)
}