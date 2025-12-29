using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectManager : MonoBehaviour
{
    public static ObjectManager Instance;
    public Transform cameraTransform;
    public GameObject prefabToSpawn;
    public float distanceInfront = 1.0f;

    [HideInInspector]
    public GameObject currentObject;

    private void Awake()
    {
        Instance = this;
    }

    public void SpawnInFrontOfMe(Transform tr)
    {
        Vector3 spawnPos = cameraTransform.position + (cameraTransform.forward * distanceInfront);

        // 2. 바닥에 평행하게 스폰되기 원하면 y축 높이 고정 (옵션 사항)
        // spawnPos.y = cameraTransform.position.y; 

        // 3. 생성 된 객체는 카메라 방향 바라본 위치시킴
        Quaternion spawnRot = Quaternion.LookRotation(cameraTransform.forward);

        // 4. 전달된 객체 즉 위치 세팅
        tr.transform.position = spawnPos;
        tr.transform.rotation = spawnRot;

        // 프리팹이 원본에서 너무 크면 여기서 축소 (예시 0.1 배로 축소)
        //spawnedObj.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
    }
}
