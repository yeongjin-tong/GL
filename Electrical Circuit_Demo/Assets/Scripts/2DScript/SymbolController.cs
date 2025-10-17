using UnityEngine;
using UnityEngine.UI;

public class SymbolController : MonoBehaviour
{
    public static SymbolController Instance { get; private set; }

    [Header("Selection Visuals")]
    [Tooltip("선택된 전선 색상")]
    public Color selectionColor = Color.yellow;

    private GameObject selectedObject;
    private Color originalWireColor; // 원래 전선 색상을 저장하기 위한 변수

    // --- 드래그 기능 관련 변수 ---
    private bool isDragging = false;
    private Vector3 offset;
    private GridManager gridManager;
    private ElectricalComponent draggedComponent;

    // ✨ 2번 조건: '상대적 위치' 드래그를 위한 변수
    private Vector3 initialMousePos; // 드래그 시작 시점의 마우스 월드 좌표
    private Vector3 initialObjectPos; // 드래그 시작 시점의 오브젝트 월드 좌표

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        gridManager = GridManager.Instance;
    }

    private void OnEnable()
    {
        if (InputManager.Instance != null)
        {
            InputManager.Instance.OnPhysicsObjectClicked += HandlePhysicsClick;
            InputManager.Instance.OnDeleteKeyPressed += HandleDeleteKey;
        }
    }

    private void OnDisable()
    {
        if (InputManager.Instance != null)
        {
            InputManager.Instance.OnPhysicsObjectClicked -= HandlePhysicsClick;
            InputManager.Instance.OnDeleteKeyPressed -= HandleDeleteKey;
        }
    }

    void Update()
    {
        if (SimulationManager.isSimulating) return;

        // --- 드래그 중일 때만 위치 업데이트 ---
        if (isDragging && Input.GetMouseButton(0))
        {
            // ✨ 2번 조건: '상대적 위치' 계산 로직
            Vector3 currentMousePos = GetMouseWorldPos();
            Vector3 mouseDelta = currentMousePos - initialMousePos; // 마우스가 시작점부터 얼마나 움직였는지 계산
            Vector3 newPosition = initialObjectPos + mouseDelta; // 오브젝트의 원래 위치에 그 차이만큼만 더해줌

            // 2D UI 환경이므로 RectTransform을 직접 조작하고 그리드에 스냅
            Vector2 localPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                (RectTransform)selectedObject.transform.parent, Camera.main.WorldToScreenPoint(newPosition), Camera.main, out localPoint);

            selectedObject.GetComponent<RectTransform>().anchoredPosition = gridManager.SnapToGrid(localPoint);

            // WireManager에게 연결된 전선을 다시 그리라고 요청
            if (draggedComponent != null)
            {
                WireManager.Instance.RedrawWiresForComponent(draggedComponent);
            }
        }

        // 드래그 종료
        if (isDragging && Input.GetMouseButtonUp(0))
        {
            isDragging = false;
            draggedComponent = null;
        }
    }

    private void HandlePhysicsClick(Collider2D hit)
    {
        // ✨ 1번 조건: '선택 후 드래그' 로직
        // 이미 선택된 오브젝트를 다시 클릭했는지 확인
        if (hit != null && selectedObject != null && hit.gameObject == selectedObject && selectedObject.GetComponent<ElectricalComponent>() != null)
        {
            // 드래그 시작!
            isDragging = true;
            draggedComponent = selectedObject.GetComponent<ElectricalComponent>();

            // ✨ 2번 조건: 드래그 시작 시점의 위치들을 기록
            initialMousePos = GetMouseWorldPos();
            initialObjectPos = selectedObject.transform.position;
            return; // 선택 로직은 건너뜀
        }

        // --- 일반 선택 로직 ---
        DeselectAll(); // 일단 모든 선택 효과 해제

        if (hit != null)
        {
            if (hit.gameObject.name.Contains("Clone") || hit.gameObject.CompareTag("Wire"))
            {
                SelectObject(hit.gameObject);
            }
        }
        else // 허공 클릭 시
        {
            selectedObject = null;
        }
    }

    private void HandleDeleteKey()
    {
        if (selectedObject != null)
        {
            // TODO: 부품 삭제 시, 연결된 모든 전선도 함께 삭제하는 로직 추가 필요
            //       (WireManager에 관련 함수를 만들어서 호출하는 것을 추천)

            // CircuitGraph에서 해당 부품 정보 제거
            var comp = selectedObject.GetComponent<ElectricalComponent>();
            if (comp != null)
            {
                CircuitGraph.Instance.RemoveComponent(comp);
            }

            Destroy(selectedObject);
            selectedObject = null;
        }
    }

    private void DeselectAll()
    {
        if (selectedObject == null) return;

        // 아웃라인 해제
        if (selectedObject.GetComponent<Outline>() != null)
        {
            selectedObject.GetComponent<Outline>().enabled = false;
        }
        // 전선 색상 원래대로
        if (selectedObject.CompareTag("Wire"))
        {
            var lr = selectedObject.GetComponent<LineRenderer>();
            if (lr != null)
            {
                lr.startColor = originalWireColor;
                lr.endColor = originalWireColor;
            }
        }
        selectedObject = null;
    }

    private void SelectObject(GameObject objToSelect)
    {
        selectedObject = objToSelect;

        // 아웃라인 활성화
        if (selectedObject.GetComponent<Outline>() != null)
        {
            selectedObject.GetComponent<Outline>().enabled = true;
        }
        // 전선 색상 변경
        if (selectedObject.CompareTag("Wire"))
        {
            var lr = selectedObject.GetComponent<LineRenderer>();
            if (lr != null)
            {
                originalWireColor = lr.startColor; // 원래 색상 저장
                lr.startColor = selectionColor;
                lr.endColor = selectionColor;
            }
        }
    }

    private Vector3 GetMouseWorldPos()
    {
        Vector3 mousePoint = Input.mousePosition;
        mousePoint.z = -Camera.main.transform.position.z;
        return Camera.main.ScreenToWorldPoint(mousePoint);
    }
}