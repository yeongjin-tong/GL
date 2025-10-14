using UnityEngine;

public class ResizableWindow : MonoBehaviour
{
    // â�� �ּ�/�ִ� ũ�⸦ �ν����Ϳ��� ����
    public Vector2 minSize = new Vector2(200, 150);
    public Vector2 maxSize = new Vector2(800, 600);

    // ��ũ��Ʈ�� �ڽ��� RectTransform�� ���� �����ϱ� ���� ����
    private RectTransform rectTransform;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    // DragHandle�� ResizeHandle�� ȣ���� �Լ���
    public RectTransform GetRectTransform()
    {
        return rectTransform;
    }
}