using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Oculus.Interaction;

public class BoltInteraction : MonoBehaviour
{
    private PartController partController;
    private Grabbable spannerGrabbable;

    private void Awake()
    {
        partController = GetComponent<PartController>();
    }

    private void OnTriggerEnter(Collider other)
    {
        // 1. 충돌 객체가 'Tool' 태그를 가졌는지 확인
        if (other.CompareTag("Tool"))
        {
            // 2. 해당 스패너가 실제 플레이어 손에 잡혀 있는지 확인 (필수 조건)
            spannerGrabbable = other.GetComponentInParent<Grabbable>();

            if (spannerGrabbable != null && spannerGrabbable.SelectingPointsCount > 0)
            {
                // 3. 조건을 만족한 볼트의 동작 실행
                Debug.Log("스패너 접촉 : 볼트 회전 시작");
                partController.OnXRInteract();
            }
        }
    }
}
