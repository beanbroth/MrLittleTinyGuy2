using System.Collections;
using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;

public class CapsuleHeadFollow : MonoBehaviour
{


    [SerializeField] XROrigin _XROrigin;
    [SerializeField] CapsuleCollider _cap;


    // Start is called before the first frame update
    void Start()
    {
        
    }



    // Update is called once per frame
    void FixedUpdate()
    {
        var height = _XROrigin.CameraInOriginSpaceHeight;

        Vector3 center = _XROrigin.CameraInOriginSpacePos;
        center.y = height / 2f;

        _cap.height = height;
        _cap.center = center;

    }


}
