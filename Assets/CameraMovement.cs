using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraMovement : MonoBehaviour
{
    GameObject drone;
    
    void Start()
    {
        drone = GameObject.FindWithTag("Drone");
    }

    Vector3 positionRelativeToDrone = new Vector3(10, 4, -17);
    void Update()
    {
        this.transform.rotation = drone.transform.rotation;
        this.transform.position = drone.transform.position + this.transform.rotation * positionRelativeToDrone;
    }
}
