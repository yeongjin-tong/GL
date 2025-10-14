using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class WireManager_3d : MonoBehaviour
{
    public static WireManager_3d Instance { get; private set; }

    [Header("Wire Settings")]
    [Tooltip("전선의 색상")]
    public Color wireColor = Color.blue;
    [Tooltip("전선의 두께")]
    public float wireWidth = 0.05f;
    [Tooltip("생성된 전선이 위치할 부모 Transform")]
    public Transform wireParent;


    [Header("Interaction Settings")]
    [Tooltip("포트에 자석처럼 달라붙는 효과가 적용될 반경 (월드 유닛)")]
    public float snapRadius = 0.5f;
    [Tooltip("포트에 마우스를 올렸을 때 강조될 색상")]
    public Color portHighlightColor = Color.green;

    // --- 내부 변수 ---
    private bool isDrawingWire = false;
    private LineRenderer currentWire;
    private ConnectionPoint firstPoint; // ✨ ConnectionPoint_3D 대신 공용 ConnectionPoint 사용
    private Transform snappedPort = null;

    void Awake()
    {
        Instance = this;
    }

    void Update()
    {
        if (SimulationManager.isSimulating) return;

        // --- 마우스 왼쪽 버튼 클릭 처리 ---
        if (Input.GetMouseButtonDown(0))
        {
            HandleLeftClick();
        }

        // --- 실시간 미리보기 업데이트 ---
        if (isDrawingWire && currentWire != null)
        {
            UpdateWirePreview(currentWire);
        }

        // --- 마우스 오른쪽 버튼으로 그리기 취소 ---
        if (isDrawingWire && Input.GetMouseButtonDown(1))
        {
            Destroy(currentWire.gameObject);
            ResetState();
        }
    }

    private void HandleLeftClick()
    {
        // 1. 만약 선을 그리고 있고, 현재 스냅된 포트가 있다면 바로 연결
        if (isDrawingWire && snappedPort != null)
        {
            ConnectionPoint secondPoint = snappedPort.GetComponent<ConnectionPoint>();
            if (secondPoint != null && secondPoint.parentComponent != firstPoint.parentComponent)
            {
                FinalizeWire(currentWire, firstPoint, secondPoint);
                ResetState();
                return; // 중요: 아래 로직을 실행하지 않도록 여기서 종료
            }
        }

        // 2. 스냅된 포트가 없다면, Raycast로 클릭 지점 확인
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            ConnectionPoint point = hit.collider.GetComponent<ConnectionPoint>();

            if (!isDrawingWire) // 그리기 시작
            {
                if (point != null)
                {
                    isDrawingWire = true;
                    firstPoint = point;
                    currentWire = CreateWire(hit.collider.bounds.center);
                }
            }
            else // 그리는 중
            {
                if (point != null) // 다른 포트를 클릭 (연결 완료)
                {
                    if (point.parentComponent != firstPoint.parentComponent)
                    {
                        FinalizeWire(currentWire, firstPoint, point);
                        ResetState();
                    }
                }
                else // 허공이나 다른 물체를 클릭 (경유지 추가)
                {
                    AddPointToWire(currentWire, hit.point);
                }
            }
        }
        else if (isDrawingWire) // 허공 클릭 (경유지 추가)
        {
            Vector3? mouseWorldPos = GetMouseWorldPositionOnPlane(currentWire.GetPosition(currentWire.positionCount - 2));
            if (mouseWorldPos.HasValue)
            {
                AddPointToWire(currentWire, mouseWorldPos.Value);
            }
        }
    }

    private void UpdateWirePreview(LineRenderer lr)
    {
        Vector3? mouseWorldPos = GetMouseWorldPositionOnPlane(lr.GetPosition(lr.positionCount - 2));
        if (!mouseWorldPos.HasValue) return;

        Vector3 endPoint;
        Transform closestPort = FindClosestPort(mouseWorldPos.Value);

        // 하이라이트 처리
        if (snappedPort != null && closestPort != snappedPort)
        {
            HighlightPort(snappedPort.GetComponent<ConnectionPoint>(), false);
        }

        if (closestPort != null)
        {
            snappedPort = closestPort;
            HighlightPort(snappedPort.GetComponent<ConnectionPoint>(), true);
            endPoint = snappedPort.GetComponent<Collider>().bounds.center;
        }
        else
        {
            snappedPort = null;
            endPoint = mouseWorldPos.Value;
        }

        lr.SetPosition(lr.positionCount - 1, endPoint);
    }

    // ✨ FinalizeWire 함수가 공용 ConnectionPoint를 사용
    private void FinalizeWire(LineRenderer lr, ConnectionPoint startPoint, ConnectionPoint endPoint)
    {
        Vector3 endPos = endPoint.GetComponent<Collider>().bounds.center;
        lr.SetPosition(lr.positionCount - 1, endPos);
        lr.name = $"Wire_{startPoint.parentComponent.name}_to_{endPoint.parentComponent.name}";
        lr.gameObject.tag = "Wire";

        if (startPoint.parentComponent != null && endPoint.parentComponent != null)
        {
            // ElectricalComponent의 AddConnection 함수를 정상적으로 호출
            startPoint.parentComponent.AddConnection(startPoint, endPoint);
            endPoint.parentComponent.AddConnection(endPoint, startPoint);
        }

        // EdgeCollider2D 추가 (2D 오버레이 UI 등에서 사용 가능)
        var edgeCollider = lr.gameObject.AddComponent<EdgeCollider2D>();
        var points = new Vector3[lr.positionCount];
        lr.GetPositions(points);
        edgeCollider.points = System.Array.ConvertAll(points, p => new Vector2(p.x, p.y));
    }

    private Transform FindClosestPort(Vector3 mousePos)
    {
        Transform closest = null;
        float minDistance = float.MaxValue;

        // FindObjectsOfType은 무거우므로, 시작 시 캐싱하는 것을 권장
        ConnectionPoint[] allPorts = FindObjectsOfType<ConnectionPoint>();

        foreach (var port in allPorts)
        {
            if (port.parentComponent == firstPoint.parentComponent) continue;

            float distance = Vector3.Distance(mousePos, port.transform.position);
            if (distance < snapRadius && distance < minDistance)
            {
                minDistance = distance;
                closest = port.transform;
            }
        }
        return closest;
    }

    public void HighlightPort(ConnectionPoint port, bool highlight)
    {
        if (port != null)
        {
            // ConnectionPoint에 있는 SetHighlight 함수를 호출
            port.SetHighlight(highlight, portHighlightColor);
        }
    }

    // --- Helper Functions ---
    private LineRenderer CreateWire(Vector3 startPos)
    {
        GameObject wireObject = new GameObject("Wire_Drawing");
        if (wireParent != null) wireObject.transform.SetParent(wireParent);

        var lr = wireObject.AddComponent<LineRenderer>();
        lr.positionCount = 2;
        lr.SetPosition(0, startPos);
        SetupLineRenderer(lr);
        return lr;
    }

    private void AddPointToWire(LineRenderer lr, Vector3 newPoint)
    {
        lr.positionCount++;
        lr.SetPosition(lr.positionCount - 1, newPoint);
    }

    private void ResetState()
    {
        if (snappedPort != null)
        {
            HighlightPort(snappedPort.GetComponent<ConnectionPoint>(), false);
        }
        isDrawingWire = false;
        currentWire = null;
        firstPoint = null;
        snappedPort = null;
    }

    private Vector3? GetMouseWorldPositionOnPlane(Vector3 referencePoint)
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        Plane plane = new Plane(Vector3.forward, referencePoint);
        if (plane.Raycast(ray, out float distance))
        {
            return ray.GetPoint(distance);
        }
        return null;
    }

    private void SetupLineRenderer(LineRenderer lr)
    {
        lr.startWidth = wireWidth;
        lr.endWidth = wireWidth;
        lr.material = new Material(Shader.Find("Unlit/Color"));
        lr.material.color = wireColor;
        lr.useWorldSpace = true;
    }
}