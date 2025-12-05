using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(MCCB))]
public class MCCB_2d : MonoBehaviour
{
    public Sprite[] state; // 0: OFF, 1: ON
    private MCCB selfSwitch;
    private Image image;

    void Awake()
    {
        selfSwitch = GetComponent<MCCB>();
        image = GetComponent<Image>();

        if (selfSwitch != null)
        {
            selfSwitch.OnStateChanged += ClickUIChange;
            selfSwitch.OnStateInit += InitState;
        }
    }

    // ✨ [수정] 마우스를 누를 때 "토글(반전)" 실행
    private void OnMouseDown()
    {
        // MCCB는 토글 방식이므로 누를 때마다 켜짐/꺼짐이 바뀝니다.
        selfSwitch.TriggerGroupToggle();
    }

    // ✨ [수정] 마우스를 뗄 때는 아무 동작도 하지 않음 (상태 유지)
    // private void OnMouseUp() { } 

    private void ClickUIChange(bool isOn)
    {
        if (state != null && state.Length > 1 && image != null)
        {
            image.sprite = isOn ? state[1] : state[0];
        }
    }

    private void InitState()
    {
        // 초기화 시 필요한 로직이 있다면 추가
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