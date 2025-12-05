using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System; // Action을 사용하기 위해 필요

public class CommonPopup : MonoBehaviour
{
    [Header("UI Components")]
    public TextMeshProUGUI messageText; // "종료하시겠습니까?" 등이 들어갈 텍스트
    public Button confirmBtn;
    public Button cancelBtn;

    // 확인, 취소 버튼을 눌렀을 때 실행될 함수를 저장할 변수
    private Action onConfirmAction;
    private Action onCancelAction;

    private void Awake()
    {
        // 버튼에 리스너 연결
        confirmBtn.onClick.AddListener(OnConfirmClicked);
        cancelBtn.onClick.AddListener(OnCancelClicked);
    }

    /// <summary>
    /// 팝업을 초기화하고 내용을 설정하는 함수 (외부에서 호출)
    /// </summary>
    /// <param name="message">팝업에 표시할 메시지</param>
    /// <param name="onConfirm">확인 버튼 클릭 시 실행할 함수 (람다식 등)</param>
    public void Setup(string message, Action onConfirm, Action onCancel = null)
    {
        messageText.text = message;
        onConfirmAction = onConfirm;
        onCancelAction = onCancel;
    }

    private void OnConfirmClicked()
    {
        // 저장된 행동(함수)이 있다면 실행
        onConfirmAction?.Invoke();
        ClosePopup();
    }

    private void OnCancelClicked()
    {
        onCancelAction?.Invoke();
        ClosePopup();
    }

    private void ClosePopup()
    {
        Destroy(gameObject);
        PopupManager.Instance.PopupInit();
    }
}