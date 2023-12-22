using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CamController : MonoBehaviour
{
    [SerializeField] float minDis;
    [SerializeField] float maxDis;

    [SerializeField] float scrollSpeed;
    [SerializeField] float dragSpeed;

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Confined;
    }

    // Update is called once per frame
    void Update()
    {
        ScrollControl();
        DragControl();
    }
    
    /// <summary>
    /// Control the z position of the camera by scrolling 
    /// </summary>
    private void ScrollControl()
    {
        this.transform.position = 
            new Vector3(this.transform.position.x, this.transform.position.y, 0) +
            Vector3.forward * Mathf.Clamp(
            this.transform.position.z +
            Input.mouseScrollDelta.y * scrollSpeed * Time.deltaTime,
            minDis, maxDis);
    }

    /// <summary>
    /// Control the x and y position of the camera by dragging 
    /// </summary>
    private void DragControl()
    {
        if (Input.GetMouseButton(1))
        {
            Vector2 mouseInput = new Vector2(-Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
            this.transform.position += (Vector3)mouseInput * dragSpeed * Time.deltaTime;
        }

        // Reset position if lost 
        if(Input.GetKeyDown(KeyCode.F))
        {
            this.transform.position = Vector3.forward * this.transform.position.z;
        }
    }
}
