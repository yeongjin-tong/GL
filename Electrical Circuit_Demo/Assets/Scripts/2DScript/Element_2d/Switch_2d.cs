using System.Net.NetworkInformation;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Switch))]
public class Switch_2d : MonoBehaviour
{
    public Sprite[] state;
    private Switch selfSwitch;
    private Image image;
    private bool stopState = false;

    void Awake()
    {
        selfSwitch = GetComponent<Switch>();

        // selfSwitch의 상태가 바뀔 때마다 UpdateVisuals 함수가 자동으로 호출
        if (selfSwitch != null)
        {
            selfSwitch.OnStateChanged += ClickUIChange;
            selfSwitch.OnStateInit += InitState;
        }
        
        image = GetComponent<Image>();
    }

    private void OnMouseDown()
    {
        if(stopState)
        {
            stopState = false;
        }
        else
        {
            selfSwitch.SetState(!selfSwitch.isOn, true);
        }
    }

    private void OnMouseDrag()
    {
        if (Input.GetMouseButtonDown(1))
        {
            stopState = true;

        }
    }

    private void OnMouseUp()
    {
        if(!stopState)
        {
            selfSwitch.SetState(!selfSwitch.isOn, true);
        }
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

    private void InitState()
    {
        stopState = false;
    }

    private void OnDestroy()
    {
        if (selfSwitch != null)
        {
            selfSwitch.OnStateChanged -= ClickUIChange;
            selfSwitch.OnStateInit -= InitState;
        }
    }
    // (상태에 따른 이미지 변경 로직은 Switch.cs의 OnStateChanged 이벤트를 구독하여 구현 가능)
}