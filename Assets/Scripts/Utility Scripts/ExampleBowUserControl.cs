using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExampleBowUserControl : MonoBehaviour
{
    Animator animator;
    int paramHash = Animator.StringToHash("Drawing");
    // Start is called before the first frame update
    void Start()
    {
        animator = GetComponent<Animator>();
    }
    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButton(0))
        {
            animator.SetBool(paramHash,true);
        }
        else
        {
            if (Input.GetMouseButtonUp(0))
            {
                animator.SetBool(paramHash, false);
            }
        }
    }
}
