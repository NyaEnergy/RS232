using System;
using System.IO.Ports;
using UnityEngine;

public class ComPortProbe : MonoBehaviour {
    private void Start() {
        try {
            string[] ports = SerialPort.GetPortNames();

            Array.Sort(ports, StringComparer.OrdinalIgnoreCase);

            if (ports.Length == 0) {
                Debug.Log("[COM] Порты не обнаружены.");
                return;
            }

            Debug.Log("[COM] Найдены: " + string.Join(", ", ports));
        } catch (Exception exception) {
            Debug.LogError("[COM] Ошибка: " + exception.Message);
        }
    }
}
