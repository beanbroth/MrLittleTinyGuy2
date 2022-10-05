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
        _rb.maxAngularVelocity = 20f;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        _rb.velocity = (_targ.position - transform.position) / Time.fixedDeltaTime;

        Quaternion rotationDifference = _targ.rotation * Quaternion.Inverse(transform.rotation);

        //rotationDifference.ToAngleAxis(out float angleInDegree, out Vector3 rotationAxis);


        Vector3 rotationDifferenceInDegree = ClampEulerAngelsToClosest(new Vector3(rotationDifference.eulerAngles.x, rotationDifference.eulerAngles.y, rotationDifference.eulerAngles.z));

        _rb.angularVelocity = (rotationDifferenceInDegree * Mathf.Deg2Rad / Time.fixedDeltaTime);

    }
    private Vector3 ClampEulerAngelsToClosest(Vector3 rotation)
    {
        if (rotation.x > 180)
        {
            rotation.x = rotation.x - 360;
        }
        if (rotation.y > 180)
        {
            rotation.y = rotation.y - 360;
        }
        if (rotation.z > 180)
        {
            rotation.z = rotation.z - 360;
        }
        return rotation;
    }
}
