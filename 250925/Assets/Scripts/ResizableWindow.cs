using UnityEngine;

public class ResizableWindow : MonoBehaviour
{
    // 창의 최소/최대 크기를 인스펙터에서 설정
    public Vector2 minSize = new Vector2(200, 150);
    public Vector2 maxSize = new Vector2(800, 600);

    // 스크립트가 자신의 RectTransform에 쉽게 접근하기 위한 변수
    private RectTransform rectTransform;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    // DragHandle과 ResizeHandle이 호출할 함수들
    public RectTransform GetRectTransform()
    {
        return rectTransform;
    }
}