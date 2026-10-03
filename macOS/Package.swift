// swift-tools-version: 6.0

// アプリ本体は PunctuationToggle.xcodeproj でビルドする。
// このパッケージは、アプリのうち OS に依存しないロジック（PunctuationToggle/Core）を
// `swift test` で単体テストするためのもの。
import PackageDescription

let package = Package(
    name: "PunctuationToggleCore",
    platforms: [.macOS(.v14)],
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
