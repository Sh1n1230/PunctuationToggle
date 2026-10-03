namespace PunctuationToggle;

/// <summary>日本語入力の「句読点」設定。</summary>
public enum PunctuationStyle
{
    /// <summary>「、。」</summary>
    ToutenKuten,

    /// <summary>「，。」</summary>
    CommaKuten,

    /// <summary>「、．」</summary>
    ToutenPeriod,

    /// <summary>「，．」</summary>
    CommaPeriod,
}

public static class PunctuationStyleExtensions
{
    /// <summary>タスクトレイなどに表示する、句読点の2文字。</summary>
    public static string Symbols(this PunctuationStyle style) => style switch
    {
        PunctuationStyle.ToutenKuten => "、。",
        PunctuationStyle.CommaKuten => "，。",
        PunctuationStyle.ToutenPeriod => "、．",
        PunctuationStyle.CommaPeriod => "，．",
        _ => throw new ArgumentOutOfRangeException(nameof(style), style, null),
    };

    /// <summary>
    /// 切り替えキーを押したときの切り替え先。
    /// 「，．」なら「、。」に、それ以外（IME の設定画面で選んだ「，。」「、．」を含む）なら「，．」にする。
    /// </summary>
    public static PunctuationStyle Toggled(this PunctuationStyle style) =>
        style == PunctuationStyle.CommaPeriod ? PunctuationStyle.ToutenKuten : PunctuationStyle.CommaPeriod;
}
