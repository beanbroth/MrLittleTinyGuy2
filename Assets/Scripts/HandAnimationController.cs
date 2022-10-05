using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class HandAnimationController : MonoBehaviour
{
    [SerializeField] InputActionProperty grabAnimationAction;

    [SerializeField] Animator handAnimator;
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        handAnimator.SetFloat("Grip", grabAnimationAction.action.ReadValue<float>());
    }
}
