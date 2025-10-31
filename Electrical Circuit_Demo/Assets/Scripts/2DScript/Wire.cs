// Wire.cs (최종 수정안)
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Wire : MonoBehaviour
{
    // 이 전선(네트워크)에 연결된 모든 연결점의 목록이 유일한 데이터 소스입니다.
    public List<ConnectionPoint> connectedPoints = new List<ConnectionPoint>();

    // 연결된 부품 목록을 쉽게 가져오기 위한 프로퍼티
    public List<ElectricalComponent> ConnectedComponents
    {
        get
        {
            // 중복을 제거하고 부모 컴포넌트 리스트를 반환합니다.
            return connectedPoints.Select(p => p.parentComponent).Distinct().ToList();
        }
    }

    private LineRenderer lineRenderer;
    private Color defaultColor;

    private void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        if (lineRenderer != null)
        {
            defaultColor = lineRenderer.startColor;
        }
    }

    /// <summary>
    /// 이 와이어가 유효한 연결(두 개의 유효한 끝점)을 가지고 있는지 확인합니다.
    /// </summary>
    public bool IsValid()
    {
        // 와이어는 반드시 2개의 연결점을 가져야 함
        if (connectedPoints.Count != 2) return false;

        // 두 연결점이 null이 아니어야 함
        if (connectedPoints[0] == null || connectedPoints[1] == null) return false;

        // 두 연결점의 부모 부품이 null이 아니어야 함 (삭제되지 않았어야 함)
        if (connectedPoints[0].parentComponent == null || connectedPoints[1].parentComponent == null) return false;

        return true; // 모든 검사를 통과하면 유효함
    }

    /// <summary>
    /// 이 와이어에 연결된 부품이 삭제될 때 호출됩니다.
    /// </summary>
    public void OnComponentDeleted(ElectricalComponent deletedComponent)
    {
        // WireManager에 이 와이어를 '수리'하도록 요청합니다.
        // (삭제된 끝점을 가상 접점으로 교체)
        WireManager.Instance.ReplaceConnectionWithVirtualJunction(this, deletedComponent);
    }

    // 색상 관련 함수는 그대로 유지
    public void SetColor(Color newColor)
    {
        // TODO: 이 Wire에 속한 모든 시각적 선(자식 LineRenderer 포함)의 색을 변경해야 함
        if (lineRenderer != null)
        {
            lineRenderer.startColor = newColor;
            lineRenderer.endColor = newColor;
        }
    }

    public void ResetColor()
    {
        if (lineRenderer != null)
        {
            SetColor(defaultColor);
        }
    }
}