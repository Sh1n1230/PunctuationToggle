namespace PunctuationToggle;

/// <summary>
/// Microsoft IME の設定値 option1（レジストリ HKCU\Software\Microsoft\IME\15.0\IMEJP\MSIME の DWORD）のうち、
/// 「句読点」設定を表すビット16〜17を読み書きする。ほかのビットの設定は変えない。
/// </summary>
/// <remarks>
/// ビット16〜17の値と句読点の対応は次のとおり（IME の設定画面での並び順とは異なる）。
/// 0: ，．  1: 、。  2: 、．  3: ，。
/// </remarks>
public static class MicrosoftImeOption1
{
    private const int PunctuationShift = 16;
    private const int PunctuationMask = 0b11 << PunctuationShift;

    /// <summary>option1 が無い環境（IME の設定を一度も変更していない）での既定値。句読点は「、。」。</summary>
    public const int DefaultValue = 0x00150200;

    public static PunctuationStyle GetPunctuationStyle(int option1) =>
        ((option1 & PunctuationMask) >> PunctuationShift) switch
        {
            0 => PunctuationStyle.CommaPeriod,
            1 => PunctuationStyle.ToutenKuten,
            2 => PunctuationStyle.ToutenPeriod,
            _ => PunctuationStyle.CommaKuten,
        };

    public static int WithPunctuationStyle(int option1, PunctuationStyle style)
    {
        var bits = style switch
        {
            PunctuationStyle.CommaPeriod => 0,
            PunctuationStyle.ToutenKuten => 1,
            PunctuationStyle.ToutenPeriod => 2,
            PunctuationStyle.CommaKuten => 3,
            _ => throw new ArgumentOutOfRangeException(nameof(style), style, null),
        };
        return (option1 & ~PunctuationMask) | (bits << PunctuationShift);
    }
}
