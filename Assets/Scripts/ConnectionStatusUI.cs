using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Text;
using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(PanelRenderer))]
public class ConnectionStatusUI : MonoBehaviour {
    [SerializeField]
    private ComPortReceiverProbe receiver;

    private PanelRenderer panelRenderer;

    private DropdownField portDropdown;
    private DropdownField baudRateDropdown;
    private DropdownField dataBitsDropdown;
    private DropdownField parityDropdown;
    private DropdownField flowControlDropdown;
    private Button connectionButton;

    private Label statusLabel;
    private Label reading1;
    private Label reading2;
    private Label reading3;

    private TextField baudRateInput;
    private TextField dataBitsInput;
    private TextField parityInput;
    private TextField stopBitsInput;
    private TextField flowControlInput;

    private static readonly Color32 AccentGreen =
        new Color32(57, 255, 143, 255);

    private static readonly Color32 DisconnectedOrange =
        new Color32(250, 145, 77, 255);

    private const float PortRefreshInterval = 1f;
    private const string NoPortsText = "Нет доступных портов";

    private const string ButtonUnavailableClass =
        "rs232-connect-button--unavailable";

    private const string ButtonReadyClass =
        "rs232-connect-button--ready";

    private const string ButtonConnectedClass =
        "rs232-connect-button--connected";

    private enum ConnectionButtonState {
        Unknown,
        Unavailable,
        Ready,
        Connected
    }

    private readonly List<string> availablePorts =
        new List<string>();

    private ConnectionButtonState buttonState =
        ConnectionButtonState.Unknown;

    private float nextPortRefreshTime;
    private bool? portSettingsAvailable;

    private void OnEnable() {
        panelRenderer = GetComponent<PanelRenderer>();
        panelRenderer.RegisterUIReloadCallback(OnUIReload);
    }

    private void OnDisable() {
        if (panelRenderer != null) {
            panelRenderer.UnregisterUIReloadCallback(
                OnUIReload
            );
        }

        UnregisterNumberFields();
        UnregisterCaretFields();

        statusLabel = null;
        connectionButton = null;
    }

    private void OnUIReload(
        PanelRenderer renderer,
        VisualElement root,
        int version) {
        UnregisterNumberFields();
        UnregisterCaretFields();

        statusLabel =
            root.Q<Label>("connection-status");

        reading1 =
            root.Q<Label>("reading-1");

        reading2 =
            root.Q<Label>("reading-2");

        reading3 =
            root.Q<Label>("reading-3");

        portDropdown =
            root.Q<DropdownField>("port-dropdown");

        baudRateDropdown =
            root.Q<DropdownField>("baudrate-input");

        dataBitsDropdown =
            root.Q<DropdownField>("databits-input");

        parityDropdown =
            root.Q<DropdownField>("parity-input");

        flowControlDropdown =
            root.Q<DropdownField>("flowcontrol-input");

        connectionButton =
            root.Q<Button>("connection-button");

        baudRateInput =
            root.Q<TextField>("baudrate-input");

        dataBitsInput =
            root.Q<TextField>("databits-input");

        parityInput =
            root.Q<TextField>("parity-input");

        stopBitsInput =
            root.Q<TextField>("stopbits-input");

        flowControlInput =
            root.Q<TextField>("flowcontrol-input");

        RegisterNumberField(baudRateInput);
        RegisterNumberField(dataBitsInput);
        RegisterNumberField(stopBitsInput);

        RegisterCaretField(baudRateInput);
        RegisterCaretField(dataBitsInput);
        RegisterCaretField(parityInput);
        RegisterCaretField(stopBitsInput);
        RegisterCaretField(flowControlInput);

        availablePorts.Clear();
        buttonState = ConnectionButtonState.Unknown;
        portSettingsAvailable = null;

        RefreshPortList();

        nextPortRefreshTime =
            Time.unscaledTime + PortRefreshInterval;

        RefreshStatus();
        RefreshReadings();
        RefreshConnectionButton();
    }

    private void Update() {
        if (portDropdown != null &&
            Time.unscaledTime >= nextPortRefreshTime) {

            nextPortRefreshTime =
                Time.unscaledTime + PortRefreshInterval;

            RefreshPortList();
        }

        RefreshStatus();
        RefreshReadings();
        RefreshConnectionButton();
    }

