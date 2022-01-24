using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class SphereBox : MonoBehaviour
{
    public float sphereSize = 10;
    public List<GameObject> planes = new List<GameObject>();
    public Material planeMaterial;
    public Renderer transitionRenderer;
    private int numHorizontal = 0;
    private int numVertical = 0;
    private float fadeoutSpeed = 1.5f;
    private int fadeDirection = 1;
    public float transitionAlpha = 0f;
    // Start is called before the first frame update
    void Start()
    {
       
    }

    public void generatePlanes(int numHorizontalTiles, int numVerticalTiles) {
        if (numHorizontalTiles != numHorizontal || numVerticalTiles != numVertical) {
            numHorizontal = numHorizontalTiles;
            numVertical = numVerticalTiles;

            for (int i = 0; i < planes.Count; i++) {
                Destroy(planes[i]);
            }
            planes = new List<GameObject>();

            // Create sphere of planes
            for (int h = 0; h < numHorizontal; h++) {
                double angleh1 = h * Math.PI / (numHorizontal);
                double angleh2 = (h + 1) * Math.PI / (numHorizontal);
                for (int v = 0; v < numVertical; v++) {
                    double anglev1 = v * -2 * Math.PI / numVertical;
                    double anglev2 = (v + 1) * -2 * Math.PI / numVertical;
                    Mesh m = new Mesh();
                    m.name = "Plane_Mesh_" + h + "_" + v;
                    m.vertices = new Vector3[] {
                        ConvertToPointOnSphere(angleh2, anglev1),
                        ConvertToPointOnSphere(angleh1, anglev1),
                        ConvertToPointOnSphere(angleh1, anglev2),
                        ConvertToPointOnSphere(angleh2, anglev2),
                    };
                    m.uv = new Vector2[] {
                        new Vector2 (0, 0),
                        new Vector2 (0, 1),
                        new Vector2 (1, 1),
                        new Vector2 (1, 0)
                    };
                    m.triangles = new int[] { 0, 1, 2, 0, 2, 3};
                    m.RecalculateNormals();
                    GameObject newPlane = new GameObject("Plane_" + h + "_" + v);
                    newPlane.transform.SetParent(transform, false);
                    MeshFilter meshFilter = newPlane.AddComponent<MeshFilter>() as MeshFilter;
                    meshFilter.mesh = m;
                    MeshRenderer meshRenderer = newPlane.AddComponent<MeshRenderer>() as MeshRenderer;
                    meshRenderer.material = new Material(planeMaterial);
                    // meshRenderer.enabled = false;

                    planes.Add(newPlane);
                }
            }
        }
    }

    Vector3 ConvertToPointOnSphere(double angle1, double angle2) {    
        return new Vector3(
            (float)( Math.Sin(angle1) * Math.Cos(angle2) )  * sphereSize, 
            (float)( Math.Cos(angle1))                      * sphereSize,
            (float)( Math.Sin(angle1) * Math.Sin(angle2) )  * sphereSize
        );
    }

    // Update is called once per frame
    void Update()
    {
        transitionAlpha = Mathf.Clamp(
            transitionAlpha + Time.deltaTime * fadeoutSpeed * fadeDirection,
            0, 1
        );
        transitionRenderer.material.SetColor("_Color", new Color(0f, 0f, 0f, transitionAlpha));
    }

    public void SetVisible(bool visible) {
        if (visible) {
            fadeDirection = -1;
        } else {
            fadeDirection = 1;
        }
    }

    public void UpdatePanoramaImage(int x, int y, Texture tile) {
        planes[(y * numVertical) + x].GetComponent<Renderer>().material.SetTexture("_MainTex", tile);
    }
}
