using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;

public class GlobeController : MonoBehaviour
{
    public ControllerManager controllerManager;
    public GameManager gameManager;
    public GameObject pin;
    public GameObject earth;
    public GameObject solutionPin;
    public GameObject displayText;
    public float rotationSpeed;
    public float pinDropSpeed;
    public float pinDepth;
    private bool isPinDropping = false;
    private bool isHoldingPin = false;
    private Vector3 pinPoint;
    private Vector3 pinOffset;
    private Vector3 solution;
    public float globeRadius;
    public float timeToSubmit;
    private bool isGuessing = false;
    private float submitTimer = 0;
    private float submitCooldown = 0;
    private int round = 0;
    private bool ready = false;
    // Start is called before the first frame update
    void Start()
    {
        controllerManager = GameObject.Find("ControllerManager").GetComponent<ControllerManager>();
        gameManager = GameObject.Find("GameManager").GetComponent<GameManager>();
        gameManager.GlobeReady();
        SetDisplayText("Loading...");
    }

    // Update is called once per frame
    void Update()
    {
        // turn text to face camera
        Vector3 relativePosition = displayText.transform.InverseTransformPoint(Camera.main.transform.position);
        relativePosition.y = 0;
        Vector3 targetPosition = displayText.transform.TransformPoint(relativePosition);

        displayText.transform.LookAt(targetPosition, displayText.transform.up);
        displayText.transform.Rotate(0, 180, 0);

        if (controllerManager != null) {
            controllerManager.leftController.TryGetFeatureValue(CommonUsages.primary2DAxis, out Vector2 primary2DAxisValue);
            if (primary2DAxisValue.x != 0) {
                this.transform.Rotate(0, - primary2DAxisValue.x * rotationSpeed, 0);
            }

            controllerManager.leftController.TryGetFeatureValue(CommonUsages.trigger, out float triggerValue);

            if (submitCooldown != 0) {
                submitCooldown -= Time.deltaTime;
                submitCooldown = Mathf.Max(submitCooldown, 0);
            }

            if (triggerValue < 0.9 && submitTimer >= (timeToSubmit / 10.0f) && isGuessing && pinPoint != Vector3.zero) {
                if (round == 1) {
                    SetDisplayText("Hold down Left Trigger to submit your guess!");
                } else {
                    SetDisplayText("e");
                }
            }

            if (triggerValue >= 0.9 && !isPinDropping && !isHoldingPin && pinPoint != Vector3.zero && submitCooldown == 0) {
                submitTimer += Time.deltaTime;
                if (isGuessing && submitTimer >= (timeToSubmit / 10.0f)) {
                    SetDisplayText("Submitting...");
                } 
                if (submitTimer >= timeToSubmit) {
                    submitCooldown = 1;
                    if (isGuessing) {
                        Submit();
                    } else {
                        ready = false;
                        pin.transform.localPosition = new Vector3(0.0f, 26.5f, 0.0f);
                        pinPoint = Vector3.zero;
                        this.GetComponent<LineRenderer>().positionCount = 0;
                        
                        gameManager.RequestNewRound();
                    }
                }
            } else if (ready) {
                submitTimer -= Time.deltaTime * 2;
                submitTimer = Mathf.Max(submitTimer, 0);
            }

            float scale = 1.0f - Mathf.Min(submitTimer / timeToSubmit, 1);
            this.transform.localScale = new Vector3(scale, scale, scale);
        }

        if (isPinDropping) {
            Vector3 pinEndPosition = this.transform.TransformPoint(pinPoint) + pinOffset;
            if (Vector3.Distance(pin.transform.position, pinEndPosition) < 0.008f) {
                isPinDropping = false;
                if (round == 1) {
                    SetDisplayText("Hold down Left Trigger to submit your guess!");
                }
            } else {
                float step = pinDropSpeed * Time.deltaTime;
                pin.transform.position = Vector3.MoveTowards(pin.transform.position, pinEndPosition, step);
            }
        }
    }

    private void SetDisplayText(string message) {
        displayText.GetComponent<TextMesh>().text = message;
        displayText.transform.GetChild(0).GetComponent<TextMesh>().text = message;
    }

    private void Submit() {
        isGuessing = false;
        pin.GetComponent<XRGrabInteractable>().enabled = false;      
        solutionPin.transform.LookAt(earth.transform.position);
        solutionPin.transform.Rotate(-90, 0, 0);  
        solutionPin.SetActive(true);

        DrawLinePath();

        float earthRadius = 6371.0f; // in km
        float distance = earthRadius * Mathf.Acos(Vector3.Dot(pinPoint.normalized, solution));
        
        SetDisplayText(
            "Your guess was " + Mathf.Round(distance) + "km away!"
            + "\n\n(hold down Left Trigger to continue)"
        );
        gameManager.Submit(distance);
    }

    private void DrawLinePath() {
        int lineSegmentCount = 100;
        Vector3[] pointsOnPath = new Vector3[lineSegmentCount];
        for (int i = 0; i < lineSegmentCount; i++) {
            pointsOnPath[i] = Vector3.Slerp(
                pinPoint.normalized,
                solution,
                (float)i / (float)(lineSegmentCount - 1)
            ) * (globeRadius + 0.08f);
        }
        LineRenderer lineRenderer = this.GetComponent<LineRenderer>();
        lineRenderer.positionCount = lineSegmentCount;
        lineRenderer.SetPositions(pointsOnPath);
        lineRenderer.Simplify(0.01f);
    } 

    public void NewRound(Vector3 normalizedSolution) {
        pin.GetComponent<XRGrabInteractable>().enabled = true;   
        isGuessing = true;
        solution = normalizedSolution;
        solutionPin.transform.localPosition = normalizedSolution * (globeRadius + 2);
        solutionPin.SetActive(false);
        ready = true;
        round++;
        if (round == 1) {
            SetDisplayText("Round " + round + "\n\nGrab the pin to start!");
        } else {
            SetDisplayText("Round " + round);
        }
    }

    public void PinGrabbed() {
        isHoldingPin = true;
        if (round == 1) {
            SetDisplayText("Guess where in the world you are!");
        } else {
            SetDisplayText("");
        }
    }

    public void PinDropped() {
        isHoldingPin = false;
        RaycastHit hit;
        bool isInternal = false;
        if (!Physics.Raycast(pin.transform.position, pin.transform.TransformDirection(Vector3.down), out hit, 5.0f)) {
            pin.transform.LookAt(earth.transform.position);
            pin.transform.Rotate(-90, 0, 0);

            Physics.Raycast(pin.transform.position, pin.transform.TransformDirection(Vector3.down), out hit, 5.0f);

            // if pin is dropped inside globe
            if (hit.distance == 0) {
                Vector3 pinPositionBehind = pin.transform.position + (pin.transform.TransformDirection(Vector3.up)).normalized;
                Physics.Raycast(pinPositionBehind, pin.transform.TransformDirection(Vector3.down), out hit, 50.0f);
                isInternal = true;
            }
        }

        pinOffset = ((pin.transform.position - hit.point).normalized * pinDepth);
        if (isInternal) pinOffset *= -1;

        pinPoint = this.transform.InverseTransformPoint(hit.point);

        isPinDropping = true;
    }
    
}