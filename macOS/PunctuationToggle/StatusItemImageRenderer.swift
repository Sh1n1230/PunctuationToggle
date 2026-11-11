import AppKit
import CoreText

/// 句読点の2文字を描いた、メニューバー用の画像を作る。
///
/// 文字のまま（`NSStatusBarButton.title`）表示すると、字形の小さい「，．」が「、。」より小さく見える（#322）ため、
/// 文字の輪郭を、どの組み合わせでも範囲の面積がそろう倍率（`SymbolIconLayout`）で描く。
enum StatusItemImageRenderer {
    /// 画像の幅と高さ（ポイント）。メニューバーの高さ（`NSStatusBar.system.thickness`、22）に上下の余白を残した大きさで、
    /// どの組み合わせでも同じ大きさにして、切り替えたときにメニューバーの項目の幅が変わらないようにする
    private static let imageSize: CGFloat = 18

    /// 画像の縁と文字の間に空ける幅（画像の幅に対する割合。Windows 版の `TrayIconRenderer` と同じ値）
    private static let paddingRatio: CGFloat = 0.06

    /// 並べる文字の間に空ける幅（文字の輪郭全体の高さに対する割合。Windows 版の `TrayIconRenderer` と同じ値）
    private static let characterGapRatio: CGFloat = 0.5

    /// 句読点の組み合わせごとの画像。メニューバーの色（ライト・ダーク、選択中）に合わせて描かれるテンプレート画像
    private static let images: [PunctuationStyle: NSImage] = {
        let outlines = PunctuationStyle.allCases.map { style in (style, makeTightOutline(style.symbols)) }
        let references = outlines.map { _, outline in bounds(of: outline) }
        return Dictionary(
            uniqueKeysWithValues: outlines.map { style, outline in
                (style, makeImage(outline: outline, references: references))
            })
    }()

    static func image(for style: PunctuationStyle) -> NSImage? {
        images[style]
    }

    private static func makeImage(outline: CGPath, references: [SymbolIconLayout.OutlineBounds]) -> NSImage {
        let placement = SymbolIconLayout.placement(
            of: bounds(of: outline),
            references: references,
            available: imageSize * (1 - 2 * paddingRatio)
        )

        // 輪郭の中心を原点に移し、拡大・縮小してから、画像の中心に移す（後に書いたものから順に適用される）
        var transform = CGAffineTransform(translationX: imageSize / 2, y: imageSize / 2)
            .scaledBy(x: placement.scale, y: placement.scale)
            .translatedBy(x: -outline.boundingBoxOfPath.midX, y: -placement.centerY)
        // 描画は表示のとき（画面の解像度ごと）に呼ばれ、メインスレッドとは限らないので、輪郭だけを渡す。
        // CGPath は Sendable とされていないが、作った後に変更できない値なので、どのスレッドから読んでも安全
        nonisolated(unsafe) let placedOutline = outline.copy(using: &transform) ?? outline
        let image = NSImage(size: NSSize(width: imageSize, height: imageSize), flipped: false) { @Sendable _ in
            guard let context = NSGraphicsContext.current?.cgContext else { return false }
            context.addPath(placedOutline)
            context.setFillColor(.black)
            context.fillPath()
            return true
        }
        image.isTemplate = true
        return image
    }

    private static func bounds(of outline: CGPath) -> SymbolIconLayout.OutlineBounds {
        let boundingBox = outline.boundingBoxOfPath
        return SymbolIconLayout.OutlineBounds(
            width: boundingBox.width, bottom: boundingBox.minY, top: boundingBox.maxY)
    }

    /// 文字ごとの輪郭を、縦の位置（ベースラインからの高さ）は保ったまま、横の間を詰めて並べた輪郭を作る。
    /// 全角の句読点は字面（1文字分の正方形）の左下に小さく描かれ、字面のまま並べると間が空きすぎるため
    private static func makeTightOutline(_ text: String) -> CGPath {
        let characterOutlines = text.map(makeOutline(of:))
        let characterBounds = characterOutlines.map(\.boundingBoxOfPath)
        let top = characterBounds.map(\.maxY).max() ?? 0
        let bottom = characterBounds.map(\.minY).min() ?? 0
        let gap = (top - bottom) * characterGapRatio

        let outline = CGMutablePath()
        var left: CGFloat = 0
        for (characterOutline, bounds) in zip(characterOutlines, characterBounds) {
            outline.addPath(characterOutline, transform: CGAffineTransform(translationX: left - bounds.minX, y: 0))
            left += bounds.width + gap
        }
        return outline
    }

    /// メニューバーのフォントで描いたときの、1文字の輪郭。
    /// メニューバーのフォント（システムフォント）は句読点の字形を持たないので、システムが代わりに使うフォント
    /// （ヒラギノ角ゴシック）を求めて、その字形を使う
    private static func makeOutline(of character: Character) -> CGPath {
        let string = String(character)
        let menuBarFont = NSFont.menuBarFont(ofSize: 0) as CTFont
        let codeUnits = Array(string.utf16)
        let font = CTFontCreateForString(
            menuBarFont, string as CFString, CFRange(location: 0, length: codeUnits.count))

        var glyphs = [CGGlyph](repeating: 0, count: codeUnits.count)
        guard
            CTFontGetGlyphsForCharacters(font, codeUnits, &glyphs, codeUnits.count),
            let glyph = glyphs.first,
            let outline = CTFontCreatePathForGlyph(font, glyph, nil)
        else {
            return CGMutablePath()
        }
        return outline
    }
}
