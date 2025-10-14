using System.Collections.Generic;
using System.Linq; // LINQ를 사용하기 위해 추가
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LinkConnect : MonoBehaviour
{
    public Button linkBtn;
    // public GameObject content3d; // ObjectManager에서 관리하므로 더 이상 필요 없을 수 있습니다.

    private void Start()
    {
        linkBtn.onClick.AddListener(CheckLinkAndConnect);
    }

    /// <summary>
    /// 2D와 3D 회로의 연결 상태를 비교하고, 일치하면 파트너로 연결합니다.
    /// </summary>
    private void CheckLinkAndConnect()
    {
        // 1. ObjectManager에서 2D 및 3D 오브젝트 목록을 가져옵니다.
        //    중간에 파괴된 오브젝트가 있을 수 있으니 목록을 정리합니다.
        ObjectManager.Instance.CleanUpList();
        List<GameObject> objects_2d = ObjectManager.Instance.objects_2d;
        List<GameObject> objects_3d = ObjectManager.Instance.objects_3d;

        // 2. 3D 오브젝트를 태그(Tag)를 키로 하는 Dictionary로 변환하여 검색 속도를 높입니다.
        Dictionary<string, GameObject> objects3D_ByTag = objects_3d.ToDictionary(obj => obj.tag, obj => obj);

        // 3. 2D와 3D 부품 개수가 다르면 즉시 실패 처리합니다.
        if (objects_2d.Count != objects_3d.Count)
        {
            UpdateLinkButtonText("링크 실패: 부품 개수 불일치");
            return;
        }

        // 4. 모든 2D 부품이 3D 파트너와 성공적으로 매칭되는지 확인합니다.
        bool allMatched = objects_2d.All(obj_2d => AreConnectionsMatching(obj_2d, objects3D_ByTag));

        if (allMatched)
        {
            // 5. 링크 성공 시, 매칭된 객체들을 Dictionary에 저장하고 파트너로 연결합니다.
            ObjectManager.Instance.objectMatching.Clear();
            foreach (var obj_2d in objects_2d)
            {
                if (objects3D_ByTag.TryGetValue(obj_2d.tag, out GameObject obj_3d))
                {
                    // ObjectManager에 매칭 정보 저장
                    ObjectManager.Instance.objectMatching.Add(obj_3d, obj_2d);

                    // 스위치 컴포넌트끼리 파트너로 연결
                    LinkSwitchPartners(obj_2d, obj_3d);
                    LinkBatteryPartners(obj_2d, obj_3d);
                }
            }
            UpdateLinkButtonText("링크 성공!");
        }
        else
        {
            UpdateLinkButtonText("링크 실패: 연결 상태 불일치");
        }
    }

    /// <summary>
    /// 2D 부품 하나가 3D 파트너와 올바르게 연결되었는지 확인하는 함수입니다.
    /// </summary>
    private bool AreConnectionsMatching(GameObject obj_2d, Dictionary<string, GameObject> objects3D_ByTag)
    {
        if (!objects3D_ByTag.TryGetValue(obj_2d.tag, out GameObject obj_3d))
        {
            Debug.LogWarning($"경고: 태그 '{obj_2d.tag}'에 해당하는 3D 부품을 찾을 수 없습니다.");
            return false;
        }

        var comp2D = obj_2d.GetComponent<ElectricalComponent>();
        var comp3D = obj_3d.GetComponent<ElectricalComponent>();
        if (comp2D == null || comp3D == null) return false;

        // ✨ 연결된 전선의 개수를 connections.Count로 비교합니다.
        if (comp2D.connections.Count != comp3D.connections.Count)
        {
            return false;
        }

        if (comp2D.connections.Count == 0)
        {
            return true;
        }

        // ✨ connections 리스트에서 연결된 부품들의 태그 목록을 만들어 비교합니다.
        var connectedTags2D = comp2D.connections.Select(c => c.connectedComponent.tag).OrderBy(tag => tag).ToList();
        var connectedTags3D = comp3D.connections.Select(c => c.connectedComponent.tag).OrderBy(tag => tag).ToList();

        return connectedTags2D.SequenceEqual(connectedTags3D);
    }

    /// <summary>
    /// 2D와 3D 스위치 컴포넌트를 찾아 서로의 'linkedPartner'로 지정합니다.
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
    /// 링크 버튼의 텍스트를 업데이트하는 헬퍼 함수입니다.
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