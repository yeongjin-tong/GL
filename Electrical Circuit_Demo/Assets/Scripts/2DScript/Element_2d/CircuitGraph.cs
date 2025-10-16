using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 회로의 모든 전기적 연결 상태를 중앙에서 관리하는 싱글턴 클래스입니다.
/// '연결 그룹(Net)'의 개념을 사용하여 어떤 부품들이 전기적으로 연결되어 있는지 추적합니다.
/// </summary>
public class CircuitGraph : MonoBehaviour
{
    // --- 싱글턴 인스턴스 ---
    public static CircuitGraph Instance { get; private set; }

    /// <summary>
    /// 전기적으로 연결된 모든 부품들의 '그룹(Net)'을 담고 있는 리스트.
    /// 각 HashSet이 하나의 독립된 전기적 노드를 나타냅니다.
    /// </summary>
    private List<HashSet<ElectricalComponent>> nets;

    private void Awake()
    {
        // 싱글턴 패턴 구현
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }

        // 넷 리스트 초기화
        nets = new List<HashSet<ElectricalComponent>>();
    }

    /// <summary>
    /// 두 부품 사이에 새로운 연결을 등록합니다.
    /// 각 부품이 속한 넷을 찾아, 필요하다면 두 넷을 하나로 합칩니다.
    /// </summary>
    /// <param name="compA">연결할 첫 번째 부품</param>
    /// <param name="compB">연결할 두 번째 부품</param>
    public void RegisterConnection(ElectricalComponent compA, ElectricalComponent compB)
    {
        var netA = FindNetContaining(compA);
        var netB = FindNetContaining(compB);

        if (netA == null && netB == null)
        {
            // Case 1: 두 부품 모두 어떤 넷에도 속해있지 않은 경우 -> 새로운 넷 생성
            var newNet = new HashSet<ElectricalComponent> { compA, compB };
            nets.Add(newNet);
        }
        else if (netA != null && netB == null)
        {
            // Case 2: compA만 넷에 속한 경우 -> compB를 compA의 넷에 추가
            netA.Add(compB);
        }
        else if (netA == null && netB != null)
        {
            // Case 3: compB만 넷에 속한 경우 -> compA를 compB의 넷에 추가
            netB.Add(compA);
        }
        else if (netA != netB)
        {
            // Case 4: 두 부품이 서로 다른 넷에 속한 경우 -> 두 넷을 하나로 병합
            netA.UnionWith(netB); // netB의 모든 요소를 netA에 추가 (중복은 알아서 처리됨)
            nets.Remove(netB);    // 이제 비어있는 netB는 리스트에서 삭제
        }
        // Case 5 (else if netA == netB): 두 부품이 이미 같은 넷에 속한 경우 -> 아무것도 하지 않음
    }

    /// <summary>
    /// 특정 부품이 속한 연결 그룹(Net) 전체를 반환합니다.
    /// 회로 분석 시 이 함수를 통해 연결된 모든 부품을 한 번에 가져올 수 있습니다.
    /// </summary>
    /// <param name="component">찾고자 하는 부품</param>
    /// <returns>부품이 속한 HashSet 형태의 넷. 부품이 어떤 넷에도 없으면 null을 반환합니다.</returns>
    public HashSet<ElectricalComponent> GetNetFor(ElectricalComponent component)
    {
        return FindNetContaining(component);
    }

    /// <summary>
    /// 특정 부품을 그래프에서 제거합니다.
    /// (예: 사용자가 부품을 삭제했을 때 호출)
    /// </summary>
    /// <param name="componentToRemove">제거할 부품</param>
    public void RemoveComponent(ElectricalComponent componentToRemove)
    {
        var net = FindNetContaining(componentToRemove);
        if (net != null)
        {
            net.Remove(componentToRemove);
            // 만약 넷에 부품이 하나만 남거나 비게 되면, 해당 넷을 제거하는 로직을 추가할 수 있습니다.
            if (net.Count < 2)
            {
                nets.Remove(net);
            }
        }
    }

    /// <summary>
    /// (주의) 현재 회로의 모든 연결 정보를 초기화합니다.
    /// </summary>
    public void ClearGraph()
    {
        nets.Clear();
    }


    /// <summary>
    /// (헬퍼 함수) 특정 부품을 포함하는 넷을 찾아서 반환합니다.
    /// </summary>
    private HashSet<ElectricalComponent> FindNetContaining(ElectricalComponent component)
    {
        foreach (var net in nets)
        {
            if (net.Contains(component))
            {
                return net;
            }
        }
        return null;
    }
}