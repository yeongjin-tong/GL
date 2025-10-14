using TMPro;
using UnityEngine;

// ✨ 이 스크립트는 이제 2D와 3D에서 공용으로 사용됩니다.
public class ConnectionPoint : MonoBehaviour
{
    public enum Direction { Up, Down, Left, Right, None } // 방향이 없는 포트를 위해 None 추가
    public Direction pointDirection = Direction.None;

    [Tooltip("같은 방향의 포트가 여러 개일 경우 구분하기 위한 번호")]
    public int portIndex = 0;

    private TextMeshProUGUI pinNum;
    
    [HideInInspector]
    public Collider2D parentCollider;
    [HideInInspector]
    public ElectricalComponent parentComponent;

    void Awake()
    {
        parentCollider = transform.parent.GetComponent<Collider2D>();
        // GetComponentInParent는 부모 계층을 따라 올라가며 컴포넌트를 찾으므로 2D/3D 모두에서 잘 작동합니다.
        parentComponent = GetComponentInParent<ElectricalComponent>();

        pinNum = GetComponentInChildren<TextMeshProUGUI>();
        
    }

    // --- 3D 전용 하이라이트 로직 ---
    // (이 부분은 3D 포트에서만 시각적으로 작동합니다)
    private Color originalColor;
    private Renderer selfRenderer;

    void Start()
    {
        selfRenderer = GetComponent<Renderer>();
        if (selfRenderer != null)
        {
            originalColor = selfRenderer.material.color;
        }
        if(pinNum != null)
        {
            PinNumberSet();
            pinNum.gameObject.SetActive(false);
        }

    }

    private void OnMouseEnter()
    {
        if (WireManager_3d.Instance != null)
            WireManager_3d.Instance.HighlightPort(this, true);
    }

    private void OnMouseExit()
    {
        if (WireManager_3d.Instance != null)
            WireManager_3d.Instance.HighlightPort(this, false);
    }

    // 외부에서 색상을 변경하고, 원래 색상을 기억하기 위한 함수
    public void SetHighlight(bool highlight, Color highlightColor)
    {
        if (selfRenderer != null)
        {
            selfRenderer.material.color = highlight ? highlightColor : originalColor;
        }
    }

    private void PinNumberSet()
    {
        pinNum.text = portIndex.ToString();
        Debug.Log("핀번호 세팅 완료!");
    }
}