using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraMgr : MonoBehaviour
{
    public Transform cameraTransform;
    public float zoomSpeed;
    public float panningSpeed;
    private Vector3 dragOrigin;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        float scroll = Input.mouseScrollDelta.y;
        float zValue = Mathf.Clamp(Camera.main.transform.position.z + scroll * zoomSpeed, -140, -60);
        cameraTransform.position = new Vector3(cameraTransform.position.x, cameraTransform.position.y, zValue);

        // While holding middle mouse
        if (Input.GetMouseButton(2))
        {
            Vector3 difference = dragOrigin - Input.mousePosition;
            cameraTransform.position += difference * panningSpeed;
        }
        dragOrigin = Input.mousePosition;
    }
}
