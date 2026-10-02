import SwiftUI

@main
struct PunctuationToggleApp: App {
    @NSApplicationDelegateAdaptor(AppDelegate.self)
    private var appDelegate

    var body: some Scene {
        // メニューバーだけで動くアプリなので、ウィンドウは AppDelegate で必要なときに作る
        Settings {
            EmptyView()
        }
    }
}
