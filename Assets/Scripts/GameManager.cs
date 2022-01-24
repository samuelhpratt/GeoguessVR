using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System.Text.RegularExpressions;

public class GameManager : MonoBehaviour
{
    public GameObject sphereBoxObject;
    public GlobeController globeController;
    private SphereBox sphereBox;
    public TextAsset PanoIdFile;
    private string[] panoIdList;
    public TextAsset StreetViewKeyFile;
    private string streetViewKey;
    private PanoramaMetadata currentPanorama;
    private int numHorizontalTiles, numVerticalTiles;
    private bool transitioning = false;

    // Start is called before the first frame update
    void Start()
    {
        sphereBox = sphereBoxObject.GetComponent<SphereBox>();
        panoIdList = Regex.Split(PanoIdFile.text, "\r\n?|\n", RegexOptions.Singleline);
        streetViewKey = StreetViewKeyFile.text;
        RequestNewPano();
    }

    // Update is called once per frame
    void Update()
    {
        if (transitioning && sphereBox.transitionAlpha == 1) {
            transitioning = false;
            RequestNewPano();
        }
    }

    [System.Serializable]
    public class Location {
        public double lat;
        public double lng;
    }
    [System.Serializable]
    public class PanoramaMetadata {
        public string date;
        public Location location;
        public string pano_id;
        public string status;
    }

    public Vector3 LocationToVector3(Location location) {
        // stolen from Sebastian Lague https://www.youtube.com/watch?v=sLqXFF8mlEU
        float latitudeInRadians = Mathf.Deg2Rad * (float)location.lat;
        float longitudeInRadians = Mathf.Deg2Rad * (float)location.lng;
        float y = Mathf.Sin(latitudeInRadians);
        float r = Mathf.Cos(latitudeInRadians);
        float x = Mathf.Sin(longitudeInRadians) * r;
        float z = - Mathf.Cos(longitudeInRadians) * r;
        return new Vector3(x, y, z);
    }
    public void GlobeReady()
    {
        globeController = GameObject.Find("Globe(Clone)").GetComponent<GlobeController>();
    }

    public void Submit(float distance) {
    }

    public void RequestNewRound() {
        sphereBox.SetVisible(false);
        transitioning = true;
    }

    public void RequestNewPano() {
        string pano = panoIdList[Random.Range(0,panoIdList.Length)];
        StartCoroutine(GetPositionMetadata(pano));
        StartCoroutine(GetPanoTiles(pano));
    }

    IEnumerator GetPositionMetadata(string panoId) {
        string url = "https://maps.googleapis.com/maps/api/streetview/metadata?key=" + streetViewKey + "&pano=" + panoId;
        UnityWebRequest req = UnityWebRequest.Get(url);
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success) {
            Debug.Log(req.error);
        }
        else {
            currentPanorama = JsonUtility.FromJson<PanoramaMetadata>(req.downloadHandler.text);
        }
        req.Dispose();
    }

    IEnumerator GetTileAtPosition(string panoId, int x, int y) {
        string url = "http://cbk0.google.com/cbk?output=tile&panoid=" + panoId + "&zoom=5&x=" + x + "&y=" + y;
        
        UnityWebRequest req = UnityWebRequestTexture.GetTexture(url);
        yield return req.SendWebRequest();
    
        if (req.result != UnityWebRequest.Result.Success) {
            if (x == 0 && y == 0) {
                Debug.Log(panoId + " failed.");
            }
        }
        else {
            Texture tileTexture = ((DownloadHandlerTexture)req.downloadHandler).texture;
            tileTexture.name = "tile_" + x + "_" + y;
            sphereBox.UpdatePanoramaImage(x, y, tileTexture);
        }
        req.Dispose();
    }

    IEnumerator GetPanoTiles(string panoId) { 
        //check pano size
        string url = "http://cbk0.google.com/cbk?output=tile&panoid=" + panoId + "&zoom=5&x=" + 26 + "&y=" + 13;
        UnityWebRequest req = UnityWebRequestTexture.GetTexture(url);
        yield return req.SendWebRequest();
    
        if (req.result != UnityWebRequest.Result.Success) {
            numHorizontalTiles = 13;
            numVerticalTiles = 26;
        }
        else {
            numHorizontalTiles = 16;
            numVerticalTiles = 32;
        }
        req.Dispose();

        sphereBox.generatePlanes(numHorizontalTiles, numVerticalTiles);
        
        // get all pano tiles
        Stack<Coroutine> requests = new Stack<Coroutine>();
        for (int y = 0; y < numHorizontalTiles; y++) {
            for (int x = 0; x < numVerticalTiles; x++) {
                requests.Push(StartCoroutine(GetTileAtPosition(panoId, x, y)));       
            }
            while (requests.Count > 0) {
                var request = requests.Pop();
                yield return request;
            }
        }
        // all tiles are done now :)
        sphereBox.SetVisible(true);
        globeController.NewRound(LocationToVector3(currentPanorama.location));
    }
}
