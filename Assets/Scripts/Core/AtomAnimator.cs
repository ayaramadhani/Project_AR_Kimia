using UnityEngine;

public class AtomAnimator : MonoBehaviour
{
    [Header("Kecepatan Rotasi per Sumbu (Derajat/Detik)")]
    public Vector3 kecepatanPutar = new Vector3(0f, 120f, 0f);

    [Header("Centang untuk putaran lintasan cincin sendiri")]
    public bool gunakanSumbuLokal = true;

    void Update()
    {
        Space ruangPutar = gunakanSumbuLokal ? Space.Self : Space.World;
        transform.Rotate(kecepatanPutar * Time.deltaTime, ruangPutar);
    }
}
