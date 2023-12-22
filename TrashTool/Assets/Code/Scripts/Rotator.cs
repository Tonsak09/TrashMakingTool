using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Rotator : MonoBehaviour
{
    [SerializeField] float rotSpeed;

    private bool dragging;
    private Rigidbody rb;

    // Start is called before the first frame update
    void Start()
    {
        rb = this.GetComponent<Rigidbody>();    
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetMouseButtonUp(0))
        {
            dragging = false;
        }
        else if(Input.GetMouseButtonDown(0))
        {
            dragging = true;
        }
        
        if (!dragging)
            return;

        Vector2 mouseInput = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
        mouseInput *= -rotSpeed * Time.deltaTime;

        rb.AddTorque(Vector3.down * mouseInput.x);
        rb.AddTorque(Vector3.right * mouseInput.y);
    }
}
