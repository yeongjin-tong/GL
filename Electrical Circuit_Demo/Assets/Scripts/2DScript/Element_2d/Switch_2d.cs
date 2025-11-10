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

    // stopState 변수를 유지합니다.
    private bool stopState = false;

    void Awake()
    {
        selfSwitch = GetComponent<Switch>();

        if (selfSwitch != null)
        {
            selfSwitch.OnStateChanged += ClickUIChange;
            selfSwitch.OnStateInit += InitState;
        }

        image = GetComponent<Image>();
    }

    /// <summary>
    /// [수정] 마우스를 누르면 "활성" 상태가 됩니다.
    /// </summary>
    private void OnMouseDown()
    {
        // 1. 만약 스위치가 "고정"된 상태였다면 (이전 클릭에서 우클릭됨)
        if (stopState)
        {
            // 이번 클릭은 "고정 해제"로만 작동합니다. (활성화되지 않음)
            stopState = false;
        }
        else
        {
            // 2. "고정"되지 않았다면, 그룹을 "활성" 상태로 만듭니다.
            selfSwitch.TriggerGroupState(true);
        }
    }

    /// <summary>
    /// [유지] OnMouseDrag는 "고정" 기능만 담당합니다.
    /// </summary>
    private void OnMouseDrag()
    {
        // 왼쪽 클릭 중에 오른쪽 클릭을 감지하면
        if (Input.GetMouseButtonDown(1))
        {
            // "고정" 상태로 만듭니다.
            stopState = true;
        }
    }

    /// <summary>
    /// [수정] 마우스를 떼면 *항상* "비활성(기본)" 상태로 돌아갑니다.
    /// </summary>
    private void OnMouseUp()
    {
        if(!stopState)
        {
            selfSwitch.TriggerGroupState(false);
        }
    }

    private void ClickUIChange(bool isOn)
    {
        if (state != null && state.Length > 1 && image != null) // 안전 장치 추가
        {
            image.sprite = isOn ? state[1] : state[0];
        }
    }

    /// <summary>
    /// 시뮬레이션이 중지되면 "고정" 상태를 해제합니다.
    /// </summary>
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
}