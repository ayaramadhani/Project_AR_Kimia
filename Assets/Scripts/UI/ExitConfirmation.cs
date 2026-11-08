
using UnityEngine;

public class ExitConfirmation : MonoBehaviour
{
    [Header("Popup Settings")]
    [SerializeField] private GameObject popupPanel;

    // Menampilkan popup konfirmasi
    public void ShowPopup()
    {
        popupPanel.SetActive(true);
    }

    // Menutup popup ketika pengguna memilih Back
    public void HidePopup()
    {
        popupPanel.SetActive(false);
    }

    // Keluar dari aplikasi
    public void ConfirmExit()
    {
        Debug.Log("Pengguna memilih Keluar.");

        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
