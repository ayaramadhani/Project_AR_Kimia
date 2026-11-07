using UnityEngine;

namespace ARKimia.Gameplay
{
    public class ProceduralRotator : MonoBehaviour
    {
        [SerializeField] private Vector3 rotationAxis = Vector3.up;
        [SerializeField] private float rotationSpeed = 30f;
        [SerializeField] private bool useLocalRotation = true;

        public void Configure(Vector3 axis, float speed, bool local = true)
        {
            rotationAxis = axis;
            rotationSpeed = speed;
            useLocalRotation = local;
        }

        private void Update()
        {
            float step = rotationSpeed * Time.deltaTime;
            transform.Rotate(rotationAxis * step, useLocalRotation ? Space.Self : Space.World);
        }
    }
}
