using UnityEngine;
using UnityEngine.UI; // UI 관련 필수 추가
using Oculus.Interaction;

public class GearSpawnManager : MonoBehaviour
{
    public GameObject gearPrefab;
    public Transform rayOrigin;
    public LayerMask workbenchLayer;

    [Header("UI Feedback")]
    public Button createButton;      // 패널창의 'Create' 버튼
    public Image statusIndicator;    // (옵션) 현재 잘 조준되고 있는지 컬러박스/이미지
    public Color activeColor = Color.green;
    public Color inactiveColor = Color.red;

    private GameObject currentGear;
    private bool isPointingWorkbench = false;

    void Update()
    {
        CheckWorkbenchRaycast();
    }

    private void CheckWorkbenchRaycast()
    {
        // 1. 레이캐스트로 작업대 레이어 충돌 확인
        RaycastHit hit;
        isPointingWorkbench = Physics.Raycast(rayOrigin.position, rayOrigin.forward, out hit, 10f, workbenchLayer);

        // 2. 버튼 활성화/비활성화 처리
        if (createButton != null)
        {
            createButton.interactable = isPointingWorkbench;
        }

        // 3. 시각적 피드백 (이미지 색상 변경)
        if (statusIndicator != null)
        {
            statusIndicator.color = isPointingWorkbench ? activeColor : inactiveColor;
        }
    }

    public void SpawnOnWorkbench()
    {
        // 인터랙트 가능하지 않으면 조기 리턴 (안전장치)
        if (!isPointingWorkbench) return;

        if (currentGear != null) Destroy(currentGear);

        RaycastHit hit;
        if (Physics.Raycast(rayOrigin.position, rayOrigin.forward, out hit, 10f, workbenchLayer))
        {
            Vector3 spawnPos = hit.point + (Vector3.up * 0.05f);
            currentGear = Instantiate(gearPrefab, spawnPos, Quaternion.identity);

            // 생성 직후 모든 부품을 초기 상태 세팅
            PartController[] parts = currentGear.GetComponentsInChildren<PartController>();
            foreach (var part in parts)
            {
                part.InitPart(); // 부품의 로컬 위치 및 회전값 초기화
            }
        }
    }
}
