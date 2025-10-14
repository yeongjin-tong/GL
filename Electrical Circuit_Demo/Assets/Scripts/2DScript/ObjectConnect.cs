using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class ObjectConnect : MonoBehaviour
{
    public Transform[] connectPoints = new Transform[2]; // 0:상, 1:하

    void Awake()
    {
        if (transform.GetComponent<BoxCollider>() != null)
        {
            return;
        }
    }

}
