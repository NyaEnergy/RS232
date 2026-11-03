using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

public class KeyboardInputProbe : MonoBehaviour {
    private const uint INPUT_KEYBOARD = 1;
    private const uint KEYEVENTF_KEYUP = 0x0002;
    private const uint KEYEVENTF_UNICODE = 0x0004;
    private const ushort VK_RETURN = 0x0D;

    private bool isWaiting;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(
        uint count,
        [In] INPUT[] inputs,
        int size
    );

    [StructLayout(LayoutKind.Sequential)]
    private struct INPUT {
        public uint type;
        public INPUTUNION data;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct INPUTUNION {
        [FieldOffset(0)]
        public MOUSEINPUT mouse;

        [FieldOffset(0)]
        public KEYBDINPUT keyboard;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MOUSEINPUT {
        public int dx;
        public int dy;
        public uint mouseData;
        public uint flags;
        public uint time;
        public IntPtr extraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KEYBDINPUT {
        public ushort virtualKey;
        public ushort scanCode;
        public uint flags;
        public uint time;
        public IntPtr extraInfo;
    }

    private IEnumerator RunTest() {
        isWaiting = true;

        yield return new WaitForSecondsRealtime(5f);

        SendMeasurement("123.456");

        isWaiting = false;
    }

    public bool SendMeasurement(string value) {
        if (string.IsNullOrEmpty(value))
            return false;

        var events = new List<INPUT>();

        foreach (char character in value) {
            bool allowed =
                (character >= '0' && character <= '9') ||
                character == '.' ||
                character == ',';

            if (!allowed) {
                Debug.LogWarning(
                    "[Keyboard] Недопустимый символ: " + character
                );
                return false;
            }

            events.Add(CreateKey(
                0, character, KEYEVENTF_UNICODE));

            events.Add(CreateKey(
                0, character,
                KEYEVENTF_UNICODE | KEYEVENTF_KEYUP));
        }

        events.Add(CreateKey(VK_RETURN, 0, 0));
        events.Add(CreateKey(
            VK_RETURN, 0, KEYEVENTF_KEYUP));

        INPUT[] inputs = events.ToArray();

        uint sent = SendInput(
            (uint) inputs.Length,
            inputs,
            Marshal.SizeOf(typeof(INPUT))
        );

        if (sent != (uint) inputs.Length) {
            Debug.LogError(
                "[Keyboard] SendInput: " +
                sent + "/" + inputs.Length +
                ". Win32 Error: " +
                Marshal.GetLastWin32Error()
            );

            return false;
        }

        Debug.Log("[Keyboard] Значение отправлено: " + value);
        return true;
    }

    private static INPUT CreateKey(
        ushort virtualKey,
        ushort scanCode,
        uint flags) {
        return new INPUT {
            type = INPUT_KEYBOARD,
            data = new INPUTUNION {
                keyboard = new KEYBDINPUT {
                    virtualKey = virtualKey,
                    scanCode = scanCode,
                    flags = flags
                }
            }
        };
    }
}
