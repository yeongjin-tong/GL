using UnityEngine;
using UnityEngine.EventSystems;

public class DragHandle : MonoBehaviour, IDragHandler
{
    private ResizableWindow window;

    void Awake()
    {
        // 부모 오브젝트에서 ResizableWindow 스크립트를 찾아옴
        window = GetComponentInParent<ResizableWindow>();
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (window != null)
        {
            // 마우스 움직임(delta)만큼 창의 위치를 이동시킴
            window.GetRectTransform().anchoredPosition += eventData.delta;
        }
    }
}