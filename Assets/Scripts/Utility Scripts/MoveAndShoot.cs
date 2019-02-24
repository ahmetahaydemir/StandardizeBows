using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveAndShoot : MonoBehaviour
{
    public float speed= 0.05f;

    public float speedRot = 25f; //how fast the object should rotate
 
    void Update()
    {
        transform.Rotate(new Vector3(0, Input.GetAxis("Mouse X"), 0) * Time.deltaTime * speedRot);
    }

    void FixedUpdate()
    {
        float moveVertical = Input.GetAxis("Horizontal");
        float moveHorizontal = Input.GetAxis("Vertical");

        Vector3 movement = new Vector3(speed*moveHorizontal, 0.0f, -speed*moveVertical);

        transform.Translate(movement);
    }
}
