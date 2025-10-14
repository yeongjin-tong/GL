using UnityEngine;

public class GridManager : MonoBehaviour
{
    public static GridManager Instance { get; private set; }

    [Tooltip("그리드를 생성하고 스냅의 기준이 될 UI 패널(RectTransform)")]
    public RectTransform gridArea;

    [Tooltip("격자 한 칸의 가로, 세로 크기 (UI 단위)")]
    public Vector2 cellSize = new Vector2(16f, 16f);

    private Vector2 gridOriginOffset; // 그리드 원점 (좌측 하단) 오프셋
    private int width, height; // Gizmos에서 사용하기 위해 멤버 변수로 변경

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;

        InitializeGrid();
    }

    private void InitializeGrid()
    {
        if (gridArea == null)
        {
            Debug.LogError("GridManager: 'Grid Area' RectTransform이 설정되지 않았습니다! 인스펙터에서 연결해주세요.");
            return;
        }

        width = Mathf.FloorToInt(gridArea.rect.width / cellSize.x);
        height = Mathf.FloorToInt(gridArea.rect.height / cellSize.y);

        // RectTransform의 피벗(Pivot)을 고려하여 그리드의 원점(좌측 하단) 위치 계산
        // (예: 피벗이 (0.5, 0.5)이면, 패널 크기의 절반만큼을 오프셋으로 설정)
        gridOriginOffset = new Vector2(gridArea.rect.width * gridArea.pivot.x, gridArea.rect.height * gridArea.pivot.y);
    }

    /// <summary>
    /// UI 로컬 좌표를 그리드 좌표(예: 5, 3)로 변환합니다.
    /// </summary>
    public Vector2Int GetGridPosition(Vector2 localPosition)
    {
        // 원점을 기준으로 한 상대 위치 계산
        Vector2 relativePos = localPosition + gridOriginOffset;

        int x = Mathf.FloorToInt(relativePos.x / cellSize.x);
        int y = Mathf.FloorToInt(relativePos.y / cellSize.y);

        return new Vector2Int(x, y);
    }

    /// <summary>
    /// 그리드 좌표(예: 5, 3)를 해당 칸의 중앙에 해당하는 UI 로컬 좌표로 변환합니다.
    /// (기존 SnapToGrid 함수의 새 이름)
    /// </summary>
    public Vector2 GetLocalPosition(int x, int y)
    {
        // 원점을 기준으로 한 칸의 위치 계산 후, 칸의 중앙으로 이동
        return new Vector2(x * cellSize.x, y * cellSize.y) - gridOriginOffset + cellSize * 0.5f;
    }

    public Vector2 SnapToGrid(Vector2 rawPosition)
    {
        float snappedX = Mathf.Round(rawPosition.x / cellSize.x) * cellSize.x;
        float snappedY = Mathf.Round(rawPosition.y / cellSize.y) * cellSize.y;

        return new Vector2(snappedX, snappedY);
    }

    /// <summary>
    /// Scene 뷰에서 그리드를 시각적으로 보여주는 함수입니다.
    /// </summary>
    private void OnDrawGizmos()
    {
        // gridArea가 설정되어 있을 때만 Gizmos를 그립니다.
        if (gridArea != null)
        {
            // RectTransform의 월드 좌표 코너를 가져옴
            Vector3[] corners = new Vector3[4];
            gridArea.GetWorldCorners(corners);
            Vector3 bottomLeft = corners[0]; // 0번 코너가 좌측 하단입니다.

            Gizmos.color = new Color(0.5f, 0.5f, 0.5f, 0.5f); // 반투명 회색

            // 그리드 크기를 다시 계산 (에디터에서의 변경을 반영하기 위해)
            int gizmoWidth = Mathf.FloorToInt(gridArea.rect.width / cellSize.x);
            int gizmoHeight = Mathf.FloorToInt(gridArea.rect.height / cellSize.y);

            // 세로선 그리기
            for (int x = 0; x <= gizmoWidth; x++)
            {
                // UI는 스케일에 영향을 받으므로, transform.right 대신 월드 벡터를 사용해야 할 수 있습니다.
                // 여기서는 Canvas 스케일을 고려하여 gridArea의 transform 방향을 사용합니다.
                Vector3 start = bottomLeft + (Vector3)gridArea.right * x * cellSize.x * gridArea.lossyScale.x;
                Vector3 end = start + (Vector3)gridArea.up * gizmoHeight * cellSize.y * gridArea.lossyScale.y;
                Gizmos.DrawLine(start, end);
            }
            // 가로선 그리기
            for (int y = 0; y <= gizmoHeight; y++)
            {
                Vector3 start = bottomLeft + (Vector3)gridArea.up * y * cellSize.y * gridArea.lossyScale.y;
                Vector3 end = start + (Vector3)gridArea.right * gizmoWidth * cellSize.x * gridArea.lossyScale.x;
                Gizmos.DrawLine(start, end);
            }
        }
    }
}