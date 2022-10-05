using System.Collections;
using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;

//this script could use "rigidbody FOllow Target" but it's just this one case so it's not too important
public class LittleGuyArmsController : MonoBehaviour
{
    [SerializeField] XROrigin xrr;
    [SerializeField] Transform _targ;


    void Start()
    { 
    }

    // Update is called once per frame
    void LateUpdate()

    {
        transform.localPosition = _targ.localPosition - xrr.CameraInOriginSpacePos;
        transform.localRotation = _targ.localRotation;

    }
}
