using System;
using System.IO.Ports;
using UnityEngine;

public class SbiSimulator : MonoBehaviour {
    private SerialPort port;

    private void Start() {
        try {
            port = new SerialPort(
                "COM10",
                9600,
                Parity.None,
                8,
                StopBits.One
            );

            port.Handshake = Handshake.None;
            port.Open();

            Debug.Log("[Simulator] COM10 открыт.");
        } catch (Exception exception) {
            Debug.LogError(
                "[Simulator] " + exception.Message
            );
        }
    }

    private void Update() {
        if (port == null || !port.IsOpen)
            return;

        try {
            int available = port.BytesToRead;

            if (available == 0)
                return;

            byte[] buffer = new byte[available];
            int received = port.Read(
                buffer, 0, buffer.Length
            );

            if (received > 0) {
                Debug.Log(
                    "[Simulator] Получены байты: " +
                    BitConverter.ToString(
                        buffer, 0, received
                    )
                );
            }
        } catch (Exception exception) {
            Debug.LogError(
                "[Simulator] " + exception.Message
            );
        }
    }

    private void OnDestroy() {
        if (port != null) {
            if (port.IsOpen)
                port.Close();

            port.Dispose();
            port = null;
        }
    }

    [ContextMenu("Send Test Measurement")]
    private void SendTestMeasurement() {
        if (!Application.isPlaying)
            return;

        string message = "   +123.456 g\r\n";

        StartCoroutine(SendDelayed(message));
    }


    private System.Collections.IEnumerator SendDelayed(string message) {
        Debug.Log("[Simulator] Отправка через 5 секунд...");

        yield return new WaitForSecondsRealtime(5f);

        if (port == null || !port.IsOpen) {
            Debug.LogWarning("[Simulator] COM10 закрыт.");
            yield break;
        }

        try {
            port.Write(message);
            Debug.Log("[Simulator] Отправлено: " + message);
        } catch (System.Exception exception) {
            Debug.LogError("[Simulator] " + exception.Message);
        }
    }

}
