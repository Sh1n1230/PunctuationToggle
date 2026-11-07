/// アプリが置かれている場所から、「アプリケーション」フォルダーに移すよう案内するかを決める。
///
/// 「ダウンロード」フォルダーなどから起動すると、Gatekeeper の App Translocation によって、隔離属性の付いたアプリは
/// 毎回違う読み取り専用の場所から実行される。アクセシビリティの許可は場所とコード署名に結び付くため、
/// 起動のたびに許可がやり直しになり、「ログイン時に起動」も次の起動では無い場所を登録してしまう。
nonisolated enum ApplicationLocation {
    /// App Translocation で実行されているアプリのパスに含まれる部分。
    /// App Translocation を判定する公開の API は無いため、パスで判定する
    static let translocationPathComponent = "/AppTranslocation/"

    /// 「アプリケーション」フォルダーに移すよう案内するか。
    /// - Parameters:
    ///   - bundlePath: アプリのパス（`Bundle.main.bundlePath`）
    ///   - homeDirectory: ホームフォルダーのパス（install.sh がインストールする `~/Applications` の判定に使う）
    static func shouldSuggestMovingToApplications(bundlePath: String, homeDirectory: String) -> Bool {
        if bundlePath.contains(translocationPathComponent) {
            return true
        }
        let applicationsDirectories = ["/Applications/", homeDirectory + "/Applications/"]
        return !applicationsDirectories.contains { directory in bundlePath.hasPrefix(directory) }
    }
}
