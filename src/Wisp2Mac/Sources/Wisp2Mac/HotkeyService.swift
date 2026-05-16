import AppKit

final class HotkeyService {
    private let onPressed: () -> Void
    private var monitor: Any?
    private(set) var config: HotkeyConfig

    init(config: HotkeyConfig, onPressed: @escaping () -> Void) {
        self.config = config
        self.onPressed = onPressed
    }

    func start() {
        guard monitor == nil else { return }
        let cfg = config
        monitor = NSEvent.addGlobalMonitorForEvents(matching: .keyDown) { [weak self] event in
            guard event.keyCode == cfg.keyCode,
                  event.modifierFlags.intersection([.command, .shift, .control, .option]) == cfg.modifiers
            else { return }
            AppLog.info("hotkey", "\(cfg.displayString) pressed")
            self?.onPressed()
        }
        if monitor == nil {
            AppLog.warn("hotkey", "Global monitor returned nil — Input Monitoring permission likely denied")
        } else {
            AppLog.info("hotkey", "Registered \(cfg.displayString)")
        }
    }

    func stop() {
        if let m = monitor {
            NSEvent.removeMonitor(m)
            monitor = nil
        }
    }

    func restart(config: HotkeyConfig) {
        stop()
        self.config = config
        start()
        AppLog.info("hotkey", "Restarted with \(config.displayString)")
    }
}
