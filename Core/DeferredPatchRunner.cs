using System;
using UnityEngine;
using UnityEngine.Localization.Settings;

namespace ElementalIndustry;

/// <summary>
/// 把「会 JIT 到本地化静态构造」的补丁推迟到游戏语言真正应用之后再装配。
///
/// 背景见 docs/experience/harmony-patch-jit-triggers-localization-ctor.md：加载器在
/// <c>ModLoader.Awake</c> 阶段就调用 <c>Harmony.PatchAll</c>，而 Mono 在 JIT 目标方法时会执行
/// 它引用到的类型的静态构造。<c>WaterHelper.HandleFilter</c> 的方法体直接访问
/// <c>Liquid.Liquids</c>，此刻 locale 还是默认的 en，`Liquid` 的液体显示名就会被固化成英文，
/// 并随存档保存——没有异常、没有日志，只能从存档里的英文字符串看出来。
/// </summary>
internal sealed class DeferredPatchRunner : MonoBehaviour
{
    /// <summary>等待上限：超过后无论如何都装配一次，避免某个异常版本把补丁永远挂不上。</summary>
    private const float MaxWaitSeconds = 30f;

    private Action? _pending;
    private string _label = "";
    private float _started;

    internal static void Schedule(string label, Action action)
    {
        try
        {
            var host = new GameObject("elemental_industry_deferred_patch");
            UnityEngine.Object.DontDestroyOnLoad(host);
            var runner = host.AddComponent<DeferredPatchRunner>();
            runner._pending = action;
            runner._label = label;
            runner._started = Time.realtimeSinceStartup;
        }
        catch (Exception exception)
        {
            // 兜底：延迟器建不起来时只能立即装配，至少不让功能消失。
            Debug.LogError($"[{Ids.ModId}] deferred patch host failed ({label}): {exception.Message}");
            try { action(); } catch (Exception inner) { Debug.LogError($"[{Ids.ModId}] deferred patch fallback failed: {inner.Message}"); }
        }
    }

    internal static void Cancel()
    {
        try
        {
            foreach (DeferredPatchRunner runner in Resources.FindObjectsOfTypeAll<DeferredPatchRunner>())
            {
                if (runner == null) continue;
                runner._pending = null;
                Destroy(runner.gameObject);
            }
        }
        catch { }
    }

    private void Update()
    {
        Action? action = _pending;
        if (action == null) return;

        bool ready;
        try { ready = LocaleReady(); }
        catch { ready = false; }

        if (!ready && Time.realtimeSinceStartup - _started < MaxWaitSeconds) return;

        _pending = null;
        try
        {
            action();
        }
        catch (Exception exception)
        {
            Debug.LogError($"[{Ids.ModId}] deferred patch '{_label}' failed: {exception.Message}");
        }
        finally
        {
            try { Destroy(gameObject); } catch { }
        }
    }

    /// <summary>游戏是否已经把存档 locale 应用到 SelectedLocale。</summary>
    private static bool LocaleReady()
    {
        // 先确认初始化完成：此时读 SelectedLocale 不会触发 WaitForCompletion。
        if (!LocalizationSettings.InitializationOperation.IsDone) return false;

        string current = LocHelper.GetCurrentLocaleCode();
        if (string.IsNullOrEmpty(current)) return false;

        string target = LocHelper.ResolveStartupLocale();
        if (string.IsNullOrEmpty(target)) return true;

        string a = Normalize(current);
        string b = Normalize(target);
        return a == b || a.StartsWith(b, StringComparison.Ordinal) || b.StartsWith(a, StringComparison.Ordinal);
    }

    private static string Normalize(string code) => (code ?? "").Trim().ToLowerInvariant();
}
