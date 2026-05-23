using System;
using System.IO;
using UnityEngine;

public class ExperimentLogger : MonoBehaviour
{
    [Header("CSV Settings")]
    [SerializeField] private string fileNamePrefix = "vr_archery_results";
    [SerializeField] private bool createNewFileOnStart = true;

    private string filePath;

    public string FilePath => filePath;

    private void Awake()
    {
        InitializeLogFile();
    }

    public void InitializeLogFile()
    {
        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");

        string fileName = createNewFileOnStart
            ? $"{fileNamePrefix}_{timestamp}.csv"
            : $"{fileNamePrefix}.csv";

        filePath = Path.Combine(Application.persistentDataPath, fileName);

        if (!File.Exists(filePath))
        {
            File.WriteAllText(filePath, ShotData.CsvHeader() + "\n");
        }

        Debug.Log($"[DEBUG] CSV path: {filePath}");
    }

    public void LogShot(ShotData data)
    {
        if (data == null)
        {
            Debug.LogWarning("[DEBUG] Tried to log null ShotData.");
            return;
        }

        if (string.IsNullOrEmpty(filePath))
        {
            InitializeLogFile();
        }

        File.AppendAllText(filePath, data.ToCsvLine() + "\n");

        Debug.Log(
            $"[DEBUG] Logged shot {data.shotNumber} | " +
            $"Mode={data.inputMode} | Hit={data.hit} | Score={data.score}"
        );
    }
}
