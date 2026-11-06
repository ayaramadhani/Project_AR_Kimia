using UnityEngine;

public class ModelRotator : MonoBehaviour
{
    public float speed = 30f;
    void Update() => transform.Rotate(0, speed * Time.deltaTime, 0, Space.World);
}
