using UnityEngine;
using UnityEngine.UI;

// �� ��ũ��Ʈ�� UI �ɺ� �̹����� �ٿ���, �ڽ��� � ��ǰ������ ���� ������ �����ϴ�.
[RequireComponent(typeof(Image))]
public class SymbolData : MonoBehaviour
{
    [Tooltip("�� �ɺ��� �巡������ �� ������ 2D ��ǰ ������")]
    public GameObject prefabToSpawn_2D;

    public bool useText;
    public string symbolName;
}