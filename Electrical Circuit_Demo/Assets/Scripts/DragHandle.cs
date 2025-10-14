using UnityEngine;
using UnityEngine.EventSystems;

public class DragHandle : MonoBehaviour, IDragHandler
{
    private ResizableWindow window;

    void Awake()
    {
        // �θ� ������Ʈ���� ResizableWindow ��ũ��Ʈ�� ã�ƿ�
        window = GetComponentInParent<ResizableWindow>();
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (window != null)
        {
            // ���콺 ������(delta)��ŭ â�� ��ġ�� �̵���Ŵ
            window.GetRectTransform().anchoredPosition += eventData.delta;
        }
    }
}