namespace PunctuationToggle;

/// <summary>
/// 切り替えキーに使えないキーを押したときに、利用者に示すキーの名前。
/// </summary>
/// <remarks>
/// 文字キーなど、よく押されるキーだけを表にしている。表に無いキーは、アプリ本体で
/// System.Windows.Forms.Keys の名前を使う（TriggerKeyRecorderForm を参照）。
/// 仮想キーコードの値は WinUser.h の VK_* と同じ。
/// </remarks>
public static class VirtualKeyNames
{
    private const int Backspace = 0x08; // VK_BACK
    private const int Tab = 0x09; // VK_TAB
    private const int Enter = 0x0D; // VK_RETURN
    private const int CapsLock = 0x14; // VK_CAPITAL
    private const int Space = 0x20; // VK_SPACE
    private const int PageUp = 0x21; // VK_PRIOR
    private const int PageDown = 0x22; // VK_NEXT
    private const int End = 0x23; // VK_END
    private const int Home = 0x24; // VK_HOME
    private const int LeftArrow = 0x25; // VK_LEFT
    private const int UpArrow = 0x26; // VK_UP
    private const int RightArrow = 0x27; // VK_RIGHT
    private const int DownArrow = 0x28; // VK_DOWN
    private const int Insert = 0x2D; // VK_INSERT
    private const int Delete = 0x2E; // VK_DELETE
    private const int Digit0 = 0x30; // '0'（数字キーは文字コードと同じ値）
    private const int Digit9 = 0x39;
    private const int LetterA = 0x41; // 'A'（英字キーは大文字の文字コードと同じ値）
    private const int LetterZ = 0x5A;
    private const int NumPad0 = 0x60; // VK_NUMPAD0
    private const int NumPad9 = 0x69; // VK_NUMPAD9
    private const int NumLock = 0x90; // VK_NUMLOCK
    private const int HankakuZenkaku = 0xF3; // VK_OEM_AUTO（日本語キーボードの「半角/全角」）
    private const int HankakuZenkakuAlternate = 0xF4; // VK_OEM_ENLW（「半角/全角」は押すたびにこの2つが入れ替わる）

    private static readonly Dictionary<int, string> Names = new()
    {
        [Backspace] = "Backspace",
        [Tab] = "Tab",
        [Enter] = "Enter",
        [CapsLock] = "Caps Lock",
        [Space] = "スペース",
        [PageUp] = "Page Up",
        [PageDown] = "Page Down",
        [End] = "End",
        [Home] = "Home",
        [LeftArrow] = "←",
        [UpArrow] = "↑",
        [RightArrow] = "→",
        [DownArrow] = "↓",
        [Insert] = "Insert",
        [Delete] = "Delete",
        [NumLock] = "Num Lock",
        [HankakuZenkaku] = "半角/全角",
        [HankakuZenkakuAlternate] = "半角/全角",
    };

    /// <summary>表にあるキーの名前。表に無いキーは null。</summary>
    public static string? For(int virtualKey) => virtualKey switch
    {
        >= Digit0 and <= Digit9 or >= LetterA and <= LetterZ => ((char)virtualKey).ToString(),
        >= NumPad0 and <= NumPad9 => $"テンキーの {virtualKey - NumPad0}",
        _ => Names.GetValueOrDefault(virtualKey),
    };
}
