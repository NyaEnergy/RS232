using System;
using System.IO.Ports;
using UnityEngine;

public class ComPortReceiverProbe : MonoBehaviour {
    private readonly System.Text.StringBuilder messageBuffer = new();

    private KeyboardInputProbe keyboardInput;
    private SerialPort port;
    private bool pendingCR;
    private float nextPortCheckTime;

    [SerializeField]
    private bool enableKeyboardOutput = false;

    public bool IsConnected =>
        port != null && port.IsOpen;

    public string ConnectedPortName =>
        IsConnected ? port.PortName : string.Empty;

    public string Reading1 { get; private set; } = "—";
    public string Reading2 { get; private set; } = "—";
    public string Reading3 { get; private set; } = "—";

    private void Awake() {
        keyboardInput = GetComponent<KeyboardInputProbe>();
    }

    // Подключение COM-порта
    public bool Connect(
        string portName,
        int baudRate,
        Parity parity,
        int dataBits,
        StopBits stopBits,
        Handshake handshake,
        bool dtr,
        bool rts) {
        if (IsConnected) {
            Debug.LogWarning(
                "[Receiver] Порт уже подключён."
            );
            return false;
        }

        // Освобождаем старый объект порта, если он остался
        // после неожиданного закрытия.
        if (port != null)
            Disconnect();

        SerialPort newPort = null;

        try {
            newPort = new SerialPort(
                portName,
                baudRate,
                parity,
                dataBits,
                stopBits
            );

            newPort.Handshake = handshake;
            newPort.DtrEnable = dtr;

            bool hardwareHandshake =
                handshake == Handshake.RequestToSend ||
                handshake == Handshake.RequestToSendXOnXOff;

            // При RTS/CTS линией RTS управляет драйвер.
            if (!hardwareHandshake)
                newPort.RtsEnable = rts;

            newPort.Open();

            port = newPort;

            messageBuffer.Clear();
            pendingCR = false;
            nextPortCheckTime = 0f;

            Debug.Log(
                "[Receiver] Подключено: " +
                port.PortName +
                ", " + baudRate +
                ", " + dataBits +
                ", " + parity +
                ", " + stopBits +
                ", " + handshake
            );

            string rtsState =
                port.Handshake == Handshake.RequestToSend ||
                port.Handshake == Handshake.RequestToSendXOnXOff
                    ? "Auto (RTS/CTS)"
                    : port.RtsEnable.ToString();

            Debug.Log(
                "[Receiver] Фактические параметры: " +
                $"Port={port.PortName}, " +
                $"Baud={port.BaudRate}, " +
                $"DataBits={port.DataBits}, " +
                $"Parity={port.Parity}, " +
                $"StopBits={port.StopBits}, " +
                $"Handshake={port.Handshake}, " +
                $"DTR={port.DtrEnable}, " +
                $"RTS={rtsState}"
            );

            return true;
        } catch (Exception exception) {
            Debug.LogError(
                "[Receiver] Ошибка подключения: " +
                exception.Message
            );

            port = null;

            if (newPort != null) {
                try {
                    newPort.Dispose();
                } catch (Exception disposeException) {
                    Debug.LogWarning(
                        "[Receiver] Ошибка освобождения порта: " +
                        disposeException.Message
                    );
                }
            }

            return false;
        }
    }

    // Штатное и аварийное отключение
    public void Disconnect() {
        SerialPort oldPort = port;
        port = null;

        messageBuffer.Clear();
        pendingCR = false;
        nextPortCheckTime = 0f;

        if (oldPort == null)
            return;

        try {
            if (oldPort.IsOpen)
                oldPort.Close();
        } catch (Exception exception) {
            Debug.LogWarning(
                "[Receiver] Ошибка закрытия порта: " +
                exception.Message
            );
        }

        // Dispose вызывается независимо от результата Close.
        try {
            oldPort.Dispose();
        } catch (Exception exception) {
            Debug.LogWarning(
                "[Receiver] Ошибка освобождения порта: " +
                exception.Message
            );
        }

        Debug.Log("[Receiver] Порт отключён.");
    }

    // Обработка неожиданной ошибки соединения
    private void HandlePortFailure(
        string operation,
        Exception exception) {
        Debug.LogError(
            "[Receiver] Ошибка " + operation + ": " +
            exception.Message
        );

        Disconnect();
    }

