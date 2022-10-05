using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerWorldGrabController : MonoBehaviour
{
    [SerializeField] InputActionProperty _grabHand;
    [SerializeField] Transform _handPos;
    Vector3 lastHandPos;

    // Start is called before the first frame update
    void Start()
    {



    }

    // Update is called once per frame
    void Update()
    {
        if (_grabHand.action.WasPressedThisFrame())
            lastHandPos = _handPos.transform.localPosition;

        if (_grabHand.action.IsPressed())
        {
            Debug.Log("Pressed");
            transform.position -= (_handPos.transform.localPosition - lastHandPos)  * transform.localScale.x;
            lastHandPos = _handPos.transform.localPosition;
        }
    }
}
