using UnityEngine;

// 이 스크립트는 반드시 Switch.cs와 함께 있어야 함
[RequireComponent(typeof(Switch))]
public class Switch3D : MonoBehaviour
{
    private Switch selfSwitch;
    public Transform objectButton;

    void Awake()
    {
        selfSwitch = GetComponent<Switch>();

        // selfSwitch의 상태가 바뀔 때마다 UpdateVisuals 함수가 자동으로 호출
        if (selfSwitch != null)
        {
            selfSwitch.OnStateChanged += UpdateVisuals;
        }
    }

    void Start()
    {
        if (selfSwitch != null)
        {
            UpdateVisuals(selfSwitch.isOn);
        }
    }

    // 3D 오브젝트가 클릭되었을 때
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

    private void UpdateVisuals(bool isOn)
    {
        if(isOn)
        {
            objectButton.localPosition = new Vector3(objectButton.localPosition.x, objectButton.localPosition.y, 0.03f);
        }
        else
        {
            objectButton.localPosition = new Vector3(objectButton.localPosition.x, objectButton.localPosition.y, 0.034f);
        }
    }

    // 오브젝트가 파괴될 때 구독 해제
    private void OnDestroy()
    {
        if (selfSwitch != null)
        {
            selfSwitch.OnStateChanged -= UpdateVisuals;
        }
    }

}