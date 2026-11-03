using System;
using System.IO.Ports;
using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(PanelRenderer))]
public class SerialConnectionControls : MonoBehaviour {
    [SerializeField]
    private ComPortReceiverProbe receiver;

    private PanelRenderer panelRenderer;

    private Button connectionButton;
    private DropdownField portField;

    private TextField baudRateField;
    private TextField dataBitsField;
    private TextField parityField;
    private TextField stopBitsField;
    private TextField flowControlField;

    private Toggle dtrToggle;
    private Toggle rtsToggle;

    private VisualElement autosaveRoot;

    private void OnEnable() {
        panelRenderer = GetComponent<PanelRenderer>();

        panelRenderer.RegisterUIReloadCallback(OnUIReload);
    }

    private void OnDisable() {
        if (panelRenderer != null)
            panelRenderer.UnregisterUIReloadCallback(OnUIReload);

        if (connectionButton != null)
            connectionButton.clicked -= OnConnectionClicked;

        DetachAutoSave();
    }

    private void OnUIReload(
        PanelRenderer renderer,
        VisualElement root,
        int version) {
        if (connectionButton != null)
            connectionButton.clicked -= OnConnectionClicked;

        connectionButton =
            root.Q<Button>("connection-button");

        portField =
            root.Q<DropdownField>("port-dropdown");

        baudRateField =
            root.Q<TextField>("baudrate-input");

        dataBitsField =
            root.Q<TextField>("databits-input");

        parityField =
            root.Q<TextField>("parity-input");

        stopBitsField =
            root.Q<TextField>("stopbits-input");

        flowControlField =
            root.Q<TextField>("flowcontrol-input");

        dtrToggle =
            root.Q<Toggle>("dtr-toggle");

        rtsToggle =
            root.Q<Toggle>("rts-toggle");

        if (connectionButton == null ||
            portField == null ||
            baudRateField == null ||
            dataBitsField == null ||
            parityField == null ||
            stopBitsField == null ||
            flowControlField == null ||
            dtrToggle == null ||
            rtsToggle == null) {
            Debug.LogError(
                "[Connection] Не найдены элементы интерфейса."
            );
            return;
        }

        connectionButton.clicked += OnConnectionClicked;

        RestoreSavedSettings();

        AttachAutoSave(root);
    }

    private void OnConnectionClicked() {
        if (receiver == null) {
            Debug.LogError(
                "[Connection] Receiver не назначен."
            );
            return;
        }

        if (receiver.IsConnected) {
            receiver.Disconnect();
            return;
        }

        if (string.IsNullOrWhiteSpace(portField.value) ||
            portField.value == "Нет доступных портов") {
            Debug.LogWarning(
                "[Connection] Выбери COM-порт."
            );
            return;
        }

        if (!int.TryParse(baudRateField.value, out int baud) ||
            baud <= 0) {
            Debug.LogWarning(
                "[Connection] Baud Rate должен быть больше нуля."
            );
            return;
        }

        if (!int.TryParse(dataBitsField.value, out int dataBits) ||
            (dataBits != 7 && dataBits != 8)) {
            Debug.LogWarning(
                "[Connection] Data Bits: допустимы 7 или 8."
            );
            return;
        }

        StopBits stopBits;

        switch (stopBitsField.value?.Trim()) {
            case "1":
                stopBits = StopBits.One;
                break;

            case "2":
                stopBits = StopBits.Two;
                break;

            default:
                Debug.LogWarning(
                    "[Connection] Stop Bits: допустимы 1 или 2."
                );
                return;
        }

        if (!TryParseParity(parityField.value, out Parity parity)) {
            Debug.LogWarning(
                "[Connection] Parity: пусто, Odd или Even."
            );
            return;
        }

        if (!TryParseHandshake(
            flowControlField.value,
            out Handshake handshake)) {
            Debug.LogWarning(
                "[Connection] Flow Control: пусто, " +
                "XON/XOFF, RTS/CTS или RTS/CTS + XON/XOFF."
            );
            return;
        }

        bool connected = receiver.Connect(
            portField.value,
            baud,
            parity,
            dataBits,
            stopBits,
            handshake,
            dtrToggle.value,
            rtsToggle.value
        );

        if (connected) {
            SerialSettingsStore.Save(new SerialSettingsData {
                portName = portField.value,
                baudRate = baudRateField.value,
                dataBits = dataBitsField.value,
                parity = parityField.value,
                stopBits = stopBitsField.value,
                flowControl = flowControlField.value,
                dtr = dtrToggle.value,
                rts = rtsToggle.value
            });
        }
    }

