using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InteractionTriggerDetector : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        if (other.GetComponentInParent<HandInput>() is var hi && hi)
        {
            hi.OnInteractionTriggerDetectorEnter(this);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.GetComponentInParent<HandInput>() is var hi && hi)
        {
            hi.OnInteractionTriggerDetectorExit(this);
        }
    }
}
