
using System.Collections.Generic;
using UnityEngine;

public static class ProgressSaveManager
{
    // Kunci khusus untuk menyimpan progress aplikasi.
    // Jangan gunakan kunci ini untuk menyimpan volume audio.
    private const string ProgressKey = "AR_KIMIA_PROGRESS_V1";

    [System.Serializable]
    private class ProgressData
    {
        public List<string> discoveredElements = new List<string>();
        public List<string> discoveredCompounds = new List<string>();
        public List<string> history = new List<string>();
    }

    private static ProgressData data;

    // Memuat data progress dari PlayerPrefs.
    private static void LoadProgress()
    {
        if (data != null)
            return;

        string savedData = PlayerPrefs.GetString(ProgressKey, "");

        if (string.IsNullOrEmpty(savedData))
        {
            data = new ProgressData();
        }
        else
        {
            data = JsonUtility.FromJson<ProgressData>(savedData);

            if (data == null)
                data = new ProgressData();

            if (data.discoveredElements == null)
                data.discoveredElements = new List<string>();

            if (data.discoveredCompounds == null)
                data.discoveredCompounds = new List<string>();

            if (data.history == null)
                data.history = new List<string>();
        }
    }

    // Menyimpan data progress.
    private static void SaveProgress()
    {
        string json = JsonUtility.ToJson(data);
        PlayerPrefs.SetString(ProgressKey, json);
        PlayerPrefs.Save();
    }

    // Menandai unsur yang sudah ditemukan.
    public static void MarkElementDiscovered(string elementID)
    {
        LoadProgress();

        if (!string.IsNullOrEmpty(elementID) &&
            !data.discoveredElements.Contains(elementID))
        {
            data.discoveredElements.Add(elementID);
            SaveProgress();
        }
    }

    // Menandai senyawa yang sudah ditemukan.
    public static void MarkCompoundDiscovered(string compoundID)
    {
        LoadProgress();

        if (!string.IsNullOrEmpty(compoundID) &&
            !data.discoveredCompounds.Contains(compoundID))
        {
            data.discoveredCompounds.Add(compoundID);
            SaveProgress();
        }
    }

    // Menambahkan objek ke riwayat.
    public static void AddToHistory(string objectID)
    {
        LoadProgress();

        if (!string.IsNullOrEmpty(objectID))
        {
            data.history.Add(objectID);
            SaveProgress();
        }
    }

    // Mengambil daftar unsur yang sudah ditemukan.
    public static List<string> GetDiscoveredElements()
    {
        LoadProgress();
        return new List<string>(data.discoveredElements);
    }

    // Mengambil daftar senyawa yang sudah ditemukan.
    public static List<string> GetDiscoveredCompounds()
    {
        LoadProgress();
        return new List<string>(data.discoveredCompounds);
    }

    // Mengambil riwayat.
    public static List<string> GetHistory()
    {
        LoadProgress();
        return new List<string>(data.history);
    }

    // Menghapus progress aplikasi, tetapi tidak menghapus pengaturan audio.
    public static void ResetAllProgress()
    {
        PlayerPrefs.DeleteKey(ProgressKey);
        PlayerPrefs.Save();

        data = new ProgressData();
    }
}
