using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class A_Star_Pathfinding : MonoBehaviour
{
    private GridManager gridManager;

    void Awake()
    {
        // 같은 오브젝트에 있는 GridManager를 가져옵니다.
        gridManager = GetComponent<GridManager>();
    }

    // 경로 찾기 메인 함수
    public List<Vector3> FindPath(Vector3 startPos, Vector3 targetPos)
    {
        Node startNode = gridManager.NodeFromWorldPoint(startPos);
        Node targetNode = gridManager.NodeFromWorldPoint(targetPos);

        List<Node> openSet = new List<Node>();
        HashSet<Node> closedSet = new HashSet<Node>();
        openSet.Add(startNode);

        while (openSet.Count > 0)
        {
            Node currentNode = openSet[0];
            for (int i = 1; i < openSet.Count; i++)
            {
                if (openSet[i].fCost < currentNode.fCost || openSet[i].fCost == currentNode.fCost && openSet[i].hCost < currentNode.hCost)
                {
                    currentNode = openSet[i];
                }
            }

            openSet.Remove(currentNode);
            closedSet.Add(currentNode);

            if (currentNode == targetNode)
            {
                return RetracePath(startNode, targetNode);
            }

            foreach (Node neighbour in gridManager.GetNeighbours(currentNode))
            {
                if (!neighbour.isWalkable || closedSet.Contains(neighbour))
                {
                    continue;
                }

                // 대각선 이동을 막습니다. (가로/세로 이동만 허용)
                if (Mathf.Abs(currentNode.gridX - neighbour.gridX) == 1 && Mathf.Abs(currentNode.gridY - neighbour.gridY) == 1)
                {
                    continue;
                }

                int newMovementCostToNeighbour = currentNode.gCost + GetDistance(currentNode, neighbour);
                if (newMovementCostToNeighbour < neighbour.gCost || !openSet.Contains(neighbour))
                {
                    neighbour.gCost = newMovementCostToNeighbour;
                    neighbour.hCost = GetDistance(neighbour, targetNode);
                    neighbour.parent = currentNode;

                    if (!openSet.Contains(neighbour))
                        openSet.Add(neighbour);
                }
            }
        }
        return null; // 경로를 찾지 못한 경우
    }

    // 찾은 경로를 역추적하고 단순화하는 함수
    List<Vector3> RetracePath(Node startNode, Node endNode)
    {
        List<Node> path = new List<Node>();
        Node currentNode = endNode;

        while (currentNode != startNode)
        {
            path.Add(currentNode);
            currentNode = currentNode.parent;
        }
        path.Reverse();

        return SimplifyPath(path.Select(node => node.worldPosition).ToList());
    }

    // 경로에서 불필요한 점을 제거하여 깔끔한 직선/코너로 만듦
    List<Vector3> SimplifyPath(List<Vector3> path)
    {
        if (path == null || path.Count == 0) return new List<Vector3>();

        List<Vector3> waypoints = new List<Vector3>();
        Vector2 directionOld = Vector2.zero;
        waypoints.Add(path[0]);

        for (int i = 1; i < path.Count; i++)
        {
            Vector2 directionNew = new Vector2(path[i - 1].x - path[i].x, path[i - 1].y - path[i].y).normalized;
            if (directionNew != directionOld)
            {
                waypoints.Add(path[i - 1]);
            }
            directionOld = directionNew;
        }
        waypoints.Add(path.Last());
        return waypoints;
    }

    // 두 노드 사이의 비용을 계산하는 함수 (맨해튼 거리)
    int GetDistance(Node nodeA, Node nodeB)
    {
        int dstX = Mathf.Abs(nodeA.gridX - nodeB.gridX);
        int dstY = Mathf.Abs(nodeA.gridY - nodeB.gridY);
        return 10 * (dstX + dstY);
    }
}