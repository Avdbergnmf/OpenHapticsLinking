using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HapticLinking : MonoBehaviour
{
    // Public Variables
    [Header("Toggles")]
    public bool enableGrabbing = true;
    public bool ButtonActsAsToggle = true;          // Toggle button? as opposed to a press-and-hold setup?
    public bool enableCustomControlPoint = false;   // if this is disabled, the controlledobject is grabbed at its origin.

    [Header("Settings")]
    public int buttonID = 1;                        // Index of the button assigned to grabbing.
    public GameObject linkedObject = null;          // This is the object that will be linked to
    public Transform customControlPoint = null;     // The location of the point you want to control, this always takes the global position of the transform object you input

    [ReadOnly] public GameObject grabbing = null;			    // Reference to the object currently grabbed

    // Private Variables
    private Vector3 relativeControlPoint;
    private Vector3 controlPoint;

    private GameObject hapticDevice = null;         // Reference to the GameObject representing the Haptic Device
    private bool buttonStatus = false;              // Is the button currently pressed?
    

    private FixedJoint joint = null;                // The Unity physics joint created between the stylus and the object being grabbed.
    private FixedJoint jointPause = null;
    private Transform parentTransform = null;

    // Timeout system - To prevent connecting & disconnecting instantly, sometimes the button would glitch out when pressed and would disconnect instantly.
    private bool timeOut = true; // True if time has passed
    private float timeOutTime = 0.1f; // How long to time out 

    void Start()
    {
        if (hapticDevice == null)
        {
            HapticPlugin[] HPs = FindObjectsOfType<HapticPlugin>();

            foreach (HapticPlugin HP in HPs)
                if (HP.hapticManipulator == this.gameObject) hapticDevice = HP.gameObject;

            if (hapticDevice == null) Debug.LogError("Please attach this script to a gameobject with the HapticPlugin Components");

            parentTransform = hapticDevice.transform.parent;
        }

        if (enableCustomControlPoint) // if a custom control point is set
        {
            if (customControlPoint == null) Debug.LogError("Custom control point enabled but not set");
            relativeControlPoint = linkedObject.transform.localToWorldMatrix.inverse.MultiplyPoint3x4(customControlPoint.position);
            //Debug.Log("Setting relativeControlPoint to: " + controlledObject.transform.position);
        }
        else relativeControlPoint = Vector3.zero;

        StartCoroutine(timeOutCoroutine()); // There was some strange issue where sometimes it would connect on start, this timeout fixed that
    }

    // Update is called once per frame
    private void FixedUpdate()
    {
        bool newButtonStatus = (hapticDevice.GetComponent<HapticPlugin>().Buttons[buttonID] == 1);
        bool oldButtonStatus = buttonStatus;
        buttonStatus = newButtonStatus;

        // Get the controlPoint
        controlPoint = linkedObject.transform.localToWorldMatrix.MultiplyPoint3x4(relativeControlPoint);

        // --------- Grabbing Functions -----------
        if (enableGrabbing && timeOut)
        {
            if (oldButtonStatus == false && newButtonStatus == true)
            {
                if (ButtonActsAsToggle)
                {
                    if (grabbing)
                        release();
                    else
                        grab();
                }
                else grab();
            }
            if (oldButtonStatus == true && newButtonStatus == false)
            {
                if (ButtonActsAsToggle)
                {
                    //Do Nothing
                }
                else release();
            }
        }
        else if (grabbing && timeOut) release();

        // Make sure haptics is ON if we're grabbing
        if (grabbing)
            hapticDevice.GetComponent<HapticPlugin>().PhysicsManipulationEnabled = true;
        else
            hapticDevice.GetComponent<HapticPlugin>().PhysicsManipulationEnabled = false;
    }

    // Begin grabbing an object. (Like closing a claw.) Normally called when the button is pressed. 
    public void grab()
    {
        if (grabbing != null) // Already grabbing
            return;
        if (linkedObject == null) // Nothing to grab
            return;

        StartCoroutine(timeOutCoroutine());

        matchPosition();

        Destroy(jointPause);
        grabbing = linkedObject;
        Debug.Log("Grabbing Object : " + grabbing.name);
        Rigidbody body = grabbing.GetComponent<Rigidbody>();

        joint = (FixedJoint)gameObject.AddComponent(typeof(FixedJoint));
        joint.connectedBody = body;

        // turn on the physics
        hapticDevice.GetComponent<HapticPlugin>().PhysicsManipulationEnabled = true;
    }

    IEnumerator timeOutCoroutine()
    {
        timeOut = false;
        yield return new WaitForSeconds(timeOutTime);
        timeOut = true;
    }

    void matchPosition()
    {
        Vector3 posDifference = transform.position - controlPoint;
        parentTransform.position -= posDifference;
    }

    //! Stop grabbing an obhject. (Like opening a claw.) Normally called when the button is released. 
    public void release()
    {
        if (grabbing == null) //Nothing to release
            return;

        Debug.Assert(joint != null);

        joint.connectedBody = null;
        Destroy(joint);

        grabbing = null;

        // turn off the physics
        hapticDevice.GetComponent<HapticPlugin>().PhysicsManipulationEnabled = false;

        jointPause = (FixedJoint)linkedObject.AddComponent(typeof(FixedJoint));
    }

    //! Returns true if there is a current object. 
    public bool isGrabbing()
    {
        return (grabbing != null);
    }
}
