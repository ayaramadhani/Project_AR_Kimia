using TMPro;
using UnityEngine;

public class ResultPopupUI : MonoBehaviour
{
    public GameObject panel;
    public TMP_Text formulaText, nameText, descText;
    public Transform spawnPoint;      // objek kosong ~0.5 m di depan kamera AR

    GameObject currentModel;

    public void Show(CompoundData compound)
    {
        panel.SetActive(true);
        if (currentModel) Destroy(currentModel);

        if (compound == null)
        {
            formulaText.text = "-";
            nameText.text = "Kombinasi tidak valid";
            descText.text = "Coba ubah jumlah atau jenis unsur.";
            return;
        }

        formulaText.text = compound.formula;
        nameText.text = compound.displayName;
        descText.text = compound.description;

        if (compound.modelPrefab)
            currentModel = Instantiate(compound.modelPrefab,
                spawnPoint.position, spawnPoint.rotation, spawnPoint);
    }

    public void Close()
    {
        panel.SetActive(false);
        if (currentModel) Destroy(currentModel);
    }
}
