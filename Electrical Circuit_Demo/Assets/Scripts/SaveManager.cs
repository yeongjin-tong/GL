using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Linq;
using Unity.VisualScripting;

public class SaveManager : MonoBehaviour
{
    public static SaveManager instance { get; private set; }

    public string address;

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

        address = Application.streamingAssetsPath;
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
        GameObject junctionPrefab = WireManager.Instance.junctionPrefab;
        symbolPrefabMap.Add(junctionPrefab.name, junctionPrefab);
        Debug.Log($"프리팹 맵 초기화 완료: 총 {symbolPrefabMap.Count}개 등록됨.");
    }

    // === 저장 기능 ===
    public void SaveCircuit()
    {
        SaveCircuit(saveFileName);
    }

    public void SaveCircuit(string fileName)
    {
        string path = Path.Combine(address, fileName);
        SaveCircuitToPath(path); // 아래에 만든 새 함수를 호출
    }

    // ✨ 2. [추가] 전체 경로를 받아서 저장하는 함수 (FileBrowser용)
    public void SaveCircuitToPath(string fullPath)
    {
        CircuitSaveData data = new CircuitSaveData();

        // --- (기존 저장 로직 그대로) ---
        ElectricalComponent[] components = content_2D.GetComponentsInChildren<ElectricalComponent>();
        foreach (var comp in components)
        {
            SymbolDataSave symbolData = new SymbolDataSave();
            symbolData.symbolID = comp.symbol_Text;

            if (comp.gameObject.name.Contains("Junction"))
            {
                symbolData.prefabName = "Junction";
            }
            else
            {
                symbolData.prefabName = comp.gameObject.name.Replace("(Clone)", "").Trim();
            }

            symbolData.instanceID = comp.instanceID;
            symbolData.position = comp.GetComponent<RectTransform>().anchoredPosition;

            if(comp.GetComponent<ITimerControl>() != null)
            {
                symbolData.timerNum = comp.GetComponent<ITimerControl>().GetTime();
            }

            foreach(ConnectionPoint port in comp.GetComponentsInChildren<ConnectionPoint>())
            {
                symbolData.portAndIndex.Add(port.name, port.portIndex);
            }

            data.symbols.Add(symbolData);
        }

        Wire[] wires = content_2D.GetComponentsInChildren<Wire>();
        foreach (var wire in wires)
        {
            WireDataSave wireData = new WireDataSave();
            wireData.startComponentID = wire.connectedPoints[0].parentComponent.instanceID;
            wireData.startPortIndex = GetPortID(wire.connectedPoints[0]);
            wireData.endComponentID = wire.connectedPoints.Last().parentComponent.instanceID;
            wireData.endPortIndex = GetPortID(wire.connectedPoints.Last());

            LineRenderer lr = wire.GetComponent<LineRenderer>();
            Vector3[] positions = new Vector3[lr.positionCount];
            lr.GetPositions(positions);
            wireData.pathPoints = new List<Vector3>(positions);

            data.wires.Add(wireData);
        }
        // ------------------------------

        string json = JsonUtility.ToJson(data, true);

        // 파일 쓰기 (전달받은 fullPath 사용)
        File.WriteAllText(fullPath, json);
        Debug.Log($"회로 저장 완료: {fullPath}");
    }

    // === 불러오기 기능 ===
    public void LoadCircuit()
    {
        LoadCircuit(saveFileName);
    }

    public void LoadCircuit(string fileName)
    {
        string path = Path.Combine(address, fileName);
        LoadCircuitFromPath(path); // 아래에 만든 새 함수를 호출
    }

    public void LoadCircuitFromPath(string fullPath)
    {
        if (!File.Exists(fullPath)) { Debug.LogWarning("파일이 없습니다: " + fullPath); return; }

        string json = File.ReadAllText(fullPath);
        CircuitSaveData data = JsonUtility.FromJson<CircuitSaveData>(json);

        // --- (기존 불러오기 로직 그대로) ---
        ClearScene(); // 씬 비우기

        Dictionary<string, ElectricalComponent> loadedComponentsMap = new Dictionary<string, ElectricalComponent>();
        foreach (var symbolData in data.symbols)
        {
            string key = string.IsNullOrEmpty(symbolData.prefabName) ? symbolData.symbolID : symbolData.prefabName;

            if (symbolPrefabMap.TryGetValue(key, out GameObject prefab))
            {
                GameObject obj = Instantiate(prefab, content_2D);
                obj.GetComponent<RectTransform>().anchoredPosition = symbolData.position;

                if(key.Contains("Junction"))
                {
                    obj.AddComponent<Junction>();
                    obj.AddComponent<ConnectionPoint>();
                }
                else
                {
                    ObjectManager.Instance.objects_2d.Add(obj);
                }

                ElectricalComponent comp = obj.GetComponent<ElectricalComponent>();
                comp.instanceID = symbolData.instanceID;
                comp.NameSetting(symbolData.symbolID);
                if (comp.GetComponent<ITimerControl>() != null)
                {
                    comp.GetComponent<ITimerControl>().SetTime(symbolData.timerNum);
                }
                loadedComponentsMap.Add(comp.instanceID, comp);

                foreach(ConnectionPoint port in comp.GetComponentsInChildren<ConnectionPoint>())
                {
                    if(symbolData.portAndIndex.ContainsKey(port.name))
                    {
                        port.portIndex = symbolData.portAndIndex[port.name];
                    }
                }

            }
            else
            {
                Debug.LogError($"프리팹을 찾을 수 없음: {symbolData.symbolID}");
            }
        }

        foreach (var wireData in data.wires)
        {
            if (loadedComponentsMap.ContainsKey(wireData.startComponentID) && loadedComponentsMap.ContainsKey(wireData.endComponentID))
            {
                ElectricalComponent startComp = loadedComponentsMap[wireData.startComponentID];
                ElectricalComponent endComp = loadedComponentsMap[wireData.endComponentID];
                ConnectionPoint startPoint = GetPortByID(startComp, wireData.startPortIndex);
                ConnectionPoint endPoint = GetPortByID(endComp, wireData.endPortIndex);

                WireManager.Instance.CreateWireWithPath(startPoint, endPoint, wireData.pathPoints);
            }
        }
        // ----------------------------------
        Debug.Log($"회로 불러오기 완료: {fullPath}");
    }

    // === 헬퍼 함수들 ===
    // 씬 초기화
    private void ClearScene()
    {
        foreach (Transform chird in content_2D)
        {
            Destroy(chird.gameObject);
        }

        if (CircuitGraph.Instance != null)
        {
            CircuitGraph.Instance.ClearGraph();
        }

        if(SymbolController.Instance != null)
        {
            SymbolController.Instance.DeselectAll();
        }
    }

    // 포트가 부품의 몇번째 자식인지 인덱스 반환
    // ✨ [수정] 포트의 ID(string)를 반환하는 함수로 변경
    private string GetPortID(ConnectionPoint point)
    {
        if (point == null) return "";
        return point.portIndex;
    }

    // ✨ [수정] ID(string)로 포트를 찾는 함수로 변경
    private ConnectionPoint GetPortByID(ElectricalComponent comp, string id)
    {
        if (comp == null) return null;

        ConnectionPoint[] allPorts = comp.GetComponentsInChildren<ConnectionPoint>(true);

        foreach (var port in allPorts)
        {
            // 저장된 ID와 포트의 ID가 같은지 확인
            if (port.portIndex == id)
            {
                return port;
            }
        }

        Debug.LogError($"GetPortByID 오류: {comp.name}에서 ID가 '{id}'인 포트를 찾을 수 없습니다.");
        return null;
    }

    // === 파일 목록 가져오기 ===
    public List<string> GetSaveFileList()
    {
        if (!Directory.Exists(address))
        {
            Directory.CreateDirectory(address);
        }

        string[] files = Directory.GetFiles(address, "*.json");
        return files.ToList();
    }

}
