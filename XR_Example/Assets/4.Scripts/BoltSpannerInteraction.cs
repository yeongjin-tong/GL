using UnityEngine;
using System.Collections;
using Oculus.Interaction;

/// <summary>
/// 스패너를 볼트에 고정하고 손 회전으로 풀기
/// 부모-자식 관계 없이 독립적으로 동작
/// </summary>
public class BoltSpannerInteraction : MonoBehaviour
{
    [Header("=== 기본 설정 ===")]
    [Tooltip("필요한 회전 바퀴 수")]
    public float requiredRotations = 3f;

    [Header("=== 왼손 스패너 설정 (월드 좌표) ===")]
    public Vector3 leftHandWorldPosition = Vector3.zero;
    public Vector3 leftHandWorldRotation = Vector3.zero;

    [Header("=== 오른손 스패너 설정 (월드 좌표) ===")]
    public Vector3 rightHandWorldPosition = Vector3.zero;
    public Vector3 rightHandWorldRotation = Vector3.zero;

    [Header("=== 회전 설정 ===")]
    [Tooltip("회전 축 (로컬 기준)")]
    public Vector3 rotationAxis = Vector3.forward;

    [Header("=== 디버그 ===")]
    public bool showDebug = true;

    // 내부 변수
    private bool isAttached = false;
    private GameObject spannerObject;
    private Transform spannerTransform;
    private Grabbable spannerGrabbable;
    private Rigidbody spannerRb;
    
    private Vector3 fixedWorldPosition;
    private Quaternion fixedWorldRotation;
    private Vector3 worldRotationAxis;
    
    private float totalRotation = 0f;
    private float lastZRotation = 0f;
    
    private Coroutine attachCoroutine;
    private PartController partController;

    void Awake()
    {
        partController = GetComponent<PartController>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isAttached) return;
        
