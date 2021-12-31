using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System.IO;
using ICSharpCode;
using CoordinateSharp;
using Newtonsoft.Json;

public class Map
{
    public Map()
    {
    }


    public IEnumerator update(double lat, double lon, Vector3 anchor)
    {
	var north = lat + 0.05;
	var west = lon - 0.05;
	var south = lat - 0.05;
	var east = lon + 0.05;
	var www = UnityWebRequest.Get("http://overpass-api.de/api/interpreter?data=[out:json];nwr(" + south + ", " + west + ", " + north + ", " + east + ");out;");
        var sendOperation = www.SendWebRequest();

        yield return sendOperation;

        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.Log("Error when trying to fetch map: " + www.error);
            Debug.Log("Response from server: " + www.responseCode);
            yield break;
        }

        //update map with new data
        this.process(www.downloadHandler.data, anchor);
	Debug.Log("Map loaded successfully");
    }



    struct Node
    {
        public double lat { get; private set; }
        public double lon { get; private set; }
	public UniversalTransverseMercator utm { get; private set; }

        public Node(double lat, double lon)
        {
            this.lat = lat;
            this.lon = lon;
	    var coord = new Coordinate(lat, lon, new EagerLoad(EagerLoadType.UTM_MGRS));
	    this.utm = coord.UTM;
        }
    }

    struct MapExtract
    {
	    public struct Tags {
		    public string height { get; set; }
	    }
	    public struct Element {
		    public string type { get; set; }
		    public long id { get; set; }
		    public double lat { get; set; }
		    public double lon { get; set; }
		    public List<long> nodes { get; set; }
		    public Tags tags { get; set; }
	    }
	    public List<Element> elements { get; set; }
    }

    void process(byte[] fileBytes, Vector3 anchor)
    {
	    var extract = JsonConvert.DeserializeObject<MapExtract>(System.Text.Encoding.UTF8.GetString(fileBytes));
	    var nodes = new Dictionary<long, Node>();
	    foreach (var element in extract.elements)
	    {
		    if (element.type == "node")
		    {
			    nodes.Add(element.id, new Node(element.lat, element.lon));
		    }
		    else if (element.type == "way")
		    {
			    var wayPositions = new List<Vector2>();
			    var wayHeight = 0.5f;
			    if (element.tags.height != null)
			    {
				    if (float.TryParse(element.tags.height, out _))
				    {
				    	wayHeight = float.Parse(element.tags.height);
				    } else {
					Debug.Log("Failed to parse height: " + element.tags.height);
				    }
			    }
			    foreach (var nodeId in element.nodes)
			    {
				    if (nodes.ContainsKey(nodeId))
				    {
					    var node = nodes[nodeId];
					    var projectedPosition = new Vector2((float)(node.utm.Easting) - anchor.x, (float)(node.utm.Northing) - anchor.z);
					    wayPositions.Add(projectedPosition);
				    }
				    else
				    {
					    wayPositions.Clear();
					    break;
				    }
			    }
			    if (wayPositions.Count > 0)
			    {
				    var gameobject = new GameObject();
				    gameobject.AddComponent<MeshFilter>();
				    gameobject.AddComponent<MeshRenderer>();
				    var mesh = gameobject.GetComponent<MeshFilter>().mesh;
				    var material = gameobject.GetComponent<MeshRenderer>().material;
				    material.shader = Shader.Find("Legacy Shaders/Particles/Alpha Blended");
				    material.SetVector("_TintColor", new Color(0.1f, 0.1f, 0.1f, 1));
				    var vertices = new List<Vector3>();

				    for (int positionIndex = 0; positionIndex < wayPositions.Count; ++positionIndex) {
					    vertices.Add(new Vector3(wayPositions[positionIndex].x, 0, wayPositions[positionIndex].y));
					    vertices.Add(new Vector3(wayPositions[positionIndex].x, wayHeight, wayPositions[positionIndex].y));
				    }

				    mesh.vertices = vertices.ToArray();

				    var triangles = new List<int>();

				    for (int j = 0; j < wayPositions.Count - 1; ++j) {
					    triangles.Add(2 * j);
					    triangles.Add(2 * j + 2);
					    triangles.Add(2 * j + 3);

					    triangles.Add(2 * j);
					    triangles.Add(2 * j + 3);
					    triangles.Add(2 * j + 1);

					    triangles.Add(2 * j + 3);
					    triangles.Add(2 * j + 2);
					    triangles.Add(2 * j);

					    triangles.Add(2 * j + 1);
					    triangles.Add(2 * j + 3);
					    triangles.Add(2 * j);
				    }
				    mesh.triangles = triangles.ToArray();
				    wayPositions.Clear();
			    }
		    }
		    else if (element.type == "relation")
		    {
			    ;
		    }
	    }
    }

    //the following block of code is currently obselete
    //it was once used to load the map from files with the osm pbf format
    //however, that ability is currently deprecated since Overpass API doesn't support osm pbf
    //still, it might be useful later in case using Overpass turns out to be impractical

    /*int x = 0;

    void process(byte[] fileBytes, Vector3 anchor)
    {
        //TODO: Should also destroy the lines

        bool somethingUnexpectedHappened = false;
        var file = new MemoryStream(fileBytes);

        var nodes = new Dictionary<long, Node>();

        while (true)
        {
            var headerLengthBytes = new byte[4];
            var headerLengthByteCount = file.Read(headerLengthBytes, 0, 4);
            
            if (headerLengthByteCount == 0)
            {
                break;
            }
            else if (headerLengthByteCount != headerLengthBytes.Length)
            {
                somethingUnexpectedHappened = true;
                break;
            }

            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(headerLengthBytes);
            }
            var headerLength = BitConverter.ToUInt32(headerLengthBytes, 0);

            var blobHeaderBytes = new byte[headerLength];
            if (file.Read(blobHeaderBytes, 0, (int)headerLength) != blobHeaderBytes.Length)
            {
                somethingUnexpectedHappened = true;
                break;
            }
            
            var blobHeader = OSMPBF.BlobHeader.Parser.ParseFrom(blobHeaderBytes);

            var blobBytes = new byte[blobHeader.Datasize];
            if (file.Read(blobBytes, 0, blobHeader.Datasize) != blobBytes.Length)
            {
                somethingUnexpectedHappened = true;
                break;
            }
            var blob = OSMPBF.Blob.Parser.ParseFrom(blobBytes);
            var blobDataBytes = new byte[0];
            switch (blob.DataCase)
            {
                case OSMPBF.Blob.DataOneofCase.Raw:
                    blobDataBytes = blob.Raw.ToByteArray();
                    break;
                case OSMPBF.Blob.DataOneofCase.ZlibData:
                    blobDataBytes = (blob.ZlibData.ToByteArray());
                    using (var memory = new MemoryStream(blob.ZlibData.ToByteArray()))
                    {
                        using (var inflater = new ICSharpCode.SharpZipLib.Zip.Compression.Streams.InflaterInputStream(memory))
                        {
                            blobDataBytes = new byte[blob.RawSize];
                            inflater.Read(blobDataBytes, 0, blob.RawSize);
                        }
                    }
                    break;
                default:
                    somethingUnexpectedHappened = true;
                    break;
            }

            if (blobHeader.Type == "OSMHeader")
            {
                var header = OSMPBF.HeaderBlock.Parser.ParseFrom(blobDataBytes);
            }
            else if (blobHeader.Type == "OSMData")
            {
                var pblock = OSMPBF.PrimitiveBlock.Parser.ParseFrom(blobDataBytes);
                
                foreach (var pgroup in pblock.Primitivegroup)
                {
                    if (pgroup.Dense != null)
                    {
                        var ids = pgroup.Dense.Id;
                        var lats = pgroup.Dense.Lat;
                        var lons = pgroup.Dense.Lon;

                        long aid = 0;
                        long alat = 0;
                        long alon = 0;

                        for (var i = 0; i < ids.Count; ++i)
                        {
                            aid += ids[i];
                            alat += lats[i];
                            alon += lons[i];
                            var id = aid;
                            var lat = 0.000000001 * (pblock.LatOffset + pblock.Granularity * alat);
                            var lon = 0.000000001 * (pblock.LonOffset + pblock.Granularity * alon);

                            nodes.Add(id, new Node(lat, lon));
                        }
                    }
                }

                foreach (var pgroup in pblock.Primitivegroup)
                {
                    if (pgroup.Ways.Count > 0)
                    {
                        foreach (var way in pgroup.Ways)
                        {
                            if (x > 10000)
                            {
                                Debug.Log("too many ways");
                                return;
                            }

			    var height = 0.5f;

			    for (var i = 0; i < way.Keys.Count; ++i) {
				    var keyIndex = way.Keys[i];
				    var valIndex = way.Vals[i];
				    var key = pblock.Stringtable.S[(int)(keyIndex)].ToStringUtf8();
				    var val = pblock.Stringtable.S[(int)(valIndex)].ToStringUtf8();

				    if (key == "height") {
					    height = float.Parse(val);
				    }
			    }




                            var refs = way.Refs;


                            long aref = 0;
                            var wayPositions = new List<Vector3>();

                            for (var i = 0; i < refs.Count; ++i)
                            {
                                aref += refs[i];
                                var refv = aref;

                                if (nodes.ContainsKey(refv))
                                {
                                    var node = nodes[refv];

                                    var coord = new Coordinate(node.lat, node.lon);
                                    var utm = coord.UTM;
                                    var projectedPosition = (new Vector3((float)(coord.UTM.Easting), height, (float)(coord.UTM.Northing)) - anchor);
                                    wayPositions.Add(projectedPosition);
                                }
                                else
                                {
                                    if (wayPositions.Count == 0)
                                    {
                                        continue;
                                    }
                                    ++x;
                                    var gameobject = new GameObject();
				    gameobject.AddComponent<MeshFilter>();
				    gameobject.AddComponent<MeshRenderer>();
				    var mesh = gameobject.GetComponent<MeshFilte--r>().mesh;
				    var material = gameobject.GetComponent<MeshRenderer>().material;
				    material.shader = Shader.Find("Legacy Shaders/Particles/Alpha Blended");
				    material.SetVector("_TintColor", new Color(0.1f, 0.1f, 0.1f, 1));

				    var vertices = new List<Vector3>();

				    for (int positionIndex = 0; positionIndex < wayPositions.Count; ++positionIndex) {
					vertices.Add(new Vector3(wayPositions[positionIndex].x, 0, wayPositions[positionIndex].z));
					vertices.Add(new Vector3(wayPositions[positionIndex].x, height, wayPositions[positionIndex].z));
				    }

				    mesh.vertices = vertices.ToArray();

				    var triangles = new List<int>();

				    for (int j = 0; j < wayPositions.Count - 1; ++j) {
					    triangles.Add(2 * j);
					    triangles.Add(2 * j + 2);
					    triangles.Add(2 * j + 3);
					    triangles.Add(2 * j);
					    triangles.Add(2 * j + 3);
					    triangles.Add(2 * j + 1);

					    triangles.Add(2 * j + 3);
					    triangles.Add(2 * j + 2);
					    triangles.Add(2 * j);
					    triangles.Add(2 * j + 1);
					    triangles.Add(2 * j + 3);
					    triangles.Add(2 * j);
				    }
				    mesh.triangles = triangles.ToArray();
				    wayPositions.Clear();
                                }
                            }

                            if (wayPositions.Count > 0)
                            {
                            	++x;
                                var gameobject = new GameObject();
				gameobject.AddComponent<MeshFilter>();
				gameobject.AddComponent<MeshRenderer>();
				var mesh = gameobject.GetComponent<MeshFilter>().mesh;
				var material = gameobject.GetComponent<MeshRenderer>().material;
				material.shader = Shader.Find("Legacy Shaders/Particles/Alpha Blended");
				material.SetVector("_TintColor", new Color(0.1f, 0.1f, 0.1f, 1));

				var vertices = new List<Vector3>();

				for (int positionIndex = 0; positionIndex < wayPositions.Count; ++positionIndex) {
				    vertices.Add(new Vector3(wayPositions[positionIndex].x, 0, wayPositions[positionIndex].z));
				    vertices.Add(new Vector3(wayPositions[positionIndex].x, height, wayPositions[positionIndex].z));
				}

				mesh.vertices = vertices.ToArray();

				var triangles = new List<int>();

				for (int j = 0; j < wayPositions.Count - 1; ++j) {
					triangles.Add(2 * j);
					triangles.Add(2 * j + 2);
					triangles.Add(2 * j + 3);
					triangles.Add(2 * j);
					triangles.Add(2 * j + 3);
					triangles.Add(2 * j + 1);

					triangles.Add(2 * j + 3);
					triangles.Add(2 * j + 2);
					triangles.Add(2 * j);
					triangles.Add(2 * j + 1);
					triangles.Add(2 * j + 3);
					triangles.Add(2 * j);
				}
				mesh.triangles = triangles.ToArray();
				wayPositions.Clear();
                            }
                        }
                    }
                }
            }
        }
    }*/
    //public void updatePos(double lat, double lon)
    //{
    //}
};
