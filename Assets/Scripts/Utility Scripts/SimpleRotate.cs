using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimpleRotate : MonoBehaviour
{
    // Update is called once per frame
    void Update()
    {
        transform.rotation *= Quaternion.Euler(0.5f,0.5f,0.5f);
    }
}
