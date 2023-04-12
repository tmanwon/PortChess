using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

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

    // Start is called before the first frame update (ie. Constructor)
    void Awake()
    {
        if (!hand) hand = GetComponent<OVRHand>();
        if (!handSkeleton) handSkeleton = GetComponent<OVRSkeleton>();
    }

    // Update is called once per frame
    void Update()
    {
        if (isLeveling)
        {
            foreach (var bone in handSkeleton.Bones)
            {
                if (bone.Id == OVRSkeleton.BoneId.Hand_IndexTip)
                {
                    float fingerZ= bone.Transform.position.y;
                    desk.transform.localPosition = new Vector3(desk.transform.localPosition.x, fingerZ, desk.transform.localPosition.z);
                    desk.transform.Translate(new Vector3(0, -0.768f, 0));
                    boardAnchor.transform.position = new Vector3(boardAnchor.transform.position.x, desk.transform.position.y + 0.03f, boardAnchor.transform.position.z);
                }
            }
        }
    }

    public void TableLeveler() {
        isLeveling = true;
    }

    public void SetLevel() {
        isLeveling = false;
    }

}
