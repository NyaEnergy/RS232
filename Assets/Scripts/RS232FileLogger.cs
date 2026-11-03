
using System;
using System.IO;
using System.Text;
using UnityEngine;

public class RS232FileLogger : MonoBehaviour {
    private StreamWriter writer;
    private string logPath;

    private void OnEnable() {
        string buildDirectory =
            Directory.GetParent(
                Application.dataPath
            ).FullName;

        string logsDirectory = Path.Combine(
            buildDirectory,
            "Logs"
        );

        try {
            OpenLog(logsDirectory);
        } catch (Exception exception) {
            Debug.LogWarning(
                "[Diagnostics] Не удалось создать лог " +
                "в папке программы: " +
                exception.Message
            );

            try {
                string fallback = Path.Combine(
                    Application.persistentDataPath,
                    "Logs"
                );

                OpenLog(fallback);
            } catch (Exception fallbackException) {
                Debug.LogError(
                    "[Diagnostics] Ошибка логирования: " +
                    fallbackException.Message
                );

                return;
            }
        }

        Application.logMessageReceived += OnLogMessage;

        Debug.Log(
            "[Diagnostics] Лог: " + logPath
        );
    }

    private void OpenLog(string directory) {
        Directory.CreateDirectory(directory);

        string filename =
            "RS232_" +
            DateTime.Now.ToString("yyyy-MM-dd") +
            ".log";

        string path = Path.Combine(
            directory,
            filename
        );

        writer = new StreamWriter(
            path,
            true,
            new UTF8Encoding(false)
        );

        writer.AutoFlush = true;
        logPath = path;
    }

    private void OnLogMessage(
        string message,
        string stackTrace,
        LogType type) {
        if (writer == null)
            return;

        bool relevant =
            message.StartsWith("[Receiver]") ||
            message.StartsWith("[Parser]") ||
            message.StartsWith("[Simulator]") ||
            message.StartsWith("[Connection]") ||
            message.StartsWith("[Settings]") ||
            message.StartsWith("[Diagnostics]") ||
            type == LogType.Error ||
            type == LogType.Exception ||
            type == LogType.Assert;

        if (!relevant)
            return;

        try {
            string timestamp =
                DateTime.Now.ToString("HH:mm:ss.fff");

            writer.WriteLine(
                $"[{timestamp}] [{type}] {message}"
            );

            if (
                type == LogType.Error ||
                type == LogType.Exception ||
                type == LogType.Assert) {
                if (!string.IsNullOrEmpty(stackTrace))
                    writer.WriteLine(stackTrace);
            }
        } catch (Exception) {
            Application.logMessageReceived -= OnLogMessage;

            try {
                writer?.Dispose();
            } catch (Exception) {
            }

            writer = null;
        }
    }

    private void OnDisable() {
        Application.logMessageReceived -= OnLogMessage;

        writer?.Dispose();
        writer = null;
    }
}
