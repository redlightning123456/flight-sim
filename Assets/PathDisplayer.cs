using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PathDisplayer
{
    List<GameObject> pathMarkers = new List<GameObject>();
    LineRenderer lineRenderer = new GameObject("Line").AddComponent<LineRenderer>();

    GameObject waypointMarker;

    public PathDisplayer(GameObject waypointMarker)
    {
        this.waypointMarker = waypointMarker;
        lineRenderer.startWidth = 1.0f;
        lineRenderer.endWidth = 1.0f;
        lineRenderer.useWorldSpace = true;
        Material material = lineRenderer.material;
        material.shader = Shader.Find("Legacy Shaders/Particles/Alpha Blended");
        material.SetVector("_TintColor", new Color(0.3f, 0, 0, 1));
    }
    
    public void updatePath(Movement.DronePath path)
    {
        foreach (var marker in pathMarkers)
        {
            GameObject.Destroy(marker);
        }
        pathMarkers.Clear();

        foreach (var waypoint in path.waypoints)
        {
            GameObject newMarker = GameObject.Instantiate(waypointMarker, waypoint.position - path.globalAnchor(), Quaternion.identity);
            Material material = newMarker.GetComponent<MeshRenderer>().material;
            material.shader = Shader.Find("Legacy Shaders/Particles/Alpha Blended");
            material.SetVector("_TintColor", new Color(0, 1, 0, 0.5f));
            pathMarkers.Add(newMarker);
        }

        lineRenderer.positionCount = path.waypoints.Count;
        for (int i = 0; i < path.waypoints.Count; ++i)
        {
            lineRenderer.SetPosition(i, path.waypoints[i].position - path.globalAnchor());
        }
    }
}
