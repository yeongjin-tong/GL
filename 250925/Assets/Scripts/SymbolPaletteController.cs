using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class SymbolPaletteController : MonoBehaviour
{
    [Tooltip("생성된 2D 부품들이 위치할 부모 RectTransform (예: 2D 회로 패널)")]
    public RectTransform spawnParent_2D;
    [Tooltip("생성된 3D 부품들이 위치할 부모 Transform")]
    public Transform spawnParent_3D;
    [Tooltip("삭제 영역으로 사용할 RectTransform")]
    public RectTransform deleteZone;

    public ScrollRect scrollrect;

    private GameObject currentPlacingPrefab;
    private GridManager gridManager;

    void Start()
    {
        gridManager = FindObjectOfType<GridManager>();

        // ✨ InputManager의 UI 클릭 방송을 구독합니다.
        if (InputManager.Instance != null)
        {
            InputManager.Instance.OnUIObjectClicked += HandleSymbolClick;
        }
    }

    // ✨ InputManager의 방송이 울리면 이 함수가 호출됩니다.
    private void HandleSymbolClick(GameObject clickedObject)
    {

        // 이미 배치 중인 프리팹이 있거나, 클릭된 오브젝트가 없으면 무시
        if (currentPlacingPrefab != null || clickedObject == null) return;

        // 클릭된 UI 요소에서 SymbolData 스크립트를 찾아봅니다.
        SymbolData symbolData = clickedObject.GetComponent<SymbolData>();

        // SymbolData를 성공적으로 찾았다면, 프리팹 생성을 시작합니다.
        if (symbolData != null && symbolData.prefabToSpawn_2D != null)
        {
            currentPlacingPrefab = Instantiate(symbolData.prefabToSpawn_2D, spawnParent_2D);

            // 심볼 이름 생성 시 text 세팅
            if(symbolData.useText)
            {
                var textObj = new GameObject("name_text");
                textObj.transform.parent = currentPlacingPrefab.transform;
                textObj.transform.localPosition = new Vector3(75f, 0f, 0f);
                textObj.transform.localScale = new Vector3(1, 1, 1);
                var txt = textObj.AddComponent<TextMeshProUGUI>();
                txt.text = symbolData.symbolName;
                txt.color = Color.black;

                var size = textObj.AddComponent<ContentSizeFitter>();
                size.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            }

            scrollrect.enabled = false;


        }
    }

    void Update()
    {
        // 배치 중인 프리팹이 있다면 마우스를 따라다니게 함
        if (currentPlacingPrefab != null)
        {
            // ✨ 1. 마우스의 화면 좌표를 캔버스의 로컬 좌표로 직접 변환합니다.
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                spawnParent_2D, // 좌표를 계산할 기준이 되는 부모 RectTransform
                Input.mousePosition,
                Camera.main, // UI용 카메라 (Screen Space - Camera 모드일 경우)
                out Vector2 localPoint
            );

            // 2. 변환된 로컬 좌표를 생성된 프리팹의 위치로 설정합니다.
            currentPlacingPrefab.GetComponent<RectTransform>().anchoredPosition = localPoint;

            // ✨ GridManager를 사용하는 경우, 월드 좌표 변환이 아닌 로컬 좌표로 스냅 로직을 수정해야 할 수 있습니다.
            //    (우선 그리드 스냅 없이 위치가 고정되는지 확인하는 것이 좋습니다.)

            // ✨ 2. 마우스 버튼에서 손을 떼면 (드래그를 끝내면) 배치 또는 파괴를 결정
            if (Input.GetMouseButtonUp(0))
            {
                // 3. 삭제 영역에 있는지 최종적으로 확인
                if (deleteZone != null && RectTransformUtility.RectangleContainsScreenPoint(deleteZone, Input.mousePosition, Camera.main))
                {
                    Debug.Log($"{currentPlacingPrefab.name} 파괴 (Delete Zone)");
                    Destroy(currentPlacingPrefab);
                }
                else
                {
                    Destroy(currentPlacingPrefab.GetComponent<SymbolData>());
                    currentPlacingPrefab.GetComponent<Image>().raycastTarget = false;
                    Debug.Log($"{currentPlacingPrefab.name} 배치 완료!");
                }


                // 4. 배치든 파괴든, 작업이 끝났으므로 상태를 초기화
                currentPlacingPrefab = null;
                scrollrect.enabled = true;
            }
        }
    }

    // 스크립트가 파괴될 때 이벤트 구독을 해제합니다. (메모리 누수 방지)
    private void OnDestroy()
    {
        if (InputManager.Instance != null)
        {
            InputManager.Instance.OnUIObjectClicked -= HandleSymbolClick;
        }
    }

    // ... (GetMouseWorldPosition 함수는 그대로 유지)
    private Vector3 GetMouseWorldPosition(Vector2 screenPosition)
    {
        Ray ray = Camera.main.ScreenPointToRay(screenPosition);
        Plane plane = new Plane(-Camera.main.transform.forward, spawnParent_2D.position);
        plane.Raycast(ray, out float distance);
        return ray.GetPoint(distance);
    }
}