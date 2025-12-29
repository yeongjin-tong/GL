using UnityEngine;
using System.Collections;
using Oculus.Interaction;
using Oculus.Interaction.HandGrab;

public class ManualBoltUnscrew : MonoBehaviour
{
    private PartController partController;
    public Transform spannerSocket;        // 볼트 중심에 위치할 빈 오브젝트 (회전축 기준점)

    [Header("회전 설정")]
    public float targetRotations = 3f;
    public Vector3 rotationAxis = Vector3.up; // 회전할 축 (기본: Y축, 볼트에 맞게 조정)
    
    private float totalRotation = 0f;
    private float lastAngle = 0f;
    private bool isSnapped = false;
    private GameObject attachedSpanner;      // 자식 오브젝트 (컴포넌트가 있는)
    private Transform spannerRoot;           // 부모 오브젝트 (Transform만, 위치/회전 제어)
    private Rigidbody spannerRb;
    private Grabbable spannerGrabbable;
    
    // 고정 위치 및 회전
    private Vector3 fixedWorldPosition;      // 고정할 월드 위치
    private Quaternion fixedWorldRotation;   // 고정할 월드 회전 (축 제외)
    private Vector3 rotationAxisWorld;       // 월드 공간의 회전축
    private Vector3 childInitialLocalPosition;  // 자식의 초기 localPosition
    private Quaternion childInitialLocalRotation; // 자식의 초기 localRotation

    private Coroutine snapCoroutine;

    void Awake()
    {
        partController = GetComponent<PartController>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isSnapped) return;

        // 1. 접촉한 뒤 1초 경과 후 SnapSpanner 실행 [요구사항 1]
        if (other.CompareTag("Tool"))
        {
            snapCoroutine = StartCoroutine(WaitAndSnap(other.transform.root.gameObject));
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // 1초가 되기 전에 떨어진 경우는 취소 처리
        if (!isSnapped && snapCoroutine != null)
        {
            StopCoroutine(snapCoroutine);
            snapCoroutine = null;
            Debug.Log("스패너가 1초 전에 떨어져서 고정 취소됨");
        }
    }

    IEnumerator WaitAndSnap(GameObject spanner)
    {
        yield return new WaitForSeconds(1.0f);
        SnapSpanner(spanner);
    }

    public void SnapSpanner(GameObject spanner)
    {
        // spannerSocket이 설정되지 않았다면 에러 로그 출력
        if (spannerSocket == null)
        {
            Debug.LogError("spannerSocket이 설정되지 않았습니다! Inspector에서 설정해주세요.");
            return;
        }

        isSnapped = true;
        
        // 🔧 spanner는 이미 루트 오브젝트 (Spanner_Root)
        spannerRoot = spanner.transform;
        attachedSpanner = spanner;
        
        // 🔧 Rigidbody와 Grabbable은 자식에서 찾기
        spannerRb = spanner.GetComponentInChildren<Rigidbody>();
        spannerGrabbable = spanner.GetComponentInChildren<Grabbable>();
        
        if (spannerRb == null)
        {
            Debug.LogError("Rigidbody를 자식에서 찾을 수 없습니다!");
        }
        if (spannerGrabbable == null)
        {
            Debug.LogError("Grabbable을 자식에서 찾을 수 없습니다!");
        }

        // 🔧 Rigidbody를 kinematic으로 설정 (물리 계산 비활성화로 렉 방지)
        if (spannerRb != null)
        {
            spannerRb.isKinematic = true; // kinematic으로 설정해야 물리 충돌 없음
            spannerRb.useGravity = false;
            spannerRb.interpolation = RigidbodyInterpolation.None; // 보간 끄기
            
            Debug.Log($"Rigidbody 설정: isKinematic={spannerRb.isKinematic}, useGravity={spannerRb.useGravity}");
        }
        else
        {
            Debug.LogError("Rigidbody를 찾을 수 없습니다!");
        }

        // 🆕 어느 손으로 잡고 있는지 감지
        bool isLeftHand = DetectHandedness(spanner);

        // 🆕 손에 따라 다른 위치/회전 사용
        Vector3 targetLocalPos;
        Vector3 targetLocalRot;
        
        if (isLeftHand)
        {
            targetLocalPos = partController.spawnerPositionLeft;
            targetLocalRot = partController.spawnerRotationLeft;
            Debug.Log("왼손으로 스패너 고정");
        }
        else
        {
            targetLocalPos = partController.spawnerPositionRight;
            targetLocalRot = partController.spawnerRotationRight;
            Debug.Log("오른손으로 스패너 고정");
        }
        
        Quaternion targetLocalRotQuat = Quaternion.Euler(targetLocalRot);
        
        // 월드 좌표 직접 사용 or 로컬 좌표 변환
        if (partController.useWorldCoordinates)
        {
            // 🌍 월드 좌표 직접 사용 (간편!)
            fixedWorldPosition = targetLocalPos;
            fixedWorldRotation = targetLocalRotQuat;
            
            Debug.Log($"[월드 좌표 직접 사용] 위치: {fixedWorldPosition}, 회전: {targetLocalRot}");
        }
        else
        {
            // 📍 로컬 좌표를 월드 좌표로 변환
            fixedWorldPosition = spannerSocket.TransformPoint(targetLocalPos);
            fixedWorldRotation = spannerSocket.rotation * targetLocalRotQuat;
            
            Debug.Log($"[좌표 변환] 로컬: {targetLocalPos} → 월드: {fixedWorldPosition}");
            Debug.Log($"[좌표 변환] SpannerSocket 위치: {spannerSocket.position}");
        }
        
        // 🔧 자식의 초기 localPosition/Rotation 저장 (중요!)
        if (spannerRb != null)
        {
            childInitialLocalPosition = spannerRb.transform.localPosition;
            childInitialLocalRotation = spannerRb.transform.localRotation;
            Debug.Log($"자식 초기 LocalPos: {childInitialLocalPosition}, LocalRot: {childInitialLocalRotation.eulerAngles}");
        }

        // 🔧 부모(Spanner_Root)의 Transform만 목표 위치/회전으로 이동
        spannerRoot.position = fixedWorldPosition;
        spannerRoot.rotation = fixedWorldRotation;

        // 회전축을 월드 공간으로 변환
        rotationAxisWorld = spannerSocket.TransformDirection(rotationAxis);

        // 초기 각도 저장 (회전축 기준) - 부모 rotation 사용
        lastAngle = GetAngleAroundAxis(spannerRoot.rotation);
        totalRotation = 0f;
        
        Debug.Log($"=== 스패너 고정 디버그 ===");
        Debug.Log($"🔧 부모(제어 대상): {spannerRoot.name}");
        Debug.Log($"  └ 위치: {spannerRoot.position}");
        Debug.Log($"  └ 회전: {spannerRoot.rotation.eulerAngles}");
        Debug.Log($"📦 자식(컴포넌트): {(spannerRb != null ? spannerRb.gameObject.name : "없음")}");
        Debug.Log($"  └ Rigidbody isKinematic: {spannerRb?.isKinematic}");
        Debug.Log($"  └ Grabbable: {(spannerGrabbable != null ? "✅" : "❌")}");
        Debug.Log($"🎯 고정 목표 위치: {fixedWorldPosition}");
        Debug.Log($"======================");
    }

