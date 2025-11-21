using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using TMPro;

public class SaveLoadUI : MonoBehaviour
{
    public static SaveLoadUI Instance { get; private set; }

    [Header("UI Panels")]
    public GameObject savePanel;
    public GameObject loadPanel;

    [Header("Save UI Elements")]
    public TMP_InputField saveNameInput;
    public Button saveConfirmBtn;
    public Button saveCancelBtn;

    [Header("Load UI Elements")]
    public Transform fileListContent;
    public GameObject fileItemPrefab; // Button with TextMeshProUGUI
    public Button loadCancelBtn;

    private void Awake()
    {
        if (Instance != null) Destroy(gameObject);
        else Instance = this;

        // Initialize UI events
        if (saveConfirmBtn != null) saveConfirmBtn.onClick.AddListener(OnSaveConfirm);
        if (saveCancelBtn != null) saveCancelBtn.onClick.AddListener(CloseSavePanel);
        if (loadCancelBtn != null) loadCancelBtn.onClick.AddListener(CloseLoadPanel);
    }

    // === Save Functionality ===
    public void OpenSavePanel()
    {
        savePanel.SetActive(true);
        loadPanel.SetActive(false);
        saveNameInput.text = "MyCircuit"; // Default name
    }

    public void CloseSavePanel()
    {
        savePanel.SetActive(false);
    }

    private void OnSaveConfirm()
    {
        string fileName = saveNameInput.text;
        if (string.IsNullOrWhiteSpace(fileName))
        {
            Debug.LogWarning("파일 이름을 입력해주세요.");
            return;
        }

        if (!fileName.EndsWith(".json")) fileName += ".json";

        SaveManager.instance.SaveCircuit(fileName);
        CloseSavePanel();
    }

    // === Load Functionality ===
    public void OpenLoadPanel()
    {
        loadPanel.SetActive(true);
        savePanel.SetActive(false);
        RefreshFileList();
    }

    public void CloseLoadPanel()
    {
        loadPanel.SetActive(false);
    }

    private void RefreshFileList()
    {
        // Clear existing items
        foreach (Transform child in fileListContent)
        {
            Destroy(child.gameObject);
        }

        // Get files from SaveManager
        List<string> files = SaveManager.instance.GetSaveFileList();

        foreach (string file in files)
        {
            GameObject item = Instantiate(fileItemPrefab, fileListContent);
            TextMeshProUGUI text = item.GetComponentInChildren<TextMeshProUGUI>();
            if (text != null) text.text = Path.GetFileNameWithoutExtension(file);

            Button btn = item.GetComponent<Button>();
            if (btn != null)
            {
                string fileName = Path.GetFileName(file);
                btn.onClick.AddListener(() => OnFileSelected(fileName));
            }
        }
    }

    private void OnFileSelected(string fileName)
    {
        SaveManager.instance.LoadCircuit(fileName);
        CloseLoadPanel();
    }
}
