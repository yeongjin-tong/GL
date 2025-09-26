using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TextEditor : MonoBehaviour
{
    [Tooltip("텍스트 수정을 위해 생성할 팝업창 GameObject 프리팹")]
    public GameObject textEditorPopupPrefab; // ✨ InputField가 아닌 GameObject를 받도록 변경
    public Transform space_2d;
    private GameObject currentPopup;
    private TMP_InputField currentInputField;
    private TextMeshProUGUI targetText; // TextMeshPro 사용을 권장합니다. (일반 Text는 Text로 변경)

    void Start()
    {
        InputManager.Instance.OnDoubleClick += HandleDoubleClick;
    }

    private void HandleDoubleClick(GameObject clickedObject)
    {
        if (clickedObject == null || currentPopup != null) return;

        targetText = clickedObject.GetComponentInChildren<TextMeshProUGUI>();

        if (targetText != null)
        {
            ShowPopup();
        }
    }

    void ShowPopup()
    {
        // 1. 팝업창 프리팹을 생성하고 위치를 설정합니다.
        currentPopup = Instantiate(textEditorPopupPrefab, space_2d);

        // ✨ 2. 팝업창 내부에 있는 InputField와 버튼들을 찾아옵니다.
        //    (이름이 정확해야 합니다. 또는 public 변수로 직접 연결해도 좋습니다.)
        currentInputField = currentPopup.GetComponentInChildren<TMP_InputField>();
        Button confirmButton = currentPopup.transform.Find("ConfirmButton").GetComponent<Button>();
        Button cancelButton = currentPopup.transform.Find("CancelButton").GetComponent<Button>();

        if (currentInputField != null)
        {
            // 3. 기존 텍스트를 InputField에 채우고, 원본 텍스트는 숨깁니다.
            currentInputField.text = targetText.text;
            //targetText.enabled = false;
            currentInputField.ActivateInputField();

            // 4. 각 버튼과 이벤트에 함수를 연결합니다.
            confirmButton.onClick.AddListener(OnConfirm);
            cancelButton.onClick.AddListener(OnCancel);
            currentInputField.onSubmit.AddListener((text) => OnConfirm()); // Enter 키로도 확인
        }
    }

    // '확인' 버튼을 눌렀을 때
    private void OnConfirm()
    {
        if (targetText != null && currentInputField != null)
        {
            targetText.text = currentInputField.text;
        }
        ClosePopup();
    }

    // '취소' 버튼을 눌렀을 때
    private void OnCancel()
    {
        ClosePopup();
    }

    // 팝업창을 닫고 정리하는 함수
    private void ClosePopup()
    {
        if (targetText != null)
        {
            targetText.enabled = true; // 원본 텍스트를 다시 보이게 함
        }
        if (currentPopup != null)
        {
            Destroy(currentPopup);
        }
    }

    private void OnDestroy()
    {
        if (InputManager.Instance != null)
            InputManager.Instance.OnDoubleClick -= HandleDoubleClick;
    }
}