    private void Update() {
        CheckPortAvailability();

        if (port == null)
            return;

        string data;

        // Проверяем отдельно только чтение COM-порта.
        // Ошибки других компонентов не должны
        // ошибочно считаться обрывом соединения.
        try {
            if (!port.IsOpen) {
                Debug.LogWarning(
                    "[Receiver] COM-порт неожиданно закрылся."
                );

                Disconnect();
                return;
            }

            data = port.ReadExisting();
        } catch (Exception exception) {
            HandlePortFailure("чтения", exception);
            return;
        }

        // Обработка полученных символов
        foreach (char character in data) {
            if (pendingCR) {
                pendingCR = false;

                if (character == '\n') {
                    string message = messageBuffer.ToString();
                    messageBuffer.Clear();

                    Debug.Log(
                        "[Receiver] Полная строка: " + message
                    );

                    if (TryExtractNumber(
                        message,
                        out string number)) {
                        Debug.Log(
                            "[Parser] Значение: " + number
                        );

                        Reading3 = Reading2;
                        Reading2 = Reading1;
                        Reading1 = number;

                        if (enableKeyboardOutput) {
                            if (keyboardInput == null) {
                                Debug.LogError(
                                    "[Receiver] KeyboardInputProbe не найден."
                                );
                            } else {
                                keyboardInput.SendMeasurement(number);
                            }
                        }
                    } else {
                        Debug.LogWarning(
                            "[Parser] Число не найдено."
                        );
                    }

                    continue;
                }

                messageBuffer.Append('\r');
            }

            if (character == '\r') {
                pendingCR = true;
            } else {
                messageBuffer.Append(character);
            }

            if (messageBuffer.Length > 256) {
                messageBuffer.Clear();
                pendingCR = false;

                Debug.LogWarning(
                    "[Receiver] Превышен размер буфера."
                );
            }
        }
    }

    // Команда запроса показаний
    [ContextMenu("Send ESC P CR LF")]
    private void SendSbiRequest() {
        if (!IsConnected) {
            Debug.LogWarning(
                "[Receiver] COM-порт закрыт."
            );
            return;
        }

        try {
            byte[] command =
            {
                0x1B,
                0x50,
                0x0D,
                0x0A
            };

            port.Write(
                command,
                0,
                command.Length
            );

            Debug.Log(
                "[Receiver] Команда отправлена: " +
                BitConverter.ToString(command)
            );
        } catch (Exception exception) {
            HandlePortFailure("записи", exception);
        }
    }

    // Извлечение последнего числа из строки
    private static bool TryExtractNumber(
        string message,
        out string number) {
        number = null;

        var current = new System.Text.StringBuilder();

        bool hasDigit = false;
        bool hasSeparator = false;

        foreach (char c in message) {
            if (c >= '0' && c <= '9') {
                current.Append(c);
                hasDigit = true;
            } else if (
                  (c == '.' || c == ',') &&
                  hasDigit &&
                  !hasSeparator) {
                current.Append(c);
                hasSeparator = true;
            } else {
                if (
                    hasDigit &&
                    char.IsDigit(
                        current[current.Length - 1])) {
                    number = current.ToString();
                }

                current.Clear();
                hasDigit = false;
                hasSeparator = false;
            }
        }

        if (
            hasDigit &&
            char.IsDigit(current[current.Length - 1])) {
            number = current.ToString();
        }

        return number != null;
    }

    // Проверка наличия COM-порта в Windows
    private void CheckPortAvailability() {
        if (port == null)
            return;

        if (Time.unscaledTime < nextPortCheckTime)
            return;

        nextPortCheckTime = Time.unscaledTime + 1f;

        try {
            if (!port.IsOpen) {
                Debug.LogWarning(
                    "[Receiver] COM-порт неожиданно закрылся."
                );

                Disconnect();
                return;
            }

            string connectedPort = port.PortName;

            string[] availablePorts =
                SerialPort.GetPortNames();

            bool found = Array.Exists(
                availablePorts,
                name => string.Equals(
                    name,
                    connectedPort,
                    StringComparison.OrdinalIgnoreCase
                )
            );

            if (!found) {
                Debug.LogWarning(
                    "[Receiver] COM-порт исчез из Windows: " +
                    connectedPort
                );

                Disconnect();
            }
        } catch (Exception exception) {
            Debug.LogWarning(
                "[Receiver] Не удалось проверить COM-порт: " +
                exception.Message
            );
        }
    }

    private void OnDestroy() {
        Disconnect();
    }
}
