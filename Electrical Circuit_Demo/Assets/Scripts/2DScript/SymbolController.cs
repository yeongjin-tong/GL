using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using System.Collections;

public class SymbolController : MonoBehaviour
{
    public static SymbolController Instance { get; private set; }

    public LineRenderer dottedLine;
    private LineRenderer instantDot;

    [Header("Selection Visuals")]
    [Tooltip("선택된 전선 색상")]
    public Color selectionColor = Color.yellow;

    private GameObject selectedObject;
    private Color originalWireColor;

    // --- 드래그 기능 관련 변수 ---
    private bool isDragging = false;
    private GridManager gridManager;
    private ElectricalComponent draggedComponent;

    // '상대적 위치' 드래그를 위한 변수
    private Vector3 initialMousePos;
    private Vector3 initialObjectPos;

    // ✨ [추가] 원래 위치로 복귀하기 위한 UI 좌표 저장 변수
    private Vector2 initialAnchorPos;

    private Vector3 dragStartPos;

    [Header("UI Settings")]
    [Tooltip("이 영역 위에서 드래그를 놓으면 원래 위치로 돌아갑니다.")]
    public RectTransform deleteZone; // ✨ [추가] 인스펙터에서 할당하세요

    [Header("Snapping")]
    [Tooltip("가상 접점에 자동으로 스냅될 거리(월드 유닛)")]
    public float junctionSnapRadius = 0.5f;

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

    // ... (ShowGuideLine, HideGuideLine 등 기존 함수 유지) ...
    public void ShowGuideLine(GameObject component)
    {
        if (instantDot == null)
        {
            instantDot = Instantiate(dottedLine, component.transform.parent);
        }
        Vector3 position = component.transform.localPosition;
        float currentX = position.x;
        Vector3 startPos = instantDot.GetPosition(0);
        Vector3 endPos = instantDot.GetPosition(1);
        instantDot.SetPosition(0, new Vector3(currentX, startPos.y, startPos.z));
        instantDot.SetPosition(1, new Vector3(currentX, endPos.y, endPos.z));
        instantDot.gameObject.SetActive(true);
    }

    public void HideGuideLine()
    {
        if (instantDot != null)
        {
            Destroy(instantDot.gameObject);
            instantDot = null;
        }
    }

    void Update()
    {
        if (SimulationManager.isSimulating) return;

        // --- 드래그 중일 때만 위치 업데이트 ---
        if (isDragging && Input.GetMouseButton(0))
        {
            Vector3 currentMousePos = GetMouseWorldPos();
            Vector3 mouseDelta = currentMousePos - initialMousePos;
            Vector3 newPosition = initialObjectPos + mouseDelta;

            Vector2 localPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                (RectTransform)selectedObject.transform.parent, Camera.main.WorldToScreenPoint(newPosition), Camera.main, out localPoint);

            selectedObject.GetComponent<RectTransform>().anchoredPosition = gridManager.SnapToGrid(localPoint);

            if (draggedComponent != null)
            {
                WireManager.Instance.RedrawWiresForComponent(draggedComponent);
            }
            ShowGuideLine(selectedObject);
        }

