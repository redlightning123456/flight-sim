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


    float yaw = 0, pitch = 0, roll = 0;
    float distanceFromDrone = 10;

    void Update()
    {
        respondToUserInput();
        this.transform.rotation = Quaternion.Euler(drone.transform.rotation.eulerAngles + new Vector3(pitch, yaw, roll));
        this.transform.position = drone.transform.position - this.transform.rotation * Vector3.forward * distanceFromDrone;
    }

    void respondToUserInput()
    {
        float positionDelta = 10.0f * Time.deltaTime;
        float rotationDelta = 35.0f * Time.deltaTime;
        float zoomDelta = 10.0f * Time.deltaTime;

        if (Input.GetKey("u"))
        {
            pitch += rotationDelta;
        }
        if (Input.GetKey("j"))
        {
            pitch -= rotationDelta;
        }
        if (Input.GetKey("h"))
        {
            yaw += rotationDelta;
        }
        if (Input.GetKey("k"))
        {
            yaw -= rotationDelta;
        }
        if (Input.GetKey("y"))
        {
            roll += rotationDelta;
        }
        if (Input.GetKey("i"))
        {
            roll -= rotationDelta;
        }
        if (Input.GetKey("w"))
        {
            if (this.distanceFromDrone - zoomDelta >= 6)
            {
                this.distanceFromDrone -= zoomDelta;
            }
        }
        if (Input.GetKey("s"))
        {
            this.distanceFromDrone += zoomDelta;
        }
    }
}
