using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Spanner : MonoBehaviour
{
    private Transform initTransform;

    private void Awake()
    {
        initTransform = transform;
    }

    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.Space))
        {
            InitObject();
        }
    }

    private void InitObject()
    {
        transform.position = initTransform.position;
        transform.rotation = initTransform.rotation;
    }
}