        if (other.CompareTag("Tool"))
        {
            if (showDebug) Debug.Log("🔧 스패너 감지! 1초 대기...");
            
            GameObject spanner = other.transform.root.gameObject;
            attachCoroutine = StartCoroutine(WaitAndAttach(spanner));
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!isAttached && attachCoroutine != null)
        {
            StopCoroutine(attachCoroutine);
            attachCoroutine = null;
            if (showDebug) Debug.Log("❌ 취소됨");
        }
    }

    IEnumerator WaitAndAttach(GameObject spanner)
    {
        yield return new WaitForSeconds(1.0f);
        AttachSpanner(spanner);
    }

    void AttachSpanner(GameObject spanner)
    {
        if (showDebug) Debug.Log("=== 스패너 고정 시작 ===");
        
        spannerObject = spanner;
        spannerTransform = spanner.transform;
        spannerGrabbable = spanner.GetComponentInChildren<Grabbable>();
        spannerRb = spanner.GetComponentInChildren<Rigidbody>();
        
        // 손 감지
        bool isLeftHand = DetectHand(spanner);
        if (showDebug) Debug.Log($"👋 {(isLeftHand ? "왼손" : "오른손")} 감지");
        
        // 선택된 위치/회전 (월드 좌표)
        fixedWorldPosition = isLeftHand ? leftHandWorldPosition : rightHandWorldPosition;
        fixedWorldRotation = Quaternion.Euler(isLeftHand ? leftHandWorldRotation : rightHandWorldRotation);
        
        // 즉시 설정
        spannerTransform.position = fixedWorldPosition;
        spannerTransform.rotation = fixedWorldRotation;
        
        // 회전 축 (월드 기준으로 변환)
        worldRotationAxis = spannerTransform.TransformDirection(rotationAxis).normalized;
        
        if (showDebug)
        {
            Debug.Log($"🌍 고정 위치: {fixedWorldPosition}");
            Debug.Log($"🔄 고정 회전: {fixedWorldRotation.eulerAngles}");
            Debug.Log($"📐 회전 축: {worldRotationAxis}");
        }
        
        // Rigidbody 설정 - 렉 방지를 위해 완전히 고정
        if (spannerRb != null)
        {
            spannerRb.velocity = Vector3.zero;
            spannerRb.angularVelocity = Vector3.zero;
            spannerRb.isKinematic = true;
            spannerRb.useGravity = false;
            spannerRb.constraints = RigidbodyConstraints.FreezeAll; // 모든 축 고정
        }
        
        // Grabbable 비활성화 - 렉 방지
        if (spannerGrabbable != null)
        {
            spannerGrabbable.enabled = false;
        }
        
        // 초기화 - 정규화된 Z 회전 값 저장
        isAttached = true;
        totalRotation = 0f;
        lastZRotation = NormalizeAngle(fixedWorldRotation.eulerAngles.z);
        
        if (showDebug) Debug.Log($"✅ 스패너 고정 완료! Z축: {lastZRotation:F1}°");
    }
    
    // 각도를 0~360 범위로 정규화
    float NormalizeAngle(float angle)
    {
        angle = angle % 360f;
        if (angle < 0) angle += 360f;
        return angle;
    }

    void Update()
    {
        if (!isAttached || spannerTransform == null) return;
        
        // 손 입력으로 회전 처리 (OVR Input)
        bool isGripping = OVRInput.Get(OVRInput.Button.PrimaryHandTrigger) || 
                          OVRInput.Get(OVRInput.Button.SecondaryHandTrigger);
        
        if (isGripping)
        {
            // 손의 회전 입력 받기
            Vector2 thumbstick = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick);
            if (thumbstick == Vector2.zero)
            {
                thumbstick = OVRInput.Get(OVRInput.Axis2D.SecondaryThumbstick);
            }
            
            // 썸스틱 좌우로 회전
            if (Mathf.Abs(thumbstick.x) > 0.1f)
            {
                float rotationSpeed = 90f; // 초당 90도
                float deltaAngle = thumbstick.x * rotationSpeed * Time.deltaTime;
                
                lastZRotation += deltaAngle;
                lastZRotation = NormalizeAngle(lastZRotation);
                
                totalRotation += Mathf.Abs(deltaAngle);
                
                if (showDebug && Time.frameCount % 30 == 0)
                {
                    Debug.Log($"🔄 회전: {totalRotation:F0}° / {requiredRotations * 360f:F0}°");
                }
                
                // 완료 체크
                if (totalRotation >= requiredRotations * 360f)
                {
                    DetachSpanner();
                }
            }
        }
    }

    void LateUpdate()
    {
        if (!isAttached || spannerTransform == null) return;
        
        // 위치 완전 고정
        spannerTransform.position = fixedWorldPosition;
        
        // 회전 설정: X, Y는 고정, Z는 lastZRotation 사용
        Vector3 fixedEuler = fixedWorldRotation.eulerAngles;
        spannerTransform.eulerAngles = new Vector3(
            fixedEuler.x,
            fixedEuler.y,
            lastZRotation
        );
    }

    void DetachSpanner()
    {
        if (showDebug) Debug.Log("✅ 볼트 풀기 완료!");
        
        isAttached = false;
        
        // Grabbable 복구
        if (spannerGrabbable != null)
        {
            spannerGrabbable.enabled = true;
        }
        
        // Rigidbody 복구
        if (spannerRb != null)
        {
            spannerRb.isKinematic = false;
            spannerRb.useGravity = true;
            spannerRb.constraints = RigidbodyConstraints.None;
        }
        
        // 부품 액션
        if (partController != null)
        {
            partController.ExecutePartAction();
        }
        
        // 초기화
        spannerObject = null;
        spannerTransform = null;
        spannerGrabbable = null;
        spannerRb = null;
    }

    bool DetectHand(GameObject spanner)
    {
        if (Camera.main != null)
        {
            Vector3 localPos = Camera.main.transform.InverseTransformPoint(spanner.transform.position);
            bool isLeft = localPos.x < 0;
            if (showDebug) Debug.Log($"카메라 기준 X: {localPos.x:F2} → {(isLeft ? "왼손" : "오른손")}");
            return isLeft;
        }
        
        OVRCameraRig cameraRig = FindObjectOfType<OVRCameraRig>();
        if (cameraRig != null && cameraRig.centerEyeAnchor != null)
        {
            Vector3 localPos = cameraRig.centerEyeAnchor.InverseTransformPoint(spanner.transform.position);
            return localPos.x < 0;
        }
        
        return false;
    }

    // Gizmo로 시각화
    void OnDrawGizmosSelected()
    {
        // 왼손 위치
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(leftHandWorldPosition, 0.05f);
        Gizmos.DrawLine(transform.position, leftHandWorldPosition);
        
        // 오른손 위치
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(rightHandWorldPosition, 0.05f);
        Gizmos.DrawLine(transform.position, rightHandWorldPosition);
        
        // 볼트 위치
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 0.03f);
        
        // 회전 축
        if (isAttached)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(fixedWorldPosition, fixedWorldPosition + worldRotationAxis * 0.1f);
        }
    }
}