    void Update()
    {
        if (!isSnapped || attachedSpanner == null) return;

        // 🆕 스패너를 실제로 잡고 있는지 체크
        bool isGrabbed = false;
        if (spannerGrabbable != null)
        {
            isGrabbed = spannerGrabbable.SelectingPointsCount > 0;
        }

        // 잡고 있지 않으면 회전 추적 안 함
        if (!isGrabbed)
        {
            return;
        }

        // 회전량 추적 (잡고 있을 때만) - 부모(Spanner_Root) rotation 사용
        float currentAngle = GetAngleAroundAxis(spannerRoot.rotation);
        float deltaAngle = Mathf.DeltaAngle(lastAngle, currentAngle);

        // 🔍 디버그: 회전 감지 확인
        if (Mathf.Abs(deltaAngle) > 0.1f)
        {
            Debug.Log($"[회전 감지] currentAngle: {currentAngle:F1}°, lastAngle: {lastAngle:F1}°, delta: {deltaAngle:F1}°");
        }

        // 시계/반시계 방향을 구분하지 않고 절대값 누적
        if (Mathf.Abs(deltaAngle) > 0.5f) // 떨림 방지
        {
            totalRotation += Mathf.Abs(deltaAngle);

            // 볼트가 점진적으로 풀리는 시각적 효과 (회전축 방향으로 이동)
            transform.Translate(rotationAxisWorld * Mathf.Abs(deltaAngle) * 0.00001f, Space.World);
            
            Debug.Log($"✅ 회전 진행: {totalRotation:F1}° / {targetRotations * 360f}°");
        }

        // 완료 조건 체크
        if (totalRotation >= targetRotations * 360f)
        {
            FinishUnscrewing();
        }

        lastAngle = currentAngle;
    }

    private float debugTimer = 0f;
    
    void FixedUpdate()
    {
        if (!isSnapped || attachedSpanner == null) return;

        // 🔧 Rigidbody가 kinematic인지 확인하고, 아니면 다시 설정!
        if (spannerRb != null)
        {
            if (!spannerRb.isKinematic)
            {
                Debug.LogWarning("Rigidbody가 kinematic이 아닙니다! 다시 설정합니다.");
                spannerRb.isKinematic = true;
                spannerRb.useGravity = false;
            }
            
            // 🔧 자식의 localPosition/Rotation을 초기값으로 강제 고정!
            spannerRb.transform.localPosition = childInitialLocalPosition;
            spannerRb.transform.localRotation = childInitialLocalRotation;
        }
        
        // 🔧 부모 위치 고정
        spannerRoot.position = fixedWorldPosition;
    }
    
