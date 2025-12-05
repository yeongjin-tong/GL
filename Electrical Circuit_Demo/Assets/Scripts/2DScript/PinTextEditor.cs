using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;
using System.Collections;

[RequireComponent(typeof(TextMeshProUGUI))]
[RequireComponent(typeof(TMP_InputField))]
public class PinTextEditor : MonoBehaviour, IPointerClickHandler
{
    private TMP_InputField inputField;
    private TextMeshProUGUI textDisplay;
    private ConnectionPoint port;

    void Awake()
    {
        inputField = GetComponent<TMP_InputField>();
        textDisplay = GetComponent<TextMeshProUGUI>();
        port = GetComponentInParent<ConnectionPoint>();

        // 초기 설정: 인풋필드는 숨기고 텍스트만 보이게
        inputField.interactable = false;

        // ✨ [수정] 코드로 이벤트 연결
        // 입력이 끝났을 때(엔터 or 포커스 잃음) 실행될 함수 등록
        inputField.onEndEdit.AddListener(OnEndEdit);
    }

    // 메모리 누수 방지를 위해 이벤트 해제
    void OnDestroy()
    {
        if (inputField != null)
        {
            inputField.onEndEdit.RemoveListener(OnEndEdit);
        }
    }

    // 핀 번호를 클릭했을 때 (인터페이스 구현)
    public void OnPointerClick(PointerEventData eventData)
    {
        // 시뮬레이션 중이 아닐 때만 수정 가능하게
        if (!SimulationManager.isSimulating)
        {
            inputField.interactable = true;
            inputField.ActivateInputField(); // 커서 활성화
        }
    }

    // 인터페이스 구현이 아니라, 리스너에 의해 호출되는 일반 함수
    // 입력이 끝났을 때 (엔터 치거나 다른 곳 클릭)
    private void OnEndEdit(string value)
    {
        StartCoroutine(DisableInputNextFrame());
        Debug.Log($"핀 번호 변경됨: {value}");

        port.TextToPinSet(value);
    }

    // 한 프레임 대기 후 비활성화하는 코루틴
    IEnumerator DisableInputNextFrame()
    {
        // 현재 프레임의 이벤트 처리가 다 끝날 때까지 대기
        yield return null;

        // 안전하게 비활성화
        inputField.interactable = false;
    }
}