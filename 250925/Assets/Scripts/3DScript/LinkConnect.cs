using System.Collections.Generic;
using System.Linq; // LINQ�� ����ϱ� ���� �߰�
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LinkConnect : MonoBehaviour
{
    public Button linkBtn;
    // public GameObject content3d; // ObjectManager���� �����ϹǷ� �� �̻� �ʿ� ���� �� �ֽ��ϴ�.

    private void Start()
    {
        linkBtn.onClick.AddListener(CheckLinkAndConnect);
    }

    /// <summary>
    /// 2D�� 3D ȸ���� ���� ���¸� ���ϰ�, ��ġ�ϸ� ��Ʈ�ʷ� �����մϴ�.
    /// </summary>
    private void CheckLinkAndConnect()
    {
        // 1. ObjectManager���� 2D �� 3D ������Ʈ ����� �����ɴϴ�.
        //    �߰��� �ı��� ������Ʈ�� ���� �� ������ ����� �����մϴ�.
        ObjectManager.Instance.CleanUpList();
        List<GameObject> objects_2d = ObjectManager.Instance.objects_2d;
        List<GameObject> objects_3d = ObjectManager.Instance.objects_3d;

        // 2. 3D ������Ʈ�� �±�(Tag)�� Ű�� �ϴ� Dictionary�� ��ȯ�Ͽ� �˻� �ӵ��� ���Դϴ�.
        Dictionary<string, GameObject> objects3D_ByTag = objects_3d.ToDictionary(obj => obj.tag, obj => obj);

        // 3. 2D�� 3D ��ǰ ������ �ٸ��� ��� ���� ó���մϴ�.
        if (objects_2d.Count != objects_3d.Count)
        {
            UpdateLinkButtonText("��ũ ����: ��ǰ ���� ����ġ");
            return;
        }

        // 4. ��� 2D ��ǰ�� 3D ��Ʈ�ʿ� ���������� ��Ī�Ǵ��� Ȯ���մϴ�.
        bool allMatched = objects_2d.All(obj_2d => AreConnectionsMatching(obj_2d, objects3D_ByTag));

        if (allMatched)
        {
            // 5. ��ũ ���� ��, ��Ī�� ��ü���� Dictionary�� �����ϰ� ��Ʈ�ʷ� �����մϴ�.
            ObjectManager.Instance.objectMatching.Clear();
            foreach (var obj_2d in objects_2d)
            {
                if (objects3D_ByTag.TryGetValue(obj_2d.tag, out GameObject obj_3d))
                {
                    // ObjectManager�� ��Ī ���� ����
                    ObjectManager.Instance.objectMatching.Add(obj_3d, obj_2d);

                    // ����ġ ������Ʈ���� ��Ʈ�ʷ� ����
                    LinkSwitchPartners(obj_2d, obj_3d);
                    LinkBatteryPartners(obj_2d, obj_3d);
                }
            }
            UpdateLinkButtonText("��ũ ����!");
        }
        else
        {
            UpdateLinkButtonText("��ũ ����: ���� ���� ����ġ");
        }
    }

    /// <summary>
    /// 2D ��ǰ �ϳ��� 3D ��Ʈ�ʿ� �ùٸ��� ����Ǿ����� Ȯ���ϴ� �Լ��Դϴ�.
    /// </summary>
    private bool AreConnectionsMatching(GameObject obj_2d, Dictionary<string, GameObject> objects3D_ByTag)
    {
        // 3D ��Ʈ�ʸ� �±׷� ã���ϴ�.
        if (!objects3D_ByTag.TryGetValue(obj_2d.tag, out GameObject obj_3d))
        {
            Debug.LogWarning($"���: �±� '{obj_2d.tag}'�� �ش��ϴ� 3D ��ǰ�� ã�� �� �����ϴ�.");
            return false;
        }

        // �� ��ǰ�� ElectricalComponent�� �����ɴϴ�.
        var comp2D = obj_2d.GetComponent<ElectricalComponent>();
        var comp3D = obj_3d.GetComponent<ElectricalComponent>();
        if (comp2D == null || comp3D == null) return false;

        // ����� ������ ������ �ٸ��� �����Դϴ�.
        if (comp2D.connectedComponents.Count != comp3D.connectedComponents.Count)
        {
            return false;
        }

        // ����� ������ ���ٸ� �������� �����մϴ�.
        if (comp2D.connectedComponents.Count == 0)
        {
            return true;
        }

        // ����� ��ǰ���� �±� ����� �����Ͽ� ���մϴ�. (������ ������� ������ ��ǰ�� ����Ǿ����� Ȯ��)
        var connectedTags2D = comp2D.connectedComponents.Select(c => c.tag).OrderBy(tag => tag).ToList();
        var connectedTags3D = comp3D.connectedComponents.Select(c => c.tag).OrderBy(tag => tag).ToList();

        return connectedTags2D.SequenceEqual(connectedTags3D);
    }

    /// <summary>
    /// 2D�� 3D ����ġ ������Ʈ�� ã�� ������ 'linkedPartner'�� �����մϴ�.
    /// </summary>
    private void LinkSwitchPartners(GameObject obj_2d, GameObject obj_3d)
    {
        Switch switch2D = obj_2d.GetComponent<Switch>();
        Switch switch3D = obj_3d.GetComponent<Switch>();

        if (switch2D != null && switch3D != null)
        {
            switch2D.linkedPartner = switch3D;
            switch3D.linkedPartner = switch2D;
        }
    }

    private void LinkBatteryPartners(GameObject obj_2d, GameObject obj_3d)
    {
        Battery battery2D = obj_2d.GetComponent<Battery>();
        Battery battery3D = obj_3d.GetComponent<Battery>();

        if (battery2D != null && battery3D != null)
        {
            battery2D.linkedPartner = battery3D;
            battery3D.linkedPartner = battery2D;
        }
    }



    /// <summary>
    /// ��ũ ��ư�� �ؽ�Ʈ�� ������Ʈ�ϴ� ���� �Լ��Դϴ�.
    /// </summary>
    private void UpdateLinkButtonText(string message)
    {
        TextMeshProUGUI btnText = linkBtn.GetComponentInChildren<TextMeshProUGUI>();
        if (btnText != null)
        {
            btnText.text = message;
        }
    }
}