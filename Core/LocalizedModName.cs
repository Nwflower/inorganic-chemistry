using System;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

namespace ElementalIndustry;

/// <summary>
/// 让模组显示名跟随游戏语言。
///
/// 官方 manifest 的 <c>&lt;Name&gt;</c> 是纯字符串：加载器的 <c>ModLoader.GetModLabel</c> 原样返回它，
/// 而模组列表行、详情页标题、物品 tooltip 的来源行和顾客对话记录全部取自这个值，没有任何查表环节
/// （0.46D 反编译：<c>ModSourceHelper.GetItemModName</c> → <c>GetModDisplayName</c> → <c>GetModLabel</c>）。
/// 所以唯一不补丁 UI 的做法就是在语言就绪后改写自己 manifest 的 <c>Name</c>；这些读取点都是使用时
/// 现读，改完即生效，不需要主动刷新。
/// </summary>
internal static class LocalizedModName
{
    private const string Key = "mod.name";

    private static ModManifest? _manifest;
    private static bool _subscribed;

    /// <summary>入口的 OnEnable 调用：只登记，不碰 <see cref="LocalizationSettings"/>。</summary>
    internal static void Schedule(ModManifest manifest)
    {
        _manifest = manifest;
        DeferredPatchRunner.Schedule("ElementalIndustry.ModName", ApplyAndSubscribe);
    }

    /// <summary>入口的 OnDisable 调用：退订并丢弃 manifest 引用。</summary>
    internal static void Reset()
    {
        if (_subscribed)
        {
            try { LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged; } catch { }
            _subscribed = false;
        }
        _manifest = null;
    }

    /// <summary>在语言已经应用到 SelectedLocale 之后执行：先写一次，再跟着之后的切换写。</summary>
    private static void ApplyAndSubscribe()
    {
        Apply();
        if (_subscribed || _manifest == null)
        {
            return;
        }
        LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
        _subscribed = true;
    }

    private static void OnLocaleChanged(Locale locale)
    {
        Apply();
    }

    private static void Apply()
    {
        ModManifest? manifest = _manifest;
        if (manifest == null)
        {
            return;
        }

        string name;
        try
        {
            name = ModHelper.GetLocalized(Ids.ModId, Key);
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"[{Ids.ModId}] cannot read '{Key}' for the mod name: {exception.Message}");
            return;
        }

        // 加载器缺键时原样返回键名；此时保留 manifest 里的名字，不写成 "mod.name"。
        if (string.IsNullOrEmpty(name) || name == Key || manifest.Name == name)
        {
            return;
        }

        manifest.Name = name;
    }
}
