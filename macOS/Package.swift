// swift-tools-version: 6.2

// アプリ本体は PunctuationToggle.xcodeproj でビルドする。
// このパッケージは、アプリのうち OS に依存しないロジック（PunctuationToggle/Core）を
// `swift test` で単体テストするためのもの。
import PackageDescription

let package = Package(
    name: "PunctuationToggleCore",
    // アプリ（PunctuationToggle.xcodeproj の MACOSX_DEPLOYMENT_TARGET）と同じ版にそろえ、同じ条件でコンパイルする
    platforms: [.macOS(.v26)],
    targets: [
        .target(
            name: "PunctuationToggleCore",
            path: "PunctuationToggle/Core"
        ),
        .testTarget(
            name: "PunctuationToggleCoreTests",
            dependencies: ["PunctuationToggleCore"],
            path: "Tests/PunctuationToggleCoreTests"
        ),
    ]
)
