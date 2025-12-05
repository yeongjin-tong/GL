using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NameChange_Popup : MonoBehaviour
{
    public TMP_InputField changeText;
    public Button confirmBtn;
    public Button cancelBtn;

    private ElectricalComponent currentComponent;

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

        if(Input.GetKeyDown(KeyCode.Escape))
        {
            OnCancel();
        }
    }

    public void ReceiveComponent(ElectricalComponent component)
    {
        currentComponent = component;
        changeText.text = currentComponent.symbolTextobj.text;

        changeText.Select();
        changeText.ActivateInputField();
    }

    // 확인 버튼
    private void OnConfirm()
    {
        ModifyingInfo();
        ClosePopup();
    }

    // 취소 버튼
    private void OnCancel()
    {
        ClosePopup();
    }

    // 팝업창을 닫고 정리하는 함수
    private void ClosePopup()
    {
        Destroy(gameObject);
        PopupManager.Instance.PopupInit();
    }

    private void ModifyingInfo()
    {
        currentComponent.NameSetting(changeText.text);
    }
}
