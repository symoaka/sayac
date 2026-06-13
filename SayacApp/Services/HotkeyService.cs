using System;
using SharpHook;
using SharpHook.Native;
using SayacApp.Models;

namespace SayacApp.Services;

/// <summary>
/// Global hotkeys via SharpHook (cross-platform). We don't pre-register combos; we
/// listen to all key presses and match against the configured hotkeys, which also
/// makes the "Listen" capture flow trivial. If the hook can't start (e.g. macOS
/// Accessibility permission not granted), hotkeys are simply inactive — never fatal.
/// Trigger events fire on the hook thread; subscribers must marshal to the UI thread.
/// </summary>
public sealed class HotkeyService : IDisposable
{
    private readonly AppSettings _settings;
    private IGlobalHook? _hook;
    private Action<HotkeyDefinition?>? _capture;

    public event Action? MiniTriggered;
    public event Action? MainTriggered;

    public bool Active => _hook is not null;

    public HotkeyService(AppSettings settings) => _settings = settings;

    public void Start()
    {
        try
        {
            _hook = new SimpleGlobalHook();
            _hook.KeyPressed += OnKeyPressed;
            _ = _hook.RunAsync();
        }
        catch
        {
            _hook = null;
        }
    }

    /// <summary>Matching is dynamic against live settings, so "applying" just reports
    /// whether the hook is running.</summary>
    public bool ApplyAll() => _hook is not null;

    public void BeginCapture(Action<HotkeyDefinition?> onCaptured) => _capture = onCaptured;

    private void OnKeyPressed(object? sender, KeyboardHookEventArgs e)
    {
        var code = e.Data.KeyCode;
        var mask = e.RawEvent.Mask;
        var ctrl = mask.HasFlag(ModifierMask.LeftCtrl) || mask.HasFlag(ModifierMask.RightCtrl);
        var alt = mask.HasFlag(ModifierMask.LeftAlt) || mask.HasFlag(ModifierMask.RightAlt);
        var shift = mask.HasFlag(ModifierMask.LeftShift) || mask.HasFlag(ModifierMask.RightShift);
        var meta = mask.HasFlag(ModifierMask.LeftMeta) || mask.HasFlag(ModifierMask.RightMeta);

        var capture = _capture;
        if (capture is not null)
        {
            if (IsModifierKey(code)) return;
            _capture = null;
            capture(new HotkeyDefinition
            {
                Ctrl = ctrl, Alt = alt, Shift = shift, Meta = meta, Key = code.ToString()
            });
            return;
        }

        if (Matches(_settings.MiniHotkey, code, ctrl, alt, shift, meta)) MiniTriggered?.Invoke();
        else if (Matches(_settings.MainHotkey, code, ctrl, alt, shift, meta)) MainTriggered?.Invoke();
    }

    private static bool Matches(HotkeyDefinition d, KeyCode code, bool ctrl, bool alt, bool shift, bool meta) =>
        d.HasKey && d.Ctrl == ctrl && d.Alt == alt && d.Shift == shift && d.Meta == meta &&
        Enum.TryParse<KeyCode>(d.Key, out var k) && k == code;

    private static bool IsModifierKey(KeyCode code) => code is
        KeyCode.VcLeftControl or KeyCode.VcRightControl or
        KeyCode.VcLeftShift or KeyCode.VcRightShift or
        KeyCode.VcLeftAlt or KeyCode.VcRightAlt or
        KeyCode.VcLeftMeta or KeyCode.VcRightMeta;

    public void Dispose()
    {
        try { _hook?.Dispose(); } catch { /* ignore */ }
        _hook = null;
    }
}
