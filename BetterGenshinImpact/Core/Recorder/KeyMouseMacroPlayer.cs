﻿using BetterGenshinImpact.Core.Recorder.Model;
using BetterGenshinImpact.Core.Simulator;
using BetterGenshinImpact.Helpers;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using BetterGenshinImpact.GameTask;
using BetterGenshinImpact.GameTask.Common;
using Microsoft.Extensions.Logging;
using Vanara.PInvoke;

namespace BetterGenshinImpact.Core.Recorder;

public class KeyMouseMacroPlayer
{
    public static async Task PlayMacro(string macro, CancellationToken ct)
    {
        if (!TaskContext.Instance().IsInitialized)
        {
            MessageBox.Show("请先在启动页，启动截图器再使用本功能");
            return;
        }

        try
        {
            TaskTriggerDispatcher.Instance().StopTimer();

            var script = JsonSerializer.Deserialize<KeyMouseScript>(macro, KeyMouseRecorder.JsonOptions) ?? throw new Exception("Failed to deserialize macro");
            script.Adapt(TaskContext.Instance().SystemInfo.CaptureAreaRect);
            SystemControl.ActivateWindow();
            for (var i = 3; i >= 1; i--)
            {
                TaskControl.Logger.LogInformation("{Sec}秒后进行重放...", i);
                await Task.Delay(1000, ct);
            }

            TaskControl.Logger.LogInformation("开始重放");
            await PlayMacro(script.MacroEvents, ct);
        }
        finally
        {
            TaskTriggerDispatcher.Instance().StartTimer();
        }
    }

    public static async Task PlayMacro(List<MacroEvent> macroEvents, CancellationToken ct)
    {
        WorkingArea = PrimaryScreen.WorkingArea;
        foreach (var e in macroEvents)
        {
            await Task.Delay((int)Math.Round(e.Time), ct);
            switch (e.Type)
            {
                case MacroEventType.KeyDown:
                    Simulation.SendInput.Keyboard.KeyDown((User32.VK)e.KeyCode!);
                    break;

                case MacroEventType.KeyUp:
                    Simulation.SendInput.Keyboard.KeyUp((User32.VK)e.KeyCode!);
                    break;

                case MacroEventType.MouseDown:
                    var buttonMouseDown = Enum.Parse<MouseButtons>(e.MouseButton!);
                    var xMouseDown = ToVirtualDesktopX(e.MouseX);
                    var yMouseDown = ToVirtualDesktopY(e.MouseY);
                    switch (buttonMouseDown)
                    {
                        case MouseButtons.Left:
                            Simulation.SendInput.Mouse.MoveMouseTo(xMouseDown, yMouseDown).LeftButtonDown();
                            break;

                        case MouseButtons.Right:
                            Simulation.SendInput.Mouse.MoveMouseTo(xMouseDown, yMouseDown).RightButtonDown();
                            break;

                        case MouseButtons.Middle:
                            Simulation.SendInput.Mouse.MoveMouseTo(xMouseDown, yMouseDown).MiddleButtonDown();
                            break;

                        case MouseButtons.None:
                            break;

                        case MouseButtons.XButton1:
                            break;

                        case MouseButtons.XButton2:
                            break;

                        default:
                            throw new ArgumentOutOfRangeException();
                    }

                    break;

                case MacroEventType.MouseUp:
                    var buttonMouseUp = Enum.Parse<MouseButtons>(e.MouseButton!);
                    var xMouseUp = ToVirtualDesktopX(e.MouseX);
                    var yMouseUp = ToVirtualDesktopY(e.MouseY);
                    switch (buttonMouseUp)
                    {
                        case MouseButtons.Left:
                            Simulation.SendInput.Mouse.MoveMouseTo(xMouseUp, yMouseUp).LeftButtonUp();
                            break;

                        case MouseButtons.Right:
                            Simulation.SendInput.Mouse.MoveMouseTo(xMouseUp, yMouseUp).RightButtonUp();
                            break;

                        case MouseButtons.Middle:
                            Simulation.SendInput.Mouse.MoveMouseTo(xMouseUp, yMouseUp).MiddleButtonUp();
                            break;

                        case MouseButtons.None:
                            break;

                        case MouseButtons.XButton1:
                            break;

                        case MouseButtons.XButton2:
                            break;

                        default:
                            throw new ArgumentOutOfRangeException();
                    }

                    break;

                case MacroEventType.MouseMoveTo:
                    Simulation.SendInput.Mouse.MoveMouseTo(ToVirtualDesktopX(e.MouseX), ToVirtualDesktopY(e.MouseY));
                    break;

                case MacroEventType.MouseMoveBy:
                    Simulation.SendInput.Mouse.MoveMouseBy(e.MouseX, e.MouseY);
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    public static Size WorkingArea;

    public static double ToVirtualDesktopX(int x)
    {
        return x * 65535 * 1d / WorkingArea.Width;
    }

    public static double ToVirtualDesktopY(int y)
    {
        return y * 65535 * 1d / WorkingArea.Height;
    }
}
