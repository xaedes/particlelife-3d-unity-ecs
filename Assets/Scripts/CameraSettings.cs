using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;


public class CameraSettings : MonoBehaviour
{
    public Camera camera;
    public CameraControl control;
    
    void Start()
    {
        if (camera == null) camera = GetComponent<Camera>();
        if (control == null) control = GetComponent<CameraControl>();
    }

    // Update is called once per frame
    void Update()
    {
        //transform.
    }
}
