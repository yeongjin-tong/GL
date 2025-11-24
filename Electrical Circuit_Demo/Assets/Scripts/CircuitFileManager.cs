using UnityEngine;
using System.Collections;
using System.IO;
using SimpleFileBrowser; // GitHub 플러그인 네임스페이스

public class CircuitFileManager : MonoBehaviour
{
    public static CircuitFileManager instance { get; private set; }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        instance = this;
    }

    private void Start()
    {
        // 파일 브라우저 필터 설정 (json 파일만 보이게)
        FileBrowser.SetFilters(true, new FileBrowser.Filter("Circuit Data", ".json"));
        FileBrowser.SetDefaultFilter(".json");
    }

    // [UI 버튼 연결용] 다른 이름으로 저장
    public void OnClickSaveAs()
    {
        StartCoroutine(ShowSaveDialogCoroutine());
    }

    // [UI 버튼 연결용] 불러오기
    public void OnClickLoad()
    {
        StartCoroutine(ShowLoadDialogCoroutine());
    }

    IEnumerator ShowSaveDialogCoroutine()
    {
        // 저장 다이얼로그 열기
        yield return FileBrowser.WaitForSaveDialog(
            FileBrowser.PickMode.Files,
            false,
            null,
            null,
            "다른 이름으로 저장", "Save"
        );

        if (FileBrowser.Success)
        {
            string path = FileBrowser.Result[0];

            // 확장자 .json 강제 적용
            if (Path.GetExtension(path) != ".json")
            {
                path += ".json";
            }

            // ✨ SaveManager에게 전체 경로를 넘겨주며 저장 요청
            SaveManager.instance.SaveCircuitToPath(path);
        }
    }

    IEnumerator ShowLoadDialogCoroutine()
    {
        // 불러오기 다이얼로그 열기
        yield return FileBrowser.WaitForLoadDialog(
            FileBrowser.PickMode.Files,
            false,
            null,
            null,
            "회로 불러오기", "Load"
        );

        if (FileBrowser.Success)
        {
            string path = FileBrowser.Result[0];

            // ✨ SaveManager에게 전체 경로를 넘겨주며 불러오기 요청
            SaveManager.instance.LoadCircuitFromPath(path);
        }
    }
}