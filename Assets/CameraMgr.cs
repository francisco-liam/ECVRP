using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraMgr : MonoBehaviour
{
    public float zoomSpeed;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        float scroll = Input.mouseScrollDelta.y;
        float zValue = Mathf.Clamp(Camera.main.transform.position.z + scroll * zoomSpeed, -100, -60);
        Camera.main.transform.position = new Vector3(0, 0, zValue);
    }
}
