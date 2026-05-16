import AppKit

final class HotkeyService {
    private let onPressed: () -> Void
    private var monitor: Any?

    init(onPressed: @escaping () -> Void) {
        self.onPressed = onPressed
    }

    func start() {
        monitor = NSEvent.addGlobalMonitorForEvents(matching: .keyDown) { [weak self] event in
            guard event.keyCode == 49,
                  event.modifierFlags.contains(.control),
                  event.modifierFlags.contains(.shift) else {
                return
            }

            AppLog.info("hotkey", "Ctrl+Shift+Space pressed")
            self?.onPressed()
        }
        AppLog.info("hotkey", "Registered Ctrl+Shift+Space monitor")
    }

    func stop() {
        if let monitor {
            NSEvent.removeMonitor(monitor)
            self.monitor = nil
        }
    }
}
