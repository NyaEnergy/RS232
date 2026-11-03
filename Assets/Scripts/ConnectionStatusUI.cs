
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

    // Фон кнопки всегда как у левой панели.
    private static readonly Color32 ButtonBackground =
        new Color32(24, 29, 40, 255);

    private static readonly Color32 DisconnectedOrange =
        new Color32(250, 145, 77, 255);

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

        RefreshPortList();
        RefreshStatus();
        RefreshReadings();
        RefreshConnectionButton();
    }

    private void Update() {
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

        string caption = connected
            ? "Отключить"
            : "Подключить";

        if (connectionButton.text != caption)
            connectionButton.text = caption;

        // Заливка совпадает с фоном левой панели.
        // Неоновую рамку задаёт RS232Style.uss.
        connectionButton.style.backgroundColor =
            new StyleColor((Color) ButtonBackground);

        connectionButton.style.color =
            new StyleColor(Color.white);
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
        if (portDropdown == null) {
            Debug.LogError(
                "[UI] port-dropdown не найден."
            );
            return;
        }

        string[] ports = SerialPort.GetPortNames();

        Array.Sort(
            ports,
            StringComparer.OrdinalIgnoreCase
        );

        if (ports.Length == 0) {
            portDropdown.choices = new List<string>
            {
                "Нет доступных портов"
            };

            portDropdown.SetValueWithoutNotify(
                "Нет доступных портов"
            );

            portDropdown.SetEnabled(false);
            return;
        }

        string selected = receiver != null
            ? receiver.ConnectedPortName
            : string.Empty;

        if (string.IsNullOrEmpty(selected))
            selected = portDropdown.value;

        if (Array.IndexOf(ports, selected) < 0)
            selected = ports[0];

        portDropdown.choices = new List<string>(ports);

        portDropdown.SetEnabled(true);
        portDropdown.SetValueWithoutNotify(selected);
    }

    // Числовые поля: только цифры.

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

        string value = evt.newValue ?? string.Empty;

        var clean = new StringBuilder(value.Length);

        foreach (char c in value) {
            if (c >= '0' && c <= '9')
                clean.Append(c);
        }

        string filtered = clean.ToString();

        if (value != filtered)
            field.SetValueWithoutNotify(filtered);
    }

    // Курсор в конце текста при получении фокуса.

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

    private void OnTextFieldFocusIn(FocusInEvent evt) {
        TextField field = evt.currentTarget as TextField;

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
