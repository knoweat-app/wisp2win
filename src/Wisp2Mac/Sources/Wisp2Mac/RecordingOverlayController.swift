import AppKit
import SwiftUI

@MainActor
final class RecordingOverlayController {
    private var panel: NSPanel?
    private var startedAt = Date()
    private var timer: Timer?
    private var elapsed = "00:00"

    func show() {
        startedAt = Date()
        elapsed = "00:00"
        ensurePanel()
        updateElapsed()
        panel?.alphaValue = 0
        panel?.orderFrontRegardless()
        NSAnimationContext.runAnimationGroup { context in
            context.duration = 0.18
            panel?.animator().alphaValue = 0.96
        }
        timer?.invalidate()
        timer = Timer.scheduledTimer(withTimeInterval: 1, repeats: true) { [weak self] _ in
            Task { @MainActor in self?.updateElapsed() }
        }
    }

    func hide() {
        timer?.invalidate()
        timer = nil
        guard let panel else { return }
        NSAnimationContext.runAnimationGroup { context in
            context.duration = 0.20
            panel.animator().alphaValue = 0
        } completionHandler: {
            panel.orderOut(nil)
        }
    }

    private func ensurePanel() {
        if panel != nil {
            positionPanel()
            return
        }

        let panel = NSPanel(
            contentRect: NSRect(x: 0, y: 0, width: 168, height: 64),
            styleMask: [.borderless, .nonactivatingPanel],
            backing: .buffered,
            defer: false
        )
        panel.isFloatingPanel = true
        panel.level = .floating
        panel.backgroundColor = .clear
        panel.isOpaque = false
        panel.hasShadow = true
        panel.ignoresMouseEvents = true
        panel.collectionBehavior = [.canJoinAllSpaces, .transient]
        panel.contentView = NSHostingView(rootView: RecordingOverlayView(elapsed: { [weak self] in self?.elapsed ?? "00:00" }))
        self.panel = panel
        positionPanel()
    }

    private func positionPanel() {
        guard let panel, let screen = NSScreen.main else { return }
        let frame = screen.visibleFrame
        panel.setFrameOrigin(NSPoint(x: frame.midX - panel.frame.width / 2, y: frame.minY + 34))
    }

    private func updateElapsed() {
        let seconds = Int(Date().timeIntervalSince(startedAt))
        elapsed = String(format: "%02d:%02d", seconds / 60, seconds % 60)
        if let hosting = panel?.contentView as? NSHostingView<RecordingOverlayView> {
            hosting.rootView = RecordingOverlayView(elapsed: { [weak self] in self?.elapsed ?? "00:00" })
        }
    }
}

struct RecordingOverlayView: View {
    let elapsed: () -> String
    @State private var pulse = false

    var body: some View {
        HStack(spacing: 10) {
            ZStack {
                Circle()
                    .fill(Color.red.opacity(0.24))
                    .frame(width: 28, height: 28)
                    .scaleEffect(pulse ? 1.34 : 1)
                    .opacity(pulse ? 0.18 : 0.46)
                Circle()
                    .fill(Color.red)
                    .frame(width: 28, height: 28)
                Image(systemName: "mic.fill")
                    .foregroundStyle(.white)
                    .font(.system(size: 14, weight: .semibold))
            }
            .frame(width: 40, height: 40)

            VStack(alignment: .leading, spacing: 1) {
                Text("Запись")
                    .font(.system(size: 15, weight: .semibold))
                    .foregroundStyle(Color(red: 0.06, green: 0.09, blue: 0.16))
                Text(elapsed())
                    .font(.system(size: 12, weight: .medium))
                    .foregroundStyle(.secondary)
            }
            Spacer(minLength: 0)
        }
        .padding(12)
        .frame(width: 168, height: 64)
        .background(.white.opacity(0.97))
        .clipShape(Capsule())
        .overlay(Capsule().stroke(.black.opacity(0.08), lineWidth: 1))
        .onAppear {
            withAnimation(.easeInOut(duration: 1.15).repeatForever(autoreverses: true)) {
                pulse = true
            }
        }
    }
}
