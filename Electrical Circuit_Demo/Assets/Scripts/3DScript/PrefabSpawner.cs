using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI; // Dropdown을 사용하기 위해 추가

public class PrefabSpawner : MonoBehaviour
{
    [Tooltip("인스펙터에서 드롭다운 UI를 연결합니다.")]
    public TMP_Dropdown prefabDropdown;
    [Tooltip("드롭다운 순서에 맞춰 생성할 프리팹 목록을 연결합니다.")]
    public List<GameObject> prefabList;

    // 현재 마우스를 따라다니는, 아직 배치되지 않은 프리팹
    private GameObject currentPlacingPrefab;

    public Transform content_3D;

    void Start()
    {
        // 드롭다운의 값이 변경될 때마다 InstantiatePrefabFromDropdown 함수가 호출되도록 이벤트 연결
        prefabDropdown.onValueChanged.AddListener(InstantiatePrefabFromDropdown);

    }

    void Update()
    {
        // 1. 현재 배치 중인 프리팹이 있다면
        if (currentPlacingPrefab != null)
        {
            // 2. 마우스 위치를 따라다니도록 함
            Vector3 mouseWorldPos = GetMouseWorldPosition();
            currentPlacingPrefab.transform.position = mouseWorldPos;

            // 3. 마우스 왼쪽 버튼을 다시 클릭하면
            if (Input.GetMouseButtonDown(0))
            {
                // 4. 배치를 완료하고, 다음 생성을 위해 currentPlacingPrefab을 비움
                Debug.Log($"{currentPlacingPrefab.name} 배치 완료!");
                currentPlacingPrefab.transform.position = new Vector3(currentPlacingPrefab.transform.position.x, currentPlacingPrefab.transform.position.y, -0.1f);
                currentPlacingPrefab = null;
            }
        }
    }

    /// <summary>
    /// 드롭다운에서 새 항목을 선택했을 때 호출되는 함수입니다.
    /// </summary>
    /// <param name="index">선택된 드롭다운 항목의 순번(index)</param>
    public void InstantiatePrefabFromDropdown(int index)
    {
        prefabDropdown.value = 0; 

        if (index > 0)
        {
            int prefabIndex = index - 1;

            if (prefabIndex < prefabList.Count)
            {
                Debug.Log($"드롭다운에서 {prefabList[prefabIndex].name} 선택됨. 생성 시작!");
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