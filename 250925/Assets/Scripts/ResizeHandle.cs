using UnityEngine;
using UnityEngine.EventSystems;

// ✨ 드래그 '시작'을 감지하기 위해 IPointerDownHandler 추가
public class ResizeHandle : MonoBehaviour, IDragHandler, IPointerDownHandler
{
    private ResizableWindow window;
    private RectTransform windowRect;

    void Start()
    {
        //Debug.Log("1. ResizeHandle 스크립트가 깨어났습니다 (Awake).");
        window = GetComponentInParent<ResizableWindow>();
        if (window != null)
        {
            //Debug.Log("2. 부모 윈도우(ResizableWindow)를 성공적으로 찾았습니다.");
            windowRect = window.GetRectTransform();
        }
        else
        {
            //Debug.LogError("오류: 부모에서 ResizableWindow 스크립트를 찾지 못했습니다! 계층 구조를 확인하세요.");
        }
    }

    // ✨ 3. 클릭이 감지되는지 확인하는 부분
    public void OnPointerDown(PointerEventData eventData)
    {
        //Debug.Log("3. ResizeHandle에 대한 클릭이 감지되었습니다! (OnPointerDown)", this.gameObject);
    }

    public void OnDrag(PointerEventData eventData)
    {
        //Debug.Log("4. 드래그가 실행 중입니다... (OnDrag)");

        if (windowRect != null)
        {
            Vector2 newSize = windowRect.sizeDelta + new Vector2(eventData.delta.x, -eventData.delta.y);
            newSize.x = Mathf.Clamp(newSize.x, window.minSize.x, window.maxSize.x);
            newSize.y = Mathf.Clamp(newSize.y, window.minSize.y, window.maxSize.y);
            windowRect.sizeDelta = newSize;
        }
        else
        {
            Debug.LogError("오류: windowRect가 비어있어 크기를 조절할 수 없습니다.");
        }
    }
}