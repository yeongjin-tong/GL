using System.Collections;
using Oculus.Interaction;
using UnityEngine;
// Interaction SDK 이벤트를 받기 위해 아래 네임스페이스가 필수는 아니지만, 
// public 함수를 통해 외부에서 호출받는 방식을 권장합니다.

public class PartController : MonoBehaviour
{
    public string myPartID;
    public bool isLastPort = false;

    private Color highlightColor = Color.green;

    [Header("애니메이션")]
    public Animator anim;
    public string triggerName;

    private Renderer render;
    private Color originalBaseColor;
    private Material mat;
    private const string BaseColorProperty = "_BaseColor";

    private Vector3 originPos;
    private Quaternion originRot;

    public Vector3 cleanUpPos;
    public Vector3 cleanUpRot;

    [Header("스패너 설정")]
    public GameObject spannerPrefab;
    public bool useWorldCoordinates = true; // 월드 좌표 사용 여부 (체크 권장!)
    
    [Header("왼손 스패너 위치")]
    public Vector3 spawnerPositionLeft = new Vector3(0, 0, 0);
    public Vector3 spawnerRotationLeft = new Vector3(0, 0, 0);
    
    [Header("오른손 스패너 위치")]
    public Vector3 spawnerPositionRight = new Vector3(0, 0, 0);
    public Vector3 spawnerRotationRight = new Vector3(0, 0, 0);
    
    public string spannerAnimTrigger = "StartSpanner";
    
    // 하위 호환성: 기존 spawnerPosition/Rotation 유지
    [HideInInspector]
    public Vector3 spawnerPosition = new Vector3(0, 0, 0);
    [HideInInspector]
    public Vector3 spawnerRotation = new Vector3(0, 0, 0);

    private Coroutine blinkCoroutine;

    private InteractableUnityEventWrapper partXR;

    void Awake()
    {
        render = GetComponent<Renderer>();
        mat = render.material;

        if (GetComponent<Animator>() != null)
            anim = GetComponent<Animator>();

        if (mat.HasProperty(BaseColorProperty))
            originalBaseColor = mat.GetColor(BaseColorProperty);
        else if (mat.HasProperty("_Color"))
            originalBaseColor = mat.GetColor("_Color");

        originPos = transform.localPosition;
        originRot = transform.localRotation;

        if(gameObject.GetComponent<InteractableUnityEventWrapper>()!= null)
        {
            partXR = gameObject.GetComponent<InteractableUnityEventWrapper>();
        }
    }

    private void Start()
    {
        if (string.IsNullOrEmpty(myPartID))
            myPartID = gameObject.name;

        if (AssemblyManager.instance != null)
            AssemblyManager.instance.RegisterPart(this);

        if(partXR != null)
        {
            partXR.WhenSelect.AddListener(OnXRInteract);
        }
    }

    // --- XR 인터랙션용 다시 추가 함수 ---

    /// <summary>
    /// XR 컨트롤러로 플레이어가 객체를 '선택(Pinch/Click)'했을 때 
    /// 외부(Interactable Unity Event Wrapper)에서 이 함수를 호출하게 됩니다.
    /// </summary>
    public void OnXRInteract()
    {
        Debug.Log($"{myPartID} 인터랙션 발생");
        if (AssemblyManager.instance != null)
        {
            AssemblyManager.instance.OnPartClick(this);
        }
    }

    // --- 하이라이트 깜빡임 기능 ---

    public void SetHighlight(bool isOn)
    {
        string colorProperty = mat.HasProperty(BaseColorProperty) ? BaseColorProperty : "_Color";
        mat.SetColor(colorProperty, isOn ? highlightColor : originalBaseColor);
    }

    public void StartBlinking()
    {
        if (blinkCoroutine != null) StopCoroutine(blinkCoroutine);
        blinkCoroutine = StartCoroutine(BlinkRoutine());
    }

    public void StopBlinking()
    {
        if (blinkCoroutine != null) StopCoroutine(blinkCoroutine);
        SetHighlight(false);
    }

    IEnumerator BlinkRoutine()
    {
        while (true)
        {
            SetHighlight(true);
            yield return new WaitForSeconds(0.5f);
            SetHighlight(false);
            yield return new WaitForSeconds(0.5f);
        }
    }

    public void ExecutePartAction()
    {
        if (anim != null) anim.SetTrigger(triggerName);

        if (spannerPrefab != null)
        {
            Quaternion rotation = Quaternion.Euler(spawnerRotation);
            GameObject spannerInstance = Instantiate(spannerPrefab, transform.position + spawnerPosition, rotation);
            Animator spannerAnim = spannerInstance.GetComponent<Animator>();
            if (spannerAnim != null) spannerAnim.SetTrigger(spannerAnimTrigger);
        }

        if (GetComponent<Collider>() != null) GetComponent<Collider>().enabled = false;
        SetHighlight(false);
    }

    public void LastAnimClear()
    {
        if (!isLastPort) return;
        AssemblyManager.instance.NextStep(true);
    }

    public void CleanUpPart() { StartCoroutine(DisableAndMoveRoutine()); }

    IEnumerator DisableAndMoveRoutine()
    {
        if (anim != null) anim.enabled = false;
        yield return null;
        transform.localPosition = cleanUpPos;
        transform.localRotation = Quaternion.Euler(cleanUpRot);
    }

    public void InitPart()
    {
        transform.localPosition = originPos;
        transform.localRotation = originRot;
        if (anim != null)
        {
            anim.enabled = true;
            anim.Play("Idle");
        }
        if (GetComponent<Collider>() != null) GetComponent<Collider>().enabled = true;
    }
}