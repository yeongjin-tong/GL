using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// ȸ���� ��� ������ ���� ���¸� �߾ӿ��� �����ϴ� �̱��� Ŭ�����Դϴ�.
/// '���� �׷�(Net)'�� ������ ����Ͽ� � ��ǰ���� ���������� ����Ǿ� �ִ��� �����մϴ�.
/// </summary>
public class CircuitGraph : MonoBehaviour
{
    // --- �̱��� �ν��Ͻ� ---
    public static CircuitGraph Instance { get; private set; }

    /// <summary>
    /// ���������� ����� ��� ��ǰ���� '�׷�(Net)'�� ��� �ִ� ����Ʈ.
    /// �� HashSet�� �ϳ��� ������ ������ ��带 ��Ÿ���ϴ�.
    /// </summary>
    private List<HashSet<ElectricalComponent>> nets;

    private void Awake()
    {
        // �̱��� ���� ����
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }

        // �� ����Ʈ �ʱ�ȭ
        nets = new List<HashSet<ElectricalComponent>>();
    }

    /// <summary>
    /// �� ��ǰ ���̿� ���ο� ������ ����մϴ�.
    /// �� ��ǰ�� ���� ���� ã��, �ʿ��ϴٸ� �� ���� �ϳ��� ��Ĩ�ϴ�.
    /// </summary>
    /// <param name="compA">������ ù ��° ��ǰ</param>
    /// <param name="compB">������ �� ��° ��ǰ</param>
    public void RegisterConnection(ElectricalComponent compA, ElectricalComponent compB)
    {
        var netA = FindNetContaining(compA);
        var netB = FindNetContaining(compB);

        if (netA == null && netB == null)
        {
            // Case 1: �� ��ǰ ��� � �ݿ��� �������� ���� ��� -> ���ο� �� ����
            var newNet = new HashSet<ElectricalComponent> { compA, compB };
            nets.Add(newNet);
        }
        else if (netA != null && netB == null)
        {
            // Case 2: compA�� �ݿ� ���� ��� -> compB�� compA�� �ݿ� �߰�
            netA.Add(compB);
        }
        else if (netA == null && netB != null)
        {
            // Case 3: compB�� �ݿ� ���� ��� -> compA�� compB�� �ݿ� �߰�
            netB.Add(compA);
        }
        else if (netA != netB)
        {
            // Case 4: �� ��ǰ�� ���� �ٸ� �ݿ� ���� ��� -> �� ���� �ϳ��� ����
            netA.UnionWith(netB); // netB�� ��� ��Ҹ� netA�� �߰� (�ߺ��� �˾Ƽ� ó����)
            nets.Remove(netB);    // ���� ����ִ� netB�� ����Ʈ���� ����
        }
        // Case 5 (else if netA == netB): �� ��ǰ�� �̹� ���� �ݿ� ���� ��� -> �ƹ��͵� ���� ����
    }

    /// <summary>
    /// Ư�� ��ǰ�� ���� ���� �׷�(Net) ��ü�� ��ȯ�մϴ�.
    /// ȸ�� �м� �� �� �Լ��� ���� ����� ��� ��ǰ�� �� ���� ������ �� �ֽ��ϴ�.
    /// </summary>
    /// <param name="component">ã���� �ϴ� ��ǰ</param>
    /// <returns>��ǰ�� ���� HashSet ������ ��. ��ǰ�� � �ݿ��� ������ null�� ��ȯ�մϴ�.</returns>
    public HashSet<ElectricalComponent> GetNetFor(ElectricalComponent component)
    {
        return FindNetContaining(component);
    }

    /// <summary>
    /// Ư�� ��ǰ�� �׷������� �����մϴ�.
    /// (��: ����ڰ� ��ǰ�� �������� �� ȣ��)
    /// </summary>
    /// <param name="componentToRemove">������ ��ǰ</param>
    public void RemoveComponent(ElectricalComponent componentToRemove)
    {
        var net = FindNetContaining(componentToRemove);
        if (net != null)
        {
            net.Remove(componentToRemove);
            // ���� �ݿ� ��ǰ�� �ϳ��� ���ų� ��� �Ǹ�, �ش� ���� �����ϴ� ������ �߰��� �� �ֽ��ϴ�.
            if (net.Count < 2)
            {
                nets.Remove(net);
            }
        }
    }

    /// <summary>
    /// (����) ���� ȸ���� ��� ���� ������ �ʱ�ȭ�մϴ�.
    /// </summary>
    public void ClearGraph()
    {
        nets.Clear();
    }


    /// <summary>
    /// (���� �Լ�) Ư�� ��ǰ�� �����ϴ� ���� ã�Ƽ� ��ȯ�մϴ�.
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