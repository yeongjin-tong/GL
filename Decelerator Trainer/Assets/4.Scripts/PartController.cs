using System.Collections;
using UnityEngine;

public class PartController : MonoBehaviour
{
    public string myPartID;

    public bool isLastPort = false;

    // 하이라이트 색상을 노란색으로 정의합니다. (인스펙터에서 변경 가능)
    private Color highlightColor = Color.green;

    [Header("연결")]
    public Animator anim;
    public string triggerName;

    private Renderer render;
    private Color originalBaseColor; // Emission이 아닌, 원래의 기본 색상을 저장할 변수
    private Material mat;

    // URP/HDRP에서 기본 색상의 속성 이름
    private const string BaseColorProperty = "_BaseColor";

    private Vector3 originPos;
    private Quaternion originRot;

    public Vector3 cleanUpPos;
    public Vector3 cleanUpRot;

    [Header("스패너 설정")]
    public GameObject spannerPrefab; // 인스펙터에서 스패너 프리팹 연결
    public Vector3 spawnerPosition = new Vector3(0, 0, 0); // 스패너 생성 위치 
    public Vector3 spawnerRotation = new Vector3(0, 0, 0);  // 스패너 생성
    public string spannerAnimTrigger = "StartSpanner"; // 스패너 애니메이터의 트리거 이름

    private Coroutine blinkCoroutine;

    void Awake()
    {
        render = GetComponent<Renderer>();
        mat = render.material;

        if(GetComponent<Animator>() != null)
        {
            anim = GetComponent<Animator>();
        }

        // 메테리얼의 원래 기본 색상을 가져와 저장합니다.
        // URP 셰이더인 경우 _BaseColor, Built-in 셰이더인 경우 _Color를 시도합니다.
        if (mat.HasProperty(BaseColorProperty))
        {
            originalBaseColor = mat.GetColor(BaseColorProperty);
        }
        else if (mat.HasProperty("_Color"))
        {
            originalBaseColor = mat.GetColor("_Color");
        }

        originPos = transform.localPosition;
        originRot = transform.localRotation;
    }

    private void Start()
    {
        if(string.IsNullOrEmpty(myPartID))
            myPartID = gameObject.name;

        if (AssemblyManager.instance != null)
        {
            AssemblyManager.instance.RegisterPart(this);
        }
    }

    // 매니저가 "너 이제 하이라이트 켜/꺼!" 라고 명령할 때 쓸 함수
    public void SetHighlight(bool isOn)
    {
        // 셰이더 속성 이름을 결정합니다.
        string colorProperty = mat.HasProperty(BaseColorProperty) ? BaseColorProperty : "_Color";

        if (isOn)
        {
            // 기본 색상을 하이라이트 색상으로 변경
            mat.SetColor(colorProperty, highlightColor);
        }
        else
        {
            // 원래 색상으로 복구
            mat.SetColor(colorProperty, originalBaseColor);
        }
    }

    // [추가] 깜빡임 시작 함수
    public void StartBlinking()
    {
        if (blinkCoroutine != null) StopCoroutine(blinkCoroutine);
        blinkCoroutine = StartCoroutine(BlinkRoutine());
    }

    // [추가] 깜빡임 정지 함수
    public void StopBlinking()
    {
        if (blinkCoroutine != null) StopCoroutine(blinkCoroutine);
        SetHighlight(false); // 꺼진 상태로 확정
    }

    // [추가] 0.5초 간격으로 켜졌다 꺼졌다 하는 로직
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


    // 매니저가 "너 애니메이션 실행해!" 라고 명령할 때 쓸 함수
    public void PlayAnimation()
    {
        if (anim != null) anim.SetTrigger(triggerName);

        // 애니메이션 실행 후 더 이상 상호작용 안 되게 Collider 끄기 (선택사항)
        GetComponent<Collider>().enabled = false;
        SetHighlight(false); // 빛 끄기
    }

    // AssemblyManager에서 호출될 최종 동작 함수를 따로 정의합니다.
    public void ExecutePartAction()
    {
        // 1. 볼트와 와셔가 빠지는 애니메이션 실행 (그룹 오브젝트에 연결된 Animator)
        if (anim != null)
        {
            anim.SetTrigger(triggerName);
        }

        // 2. 스패너 생성 및 실행
        if (spannerPrefab != null)
        {
            // 볼트 위치와 회전을 기준으로 스패너 생성 (약간의 오프셋 적용)
            Quaternion rotation = Quaternion.Euler(spawnerRotation);

            // 볼트 위치를 기준으로 생성 (볼트의 부모가 아닌 볼트 자신의 위치를 사용해야 정확함)
            GameObject spannerInstance = Instantiate(spannerPrefab,
                                                     transform.position + spawnerPosition,
                                                     rotation);

            spannerInstance.transform.rotation = rotation;

            // 생성된 스패너의 애니메이션 실행
            Animator spannerAnim = spannerInstance.GetComponent<Animator>();
            if (spannerAnim != null)
            {
                spannerAnim.SetTrigger(spannerAnimTrigger);
            }
        }

        // 3. 더 이상 클릭 안 되게 처리
        GetComponent<Collider>().enabled = false;
        SetHighlight(false);
    }

    void OnMouseDown()
    {
        // 매니저에게 "나 클릭됨" 보고
        AssemblyManager.instance.OnPartClick(this);
    }

    // 애니메이션에서 종료시 실행 (삭제 x)
    public void LastAnimClear()
    {
        if (!isLastPort) return;

        AssemblyManager.instance.NextStep(true);
    }


    public void CleanUpPart()
    {
        StartCoroutine(DisableAndMoveRoutine());
    }

    IEnumerator DisableAndMoveRoutine()
    {
        // 1. 애니메이터 끄기
        if (anim != null)
        {
            anim.enabled = false;
        }
        yield return null;

        transform.localPosition = cleanUpPos;
        transform.localRotation = Quaternion.Euler(cleanUpRot);
    }

    public void InitPart()
    {
        transform.localPosition = originPos;
        transform.localRotation = originRot;

        anim.enabled = true;
        anim.Play("Idle");
        GetComponent<Collider>().enabled = true;

    }

}