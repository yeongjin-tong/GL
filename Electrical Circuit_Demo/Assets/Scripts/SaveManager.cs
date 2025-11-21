using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Linq;

public class SaveManager : MonoBehaviour
{
    public static SaveManager instance { get; private set; }

    public string adress = "C:\\Users\\USER\\Desktop\\work_git\\Electrical Circuit_Demo\\streamingAssets";

    public string saveFileName = "myCircuit.json";      // 저장할 파일 이름
    public Transform content_2D;

    public Dictionary<string, GameObject> symbolPrefabMap = new Dictionary<string, GameObject>();

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
        InitializePrefabMap();
    }

    private void InitializePrefabMap()
    {
        symbolPrefabMap.Clear();

        // 1. 씬에서 SymbolPaletteController 찾기
        SymbolPaletteController palette = FindObjectOfType<SymbolPaletteController>();

        if(palette == null || palette.spawnParent_2D == null)
        {
            return;
        }

        SymbolData[] allSymbolsData = palette.GetComponentsInChildren<SymbolData>(true);

        foreach (var data in allSymbolsData)
        {
            // (중요) data.symbolName 체크를 빼거나, prefabToSpawn_2D만 체크해도 됩니다.
            // 여기서는 안전하게 프리팹이 연결되어 있는지만 확인합니다.
            if (data != null && data.prefabToSpawn_2D != null)
            {
                // ✨ [핵심 수정] 맵의 키(Key)로 '원본 프리팹의 이름'을 사용합니다.
                string prefabNameKey = data.prefabToSpawn_2D.name;

                if (!symbolPrefabMap.ContainsKey(prefabNameKey))
                {
                    symbolPrefabMap.Add(prefabNameKey, data.prefabToSpawn_2D);
                    // Debug.Log($"프리팹 등록됨: 키={prefabNameKey}"); // 확인용 로그
                }
            }
        }
        Debug.Log($"프리팹 맵 초기화 완료: 총 {symbolPrefabMap.Count}개 등록됨.");
    }

    // === 저장 기능 ===
    public void SaveCircuit()
    {
        SaveCircuit(saveFileName);
    }

    public void SaveCircuit(string fileName)
    {
        CircuitSaveData data = new CircuitSaveData();

        ElectricalComponent[] components = content_2D.GetComponentsInChildren<ElectricalComponent>();
        foreach(var comp in components)
        {
            SymbolDataSave symbolData = new SymbolDataSave();
            symbolData.symbolID = comp.symbol_ID;   // 사용자 정의 ID (예: PB1)
            // ✨ 프리팹 이름 저장 (Clone 제거)
            symbolData.prefabName = comp.gameObject.name.Replace("(Clone)", "").Trim();
            symbolData.instanceID = comp.instanceID;
            symbolData.position = comp.GetComponent<RectTransform>().anchoredPosition;  // UI 좌표 사용
            data.symbols.Add(symbolData);
        }

        Wire[] wires = content_2D.GetComponentsInChildren<Wire>();
        foreach(var wire in wires)
        {
            WireDataSave wireData = new WireDataSave();
            // 시작/끝 연결 정보 저장
            wireData.startComponentID = wire.connectedPoints[0].parentComponent.instanceID;
            wireData.startPortIndex = GetPortIndex(wire.connectedPoints[0]);
            wireData.endComponentID = wire.connectedPoints.Last().parentComponent.instanceID;
            wireData.endPortIndex = GetPortIndex(wire.connectedPoints.Last());

            //경로점 저장
            LineRenderer lr = wire.GetComponent<LineRenderer>();
            Vector3[] positions = new Vector3[lr.positionCount];
            lr.GetPositions(positions);
            wireData.pathPoints = new List<Vector3>(positions);

            data.wires.Add(wireData);
        }

        string json = JsonUtility.ToJson(data, true);
        string path = Path.Combine(adress, fileName);
        File.WriteAllText(path, json);
        Debug.Log($"회로 저장 완료: {path}");
    }

    // === 불러오기 기능 ===
    public void LoadCircuit()
    {
        LoadCircuit(saveFileName);
    }

    public void LoadCircuit(string fileName)
    {
        string path = Path.Combine(adress, fileName);
        if (!File.Exists(path)) { Debug.LogWarning("저장된 파일이 없습니다."); return; }

        string json = File.ReadAllText(path);
        CircuitSaveData data = JsonUtility.FromJson<CircuitSaveData>(json);

        // 현재 씬 비우기
        ClearScene();

        Dictionary<string, ElectricalComponent> loadedComponentsMap = new Dictionary<string, ElectricalComponent>();
        foreach(var symbolData in data.symbols)
        {
            // ✨ 저장된 프리팹 이름으로 찾기 (없으면 하위 호환성을 위해 symbolID 사용)
            string key = string.IsNullOrEmpty(symbolData.prefabName) ? symbolData.symbolID : symbolData.prefabName;

            if(symbolPrefabMap.TryGetValue(key, out GameObject prefab))
            {
                GameObject obj = Instantiate(prefab, content_2D);
                obj.GetComponent<RectTransform>().anchoredPosition = symbolData.position;

                ElectricalComponent comp = obj.GetComponent<ElectricalComponent>();
                comp.instanceID = symbolData.instanceID;
                comp.symbol_ID = symbolData.symbolID; // ✨ 사용자 정의 ID 복구
                loadedComponentsMap.Add(comp.instanceID, comp);
            }
            else
            {
                Debug.LogError($"프리팹을 찾을 수 없음: {symbolData.symbolID}");
            }
        }

        foreach(var wireData in data.wires)
        {
            // 저장된 ID로 시작/끝 부품 찾기
            ElectricalComponent startComp = loadedComponentsMap[wireData.startComponentID];
            ElectricalComponent endComp = loadedComponentsMap[wireData.endComponentID];
            // 포트 인덱스로 정확한 포트 찾기 (헬퍼 함수 사용)
            ConnectionPoint startPoint = GetPortByIndex(startComp, wireData.startPortIndex);
            ConnectionPoint endPoint = GetPortByIndex(endComp, wireData.endPortIndex);

            // WireManager를 이용해 선 생성 (경로 정보 포함 함수 활용)
            WireManager.Instance.CreateWireWithPath(startPoint, endPoint, wireData.pathPoints);
        }
    }

    // === 헬퍼 함수들 ===
    // 씬 초기화
    private void ClearScene()
    {
        for(int i = content_2D.childCount -1; i >= 0; i++)
        {
            GameObject child = content_2D.GetChild(i).gameObject;
            Destroy(child);
        }

        if(CircuitGraph.Instance != null)
        {
            CircuitGraph.Instance.ClearGraph();
        }

        if(SymbolController.Instance != null)
        {
            SymbolController.Instance.DeselectAll();
        }
    }

    // 포트가 부품의 몇번째 자식인지 인덱스 반환
    private int GetPortIndex(ConnectionPoint point)
    {
        if (point == null || point.parentComponent == null)
        {
            Debug.LogError("GetPortIndex 실패: 유효하지 않은 포인트입니다.");
            return -1;
        }

        ConnectionPoint[] allPorts = point.parentComponent.GetComponentsInChildren<ConnectionPoint>(true);

        for(int i = 0; i < allPorts.Length; i++)
        {
            if (allPorts[i] == point)
            {
                return i;
            }
        }

        Debug.LogError($"GetPortIndex 오류: {point.parentComponent.name}에서 해당 포트를 찾을 수 없습니다.");
        return -1;
    }

    // 인덱스로 포트 찾기
    private ConnectionPoint GetPortByIndex(ElectricalComponent comp, int index)
    {
        if(comp == null)
        {
            Debug.LogError($"GetPortByIndex 실패: 컴포넌트가 null입니다.");
            return null;
        }

        ConnectionPoint[] allPorts = comp.GetComponentsInChildren<ConnectionPoint>(true);

        if(index >= 0 && index < allPorts.Length)
        {
            return allPorts[index];
        }
        else
        {
            Debug.LogError($"GetPortByIndex 오류: {comp.name}의 포트 인덱스({index})가 범위를 벗어났습니다. (총 개수: {allPorts.Length})");
            return null;
        }
    }

    // === 파일 목록 가져오기 ===
    public List<string> GetSaveFileList()
    {
        if (!Directory.Exists(adress))
        {
            Directory.CreateDirectory(adress);
        }

        string[] files = Directory.GetFiles(adress, "*.json");
        return files.ToList();
    }

}
