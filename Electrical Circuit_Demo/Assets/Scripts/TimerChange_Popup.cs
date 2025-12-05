using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TimerChange_Popup : MonoBehaviour
{
    public TMP_InputField changeNameText;
    public TMP_InputField changeTimerText;
    public Button confirmBtn;
    public Button cancelBtn;

    // 이름 변경을 위해 기본 컴포넌트 저장
    private ElectricalComponent targetComponent;
    // 시간 변경을 위해 인터페이스 저장 (Timer든 Flicker든 여기에 다 들어감)
    private ITimerControl targetTimer;

    private void Start()
    {
        confirmBtn.onClick.AddListener(OnConfirm);
        cancelBtn.onClick.AddListener(OnCancel);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            OnConfirm();
        }

        if(Input.GetKeyDown(KeyCode.Tab))
        {
            if(changeNameText.isFocused)
            {
                changeTimerText.Select();
            }
            else
            {
                changeNameText.Select();
            }
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            OnCancel();
        }
    }

    // ✨ 팝업을 열 때 데이터를 받는 함수
    public void ReceiveComponent(GameObject obj)
    {
        // 1. 기본 컴포넌트 (이름용) 가져오기
        targetComponent = obj.GetComponent<ElectricalComponent>();

        // 2. 타이머 인터페이스 (시간용) 가져오기
        targetTimer = obj.GetComponent<ITimerControl>();

        // 3. UI에 현재 값 표시
        if (targetComponent != null)
        {
            changeNameText.text = targetComponent.symbolTextobj.text;
        }

        if (targetTimer != null)
        {
            changeTimerText.text = targetTimer.GetTime().ToString();
        }

        changeNameText.Select();
        changeNameText.ActivateInputField();
    }

    private void OnConfirm()
    {
        // 1. 이름 저장
        if (targetComponent != null)
        {
            targetComponent.NameSetting(changeNameText.text);
        }

        // 2. 시간 저장 (Timer든 Flicker든 상관없이 동작함)
        if (targetTimer != null)
        {
            if (float.TryParse(changeTimerText.text, out float newTime))
            {
                targetTimer.SetTime(newTime);
            }
        }

        ClosePopup();
    }

    private void OnCancel()
    {
        ClosePopup();
    }

    private void ClosePopup()
    {
        Destroy(gameObject);
        PopupManager.Instance.PopupInit();
    }
}