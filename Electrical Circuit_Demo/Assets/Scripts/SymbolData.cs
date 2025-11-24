using UnityEngine;
using UnityEngine.UI;

// 이 스크립트는 UI 심볼 이미지에 붙어, 자신이 어떤 프리팹을 생성할지 정의합니다.
[RequireComponent(typeof(Image))]
public class SymbolData : MonoBehaviour
{
    [Tooltip("이 심볼을 드래그했을 때 생성될 2D 프리팹")]
    public GameObject prefabToSpawn_2D;
}