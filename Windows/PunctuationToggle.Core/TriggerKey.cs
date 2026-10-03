namespace PunctuationToggle;

/// <summary>
/// 句読点を切り替えるキー。
/// </summary>
/// <remarks>
/// 修飾キー（Ctrl など）は「単独で押して離したとき」に切り替え、ショートカットとしての動作は残す。
/// それ以外のキー（F13 や「無変換」など）は、修飾キーなしで押されたときに切り替え、
/// そのキー本来の入力はほかのアプリに渡さない。
/// 文字の入力に使うキーを選ぶと文字が打てなくなるため、選べるキーは <see cref="SupportedKeys"/> に限っている。
/// </remarks>
public sealed record TriggerKey
{
    private TriggerKey(int virtualKey, string displayName, bool isModifier)
    {
        VirtualKey = virtualKey;
        DisplayName = displayName;
        IsModifier = isModifier;
    }

    /// <summary>Windows の仮想キーコード（VK_*）。左右の修飾キーは区別する（VK_RCONTROL など）。</summary>
    public int VirtualKey { get; }

    public string DisplayName { get; }

    public bool IsModifier { get; }

    /// <summary>メニューに表示する操作の説明。</summary>
    public string UsageDescription =>
        IsModifier ? $"{DisplayName}を単独で押すと切り替え" : $"{DisplayName}を押すと切り替え";

    public static TriggerKey RightControl { get; } = new(VirtualKeys.RightControl, "右Ctrl", isModifier: true);

    /// <summary>初期設定の切り替えキー。</summary>
    public static TriggerKey Default => RightControl;

    /// <summary>切り替えキーとして選べるキー（仮想キーコード → キー）。</summary>
    public static IReadOnlyDictionary<int, TriggerKey> SupportedKeys { get; } = CreateSupportedKeys();

    /// <summary>仮想キーコードから切り替えキーを求める。選べないキーの場合は null。</summary>
    public static TriggerKey? FromVirtualKey(int virtualKey) =>
        SupportedKeys.GetValueOrDefault(virtualKey);

    private static Dictionary<int, TriggerKey> CreateSupportedKeys()
    {
        var keys = new List<TriggerKey>
        {
            RightControl,
            new(VirtualKeys.LeftControl, "左Ctrl", isModifier: true),
            new(VirtualKeys.LeftShift, "左Shift", isModifier: true),
            new(VirtualKeys.RightShift, "右Shift", isModifier: true),
            new(VirtualKeys.LeftAlt, "左Alt", isModifier: true),
            new(VirtualKeys.RightAlt, "右Alt", isModifier: true),
            new(VirtualKeys.LeftWindows, "左Windows", isModifier: true),
            new(VirtualKeys.RightWindows, "右Windows", isModifier: true),
            new(VirtualKeys.Convert, "変換", isModifier: false),
            new(VirtualKeys.NonConvert, "無変換", isModifier: false),
            new(VirtualKeys.Applications, "アプリケーション", isModifier: false),
            new(VirtualKeys.Pause, "Pause", isModifier: false),
            new(VirtualKeys.ScrollLock, "Scroll Lock", isModifier: false),
        };
        for (var number = 1; number <= VirtualKeys.FunctionKeyCount; number++)
        {
            keys.Add(new(VirtualKeys.F1 + number - 1, $"F{number}", isModifier: false));
        }
        return keys.ToDictionary(key => key.VirtualKey);
    }
}

/// <summary>Windows の仮想キーコード。</summary>
public static class VirtualKeys
{
    public const int Pause = 0x13;
    public const int Convert = 0x1C;
    public const int NonConvert = 0x1D;
    public const int Escape = 0x1B;
    public const int LeftWindows = 0x5B;
    public const int RightWindows = 0x5C;
    public const int Applications = 0x5D;
    public const int F1 = 0x70;
    public const int FunctionKeyCount = 24;
    public const int ScrollLock = 0x91;
    public const int LeftShift = 0xA0;
    public const int RightShift = 0xA1;
    public const int LeftControl = 0xA2;
    public const int RightControl = 0xA3;
    public const int LeftAlt = 0xA4;
    public const int RightAlt = 0xA5;

    /// <summary>左右を区別した修飾キー（Shift・Ctrl・Alt・Windows）。</summary>
    public static IReadOnlyList<int> Modifiers { get; } =
    [
        LeftShift, RightShift, LeftControl, RightControl, LeftAlt, RightAlt, LeftWindows, RightWindows,
    ];

    public static bool IsModifier(int virtualKey) => Modifiers.Contains(virtualKey);
}
