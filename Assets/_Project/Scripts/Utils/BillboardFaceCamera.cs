using UnityEngine;

public class BillboardFaceCamera : MonoBehaviour
{
    void LateUpdate()
    {
        if (Camera.main)
            transform.rotation = Quaternion.LookRotation(transform.position - Camera.main.transform.position);
    }
}
