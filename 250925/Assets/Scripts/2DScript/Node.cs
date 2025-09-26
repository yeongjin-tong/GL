using UnityEngine;

public class Node
{
    public bool isWalkable;
    public Vector3 worldPosition;
    public int gridX, gridY;

    public int gCost; // 시작점으로부터의 비용

    // ✨ 에러의 원인: 이 변수가 누락되었을 가능성이 높습니다.
    public int hCost; // 목표점까지의 예상 비용 

    public Node parent; // 경로 추적을 위한 부모 노드

    public Node(bool _isWalkable, Vector3 _worldPos, int _gridX, int _gridY)
    {
        isWalkable = _isWalkable;
        worldPosition = _worldPos;
        gridX = _gridX;
        gridY = _gridY;
    }

    // ✨ 에러의 원인: 이 프로퍼티(속성)가 누락되었을 가능성이 높습니다.
    // A* 알고리즘에서 노드를 비교하기 위한 F-Cost (G+H)
    public int fCost
    {
        get { return gCost + hCost; }
    }
}