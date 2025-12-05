using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PopupManager : MonoBehaviour
{
    public static PopupManager Instance { get; private set; }

    public GameObject namePopupPrefab;
    public GameObject timerPopupPrefab;
    public GameObject commonPopupPrefab;
    public Transform popupSpace;

    [HideInInspector]
    public GameObject currentPopup;



    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        currentPopup = null;
    }

    public void ShowCommonPopup(string message, System.Action onConfirmLogic, System.Action onCancelLogic = null)
    {
        if (currentPopup != null) { return; };

        currentPopup = Instantiate(commonPopupPrefab, popupSpace);
        CommonPopup popup = currentPopup.GetComponent<CommonPopup>();

        if(popup != null)
        {
            popup.Setup(message, onConfirmLogic, onCancelLogic);
        }
    }

    public void CreatePopup(GameObject clickObj)
    {
        if (clickObj.GetComponent<ElectricalComponent>() == null || currentPopup != null || clickObj.GetComponent<ElectricalComponent>().symbolTextobj == null) { return; }

        // ✨ 핵심: ITimerControl 인터페이스가 있는지 확인
        // (Timer나 FlickerRelayCoil 등 시간 조절 기능이 있는 모든 부품을 감지함)
        ITimerControl timerControl = clickObj.GetComponent<ITimerControl>();

        if (timerControl != null)
        {
            // 타이머 기능이 있는 경우 -> timerPopupPrefab 생성
            currentPopup = Instantiate(timerPopupPrefab, popupSpace);
            var popup = currentPopup.GetComponent<TimerChange_Popup>();
            if (popup != null)
            {
                // GameObject 자체를 넘겨줘서 내부에서 GetComponent 하게 함
                popup.ReceiveComponent(clickObj);
            }
        }
        else
        {
            // 타이머 기능이 없는 일반 부품 -> namePopupPrefab 생성
            currentPopup = Instantiate(namePopupPrefab, popupSpace);
            var popup = currentPopup.GetComponent<NameChange_Popup>();
            if (popup != null)
            {
                popup.ReceiveComponent(clickObj.GetComponent<ElectricalComponent>());
            }
        }
    }

    public void PopupInit()
    {
        if (currentPopup != null)
        {
            currentPopup = null;
        }
    }
}
