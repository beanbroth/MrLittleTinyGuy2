using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

public class HandPresence : MonoBehaviour
{
    public InputDeviceCharacteristics controllerCharacteristics;
    private InputDevice targetDevice;
    public Animator handAnimator;

    Collider[] _handColliders;
    public void EnableHandColliders()
    {
        foreach (Collider item in _handColliders)
        {
            item.enabled = true;
        }
    }
    public void DisableHandColliders()
    {
        foreach (Collider item in _handColliders)
        {
            item.enabled = false;
        }
    }

    void Start()
    {
        _handColliders = GetComponentsInChildren<Collider>();
        TryInitialize();
    }

    void TryInitialize()
    {
        List<InputDevice> devices = new List<InputDevice>();

        InputDevices.GetDevicesWithCharacteristics(controllerCharacteristics, devices);
        if (devices.Count > 0)
        {
            targetDevice = devices[0];
        }
    }

    void UpdateHandAnimation()
    {
        if (targetDevice.TryGetFeatureValue(CommonUsages.trigger, out float triggerValue))
        {
            handAnimator.SetFloat("Grip", triggerValue);
        }
        else
        {
            handAnimator.SetFloat("Grip", 0);

        }

        if (targetDevice.TryGetFeatureValue(CommonUsages.grip, out float gripValue))
        {
            handAnimator.SetFloat("Trigger", gripValue);
        }
        else
        {
            handAnimator.SetFloat("Trigger", 0);
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (!targetDevice.isValid)
        {
            TryInitialize();
        }
        else
        {
            UpdateHandAnimation();
        }
    }
}
