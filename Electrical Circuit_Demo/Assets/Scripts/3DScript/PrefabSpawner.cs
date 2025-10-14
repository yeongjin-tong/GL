using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI; // Dropdown�� ����ϱ� ���� �߰�

public class PrefabSpawner : MonoBehaviour
{
    [Tooltip("�ν����Ϳ��� ��Ӵٿ� UI�� �����մϴ�.")]
    public TMP_Dropdown prefabDropdown;
    [Tooltip("��Ӵٿ� ������ ���� ������ ������ ����� �����մϴ�.")]
    public List<GameObject> prefabList;

    // ���� ���콺�� ����ٴϴ�, ���� ��ġ���� ���� ������
    private GameObject currentPlacingPrefab;

    public Transform content_3D;

    void Start()
    {
        // ��Ӵٿ��� ���� ����� ������ InstantiatePrefabFromDropdown �Լ��� ȣ��ǵ��� �̺�Ʈ ����
        prefabDropdown.onValueChanged.AddListener(InstantiatePrefabFromDropdown);

    }

    void Update()
    {
        // 1. ���� ��ġ ���� �������� �ִٸ�
        if (currentPlacingPrefab != null)
        {
            // 2. ���콺 ��ġ�� ����ٴϵ��� ��
            Vector3 mouseWorldPos = GetMouseWorldPosition();
            currentPlacingPrefab.transform.position = mouseWorldPos;

            // 3. ���콺 ���� ��ư�� �ٽ� Ŭ���ϸ�
            if (Input.GetMouseButtonDown(0))
            {
                // 4. ��ġ�� �Ϸ��ϰ�, ���� ������ ���� currentPlacingPrefab�� ���
                Debug.Log($"{currentPlacingPrefab.name} ��ġ �Ϸ�!");
                currentPlacingPrefab.transform.position = new Vector3(currentPlacingPrefab.transform.position.x, currentPlacingPrefab.transform.position.y, -0.1f);
                currentPlacingPrefab = null;
            }
        }
    }

    /// <summary>
    /// ��Ӵٿ�� �� �׸��� �������� �� ȣ��Ǵ� �Լ��Դϴ�.
    /// </summary>
    /// <param name="index">���õ� ��Ӵٿ� �׸��� ����(index)</param>
    public void InstantiatePrefabFromDropdown(int index)
    {
        prefabDropdown.value = 0; 

        if (index > 0)
        {
            int prefabIndex = index - 1;

            if (prefabIndex < prefabList.Count)
            {
                Debug.Log($"��Ӵٿ�� {prefabList[prefabIndex].name} ���õ�. ���� ����!");
                currentPlacingPrefab = Instantiate(prefabList[prefabIndex], content_3D);
                ObjectManager.Instance.objects_3d.Add(currentPlacingPrefab);
            }
        }
    }

    private Vector3 GetMouseWorldPosition()
    {
        Vector3 mousePoint = Input.mousePosition;
        mousePoint.z = -Camera.main.transform.position.z;
        return Camera.main.ScreenToWorldPoint(mousePoint);
    }
}