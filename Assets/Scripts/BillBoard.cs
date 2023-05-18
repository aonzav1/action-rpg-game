using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BillBoard : MonoBehaviour
{
    Camera mainCamera;
    void Start()
    {
        mainCamera = Camera.main;
    }
    void LateUpdate()
    {
        transform.LookAt(mainCamera.transform);
        transform.eulerAngles = new Vector3(0,transform.eulerAngles.y,0);
        transform.Rotate(0, 180, 0);
    }
}
