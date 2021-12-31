using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CoordinateSharp;
using UnityEngine.Networking;
public class Movement : MonoBehaviour
{
    List<DronePath> dronePaths = new List<DronePath>();
    int dronePathIndex = 0;
    int destIndex = 0;
    EndpointBehavior whenAtEndpoint = EndpointBehavior.Cycle;
    public GameObject waypointMarker;
    PathDisplayer pathDisplayer;
    Map map;
    float speed = 0;
    bool finishedLoadingPaths = false;

    enum EndpointBehavior
    {
        Cycle,
        Halt
    }

    public struct DronePath
    {
        public int number;
        public List<Waypoint> waypoints;

        public DronePath(int number, List<Waypoint> waypoints)
        {
            this.number = number;
            this.waypoints = waypoints;
        }

        public Vector3 globalAnchor()
        {
            return new Vector3(waypoints[0].position.x, 0, waypoints[0].position.z);
        }
    };

    public class Waypoint
    {
        public Vector3 position { get; private set; }
	public double lat { get; private set; }
	public double lon { get; private set; }
	public double height { get; private set; }
        public double speed { get; private set; }


        public Waypoint(double lat, double lon, double height, double speed)
        {
            this.lat = lat;
	    this.lon = lon;
	    Coordinate coord = new Coordinate(lat, lon, new EagerLoad(EagerLoadType.UTM_MGRS));
	    this.position = new Vector3((float)(coord.UTM.Easting), (float)(height), (float)(coord.UTM.Northing));
	    this.height = height;
            this.speed = speed;
        }
    };

    IEnumerator loadDrones()
    {
        var www = UnityWebRequest.Get("http://localhost:8080/waypoints.xlsx");
        var sendOperation = www.SendWebRequest();

        yield return sendOperation;

        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.Log("Error when trying to fetch drone paths: " + www.error);
            Debug.Log("Response from server: " + www.responseCode);
            yield break;
        }


        using (var stream = new System.IO.MemoryStream(www.downloadHandler.data))
        {
            using (var reader = ExcelDataReader.ExcelReaderFactory.CreateReader(stream))
            {
                do
                {
                    if (reader.Name.StartsWith("d"))
                    {
                        int droneNumber;
                        if (int.TryParse(reader.Name.Substring(1), out droneNumber))
                        {
                            dronePaths.Add(new DronePath(droneNumber, new List<Waypoint>()));
                            reader.Read(); //start from the second row to ignore ["lat", "lon", "height", "speed"] head row
                            while (reader.Read())
                            {
                                float lat = (float)reader.GetDouble(0);
                                float lon = (float)reader.GetDouble(1);
                                float height = (float)reader.GetDouble(2);
                                float speed = (float)reader.GetDouble(3);
                                dronePaths[dronePaths.Count - 1].waypoints.Add(new Waypoint(lat, lon, height, speed));
                            }
                        }
                    }
                } while (reader.NextResult());
            }
        }
        dronePaths.Sort((a, b) => a.number.CompareTo(b.number));
        finishedLoadingPaths = true;
    }

    void advanceWaypoint()
    {
        destIndex += 1;
        if (destIndex >= dronePaths[dronePathIndex].waypoints.Count)
        {
            destIndex = 0;
            dronePathIndex += 1;
            if (dronePathIndex == dronePaths.Count)
            {
                dronePathIndex = 0;
            }
            pathDisplayer.updatePath(this.dronePaths[dronePathIndex]);
        }
    }

    private void OnGUI()
    {
        GUIStyle style = new GUIStyle();
        style.normal.textColor = Color.black;
        style.fontSize = 16;

        if (finishedLoadingPaths)
        {
            Vector3 coords;
            if (dronePathIndex < dronePaths.Count)
            {
                coords = transform.position + dronePaths[dronePathIndex].globalAnchor();
            }
            else
            {
                coords = transform.position + dronePaths[dronePaths.Count - 1].globalAnchor();
            }

            GUI.Label(new Rect(100, 100, 600, 100),
                "Drone #: " + (dronePathIndex + 1).ToString() + "\n" +
                "Waypoint #: " + (destIndex - 1 + 1).ToString() + "\n" +
                "Position (UTM northing/easting/height): " + coords.x + ", " + coords.z + ", " + coords.y,
                style
                );
        }
        else
        {
            GUI.Label(new Rect(100, 100, 600, 100),
                "Drone #: " + "N\\A" + "\n" +
                "Waypoint #: " + "N\\A" + "\n" +
                "Position (UTM northing/easting/height): " + "N\\A" + ", " + "N\\A" + ", " + "N\\A",
                style
                );
        }
    }

    void moveTo(Vector3 point)
    {
        this.transform.position = point;

        //var coord = UniversalTransverseMercator.ConvertUTMtoLatLong(new UniversalTransverseMercator("S", 36, point.x, point.z));
        //map.updatePos(coord.Latitude.ToDouble(), coord.Longitude.ToDouble());
    }

    IEnumerator Start()
    {
        pathDisplayer = new PathDisplayer(waypointMarker);
        map = new Map();
        yield return StartCoroutine(loadDrones());
        StartCoroutine(map.update(this.dronePaths[0].waypoints[0].lat, this.dronePaths[0].waypoints[0].lon, this.dronePaths[0].globalAnchor()));
        pathDisplayer.updatePath(this.dronePaths[0]);
        this.moveTo(dronePaths[0].waypoints[0].position - dronePaths[dronePathIndex].globalAnchor());
        this.speed = (float)(dronePaths[dronePathIndex].waypoints[destIndex].speed);
        advanceWaypoint();
        this.lookAtDest();
    }

    void lookAtDest()
    {
        Vector3 vectorToDest = dronePaths[dronePathIndex].waypoints[destIndex].position - dronePaths[dronePathIndex].globalAnchor() - this.transform.position;
        if (vectorToDest != Vector3.zero)
            this.transform.rotation = Quaternion.LookRotation(vectorToDest);
    }

    void moveTowardDest()
    {
        Vector3 vectorToDest = dronePaths[dronePathIndex].waypoints[destIndex].position - dronePaths[dronePathIndex].globalAnchor() - this.transform.position;
        if (vectorToDest != Vector3.zero)
            this.transform.rotation = Quaternion.Slerp(this.transform.rotation, Quaternion.LookRotation(vectorToDest), Time.deltaTime * this.speed / 10.0f);

        this.moveTo(this.transform.position + Time.deltaTime * this.speed * (this.transform.rotation * Vector3.forward));

        if (Vector3.Distance(transform.position, dronePaths[dronePathIndex].waypoints[destIndex].position - dronePaths[dronePathIndex].globalAnchor()) <= Time.maximumDeltaTime * this.speed)
        {
            this.speed = (float)(dronePaths[dronePathIndex].waypoints[destIndex].speed);
            advanceWaypoint();
            if (destIndex == 0)
            {
                this.moveTo(dronePaths[dronePathIndex].waypoints[0].position - dronePaths[dronePathIndex].globalAnchor());
                this.lookAtDest();
            }
        }
    }

    void Update()
    {
        if (finishedLoadingPaths)
        {
            switch (this.whenAtEndpoint)
            {
                case EndpointBehavior.Cycle:
                    if (dronePathIndex >= dronePaths.Count)
                    {
                        dronePathIndex = 0;
                        destIndex = 0;
                    }
                    this.moveTowardDest();
                    break;
                case EndpointBehavior.Halt:
                    if (dronePathIndex < dronePaths.Count)
                    {
                        this.moveTowardDest();
                    }
                    break;
            }
        }
    }
}
