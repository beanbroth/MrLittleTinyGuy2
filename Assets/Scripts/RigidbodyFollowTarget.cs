using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RigidbodyFollowTarget : MonoBehaviour
{
    Rigidbody _rb;
    [SerializeField] Transform _targ;

    void Start()
    {
        _rb = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        _rb.velocity = (_targ.position - transform.position) / Time.fixedDeltaTime;

        Quaternion rotationDifference = _targ.rotation * Quaternion.Inverse(transform.rotation);

        rotationDifference.ToAngleAxis(out float angleInDegree, out Vector3 rotationAxis);
        Vector3 rotationDifferenceInDegree = angleInDegree * rotationAxis;

        _rb.angularVelocity = (rotationDifferenceInDegree * Mathf.Deg2Rad / Time.fixedDeltaTime);

    }
}
