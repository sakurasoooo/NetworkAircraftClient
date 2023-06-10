using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SelfRotation : MonoBehaviour
{
    // random speed and direction
    public float speed = 1.0f;
    public float direction = 1.0f;
    // Start is called before the first frame update
    void Start()
    {
        speed = Random.Range(0.5f, 2.0f);
        direction = Random.Range(-1.0f, 1.0f);
    }

    // Update is called once per frame
    void Update()
    {
        // Rotate the object around its local Z axis at 1 degree per second
        transform.Rotate(Vector3.forward * direction * speed * Time.deltaTime * 10);
    }
}
