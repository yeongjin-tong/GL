using System.Collections.Generic;
using UnityEngine;

public class GridManager : MonoBehaviour
{
    public static GridManager Instance { get; private set; }

    [Tooltip("와이어가 피해 가야 할 부품(장애물)들이 있는 레이어")]
    [SerializeField] private LayerMask obstacleLayer;

    [Tooltip("격자의 가로 칸 수")]
    [SerializeField] private int width;

    [Tooltip("격자의 세로 칸 수")]
    [SerializeField] private int height;

    [Tooltip("격자 한 칸의 크기 (월드 유닛)")]
    [SerializeField] private float cellSize;

    // ✨ GameObject 배열 대신, 더 많은 정보를 담을 수 있는 Node 배열을 사용합니다.
    private Node[,] grid;

    void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;

        CreateGrid();
    }

    // ✨ 그리드를 생성하고 장애물을 감지하는 핵심 함수
    void CreateGrid()
    {
        grid = new Node[width, height];

        // 그리드의 시작점(좌측 하단) 계산
        Vector3 worldBottomLeft = transform.position - Vector3.right * width * cellSize / 2 - Vector3.up * height * cellSize / 2;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                // 각 칸의 월드 좌표 계산
                Vector3 worldPoint = worldBottomLeft + Vector3.right * (x * cellSize + cellSize / 2) + Vector3.up * (y * cellSize + cellSize / 2);

                // ✨ Physics2D.OverlapCircle을 사용해 해당 위치에 장애물 레이어를 가진 콜라이더가 있는지 확인
                bool isWalkable = !(Physics2D.OverlapCircle(worldPoint, cellSize / 2, obstacleLayer));

                // 확인된 정보를 바탕으로 새로운 노드 생성
                grid[x, y] = new Node(isWalkable, worldPoint, x, y);
            }
        }
    }

    // 월드 좌표를 그리드 좌표로 변환하는 함수
    public Vector2Int GetGridPosition(Vector3 worldPosition)
    {
        // 그리드 원점을 기준으로 상대 위치 계산
        Vector3 relativePos = worldPosition - (transform.position - Vector3.right * width * cellSize / 2 - Vector3.up * height * cellSize / 2);

        int x = Mathf.FloorToInt(relativePos.x / cellSize);
        int y = Mathf.FloorToInt(relativePos.y / cellSize);

        // 그리드 범위 안에 있도록 좌표값 보정
        x = Mathf.Clamp(x, 0, width - 1);
        y = Mathf.Clamp(y, 0, height - 1);

        return new Vector2Int(x, y);
    }

    // 그리드 좌표를 해당 칸의 중앙 월드 좌표로 변환하는 함수
    public Vector3 GetWorldPosition(int x, int y)
    {
        Vector3 worldBottomLeft = transform.position - Vector3.right * width * cellSize / 2 - Vector3.up * height * cellSize / 2;
        return worldBottomLeft + new Vector3(x, y, 0) * cellSize + new Vector3(cellSize, cellSize, 0) * 0.5f;
    }

    // Scene 뷰에서 그리드와 장애물을 시각적으로 보여주는 Gizmos
    private void OnDrawGizmos()
    {
        // ✨ 그리드의 외곽선 표시
        Gizmos.color = Color.white;
        Gizmos.DrawWireCube(transform.position, new Vector3(width * cellSize, height * cellSize, 1));

        if (grid != null) // 그리드가 생성되었을 때만 실행
        {
            foreach (Node n in grid)
            {
                // ✨ 장애물(통과 불가) 노드는 빨간색, 길(통과 가능) 노드는 투명하게 표시
                Gizmos.color = (n.isWalkable) ? new Color(1, 1, 1, 0.1f) : Color.red;
                Gizmos.DrawCube(n.worldPosition, Vector3.one * (cellSize - 0.1f));
            }
        }
    }
    // 월드 좌표로 노드를 찾는 함수 (A_Star_Pathfinding에서 사용)
    public Node NodeFromWorldPoint(Vector3 worldPosition)
    {
        Vector3 relativePos = worldPosition - (transform.position - Vector3.right * width * cellSize / 2 - Vector3.up * height * cellSize / 2);
        int x = Mathf.Clamp(Mathf.FloorToInt(relativePos.x / cellSize), 0, width - 1);
        int y = Mathf.Clamp(Mathf.FloorToInt(relativePos.y / cellSize), 0, height - 1);
        return grid[x, y];
    }

    // 특정 노드의 상하좌우 이웃 노드를 반환하는 함수 (A_Star_Pathfinding에서 사용)
    public List<Node> GetNeighbours(Node node)
    {
        List<Node> neighbours = new List<Node>();

        // 위쪽
        if (node.gridY + 1 < height) neighbours.Add(grid[node.gridX, node.gridY + 1]);
        // 아래쪽
        if (node.gridY - 1 >= 0) neighbours.Add(grid[node.gridX, node.gridY - 1]);
        // 오른쪽
        if (node.gridX + 1 < width) neighbours.Add(grid[node.gridX + 1, node.gridY]);
        // 왼쪽
        if (node.gridX - 1 >= 0) neighbours.Add(grid[node.gridX - 1, node.gridY]);

        return neighbours;
    }
}