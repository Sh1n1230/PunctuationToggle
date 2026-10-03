// アプリアイコン（Assets.xcassets/AppIcon.appiconset）の PNG を生成する。
// 使い方: swift macOS/Scripts/generate_app_icon.swift
import AppKit

let iconSetDirectory = URL(fileURLWithPath: #filePath)
    .deletingLastPathComponent()
    .appendingPathComponent("../PunctuationToggle/Assets.xcassets/AppIcon.appiconset")
    .standardizedFileURL

/// 1024pt のキャンバスに描いたアイコンを、指定したピクセル数の PNG にする。
func renderIcon(pixelSize: Int) -> Data {
    let canvasSize: CGFloat = 1024
    let bitmap = NSBitmapImageRep(
        bitmapDataPlanes: nil, pixelsWide: pixelSize, pixelsHigh: pixelSize,
        bitsPerSample: 8, samplesPerPixel: 4, hasAlpha: true, isPlanar: false,
        colorSpaceName: .deviceRGB, bytesPerRow: 0, bitsPerPixel: 0
    )!
    bitmap.size = NSSize(width: canvasSize, height: canvasSize)

    NSGraphicsContext.saveGraphicsState()
    NSGraphicsContext.current = NSGraphicsContext(bitmapImageRep: bitmap)

    // macOS のアイコンの標準的な余白と角丸（1024pt に対して 824pt の角丸四角形）
    let tileRect = NSRect(x: 100, y: 100, width: 824, height: 824)
    let tilePath = NSBezierPath(roundedRect: tileRect, xRadius: 185, yRadius: 185)
    let gradient = NSGradient(
        starting: NSColor(calibratedRed: 0.20, green: 0.42, blue: 0.95, alpha: 1),
        ending: NSColor(calibratedRed: 0.11, green: 0.20, blue: 0.62, alpha: 1)
    )!
    gradient.draw(in: tilePath, angle: -90)

    // 上段に「、。」、下段に「，．」を描き、間に切り替えを表す両向きの矢印を置く
    let font = NSFont(name: "HiraginoSans-W7", size: 300) ?? .boldSystemFont(ofSize: 300)
    let paragraphStyle = NSMutableParagraphStyle()
    paragraphStyle.alignment = .center
    func drawText(_ text: String, centerY: CGFloat, alpha: CGFloat) {
        let attributes: [NSAttributedString.Key: Any] = [
            .font: font,
            .foregroundColor: NSColor.white.withAlphaComponent(alpha),
            .paragraphStyle: paragraphStyle,
        ]
        let textSize = (text as NSString).size(withAttributes: attributes)
        // 全角の句読点は字面の左下に寄っているので、少し右上にずらして中央に見せる
        let textRect = NSRect(x: 160, y: centerY - textSize.height / 2 + 60, width: 824, height: textSize.height)
        (text as NSString).draw(in: textRect, withAttributes: attributes)
    }
    drawText("、。", centerY: 690, alpha: 1)
    drawText("，．", centerY: 330, alpha: 0.75)

    let arrow = NSBezierPath()
    arrow.lineWidth = 28
    arrow.lineCapStyle = .round
    arrow.lineJoinStyle = .round
    arrow.move(to: NSPoint(x: 512, y: 600))
    arrow.line(to: NSPoint(x: 512, y: 420))
    for (tipY, direction) in [(CGFloat(600), CGFloat(-1)), (CGFloat(420), CGFloat(1))] {
        arrow.move(to: NSPoint(x: 466, y: tipY + 46 * direction))
        arrow.line(to: NSPoint(x: 512, y: tipY))
        arrow.line(to: NSPoint(x: 558, y: tipY + 46 * direction))
    }
    NSColor.white.withAlphaComponent(0.9).setStroke()
    arrow.stroke()

    NSGraphicsContext.restoreGraphicsState()
    return bitmap.representation(using: .png, properties: [:])!
}

struct IconImage: Encodable {
    var idiom = "mac"
    let scale: String
    let size: String
    let filename: String
}

struct AssetCatalogContents: Encodable {
    struct Metadata: Encodable {
        var author = "xcode"
        var version = 1
    }

    let images: [IconImage]
    var info = Metadata()
}

var images: [IconImage] = []
for pointSize in [16, 32, 128, 256, 512] {
    for scale in [1, 2] {
        let filename = "icon_\(pointSize)x\(pointSize)@\(scale)x.png"
        try renderIcon(pixelSize: pointSize * scale).write(to: iconSetDirectory.appendingPathComponent(filename))
        images.append(IconImage(scale: "\(scale)x", size: "\(pointSize)x\(pointSize)", filename: filename))
    }
}

let encoder = JSONEncoder()
encoder.outputFormatting = [.prettyPrinted, .sortedKeys]
try encoder.encode(AssetCatalogContents(images: images))
    .write(to: iconSetDirectory.appendingPathComponent("Contents.json"))
print("生成しました: \(iconSetDirectory.path)")