    private static bool TryParseParity(
        string value,
        out Parity parity) {
        string text = (value ?? "")
            .Trim()
            .ToUpperInvariant();

        switch (text) {
            case "":
            case "NONE":
                parity = Parity.None;
                return true;

            case "ODD":
                parity = Parity.Odd;
                return true;

            case "EVEN":
                parity = Parity.Even;
                return true;

            default:
                parity = Parity.None;
                return false;
        }
    }

    private static bool TryParseHandshake(
        string value,
        out Handshake handshake) {
        string text = (value ?? "")
            .Trim()
            .ToUpperInvariant()
            .Replace(" ", "");

        switch (text) {
            case "":
            case "NONE":
                handshake = Handshake.None;
                return true;

            case "XON/XOFF":
                handshake = Handshake.XOnXOff;
                return true;

            case "RTS/CTS":
                handshake = Handshake.RequestToSend;
                return true;

            case "RTS/CTS+XON/XOFF":
                handshake = Handshake.RequestToSendXOnXOff;
                return true;

            default:
                handshake = Handshake.None;
                return false;
        }
    }

    private void RestoreSavedSettings() {
        if (!System.IO.File.Exists(SerialSettingsStore.FilePath))
            return;

        SerialSettingsData saved = SerialSettingsStore.Load();

        baudRateField.SetValueWithoutNotify(saved.baudRate ?? "9600");
        dataBitsField.SetValueWithoutNotify(saved.dataBits ?? "8");
        parityField.SetValueWithoutNotify(saved.parity ?? "");
        stopBitsField.SetValueWithoutNotify(saved.stopBits ?? "1");
        flowControlField.SetValueWithoutNotify(saved.flowControl ?? "");

        dtrToggle.SetValueWithoutNotify(saved.dtr);
        rtsToggle.SetValueWithoutNotify(saved.rts);

        // Выбор порта выполняем после обновления
        // списка доступных COM-портов.
        DropdownField dropdown = portField;

        dropdown.schedule.Execute(() =>
        {
            if (dropdown.panel == null)
                return;

            if (!string.IsNullOrEmpty(saved.portName) &&
                dropdown.choices != null &&
                dropdown.choices.Contains(saved.portName)) {
                dropdown.SetValueWithoutNotify(saved.portName);
            }
        }).ExecuteLater(0);

        Debug.Log("[Settings] Настройки загружены.");
    }

    private void AttachAutoSave(VisualElement root) {
        DetachAutoSave();

        autosaveRoot = root;

        autosaveRoot.RegisterCallback<ChangeEvent<string>>(
            OnTextSettingChanged
        );

        autosaveRoot.RegisterCallback<ChangeEvent<bool>>(
            OnToggleSettingChanged
        );
    }

    private void DetachAutoSave() {
        if (autosaveRoot == null)
            return;

        autosaveRoot.UnregisterCallback<ChangeEvent<string>>(
            OnTextSettingChanged
        );

        autosaveRoot.UnregisterCallback<ChangeEvent<bool>>(
            OnToggleSettingChanged
        );

        autosaveRoot = null;
    }

    private void OnTextSettingChanged(ChangeEvent<string> evt) {
        if (!ReferenceEquals(evt.target, portField) &&
            !ReferenceEquals(evt.target, baudRateField) &&
            !ReferenceEquals(evt.target, dataBitsField) &&
            !ReferenceEquals(evt.target, parityField) &&
            !ReferenceEquals(evt.target, stopBitsField) &&
            !ReferenceEquals(evt.target, flowControlField)) {
            return;
        }

        SaveCurrentSettings();
    }

    private void OnToggleSettingChanged(ChangeEvent<bool> evt) {
        if (!ReferenceEquals(evt.target, dtrToggle) &&
            !ReferenceEquals(evt.target, rtsToggle)) {
            return;
        }

        SaveCurrentSettings();
    }

    private void SaveCurrentSettings() {
        if (portField == null ||
            baudRateField == null ||
            dataBitsField == null ||
            parityField == null ||
            stopBitsField == null ||
            flowControlField == null ||
            dtrToggle == null ||
            rtsToggle == null) {
            return;
        }

        SerialSettingsStore.Save(new SerialSettingsData {
            portName = portField.value,
            baudRate = baudRateField.value,
            dataBits = dataBitsField.value,
            parity = parityField.value,
            stopBits = stopBitsField.value,
            flowControl = flowControlField.value,
            dtr = dtrToggle.value,
            rts = rtsToggle.value
        });
    }
}

