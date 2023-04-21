/*
 * Tillman Won
 * AP CS50
 * Cmdr. Schenk
 * 5th Period
 * Master Project - Table Calibration Class
 * 27 April 2023
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

// Controller class managing the height level of the desk and calibration
public class TableCalibration : MonoBehaviour
{
    [SerializeField]
    private GameObject desk;
    [SerializeField]
    private GameObject calibrationButton;
    [SerializeField]
    private GameObject boardAnchor;
    [SerializeField]
    private OVRHand hand;
    [SerializeField]
    private OVRSkeleton handSkeleton;
    private bool isLeveling;

    // Constructor
    void Awake()
    {
        // If hand/handSkeleton objects are null, instantiate them
        if (!hand) hand = GetComponent<OVRHand>();
        if (!handSkeleton) handSkeleton = GetComponent<OVRSkeleton>();
    }

    // Update is called once per frame by Unity
    void Update()
    {
        // If user is pressing the calibration button
        if (isLeveling)
        {
            // For each bone in Oculus Skeleton Rig...
            foreach (var bone in handSkeleton.Bones)
            {
                if (bone.Id == OVRSkeleton.BoneId.Hand_IndexTip)
                {
                    // Move desk height to right hand index finger
                    float fingerZ= bone.Transform.position.y;
                    desk.transform.localPosition = new Vector3(desk.transform.localPosition.x, fingerZ, desk.transform.localPosition.z);
                    desk.transform.Translate(new Vector3(0, -0.765f, 0));
                    boardAnchor.transform.position = new Vector3(boardAnchor.transform.position.x, desk.transform.position.y + 0.03f, boardAnchor.transform.position.z);
                }
            }
        }
    }

    // Button action event for when user is touching calibration button
    public void TableLeveler() {
        isLeveling = true;
    }

    // Button action event for when user releases calibration button
    public void SetLevel() {
        isLeveling = false;
    }

}
