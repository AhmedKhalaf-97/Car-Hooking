using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HookDetector : MonoBehaviour
{
    public GrapplingHook grapplingHook;

    void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Hookable")
        {
            grapplingHook.hooked = true;
            grapplingHook.SetHookedObj(other.gameObject);
        }

        if (other.tag == "Ground")
        {
            grapplingHook.ReturnHook();
        }
    }
}
