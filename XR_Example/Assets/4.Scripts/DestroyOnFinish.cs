using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DestroyOnFinish : MonoBehaviour
{
    public float animationDuration = 1.5f;

    private void Start()
    {
        
    }

    public void EndEvent()
    {
        Destroy(gameObject);
    }
}
