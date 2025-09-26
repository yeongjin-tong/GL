using UnityEngine;
// ✨ EventSystems 네임스페이스와 인터페이스가 더 이상 필요 없으므로 삭제합니다.

public class ConnectionPoint_3D : MonoBehaviour
{
    private Color myColor;

    private void Awake()
    {
        myColor = gameObject.GetComponent<MeshRenderer>().material.color;
    }

    // ✨ 마우스 커서가 이 오브젝트의 콜라이더 안으로 들어왔을 때 자동으로 호출됩니다.
    private void OnMouseEnter()
    {
        // WireManager에게 이 포트를 하이라이트하라고 알림
        // (단, 다른 UI가 위에 있으면 작동하지 않을 수 있음)
        if (WireManager_3d.Instance != null)
        {
            WireManager_3d.Instance.HighlightPort(this, true, myColor);
        }
    }

    // ✨ 마우스 커서가 콜라이더를 벗어났을 때 자동으로 호출됩니다.
    private void OnMouseExit()
    {
        // WireManager에게 하이라이트를 해제하라고 알림
        if (WireManager_3d.Instance != null)
        {
            WireManager_3d.Instance.HighlightPort(this, false, myColor);
        }
    }
}