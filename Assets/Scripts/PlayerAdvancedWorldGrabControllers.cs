using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerAdvancedWorldGrabControllers : MonoBehaviour
{
    [SerializeField] InputActionProperty _leftHandGrab;
    [SerializeField] Transform _leftHandPos;
    Vector3 _leftLastHandPos;

    [SerializeField] InputActionProperty _rightHandGrab;
    [SerializeField] Transform _rightHandPos;
    Vector3 _rightLastHandPos;

    [SerializeField] Transform _head;

    float _lastYRot;

    // Update is called once per frame
    void Update()
    {
        Vector3 leftOnlyY = new Vector3(_leftHandPos.localPosition.x, 0, _leftHandPos.localPosition.z);
        Vector3 rightOnlyY = new Vector3(_rightHandPos.localPosition.x, 0, _rightHandPos.localPosition.z);

        Vector3 dif = leftOnlyY - rightOnlyY;

        if (_rightHandGrab.action.WasPressedThisFrame())
        {
            _rightLastHandPos = _rightHandPos.transform.position;
            _lastYRot = Vector3.SignedAngle(dif, Vector3.forward, Vector3.up);
        }
        if (_leftHandGrab.action.WasPressedThisFrame())
        {
            _leftLastHandPos = _leftHandPos.transform.position;
            _lastYRot = Vector3.SignedAngle(dif, Vector3.forward, Vector3.up);
        }


        if (!(_leftHandGrab.action.IsPressed() || _rightHandGrab.action.IsPressed()))
        {
            return;
        }

        if (_leftHandGrab.action.IsPressed() && _rightHandGrab.action.IsPressed())
        {

            Vector3 mid = Vector3.Lerp(_leftHandPos.position, _rightHandPos.position, .5f / Vector3.Distance(_leftHandPos.position, _rightHandPos.position));

            transform.position -= ((_leftHandPos.transform.position - _leftLastHandPos) + (_rightHandPos.transform.position - _rightLastHandPos));
           _rightLastHandPos = _rightHandPos.transform.position;
            _leftLastHandPos = _leftHandPos.transform.position;

            transform.RotateAround(mid, Vector3.up, Vector3.SignedAngle(dif, Vector3.forward, Vector3.up) -_lastYRot);
            _lastYRot = Vector3.SignedAngle(dif, Vector3.forward, Vector3.up);


        }
        else if (_leftHandGrab.action.IsPressed() && !_rightHandGrab.action.IsPressed())
        {
            Debug.Log("Pressed");
            transform.position -= (_leftHandPos.transform.position - _leftLastHandPos);
            _leftLastHandPos = _leftHandPos.transform.position;
        }
        else if (_rightHandGrab.action.IsPressed() && !_leftHandGrab.action.IsPressed())
        {
            Debug.Log("Pressed");
            transform.position -= (_rightHandPos.transform.position - _rightLastHandPos);
            _rightLastHandPos = _rightHandPos.transform.position;
        }



    }
}