    void LateUpdate()
    {
        if (!isSnapped || attachedSpanner == null) return;

        // 🔧 잡고 있는지 확인
        bool isGrabbed = spannerGrabbable != null && spannerGrabbable.SelectingPointsCount > 0;
        
        // 디버그: 1초마다 위치 확인
        debugTimer += Time.deltaTime;
        if (debugTimer >= 1f)
        {
            float distance = Vector3.Distance(spannerRoot.position, fixedWorldPosition);
            Debug.Log($"[LateUpdate] 부모 거리: {distance:F3}, 잡힘: {isGrabbed}, RB kinematic: {spannerRb?.isKinematic}");
            debugTimer = 0f;
        }
        
        // 1. 부모(Spanner_Root)의 Position을 항상 고정
        spannerRoot.position = fixedWorldPosition;

        // 2. 부모(Spanner_Root)의 Rotation만 제어
        if (isGrabbed)
        {
            // 잡고 있을 때: 자식의 rotation을 읽어서 부모에 적용
            Transform childTransform = spannerRb != null ? spannerRb.transform : spannerRoot;
            float currentAngle = GetAngleAroundAxis(childTransform.rotation);
            
            // 부모의 rotation만 변경
            spannerRoot.rotation = fixedWorldRotation * Quaternion.AngleAxis(currentAngle, rotationAxis);
        }
        else
        {
            // 놓았을 때: 부모의 rotation을 마지막 각도로 고정
            spannerRoot.rotation = fixedWorldRotation * Quaternion.AngleAxis(lastAngle, rotationAxis);
        }
    }

    // 회전축을 중심으로 한 각도 계산
    private float GetAngleAroundAxis(Quaternion rotation)
    {
        // 기준 방향 벡터 (회전축에 수직인 벡터)
        Vector3 referenceVector = Vector3.ProjectOnPlane(fixedWorldRotation * Vector3.forward, rotationAxisWorld);
        Vector3 currentVector = Vector3.ProjectOnPlane(rotation * Vector3.forward, rotationAxisWorld);
        
        if (referenceVector.sqrMagnitude < 0.001f || currentVector.sqrMagnitude < 0.001f)
        {
            Debug.LogWarning($"[GetAngleAroundAxis] 벡터 크기 너무 작음! ref: {referenceVector.sqrMagnitude}, cur: {currentVector.sqrMagnitude}");
            return 0f;
        }
        
        referenceVector.Normalize();
        currentVector.Normalize();
        
        float angle = Vector3.SignedAngle(referenceVector, currentVector, rotationAxisWorld);
        return angle;
    }

    // 어느 손으로 잡고 있는지 감지
    private bool DetectHandedness(GameObject spanner)
    {
        // 방법 1: 스패너의 현재 위치로 판단 (가장 간단하고 확실)
        // 카메라(머리) 기준으로 왼쪽에 있으면 왼손, 오른쪽에 있으면 오른손
        if (Camera.main != null)
        {
            Transform cameraTransform = Camera.main.transform;
            Vector3 localPos = cameraTransform.InverseTransformPoint(spanner.transform.position);
            
            bool isLeft = localPos.x < 0;
            Debug.Log($"손 감지: {(isLeft ? "왼손" : "오른손")} (카메라 기준 로컬 X: {localPos.x:F2})");
            return isLeft;
        }
        
        // 방법 2: OVRCameraRig 사용 (카메라가 없을 경우)
        OVRCameraRig cameraRig = FindObjectOfType<OVRCameraRig>();
        if (cameraRig != null && cameraRig.centerEyeAnchor != null)
        {
            Transform centerEye = cameraRig.centerEyeAnchor;
            Vector3 localPos = centerEye.InverseTransformPoint(spanner.transform.position);
            
            bool isLeft = localPos.x < 0;
            Debug.Log($"손 감지 (OVR): {(isLeft ? "왼손" : "오른손")} (로컬 X: {localPos.x:F2})");
            return isLeft;
        }
        
        // 방법 3: 스패너 이름에 "Left" 또는 "Right" 포함 여부로 판단 (폴백)
        string spannerName = spanner.name.ToLower();
        if (spannerName.Contains("left"))
        {
            Debug.Log("스패너 이름으로 왼손 감지");
            return true;
        }
        if (spannerName.Contains("right"))
        {
            Debug.Log("스패너 이름으로 오른손 감지");
            return false;
        }
        
        // 기본값: 오른손으로 처리
        Debug.Log("손 감지 실패 - 기본값(오른손) 사용");
        return false;
    }

    void FinishUnscrewing()
    {
        isSnapped = false;
        
        // 스패너를 원래 상태로 복구 (잡을 수 있고 중력 영향 받음)
        if (attachedSpanner != null)
        {
            if (spannerRb != null)
            {
                // Kinematic 해제 (자유롭게 움직일 수 있게)
                spannerRb.isKinematic = false;
                spannerRb.useGravity = true;
            }
            
            // 더 이상 필요 없다면 파괴 (또는 유지)
            // Destroy(attachedSpanner, 1.0f);
        }
        
        partController.ExecutePartAction();
        
        Debug.Log($"볼트 풀기 완료! 총 회전량: {totalRotation / 360f:F1}바퀴");
    }
}
