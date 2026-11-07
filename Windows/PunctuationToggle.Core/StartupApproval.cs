namespace PunctuationToggle;

/// <summary>「ログイン時に起動」の状態。</summary>
public enum LaunchAtLoginState
{
    /// <summary>登録されていない（または別の場所の exe が登録されている）。</summary>
    Off,

    /// <summary>登録されていて、ログイン時に起動する。</summary>
    On,

    /// <summary>
    /// 登録されているが、Windows の設定の「スタートアップ アプリ」やタスク マネージャーで無効にされていて、ログイン時に起動しない。
    /// </summary>
    DisabledByWindows,
}

/// <summary>
/// Windows の設定の「スタートアップ アプリ」やタスク マネージャーで、Run キーの登録を消さずに自動起動を止めたかを読み取る。
/// </summary>
/// <remarks>
/// 止めた状態は、HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run の、
/// Run キーと同じ名前の REG_BINARY の値（12 バイト。2 バイト目以降は止めた日時）に保存される。公開されていない仕様で、
/// 先頭のバイトの最下位ビットが 1（0x03 など）なら無効、0（0x02・0x06 など）か、値が無ければ有効。
/// 非公開の値なので、アプリからは読むだけで書き換えない（有効にするのは利用者に Windows の設定で行ってもらう）。
/// </remarks>
public static class StartupApproval
{
    private const byte DisabledBit = 0x01;

    /// <summary>StartupApproved\Run の値から、自動起動を止めているかを判定する。</summary>
    /// <param name="value">値（REG_BINARY）。値が無い場合や形式が違う場合は null。</param>
    public static bool IsDisabled(byte[]? value) => value is { Length: > 0 } && (value[0] & DisabledBit) != 0;
}
