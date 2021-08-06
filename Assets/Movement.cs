using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Movement : MonoBehaviour
{
    class Waypoint
    {
        public Vector3 position;
        public float speed;

        //slightly modified mercatorEncode from https://gist.github.com/leebyron/997450
        static double[] mercatorEncode(double lon, double lat, double lonOrigin)
        {
            double[] p = new double[2];
            p[0] = (lon - lonOrigin) / (2 * Mathf.PI);
            while (p[0] < 0) p[0]++;
            p[1] = 1 - (Mathf.Log(Mathf.Tan((float)(Mathf.PI / 4 + lat / 2))) * 0.31830988618 + 1) / 2;
            return p;
        }

        static double[] LonLatTo2DCartesianCoordinates(double lon, double lat)
        {
            return mercatorEncode(lon * Mathf.PI / 180, lat * Mathf.PI / 180, -Mathf.PI);
        }
        
        static Vector3 positionFormatTo3DCartesianCoordinates(Vector3 position)
        {
            double[] surfaceCoordinates = LonLatTo2DCartesianCoordinates(position[0], position[1]);
            return new Vector3((float)surfaceCoordinates[0], position[2], (float)surfaceCoordinates[1]);
        }
        
        public Waypoint(Vector3 position, float speed)
        {
            this.position = position;
            this.speed = speed;
        }
    };

    ////for real lon/lat/height sample
    //List<Waypoint> waypoints = new List<Waypoint>()
    //{
    //    new Waypoint(new Vector3(32.88271705f, 35.03533566f, 2.0f), 20),
    //    new Waypoint(new Vector3(32.97832839f, 35.14022953f, 0.0f), 20),
    //    new Waypoint(new Vector3(32.56478879f, 35.24022953f, 2.0f), 20),
    //    new Waypoint(new Vector3(32.46478879f, 35.03533566f, 0.0f), 20),
    //    new Waypoint(new Vector3(32.36478879f, 35.05458656f, 2.0f), 0)
    //};

    //for pilot test data
    List<Waypoint> waypoints = new List<Waypoint>()
    {
        new Waypoint(new Vector3(15.2f, 97.4f, 121.8f), 20),
        new Waypoint(new Vector3(-65.6f, 115.8f, 66.8f), 20),
        new Waypoint(new Vector3(85.2f, 45.2f, -58.6f), 20),
        new Waypoint(new Vector3(-85.7f, 53.8f, -72.9f), 0)
    };

    int nextWaypointIndex = 0;

    void Start()
    {
        if (waypoints.Count > 0)
        {
            this.transform.position = waypoints[nextWaypointIndex].position;
            nextWaypointIndex += 1;
        }
        else
        {
            Debug.LogError("No waypoints were specified for the drone. It has nowhere to go.");
        }
    }

    void Update()
    {
        if (nextWaypointIndex < waypoints.Count)
        {
            this.transform.rotation = Quaternion.LookRotation(waypoints[nextWaypointIndex].position -  this.transform.position);

            this.transform.position += Time.deltaTime * waypoints[nextWaypointIndex - 1].speed * (this.transform.rotation * Vector3.forward);
            
            if (Vector3.Distance(transform.position, waypoints[nextWaypointIndex].position) <= Time.maximumDeltaTime * waypoints[nextWaypointIndex - 1].speed)
            {
                nextWaypointIndex += 1;
            }
            
        }
    }
}