    private void RefreshStatus() {
        if (statusLabel == null)
            return;

        bool connected =
            receiver != null && receiver.IsConnected;

        string status = connected
            ? "Подключено: " + receiver.ConnectedPortName
            : "Нет подключения";

        if (statusLabel.text != status)
            statusLabel.text = status;

        statusLabel.style.color = new StyleColor(
            connected
                ? (Color) AccentGreen
                : (Color) DisconnectedOrange
        );
    }

    private void RefreshConnectionButton() {
        if (connectionButton == null)
            return;

        bool connected =
            receiver != null && receiver.IsConnected;

        bool canConnect =
            !connected &&
            receiver != null &&
            HasSelectedPort();

        ConnectionButtonState state = connected
            ? ConnectionButtonState.Connected
            : canConnect
                ? ConnectionButtonState.Ready
                : ConnectionButtonState.Unavailable;

        string caption = connected
            ? "Отключить"
            : "Подключить";

        if (connectionButton.text != caption)
            connectionButton.text = caption;

        if (buttonState != state) {
            connectionButton.EnableInClassList(
                ButtonUnavailableClass,
                state == ConnectionButtonState.Unavailable
            );

            connectionButton.EnableInClassList(
                ButtonReadyClass,
                state == ConnectionButtonState.Ready
            );

            connectionButton.EnableInClassList(
                ButtonConnectedClass,
                state == ConnectionButtonState.Connected
            );

            buttonState = state;
        }

        bool shouldEnable =
            state != ConnectionButtonState.Unavailable;

        if (connectionButton.enabledSelf != shouldEnable) {
            connectionButton.SetEnabled(shouldEnable);
        }
    }

    private bool HasSelectedPort() {
        if (portDropdown == null ||
            !portDropdown.enabledInHierarchy) {
            return false;
        }

        string selectedPort = portDropdown.value;

        if (string.IsNullOrWhiteSpace(selectedPort))
            return false;

        foreach (string port in availablePorts) {
            if (string.Equals(
                port,
                selectedPort,
                StringComparison.OrdinalIgnoreCase)) {
                return true;
            }
        }

        return false;
    }

    private void RefreshReadings() {
        if (receiver == null ||
            reading1 == null ||
            reading2 == null ||
            reading3 == null) {
            return;
        }

        if (reading1.text != receiver.Reading1)
            reading1.text = receiver.Reading1;

        if (reading2.text != receiver.Reading2)
            reading2.text = receiver.Reading2;

        if (reading3.text != receiver.Reading3)
            reading3.text = receiver.Reading3;
    }

    private void RefreshPortList() {
        if (portDropdown == null)
            return;

        string[] ports;

        try {
            ports = SerialPort.GetPortNames();
        } catch (Exception exception) {
            Debug.LogWarning(
                "[UI] Не удалось получить список COM-портов: " +
                exception.Message
            );

            ports = Array.Empty<string>();
        }

        Array.Sort(
            ports,
            StringComparer.OrdinalIgnoreCase
        );

        // Блокируем настройки, если COM-портов нет.
        // Сами выбранные значения и JSON не изменяем.
        RefreshPortSettingsAvailability(
            ports.Length > 0
        );

        if (ports.Length == 0) {
            availablePorts.Clear();

            bool placeholderMissing =
                portDropdown.choices == null ||
                portDropdown.choices.Count != 1 ||
                portDropdown.choices[0] != NoPortsText;

            if (placeholderMissing) {
                portDropdown.choices =
                    new List<string> { NoPortsText };
            }

            if (portDropdown.value != NoPortsText) {
                portDropdown.SetValueWithoutNotify(
                    NoPortsText
                );
            }

            if (portDropdown.enabledSelf) {
                portDropdown.SetEnabled(false);
            }

            return;
        }

        bool changed =
            ports.Length != availablePorts.Count ||
            !portDropdown.enabledSelf;

        if (!changed) {
            for (int i = 0; i < ports.Length; i++) {
                if (!string.Equals(
                    ports[i],
                    availablePorts[i],
                    StringComparison.OrdinalIgnoreCase)) {

                    changed = true;
                    break;
                }
            }
        }

        if (!changed)
            return;

        availablePorts.Clear();
        availablePorts.AddRange(ports);

        string selected =
            receiver != null && receiver.IsConnected
                ? receiver.ConnectedPortName
                : portDropdown.value;

        if (Array.FindIndex(
            ports,
            port => string.Equals(
                port,
                selected,
                StringComparison.OrdinalIgnoreCase
            )
        ) < 0) {
            selected = ports[0];
        }

        portDropdown.choices =
            new List<string>(ports);

        portDropdown.SetEnabled(true);
        portDropdown.SetValueWithoutNotify(selected);
    }

