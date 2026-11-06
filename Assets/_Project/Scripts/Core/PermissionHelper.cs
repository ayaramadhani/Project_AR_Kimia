using UnityEngine;
#if UNITY_ANDROID
using UnityEngine.Android;
#endif

public class PermissionHelper : MonoBehaviour
{
    void Start()
    {
#if UNITY_ANDROID
        if (!Permission.HasUserAuthorizedPermission(Permission.Camera))
            Permission.RequestUserPermission(Permission.Camera);
#endif
    }
}
