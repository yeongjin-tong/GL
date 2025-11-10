// SymbolController.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using System.Collections;

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

    // '상대적 위치' 드래그를 위한 변수
    private Vector3 initialMousePos; // 드래그 시작 시점의 마우스 월드 좌표
    private Vector3 initialObjectPos; // 드래그 시작 시점의 오브젝트 월드 좌표

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


    void Update()
    {
        if (SimulationManager.isSimulating) return;

        // --- 드래그 중일 때만 위치 업데이트 ---
        if (isDragging && Input.GetMouseButton(0))
        {
            // (기존 드래그 위치 업데이트 로직)
            Vector3 currentMousePos = GetMouseWorldPos();
            Vector3 mouseDelta = currentMousePos - initialMousePos;
            Vector3 newPosition = initialObjectPos + mouseDelta;

            Vector2 localPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                (RectTransform)selectedObject.transform.parent, Camera.main.WorldToScreenPoint(newPosition), Camera.main, out localPoint);

            selectedObject.GetComponent<RectTransform>().anchoredPosition = gridManager.SnapToGrid(localPoint);

            // RedrawWiresForComponent의 반환값(충돌 여부)을 isDragValid에 저장
            if (draggedComponent != null)
            {
                WireManager.Instance.RedrawWiresForComponent(draggedComponent);
            }
        }

        // 드래그 종료 또는 *새 부품 배치* 시
        if (Input.GetMouseButtonUp(0))
        {
            if (isDragging) // Case 1: "드래그" 중이던 기존 부품을 놓았을 때
            {
                if (draggedComponent != null)
                {
                    CheckForNearbyConnections(draggedComponent);
                    isDragging = false;
                    draggedComponent = null;
                }
            }
            else if (selectedObject != null) // Case 2: "새 부품"을 배치했을 때 (드래그 중이 아님)
            {
                ElectricalComponent comp = selectedObject.GetComponent<ElectricalComponent>();
                if (comp != null)
                {
                    // 새로 배치된 부품에 대해 스냅을 확인합니다.
                    CheckForNearbyConnections(comp);
                }
            }
        }
    }

    /// <summary>
    /// "Missing 에러"와 "선 모양 유지"를 위해 로직 전면 수정
    /// </summary>
    public void CheckForNearbyConnections(ElectricalComponent droppedComp)
    {
        var allVirtualJunctions = new List<VirtualJunction>(FindObjectsOfType<VirtualJunction>());
        if (allVirtualJunctions.Count == 0) return;

        List<ConnectionPoint> compPorts = droppedComp.GetComponentsInChildren<ConnectionPoint>().ToList();

        foreach (var port in compPorts)
        {
            VirtualJunction closestVJ = null;
            float minDistance = float.MaxValue;

            // 1. 이 포트에 가장 가까운 *스냅 반경 내의* 가상 접점을 찾습니다.
            foreach (var vj in allVirtualJunctions)
            {
                if (vj == null) continue; // 이미 처리된 접점일 수 있음
                float dist = Vector3.Distance(port.transform.position, vj.transform.position);
                if (dist < junctionSnapRadius && dist < minDistance)
                {
                    minDistance = dist;
                    closestVJ = vj;
                }
            }

            // 2. 스냅할 가상 접점(VJ)을 찾았다면
            if (closestVJ != null)
            {
                ConnectionPoint vjPoint = closestVJ.GetComponent<ConnectionPoint>();
                if (vjPoint == null || vjPoint.parentComponent == null) continue;

                // 3. VJ에 연결된 "originalWire" (예: R -> VJ 선)를 찾습니다.
                Wire originalWire = FindWireConnectedTo(vjPoint);
                if (originalWire == null) continue;

                // 4. "originalWire"의 "다른 쪽 끝점"(예: 'R' 포트)을 찾습니다.
                ConnectionPoint staticPoint = (originalWire.connectedPoints[0] == vjPoint)
                    ? originalWire.connectedPoints[1]
                    : originalWire.connectedPoints[0];

                // 5. VJ가 "originalWire" 리스트의 몇 번째 인덱스인지 찾습니다.
                int vjIndex = originalWire.connectedPoints.IndexOf(vjPoint);
                if (vjIndex == -1) continue; // 오류 방지

                // 6. [데이터 치유] "originalWire"의 연결점을 VJ에서 새 부품의 'port'로 교체합니다.
                originalWire.connectedPoints[vjIndex] = port;

                // 7. [회로도 수정] CircuitGraph에서 VJ 부품을 제거하고,
                CircuitGraph.Instance.RemoveComponent(vjPoint.parentComponent);
                // 새 연결(예: R <-> RL)을 등록합니다.
                CircuitGraph.Instance.RegisterConnection(staticPoint.parentComponent, port.parentComponent);

                // 8. [시각적 수정] "originalWire"가 새 부품(droppedComp)을 따라가도록
                //    RedrawWire를 호출합니다. (이때 "L"자 모양은 유지됩니다)
                WireManager.Instance.RedrawWire(originalWire, droppedComp);

                // 9. [청소] VJ(검은 점)는 역할을 다했으므로 파괴합니다.
                Destroy(closestVJ.gameObject);

                // 10. 다른 포트가 이 VJ에 중복 연결하는 것을 방지합니다.
                allVirtualJunctions.Remove(closestVJ);
            }
        }
    }

    /// <summary>
    /// 지정된 ConnectionPoint에 연결된 Wire를 찾습니다.
    /// </summary>
    private Wire FindWireConnectedTo(ConnectionPoint point)
    {
        if (point == null) return null;

        Wire[] allWires = FindObjectsOfType<Wire>();
        foreach (var wire in allWires)
        {
            if (wire.connectedPoints.Contains(point))
                return wire;
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
            return;
        }

        DeselectAll();

        if (hit != null)
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

    private void HandleDeleteKey()
    {
        if (selectedObject == null) return;

        Wire wireToDelete = selectedObject.GetComponent<Wire>();
        ElectricalComponent componentToDelete = selectedObject.GetComponent<ElectricalComponent>();

        if (wireToDelete != null)
        {
            DeleteWire(wireToDelete);
        }
        else if (componentToDelete != null)
        {
            DeleteComponent(componentToDelete);
        }
        else
        {
            Destroy(selectedObject);
        }

        selectedObject = null;
    }

    private void DeleteWire(Wire wireToDelete)
    {
        List<Junction> connectedJunctions = new List<Junction>();
        foreach (var point in wireToDelete.connectedPoints)
        {
            if (point.parentComponent is Junction junction)
            {
                connectedJunctions.Add(junction);
            }
        }

        Destroy(wireToDelete.gameObject);

        if (connectedJunctions.Count > 0)
        {
            StartCoroutine(DelayedCheckAndHeal(connectedJunctions));
        }

        StartCoroutine(DelayedRebuildGraph());
    }

    private void DeleteComponent(ElectricalComponent componentToDelete)
    {
        Wire[] allWires = FindObjectsOfType<Wire>();
        List<Wire> connectedWires = new List<Wire>();
        foreach (var wire in allWires)
        {
            if (wire.ConnectedComponents.Contains(componentToDelete))
            {
                connectedWires.Add(wire);
            }
        }

        foreach (Wire wire in connectedWires)
        {
            wire.OnComponentDeleted(componentToDelete);
        }

        CircuitGraph.Instance.RemoveComponent(componentToDelete);
        Destroy(componentToDelete.gameObject);
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
            if (junction != null)
            {
                junction.CheckAndHeal();
            }
        }
    }

    public void DeselectAll()
    {
        if (selectedObject == null) return;

        if (selectedObject.GetComponent<Outline>() != null)
        {
            selectedObject.GetComponent<Outline>().enabled = false;
        }
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

        if (selectedObject.GetComponent<Outline>() != null)
        {
            selectedObject.GetComponent<Outline>().enabled = true;
        }
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