    private void RefreshPortSettingsAvailability(
        bool hasPorts) {

        // Если портов нет, поддерживаем блокировку.
        // Если они доступны, обновляем состояние
        // только при переходе из недоступного режима.
        if (hasPorts && portSettingsAvailable == true)
            return;

        SetSettingDropdownAvailability(
            baudRateDropdown,
            hasPorts
        );

        SetSettingDropdownAvailability(
            dataBitsDropdown,
            hasPorts
        );

        SetSettingDropdownAvailability(
            parityDropdown,
            hasPorts
        );

        SetSettingDropdownAvailability(
            flowControlDropdown,
            hasPorts
        );

        portSettingsAvailable = hasPorts;
    }

    private static void SetSettingDropdownAvailability(
        DropdownField field,
        bool hasPorts) {

        if (field == null)
            return;

        // Полностью запрещаем раскрытие списка.
        if (field.enabledSelf != hasPorts) {
            field.SetEnabled(hasPorts);
        }

        // Скрываем только отображение выбранного значения.
        // Значения DropdownField и Choices сохраняются.
        VisualElement valueText =
            field.Q<VisualElement>(
                className: "unity-base-popup-field__text"
            );

        if (valueText != null) {
            if (hasPorts) {
                valueText.style.visibility =
                    StyleKeyword.Null;
            } else {
                valueText.style.visibility =
                    Visibility.Hidden;
            }
        }
    }

    private void RegisterNumberField(TextField field) {
        if (field == null)
            return;

        field.RegisterValueChangedCallback(
            OnNumericValueChanged
        );
    }

    private void UnregisterNumberFields() {
        if (baudRateInput != null) {
            baudRateInput.UnregisterValueChangedCallback(
                OnNumericValueChanged
            );
        }

        if (dataBitsInput != null) {
            dataBitsInput.UnregisterValueChangedCallback(
                OnNumericValueChanged
            );
        }

        if (stopBitsInput != null) {
            stopBitsInput.UnregisterValueChangedCallback(
                OnNumericValueChanged
            );
        }
    }

    private void OnNumericValueChanged(
        ChangeEvent<string> evt) {

        TextField field = evt.target as TextField;

        if (field == null)
            return;

        string value =
            evt.newValue ?? string.Empty;

        var clean =
            new StringBuilder(value.Length);

        foreach (char c in value) {
            if (c >= '0' && c <= '9') {
                clean.Append(c);
            }
        }

        string filtered = clean.ToString();

        if (value != filtered) {
            field.SetValueWithoutNotify(filtered);
        }
    }

    private void RegisterCaretField(TextField field) {
        if (field == null)
            return;

        field.selectAllOnFocus = false;
        field.selectAllOnMouseUp = false;

        field.RegisterCallback<FocusInEvent>(
            OnTextFieldFocusIn
        );
    }

    private void UnregisterCaretFields() {
        UnregisterCaretField(baudRateInput);
        UnregisterCaretField(dataBitsInput);
        UnregisterCaretField(parityInput);
        UnregisterCaretField(stopBitsInput);
        UnregisterCaretField(flowControlInput);
    }

    private void UnregisterCaretField(TextField field) {
        if (field == null)
            return;

        field.UnregisterCallback<FocusInEvent>(
            OnTextFieldFocusIn
        );
    }

    private void OnTextFieldFocusIn(
        FocusInEvent evt) {

        TextField field =
            evt.currentTarget as TextField;

        if (field == null)
            return;

        field.schedule.Execute(() => {
            if (field.panel == null)
                return;

            int end = field.value != null
                ? field.value.Length
                : 0;

            field.SelectRange(end, end);

        }).ExecuteLater(1);
    }
}
