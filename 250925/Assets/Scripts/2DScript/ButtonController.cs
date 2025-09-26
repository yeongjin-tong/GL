using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class ButtonController : MonoBehaviour
{
    [Header("메인 화면")]
    public Button selectBtn_2d;
    public Button selectBtn_3d;

    public GameObject content_3d;
    public GameObject modeScreen;

    [Header("2D 화면")]
    public Button initBtn;
    public Button playBtn;
    public Button helpBtn;
    public Button helpCloseBtn;
    public Button backBtn_2d;

    private GameObject deleteObj;
    public GameObject helpPanel;
    [Header("3D 화면")]
    public Button window_2dBtn;
    public Button backBtn_3d;
    public Button initBtn_3d;

    private RectTransform panel_2D;
    private void Awake()
    {
        initBtn.onClick.AddListener(ObjectInit);
        playBtn.onClick.AddListener(ModeBtnEvent);
        selectBtn_3d.onClick.AddListener(() => ModeSelect(1) );
        selectBtn_2d.onClick.AddListener(() => ModeSelect(2) );
        window_2dBtn.onClick.AddListener(WindowScreen_2d);
        backBtn_3d.onClick.AddListener(() => ModeSelect(0));
        backBtn_2d.onClick.AddListener(() => ModeSelect(0));
        initBtn_3d.onClick.AddListener(ObjectInit);

        helpBtn.onClick.AddListener(() =>
        {
            helpPanel.SetActive(true);
        });
        helpCloseBtn.onClick.AddListener(() =>
        {
            helpPanel.SetActive(false);
        });
    }

    private void OnEnable()
    {
        if (InputManager.Instance != null)
        {
            InputManager.Instance.OnPhysicsObjectClicked += HandlePhysicsClick;
            InputManager.Instance.OnDeleteKeyPressed += HandleDeleteKey;
        }
    }

    private void OnDisable()
    {
        if (InputManager.Instance != null)
        {
            InputManager.Instance.OnPhysicsObjectClicked -= HandlePhysicsClick;
            InputManager.Instance.OnDeleteKeyPressed -= HandleDeleteKey;
        }
    }

    private void Start()
    {
        ModeSelect(0);
    }

    // ✨ 물리 오브젝트 클릭 방송을 수신하여 처리
    private void HandlePhysicsClick(Collider2D hit)
    {
        // 모든 오브젝트 아웃라인 초기화
        DeselectAll();

        if (hit != null)
        {
            // 부품 또는 전선이 클릭된 경우
            if (hit.gameObject.name.Contains("Clone") || hit.gameObject.CompareTag("Wire")) // 전선 태그(Wire) 추가
            {
                SelectObject(hit.gameObject);
            }
        }
        else // 허공 클릭 시
        {
            deleteObj = null;
        }
    }

    // ✨ Delete 키 입력 방송을 수신하여 처리
    private void HandleDeleteKey()
    {
        if (deleteObj != null)
        {
            Destroy(deleteObj);
            deleteObj = null;
        }
    }

    private void DeselectAll()
    {
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        foreach (GameObject obj in allObjects)
        {
            if (obj.name.Contains("Clone"))
            {
                if (obj.GetComponent<Outline>() != null) obj.GetComponent<Outline>().enabled = false;
            }
            if (obj.CompareTag("Wire"))
            {
                if (obj.GetComponent<LineRenderer>() != null)
                {
                    obj.GetComponent<LineRenderer>().startColor = Color.gray;
                    obj.GetComponent<LineRenderer>().endColor = Color.gray;
                }
            }
        }
    }

    private void SelectObject(GameObject objToSelect)
    {
        deleteObj = objToSelect;
        if (deleteObj.GetComponent<Outline>() != null)
        {
            deleteObj.GetComponent<Outline>().enabled = true;
        }
        if (deleteObj.GetComponent<LineRenderer>() != null)
        {
            deleteObj.GetComponent<LineRenderer>().startColor = Color.yellow;
            deleteObj.GetComponent<LineRenderer>().endColor = Color.yellow;
        }
    }


    private void ObjectInit()
    {
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        foreach (GameObject obj in allObjects)
        {
            if(obj.name.Contains("Clone"))
            {
                Destroy(obj);
            }
        }
    }

    private void ModeBtnEvent()
    {
        SimulationManager.ToggleSimulation();
        bool sim = SimulationManager.isSimulating;
        
        if (sim == false)
        {
            playBtn.GetComponentInChildren<TextMeshProUGUI>().text = "Play";
        }
        else
        {
            playBtn.GetComponentInChildren<TextMeshProUGUI>().text = "Stop";

            DeselectAll();   // 아웃라인 초기화
        }
    }

    private void ModeSelect(int index)                          // 0 : 메인 화면        1 : 3d 화면       2 : 2d 화면
    {
        content_3d.SetActive(false);

        for (int i = 0; i < modeScreen.transform.childCount; i++)
        {
            modeScreen.transform.GetChild(i).gameObject.SetActive(false);
        }

        modeScreen.transform.GetChild(index).gameObject.SetActive(true);

        if (index == 1)
        {
            content_3d.SetActive(true);
        }
        else if (index == 2)
        {
            panel_2D.localPosition = Vector3.zero;
            panel_2D.sizeDelta = new Vector2(1920f, 1080f);
        }

        if (modeScreen.transform.GetChild(2).GetComponent<RectTransform>() != null)
        {
            panel_2D = modeScreen.transform.GetChild(2).GetComponent<RectTransform>();
        }
    }

    private void WindowScreen_2d()
    {
        panel_2D.gameObject.SetActive(true);
        panel_2D.localPosition = new Vector3(410f, -20f, 0f);
        panel_2D.sizeDelta = new Vector2(1200f, 800f);
    }

    private void Init_3D()
    {
        List<LineRenderer> lines = WireManager_3d.Instance.wireList;
        foreach (LineRenderer lr in lines)
        {
            Destroy(lr.gameObject);
        }
        WireManager_3d.Instance.wireList.Clear();
    }

}
