// swift-tools-version: 6.2

// アプリ本体は PunctuationToggle.xcodeproj でビルドする。
// このパッケージは、アプリのうち OS に依存しないロジック（PunctuationToggle/Core）を
// `swift test` で単体テストするためのもの。
import PackageDescription

/// アプリ（PunctuationToggle.xcodeproj のアプリのターゲット）と同じ条件で Core をコンパイルするための設定。
/// アプリのビルド設定を変えたら、ここも合わせる。
///
/// Core の型は、どのスレッドからも使える値と判定だけなので、`nonisolated` を明示して既定のメインアクターへの隔離を外している
/// （テストの引数の評価や、イベントタップのコールバックのようなメインアクターの外からも使えるようにするため）。
/// Core に型を追加するときも `nonisolated` を付ける。付け忘れた場合も、アプリと同じ条件なのでテストで同じように気付ける。
let swiftSettingsMatchingApp: [SwiftSetting] = [
    // SWIFT_DEFAULT_ACTOR_ISOLATION = MainActor（SE-0466）
    .defaultIsolation(MainActor.self),
    // SWIFT_APPROACHABLE_CONCURRENCY = YES が有効にする機能のうち、Swift 6 の言語モードで既定になっていないもの
    .enableUpcomingFeature("NonisolatedNonsendingByDefault"),
    .enableUpcomingFeature("InferIsolatedConformances"),
    // SWIFT_UPCOMING_FEATURE_MEMBER_IMPORT_VISIBILITY = YES
    .enableUpcomingFeature("MemberImportVisibility"),
    // SWIFT_TREAT_WARNINGS_AS_ERRORS = YES（SE-0480）
    .treatAllWarnings(as: .error),
]

let package = Package(
    name: "PunctuationToggleCore",
    // アプリ（PunctuationToggle.xcodeproj の MACOSX_DEPLOYMENT_TARGET）と同じ版にそろえる
    platforms: [.macOS(.v26)],
    targets: [
        .target(
            name: "PunctuationToggleCore",
            path: "PunctuationToggle/Core",
            swiftSettings: swiftSettingsMatchingApp
        ),
        .testTarget(
            name: "PunctuationToggleCoreTests",
            dependencies: ["PunctuationToggleCore"],
            path: "Tests/PunctuationToggleCoreTests",
            swiftSettings: swiftSettingsMatchingApp
        ),
    ]
)
