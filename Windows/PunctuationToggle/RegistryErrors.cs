namespace PunctuationToggle;

/// <summary>レジストリの読み書きの失敗を見分ける。</summary>
internal static class RegistryErrors
{
    /// <summary>
    /// レジストリにアクセスできなかったことを表す例外か。
    /// アクセス権が変更されている場合や、キーが削除されつつある場合などに投げられる。
    /// </summary>
    public static bool IsAccessFailure(Exception exception) =>
        exception is UnauthorizedAccessException or System.Security.SecurityException or IOException;
}
