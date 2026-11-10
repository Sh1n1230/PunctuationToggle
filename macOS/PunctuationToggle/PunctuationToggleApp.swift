import SwiftUI

@main
struct PunctuationToggleApp: App {
    @NSApplicationDelegateAdaptor(AppDelegate.self)
    private var appDelegate

    var body: some Scene {
        // メニューバーだけで動くアプリなので、ウィンドウは AppDelegate で必要なときに作る。
        // App にはシーンが1つ要るので、中身の無い Settings を置く
        Settings {
            EmptyView()
        }
        // Settings はアプリのメニューに「設定…」（⌘,）を加える。メニューバーは表示されないが、
        // 「PunctuationToggle について」やアラートが前面にあるときに ⌘, を押すと、何も無い設定ウィンドウが開くため、項目を無くす
        .commands {
            CommandGroup(replacing: .appSettings) {}
        }
    }
}
