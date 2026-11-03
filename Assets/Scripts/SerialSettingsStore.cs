using System;
using System.IO;
using System.Text;
using UnityEngine;

[Serializable]
public class SerialSettingsData {
    public string portName = "";
    public string baudRate = "9600";
    public string dataBits = "8";
    public string parity = "";
    public string stopBits = "1";
    public string flowControl = "";
    public bool dtr = false;
    public bool rts = false;
}

public static class SerialSettingsStore {
    private const string FileName = "serial-settings.json";

    public static string FilePath =>
        Path.Combine(Application.persistentDataPath, FileName);

    public static bool Save(SerialSettingsData settings) {
        if (settings == null) {
            Debug.LogError(
                "[Settings] Нечего сохранять: настройки отсутствуют."
            );
            return false;
        }

        try {
            Directory.CreateDirectory(
                Application.persistentDataPath
            );

            string json = JsonUtility.ToJson(
                settings,
                true
            );

            File.WriteAllText(
                FilePath,
                json,
                new UTF8Encoding(false)
            );

            Debug.Log("[Settings] Сохранено: " + FilePath);
            return true;
        } catch (Exception exception) {
            Debug.LogError(
                "[Settings] Ошибка сохранения: " +
                exception.Message
            );

            return false;
        }
    }

    public static SerialSettingsData Load() {
        if (!File.Exists(FilePath))
            return new SerialSettingsData();

        try {
            string json = File.ReadAllText(
                FilePath,
                Encoding.UTF8
            );

            SerialSettingsData settings =
                JsonUtility.FromJson<SerialSettingsData>(json);

            if (settings == null)
                throw new FormatException(
                    "Файл не содержит настроек."
                );

            return settings;
        } catch (Exception exception) {
            Debug.LogWarning(
                "[Settings] Не удалось загрузить настройки, " +
                "используются значения по умолчанию: " +
                exception.Message
            );

            return new SerialSettingsData();
        }
    }
}
