using System.Collections.Generic;
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

    private DropdownField baudRateField;
    private DropdownField dataBitsField;
    private DropdownField parityField;
    private DropdownField flowControlField;

    private bool dtrEnabled;
    private bool rtsEnabled;

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
            root.Q<DropdownField>("baudrate-input");

        dataBitsField =
            root.Q<DropdownField>("databits-input");

        parityField =
            root.Q<DropdownField>("parity-input");

        flowControlField =
            root.Q<DropdownField>("flowcontrol-input");

        if (connectionButton == null ||
            portField == null ||
            baudRateField == null ||
            dataBitsField == null ||
            parityField == null ||
            flowControlField == null) {
            Debug.LogError(
                "[Connection] Не найдены элементы интерфейса."
            );
            return;
        }

        baudRateField.choices = new List<string> {
            "1200", "2400", "4800", "9600", "19200"
        };
        baudRateField.SetValueWithoutNotify("9600");

        dataBitsField.choices = new List<string> {
            "7", "8"
        };
        dataBitsField.SetValueWithoutNotify("8");

        parityField.choices = new List<string> {
            "None", "Odd", "Even"
        };
        parityField.SetValueWithoutNotify("Odd");

        flowControlField.choices = new List<string> {
            "None", "XON/XOFF"
        };
        flowControlField.SetValueWithoutNotify("None");

        // Значения сигналов по умолчанию.
        dtrEnabled = false;
        rtsEnabled = false;

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

        if (!TryParseParity(parityField.value, out Parity parity)) {
            Debug.LogWarning(
                "[Connection] Parity: None, Odd или Even."
            );
            return;
        }

        if (!TryParseHandshake(
            flowControlField.value,
            out Handshake handshake)) {
            Debug.LogWarning(
                "[Connection] Flow Control: допустимы " +
                "None или XON/XOFF."
            );
            return;
        }

        bool connected = receiver.Connect(
            portField.value,
            baud,
            parity,
            dataBits,
            StopBits.One,
            handshake,
            dtrEnabled,
            rtsEnabled
        );

        if (connected) {
            SaveCurrentSettings();
        }
    }

    private static string NormalizeSavedParity(string value) {
        string text = (value ?? "")
            .Trim()
            .ToUpperInvariant();

        switch (text) {
            case "":
            case "NONE":
                return "None";

            case "ODD":
                return "Odd";

            case "EVEN":
                return "Even";

            default:
                return "Odd";
        }
    }

    private static string NormalizeSavedFlowControl(
        string value) {
        string text = (value ?? "")
            .Trim()
            .ToUpperInvariant()
            .Replace(" ", "");

        switch (text) {
            case "XON/XOFF":
                return "XON/XOFF";

            case "":
            case "NONE":
            default:
                return "None";
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

            default:
                handshake = Handshake.None;
                return false;
        }
    }

    private void RestoreSavedSettings() {
        if (!System.IO.File.Exists(SerialSettingsStore.FilePath))
            return;

        SerialSettingsData saved = SerialSettingsStore.Load();

        string savedBaudRate = saved.baudRate?.Trim();
        baudRateField.SetValueWithoutNotify(
            baudRateField.choices.Contains(savedBaudRate)
                ? savedBaudRate
                : "9600"
        );

        string savedDataBits = saved.dataBits?.Trim();
        dataBitsField.SetValueWithoutNotify(
            dataBitsField.choices.Contains(savedDataBits)
                ? savedDataBits
                : "8"
        );

        parityField.SetValueWithoutNotify(
            NormalizeSavedParity(saved.parity)
        );

        flowControlField.SetValueWithoutNotify(
            NormalizeSavedFlowControl(saved.flowControl)
        );

        // Сохраняем значения сигналов из старого JSON.
        dtrEnabled = saved.dtr;
        rtsEnabled = saved.rts;

        // Выбор порта после обновления списка COM-портов.
        DropdownField dropdown = portField;

        dropdown.schedule.Execute(() => {
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
    }

    private void DetachAutoSave() {
        if (autosaveRoot == null)
            return;

        autosaveRoot.UnregisterCallback<ChangeEvent<string>>(
            OnTextSettingChanged
        );

        autosaveRoot = null;
    }

    private void OnTextSettingChanged(ChangeEvent<string> evt) {
        if (!ReferenceEquals(evt.target, portField) &&
            !ReferenceEquals(evt.target, baudRateField) &&
            !ReferenceEquals(evt.target, dataBitsField) &&
            !ReferenceEquals(evt.target, parityField) &&
            !ReferenceEquals(evt.target, flowControlField)) {
            return;
        }

        SaveCurrentSettings();
    }

    private void SaveCurrentSettings() {
        if (portField == null ||
            baudRateField == null ||
            dataBitsField == null ||
            parityField == null ||
            flowControlField == null) {
            return;
        }

        SerialSettingsStore.Save(new SerialSettingsData {
            portName = portField.value,
            baudRate = baudRateField.value,
            dataBits = dataBitsField.value,
            parity = parityField.value,
            stopBits = "1",
            flowControl = flowControlField.value,
            dtr = dtrEnabled,
            rts = rtsEnabled
        });
    }
}
