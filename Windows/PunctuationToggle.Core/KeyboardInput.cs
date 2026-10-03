namespace PunctuationToggle;

public enum KeyboardInputKind
{
    KeyDown,
    KeyUp,

    /// <summary>
    /// マウスのクリックやホイール操作。
    /// Ctrl＋クリックなどを、修飾キーの単独押しと区別するために使う。
    /// </summary>
    Mouse,
}

/// <summary>
/// キーボード・マウスの入力のうち、切り替えキーの判定に必要な情報だけを取り出したもの。
/// 低レベルフックで受け取った入力から作る（KeyboardHook を参照）。
/// </summary>
/// <param name="Kind">入力の種類。</param>
/// <param name="VirtualKey">キーの仮想キーコード（<see cref="KeyboardInputKind.Mouse"/> では 0）。</param>
/// <param name="IsRepeat">キーを押しっぱなしにしたことによるリピートか。</param>
/// <param name="OtherModifiersHeld">このキー以外の修飾キー（Shift・Ctrl・Alt・Windows）が押されているか。</param>
public readonly record struct KeyboardInput(
    KeyboardInputKind Kind,
    int VirtualKey = 0,
    bool IsRepeat = false,
    bool OtherModifiersHeld = false)
{
    public static KeyboardInput KeyDown(int virtualKey, bool isRepeat = false, bool otherModifiersHeld = false) =>
        new(KeyboardInputKind.KeyDown, virtualKey, isRepeat, otherModifiersHeld);

    public static KeyboardInput KeyUp(int virtualKey) => new(KeyboardInputKind.KeyUp, virtualKey);

    public static KeyboardInput Mouse { get; } = new(KeyboardInputKind.Mouse);
}