        // --- 드래그 종료 또는 *새 부품 배치* 시 ---
        if (Input.GetMouseButtonUp(0))
        {
            if (isDragging)
            {
                // ✨ [핵심 수정] 삭제 영역(deleteZone) 위에 있는지 확인
                if (IsMouseOverDeleteZone())
                {
                    Debug.Log("삭제 영역 감지: 원래 위치로 되돌립니다.");

                    // 1. 위치 원상 복구
                    selectedObject.GetComponent<RectTransform>().anchoredPosition = initialAnchorPos;

                    // 2. 와이어도 원래 위치에 맞게 다시 그리기
                    if (draggedComponent != null)
                    {
                        WireManager.Instance.RedrawWiresForComponent(draggedComponent);
                    }

                    // 3. 상태 초기화 (CheckForNearbyConnections 실행 안 함)
                    isDragging = false;
                    draggedComponent = null;
                }
                else // 삭제 영역이 아니라면 정상 배치 로직 수행
                {
                    if (draggedComponent != null)
                    {
                        CheckForNearbyConnections(draggedComponent);
                        isDragging = false;
                        draggedComponent = null;
                    }
                }
            }
            else if (selectedObject != null) // Case 2: "새 부품"을 배치했을 때
            {
                ElectricalComponent comp = selectedObject.GetComponent<ElectricalComponent>();
                if (comp != null)
                {
                    CheckForNearbyConnections(comp);
                }
            }

            // ✨ [추가] 이동 커맨드 등록 (위치가 조금이라도 변했다면)
            if (Vector3.Distance(dragStartPos, selectedObject.transform.position) > 0.01f)
            {
                // CommandManager에게 "나 이거 이동했어"라고 알림
                CommandManager.Instance.AddCommand(new Command_Move(
                    selectedObject.transform,
                    dragStartPos,
                    selectedObject.transform.position,
                    draggedComponent
                ));
            }

            HideGuideLine();
        }
    }

    /// <summary>
    /// ✨ [새 함수] 마우스가 삭제 영역 위에 있는지 확인합니다.
    /// </summary>
    private bool IsMouseOverDeleteZone()
    {
        if (deleteZone == null) return false;

        // 마우스 포인터가 deleteZone 사각형 안에 있는지 검사
        // Canvas Render Mode가 Overlay라면 Camera 인자에 null을 넣고, Camera라면 Camera.main을 넣습니다.
        // 여기서는 안전하게 Camera.main을 사용합니다.
        return RectTransformUtility.RectangleContainsScreenPoint(deleteZone, Input.mousePosition, Camera.main);
    }

    // ... (CheckForNearbyConnections, FindWireConnectedTo 함수 등 기존 로직 유지) ...
    public void CheckForNearbyConnections(ElectricalComponent droppedComp)
    {
        var allVirtualJunctions = new List<VirtualJunction>(FindObjectsOfType<VirtualJunction>());
        if (allVirtualJunctions.Count == 0) return;

        List<ConnectionPoint> compPorts = droppedComp.GetComponentsInChildren<ConnectionPoint>().ToList();

        foreach (var port in compPorts)
        {
            VirtualJunction closestVJ = null;
            float minDistance = float.MaxValue;

            foreach (var vj in allVirtualJunctions)
            {
                if (vj == null) continue;
                float dist = Vector3.Distance(port.transform.position, vj.transform.position);
                if (dist < junctionSnapRadius && dist < minDistance)
                {
                    minDistance = dist;
                    closestVJ = vj;
                }
            }

            if (closestVJ != null)
            {
                ConnectionPoint vjPoint = closestVJ.GetComponent<ConnectionPoint>();
                if (vjPoint == null || vjPoint.parentComponent == null) continue;

                Wire originalWire = FindWireConnectedTo(vjPoint);
                if (originalWire == null) continue;

                ConnectionPoint staticPoint = (originalWire.connectedPoints[0] == vjPoint)
                    ? originalWire.connectedPoints[1]
                    : originalWire.connectedPoints[0];

                int vjIndex = originalWire.connectedPoints.IndexOf(vjPoint);
                if (vjIndex == -1) continue;

                originalWire.connectedPoints[vjIndex] = port;

                CircuitGraph.Instance.RemoveComponent(vjPoint.parentComponent);
                CircuitGraph.Instance.RegisterConnection(staticPoint.parentComponent, port.parentComponent);

                WireManager.Instance.RedrawWire(originalWire, droppedComp);

                Destroy(closestVJ.gameObject);
                allVirtualJunctions.Remove(closestVJ);
            }
        }
    }

    private Wire FindWireConnectedTo(ConnectionPoint point)
    {
        if (point == null) return null;
        Wire[] allWires = FindObjectsOfType<Wire>();
        foreach (var wire in allWires)
        {
            if (wire.connectedPoints.Contains(point)) return wire;
        }
        return null;
    }

    private void HandlePhysicsClick(Collider2D hit)
    {
        if (hit != null && selectedObject != null && hit.gameObject == selectedObject && selectedObject.GetComponent<ElectricalComponent>() != null)
        {
            isDragging = true;
            draggedComponent = selectedObject.GetComponent<ElectricalComponent>();
            initialMousePos = GetMouseWorldPos();
            initialObjectPos = selectedObject.transform.position;

            // ✨ [수정] 드래그 시작 시점의 UI 좌표(앵커 포지션) 저장
            initialAnchorPos = selectedObject.GetComponent<RectTransform>().anchoredPosition;

            dragStartPos = selectedObject.transform.position;

            return;
        }

        DeselectAll();

        if (hit != null && WireManager.Instance.currentWire == null)
        {
            if (hit.gameObject.name.Contains("Clone") || hit.gameObject.CompareTag("Wire"))
            {
                SelectObject(hit.gameObject);
            }
        }
        else
        {
            selectedObject = null;
        }
    }

    // ... (나머지 함수들 HandleDeleteKey, DeleteWire, DeleteComponent 등은 그대로 유지) ...
    private void HandleDeleteKey()
    {
        if (selectedObject == null) return;
        Wire wireToDelete = selectedObject.GetComponent<Wire>();
        ElectricalComponent componentToDelete = selectedObject.GetComponent<ElectricalComponent>();

        if (wireToDelete != null) DeleteWire(wireToDelete);
        else if (componentToDelete != null) DeleteComponent(componentToDelete);
        else Destroy(selectedObject);
        selectedObject = null;
    }

    private void DeleteWire(Wire wireToDelete)
    {
        //List<Junction> connectedJunctions = new List<Junction>();
        //foreach (var point in wireToDelete.connectedPoints)
        //{
        //    if (point.parentComponent is Junction junction) connectedJunctions.Add(junction);
        //}
        //Destroy(wireToDelete.gameObject);
        //if (connectedJunctions.Count > 0) StartCoroutine(DelayedCheckAndHeal(connectedJunctions));
        //StartCoroutine(DelayedRebuildGraph());

        // ✨ [수정] Undo를 위해 Destroy 대신 SetActive(false) 사용 및 커맨드 등록

        // (기존의 복잡한 Junction 치유 로직은 Undo 구현 시 매우 복잡해지므로,
        //  Undo 기능을 위해 단순 비활성화 방식으로 변경하는 것을 권장합니다.)

        // 1. 커맨드 등록 (삭제 행동 = false)
        CommandManager.Instance.AddCommand(new Command_ToggleActive(wireToDelete.gameObject, false));

        // 2. 즉시 실행 (끄기)
        wireToDelete.gameObject.SetActive(false);

        // 3. 그래프 재빌드
        StartCoroutine(DelayedRebuildGraph());
    }

    private void DeleteComponent(ElectricalComponent componentToDelete)
    {
        //Wire[] allWires = FindObjectsOfType<Wire>();
        //List<Wire> connectedWires = new List<Wire>();
        //foreach (var wire in allWires)
        //{
        //    if (wire.ConnectedComponents.Contains(componentToDelete)) connectedWires.Add(wire);
        //}
        //foreach (Wire wire in connectedWires) wire.OnComponentDeleted(componentToDelete);

        //CircuitGraph.Instance.RemoveComponent(componentToDelete);
        //Destroy(componentToDelete.gameObject);

        // ✨ [수정] Undo를 위해 Destroy 대신 SetActive(false) 사용 및 커맨드 등록

        // 1. 커맨드 등록
        CommandManager.Instance.AddCommand(new Command_ToggleActive(componentToDelete.gameObject, false));

        // 2. 즉시 실행
        componentToDelete.gameObject.SetActive(false);
        CircuitGraph.Instance.RemoveComponent(componentToDelete);

        // (참고: 기존의 '와이어 치유' 로직인 wire.OnComponentDeleted는 
        //  부품을 '영구 삭제'할 때 쓰는 것이라 Undo와 궁합이 안 좋습니다. 
        //  Undo를 지원하려면 부품을 껐을 때 연결된 와이어도 같이 꺼지거나, 
        //  그대로 남아있게(시각적으로만 끊김) 두는 방식이 안전합니다.)
    }

    private IEnumerator DelayedRebuildGraph()
    {
        yield return new WaitForEndOfFrame();
        CircuitGraph.Instance.RebuildGraph();
    }

    private IEnumerator DelayedCheckAndHeal(List<Junction> junctionsToCheck)
    {
        yield return new WaitForEndOfFrame();
        foreach (var junction in junctionsToCheck)
        {
            if (junction != null) junction.CheckAndHeal();
        }
    }

    public void DeselectAll()
    {
        if (selectedObject == null) return;
        if (selectedObject.GetComponent<Outline>() != null) selectedObject.GetComponent<Outline>().enabled = false;
        if (selectedObject.CompareTag("Wire"))
        {
            var lr = selectedObject.GetComponent<LineRenderer>();
            if (lr != null) { lr.startColor = originalWireColor; lr.endColor = originalWireColor; }
        }
        selectedObject = null;
    }

    private void SelectObject(GameObject objToSelect)
    {
        selectedObject = objToSelect;
        if (selectedObject.GetComponent<Outline>() != null) selectedObject.GetComponent<Outline>().enabled = true;
        if (selectedObject.CompareTag("Wire"))
        {
            var lr = selectedObject.GetComponent<LineRenderer>();
            if (lr != null)
            {
                originalWireColor = lr.startColor;
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