﻿using BetterGenshinImpact.Core.Monitor;
using BetterGenshinImpact.GameTask;
using BetterGenshinImpact.GameTask.Common;
using BetterGenshinImpact.GameTask.Common.Element.Assets;
using BetterGenshinImpact.Model;
using Gma.System.MouseKeyHook;
using Microsoft.Extensions.Logging;
using SharpDX.DirectInput;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BetterGenshinImpact.Core.Recorder;

public class GlobalKeyMouseRecord : Singleton<GlobalKeyMouseRecord>
{
    private readonly ILogger<GlobalKeyMouseRecord> _logger = App.GetLogger<GlobalKeyMouseRecord>();

    private KeyMouseRecorder? _recorder;

    private readonly Dictionary<Keys, bool> _keyDownState = new();

    private DirectInputMonitor? _directInputMonitor;

    private readonly System.Timers.Timer _timer = new();

    private bool _isInMainUi = false; // 是否在主界面

    public GlobalKeyMouseRecord()
    {
        _timer.Elapsed += Tick;
        _timer.Interval = 50; // ms
    }

    public async Task StartRecord()
    {
        if (!TaskContext.Instance().IsInitialized)
        {
            MessageBox.Show("请先在启动页，启动截图器再使用本功能");
            return;
        }

        TaskTriggerDispatcher.Instance().StopTimer();

        _logger.LogInformation("录制：{Text}", "实时任务已暂停");
        _logger.LogInformation("注意：录制时遇到主界面（鼠标永远在界面中心）和其他界面（鼠标可自由移动，比如地图等）的切换，请把手离开鼠标等待录制模式切换日志");

        SystemControl.ActivateWindow();
        for (var i = 3; i >= 1; i--)
        {
            _logger.LogInformation("{Sec}秒后启动录制...", i);
            await Task.Delay(1000);
        }

        _timer.Start();

        _recorder = new KeyMouseRecorder();
        _directInputMonitor = new DirectInputMonitor();
        _directInputMonitor.Start();

        _logger.LogInformation("录制：{Text}", "已启动");
    }

    public string StopRecord()
    {
        var macro = _recorder?.ToJsonMacro() ?? string.Empty;
        _recorder = null;
        _directInputMonitor?.Stop();
        _directInputMonitor?.Dispose();
        _directInputMonitor = null;

        _timer.Stop();

        _logger.LogInformation("录制：{Text}", "结束录制");

        TaskTriggerDispatcher.Instance().StartTimer();
        return macro;
    }

    public void Tick(object? sender, EventArgs e)
    {
        var ra = TaskControl.CaptureToRectArea();
        var iconRa = ra.Find(ElementAssets.Instance.FriendChat);
        var exist = iconRa.IsExist();
        if (exist != _isInMainUi)
        {
            _logger.LogInformation("录制：{Text}", exist ? "进入主界面，捕获鼠标相对移动" : "离开主界面，捕获鼠标绝对移动");
        }
        _isInMainUi = exist;
        iconRa.Dispose();
        ra.Dispose();
    }

    public void GlobalHookKeyDown(KeyEventArgs e)
    {
        // 排除热键
        if (e.KeyCode.ToString() == TaskContext.Instance().Config.HotKeyConfig.KeyMouseMacroRecordHotkey)
        {
            return;
        }

        if (_keyDownState.TryGetValue(e.KeyCode, out var v))
        {
            if (v)
            {
                return; // 处于按下状态的不再记录
            }
            else
            {
                _keyDownState[e.KeyCode] = true;
            }
        }
        else
        {
            _keyDownState.Add(e.KeyCode, true);
        }
        // Debug.WriteLine($"KeyDown: {e.KeyCode}");
        _recorder?.KeyDown(e);
    }

    public void GlobalHookKeyUp(KeyEventArgs e)
    {
        if (e.KeyCode.ToString() == TaskContext.Instance().Config.HotKeyConfig.Test1Hotkey)
        {
            return;
        }

        if (_keyDownState.ContainsKey(e.KeyCode) && _keyDownState[e.KeyCode])
        {
            // Debug.WriteLine($"KeyUp: {e.KeyCode}");
            _keyDownState[e.KeyCode] = false;
            _recorder?.KeyUp(e);
        }
    }

    public void GlobalHookMouseDown(MouseEventExtArgs e)
    {
        // Debug.WriteLine($"MouseDown: {e.Button}");
        _recorder?.MouseDown(e);
    }

    public void GlobalHookMouseUp(MouseEventExtArgs e)
    {
        // Debug.WriteLine($"MouseUp: {e.Button}");
        _recorder?.MouseUp(e);
    }

    public void GlobalHookMouseMoveTo(MouseEventExtArgs e)
    {
        if (_isInMainUi)
        {
            return;
        }
        // Debug.WriteLine($"MouseMove: {e.X}, {e.Y}");
        _recorder?.MouseMoveTo(e);
    }

    public void GlobalHookMouseMoveBy(MouseState state)
    {
        if (state is { X: 0, Y: 0 } || !_isInMainUi)
        {
            return;
        }
        // Debug.WriteLine($"MouseMoveBy: {state.X}, {state.Y}");
        _recorder?.MouseMoveBy(state);
    }
}
