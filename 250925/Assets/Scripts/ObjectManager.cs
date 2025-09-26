using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class ObjectManager : MonoBehaviour
{
    public static ObjectManager Instance { get; private set; }
    public List<GameObject> objects_2d = new List<GameObject>();
    public List<GameObject> objects_3d = new List<GameObject>();

    public Dictionary<GameObject, GameObject> objectMatching = new Dictionary<GameObject, GameObject>();


    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }


    public void CleanUpList()
    {
        objects_2d.RemoveAll(obj => obj == null);
        objects_3d.RemoveAll(obj => obj == null);
    }
}
