using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ButtonController : MonoBehaviour
{
    // 1. StartPanel
    public Button startBtn;

    // 2. MainPanel
    public Button createBtn;
    public Button resetBtn;
    public Button deleteBtn;
    public Button prevBtn;
    public Button nextBtn;

    public GameObject deceleratorPrefab;
    public GameObject descriptionObj;

    private GameObject currentObject;

    private void Awake()
    {
        startBtn.onClick.AddListener(() => { PageManager.instance.ShowPanel(1); });
        createBtn.onClick.AddListener(CreateObject);
        deleteBtn.onClick.AddListener(DeleteObject);
        nextBtn.onClick.AddListener(NextBtn_Event);
        prevBtn.onClick.AddListener(PrevBtn_Event);

        descriptionObj.SetActive(false);
    }

    private void Start()
    {
        resetBtn.onClick.AddListener(AssemblyManager.instance.InitAllParts);

        AssemblyManager.instance.OnStepChanged += UpdateButtonState;

        UpdateButtonState();
    }

    private void UpdateButtonState()
    {
        if(currentObject == null)
        {
            prevBtn.gameObject.SetActive(false);
            nextBtn.gameObject.SetActive(false);
            return;
        }

        prevBtn.gameObject.SetActive(true);
        nextBtn.gameObject.SetActive(true);

        int order = AssemblyManager.instance.currentMainIndex;

        if(order == 0)
        {
            prevBtn.gameObject.SetActive(false);
        }
        else if (order == AssemblyManager.instance.scenarioList.Count)
        {
            nextBtn.gameObject.SetActive(false);
        }
        else
        {
            prevBtn.gameObject.SetActive(true);
            nextBtn.gameObject.SetActive(true);
        }
    }

    private void CreateObject()
    {
        if (currentObject == null)
        {
            currentObject = Instantiate(deceleratorPrefab);

            foreach(MeshRenderer part in currentObject.GetComponentsInChildren<MeshRenderer>())
            {
                part.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }

            descriptionObj.SetActive(true);
        }

        UpdateButtonState();
    }

    private void DeleteObject()
    {
        AssemblyManager.instance.InitAllParts();

        AssemblyManager.instance.partRegistry.Clear();

        descriptionObj.SetActive(false);

        if (currentObject != null)
        {
            Destroy(currentObject);
            currentObject = null;
        }

        UpdateButtonState();
    }

    private void NextBtn_Event()
    {
        int order = AssemblyManager.instance.currentMainIndex;


        AssemblyManager.instance.NextStep(true);
    }


    private void PrevBtn_Event()
    {
        AssemblyManager.instance.NextStep(false);
    }


    // 그림자 테스트
    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.Space) && currentObject != null)
        {
            foreach(MeshRenderer part in currentObject.GetComponentsInChildren<MeshRenderer>())
            {
                if(part.shadowCastingMode == UnityEngine.Rendering.ShadowCastingMode.Off)
                {
                    part.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                }
                else
                {
                    part.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                }

                
            }
        }
    }
}
