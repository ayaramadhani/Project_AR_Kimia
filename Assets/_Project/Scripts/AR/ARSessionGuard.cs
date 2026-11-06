using System.Collections;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

public class ARSessionGuard : MonoBehaviour
{
    public GameObject unsupportedPanel;

    IEnumerator Start()
    {
        yield return ARSession.CheckAvailability();

        if (ARSession.state == ARSessionState.Unsupported)
        {
            if (unsupportedPanel) unsupportedPanel.SetActive(true);
        }
        else if (ARSession.state == ARSessionState.NeedsInstall)
        {
            yield return ARSession.Install();
        }
    }
}
