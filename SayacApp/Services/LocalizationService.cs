using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace SayacApp.Services;

/// <summary>
/// Tiny runtime-switchable localization layer. XAML binds to the indexer:
///   Text="{Binding [Add], Source={x:Static svc:LocalizationService.Instance}}"
/// On language change we raise PropertyChanged(null) which refreshes every binding,
/// including indexer bindings, so the whole UI re-localizes live.
/// </summary>
public sealed class LocalizationService : INotifyPropertyChanged
{
    public static LocalizationService Instance { get; } = new();

    public event PropertyChangedEventHandler? PropertyChanged;
    public event Action? LanguageChanged;

    private string _language = "tr";

    private LocalizationService() { }

    public string Language
    {
        get => _language;
        set
        {
            var lang = value == "en" ? "en" : "tr";
            if (_language == lang) return;
            _language = lang;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(null));
            LanguageChanged?.Invoke();
        }
    }

    public string this[string key]
    {
        get
        {
            var table = _language == "en" ? En : Tr;
            return table.TryGetValue(key, out var v) ? v : key;
        }
    }

    public string Get(string key) => this[key];

    private static readonly Dictionary<string, string> Tr = new()
    {
        ["AppTitle"] = "Soonish",
        ["MiniTitle"] = "Soonish Mini",
        ["ColName"] = "Sayaç adı",
        ["When"] = "Ne zaman",
        ["DateLabel"] = "Tarih",
        ["TimeLabel"] = "Saat",
        ["QuickAdjust"] = "Hızlı ayarla",
        ["Icon"] = "Simge",
        ["ColTarget"] = "Hedef tarih",
        ["ColTime"] = "Saat",
        ["Add"] = "Ekle",
        ["Settings"] = "Ayar",
        ["SettingsLong"] = "Ayarlar",
        ["Delete"] = "Sil",
        ["Close"] = "Kapat",
        ["NoCounters"] = "Henüz sayaç yok. İlk sayacı yukarıdan ekleyin.",
        ["Completed"] = "Tamamlandı",
        ["DayUnit"] = "gün",
        ["Target"] = "Hedef",
        ["NameEmpty"] = "Sayaç adı boş olamaz.",
        ["Warning"] = "Uyarı",
        ["CounterSettings"] = "Sayaç ayarları",
        ["PinToMini"] = "Miniye sabitle",
        ["ShowInMini"] = "Minide göster",
        ["BgColor"] = "Arka plan rengi",
        ["Transparent"] = "Transparan",
        ["TextColor"] = "Yazı rengi",
        ["FontSize"] = "Font boyutu",
        ["AutoBoxSize"] = "Otomatik kutu boyutu",
        ["BoxWidth"] = "Kutu genişliği",
        ["BoxHeight"] = "Kutu yüksekliği",
        ["CustomShortcuts"] = "Özel kısayollar",
        ["ToggleMini"] = "Miniyi aç/kapat",
        ["ToggleMain"] = "Ana paneli aç/kapat",
        ["Listen"] = "Dinle",
        ["MiniControls"] = "Mini kontroller",
        ["MiniLocked"] = "Mini kilitli",
        ["Show"] = "Göster",
        ["All"] = "Hepsi",
        ["Language"] = "Dil",
        ["Appearance"] = "Görünüm",
        ["Theme"] = "Tema",
        ["ThemeSystem"] = "Sistem",
        ["ThemeLight"] = "Açık",
        ["ThemeDark"] = "Koyu",
        ["MiniOpacity"] = "Mini saydamlık",
        ["HotkeyHint"] = "Bir Dinle düğmesine basın, sonra modifier + tuş kombinasyonunu girin.",
        ["PressCombo"] = "Kombinasyona basın...",
        ["NeedModifier"] = "En az bir modifier kullanın: Win, Ctrl, Alt veya Shift.",
        ["HotkeyInUse"] = "Bu kısayol başka bir işlem için kullanılıyor.",
        ["HotkeyBindFailed"] = "Kısayol kaydedilemedi, başka bir kombinasyon seçin.",
        ["TrayToggleMain"] = "Sayaçları Aç/Kapat",
        ["TrayToggleMini"] = "Miniyi Aç/Kapat",
        ["TrayToggleLock"] = "Mini Kilidi Aç/Kapat",
        ["TraySettings"] = "Ayarlar",
        ["TrayExit"] = "Çıkış",
    };

    private static readonly Dictionary<string, string> En = new()
    {
        ["AppTitle"] = "Soonish",
        ["MiniTitle"] = "Soonish Mini",
        ["ColName"] = "Counter name",
        ["When"] = "When",
        ["DateLabel"] = "Date",
        ["TimeLabel"] = "Time",
        ["QuickAdjust"] = "Quick adjust",
        ["Icon"] = "Icon",
        ["ColTarget"] = "Target date",
        ["ColTime"] = "Time",
        ["Add"] = "Add",
        ["Settings"] = "Settings",
        ["SettingsLong"] = "Settings",
        ["Delete"] = "Delete",
        ["Close"] = "Close",
        ["NoCounters"] = "No counters yet. Add your first one above.",
        ["Completed"] = "Completed",
        ["DayUnit"] = "d",
        ["Target"] = "Target",
        ["NameEmpty"] = "Counter name cannot be empty.",
        ["Warning"] = "Warning",
        ["CounterSettings"] = "Counter settings",
        ["PinToMini"] = "Pin to mini",
        ["ShowInMini"] = "Show in mini",
        ["BgColor"] = "Background color",
        ["Transparent"] = "Transparent",
        ["TextColor"] = "Text color",
        ["FontSize"] = "Font size",
        ["AutoBoxSize"] = "Auto box size",
        ["BoxWidth"] = "Box width",
        ["BoxHeight"] = "Box height",
        ["CustomShortcuts"] = "Custom shortcuts",
        ["ToggleMini"] = "Toggle mini",
        ["ToggleMain"] = "Toggle main panel",
        ["Listen"] = "Listen",
        ["MiniControls"] = "Mini controls",
        ["MiniLocked"] = "Mini locked",
        ["Show"] = "Show",
        ["All"] = "All",
        ["Language"] = "Language",
        ["Appearance"] = "Appearance",
        ["Theme"] = "Theme",
        ["ThemeSystem"] = "System",
        ["ThemeLight"] = "Light",
        ["ThemeDark"] = "Dark",
        ["MiniOpacity"] = "Mini opacity",
        ["HotkeyHint"] = "Press a Listen button, then enter a modifier + key combination.",
        ["PressCombo"] = "Press a combination...",
        ["NeedModifier"] = "Use at least one modifier: Win, Ctrl, Alt or Shift.",
        ["HotkeyInUse"] = "This shortcut is already used by another action.",
        ["HotkeyBindFailed"] = "Could not register the shortcut, pick another combination.",
        ["TrayToggleMain"] = "Show/Hide Counters",
        ["TrayToggleMini"] = "Show/Hide Mini",
        ["TrayToggleLock"] = "Lock/Unlock Mini",
        ["TraySettings"] = "Settings",
        ["TrayExit"] = "Exit",
    };
}
