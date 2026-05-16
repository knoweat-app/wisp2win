import AppKit
import Combine

@MainActor
final class AppDelegate: NSObject, NSApplicationDelegate {
    let state = AppState()
    private var statusItem: NSStatusItem?
    private var hotkeyService: HotkeyService?
    private var cancellables = Set<AnyCancellable>()

    // Menu items we update dynamically
    private var dictationMenuItem: NSMenuItem?
    private var transcriptMenuItem: NSMenuItem?

    func applicationDidFinishLaunching(_ notification: Notification) {
        AppLog.info("app", "Started")
        NSApp.setActivationPolicy(.accessory)

        checkAccessibilityPermission()
        configureStatusItem()
        startHotkeyService()
        observeStateChanges()
    }

    func applicationWillTerminate(_ notification: Notification) {
        hotkeyService?.stop()
    }

    // MARK: - Accessibility

    private func checkAccessibilityPermission() {
        let trusted = AXIsProcessTrusted()
        if !trusted {
            AppLog.warn("accessibility", "Accessibility not granted — text insertion will fail")
            DispatchQueue.main.asyncAfter(deadline: .now() + 0.5) {
                self.showAccessibilityAlert()
            }
        } else {
            AppLog.info("accessibility", "Accessibility granted")
        }
    }

    private func showAccessibilityAlert() {
        let alert = NSAlert()
        alert.messageText = "Нужен доступ к Accessibility"
        alert.informativeText = """
        Wisp2Win вставляет текст в активное приложение через Accessibility API.

        Откройте: Системные настройки → Конфиденциальность и безопасность → Универсальный доступ → добавьте Wisp2Mac.

        После разрешения перезапустите приложение.
        """
        alert.addButton(withTitle: "Открыть настройки")
        alert.addButton(withTitle: "Позже")
        alert.alertStyle = .warning

        if alert.runModal() == .alertFirstButtonReturn {
            NSWorkspace.shared.open(
                URL(string: "x-apple.systempreferences:com.apple.preference.security?Privacy_Accessibility")!
            )
        }
    }

    // MARK: - Hotkey

    private func startHotkeyService() {
        hotkeyService = HotkeyService(config: state.settings.hotkey) {
            Task { @MainActor in await self.state.toggleDictation() }
        }
        hotkeyService?.start()
    }

    // MARK: - Status Item

    private func configureStatusItem() {
        let item = NSStatusBar.system.statusItem(withLength: NSStatusItem.variableLength)
        item.button?.image = NSImage(systemSymbolName: "mic.fill", accessibilityDescription: "Wisp2Mac")
        item.button?.image?.isTemplate = true

        let menu = NSMenu()
        menu.delegate = self

        let dictation = NSMenuItem(title: "Диктовать", action: #selector(toggleDictation), keyEquivalent: "")
        menu.addItem(dictation)
        dictationMenuItem = dictation

        menu.addItem(.separator())
        menu.addItem(NSMenuItem(title: "Настройки…", action: #selector(openSettings), keyEquivalent: ","))
        menu.addItem(.separator())

        let transcript = NSMenuItem(title: "Нет записей", action: nil, keyEquivalent: "")
        transcript.isEnabled = false
        menu.addItem(transcript)
        transcriptMenuItem = transcript

        menu.addItem(.separator())
        menu.addItem(NSMenuItem(title: "Выход", action: #selector(quit), keyEquivalent: "q"))

        item.menu = menu
        statusItem = item
    }

    private func updateMenu() {
        if state.isRecording {
            dictationMenuItem?.title = "● Остановить и вставить"
            statusItem?.button?.image = NSImage(systemSymbolName: "mic.circle.fill",
                                                accessibilityDescription: "Запись…")
        } else {
            dictationMenuItem?.title = "Диктовать  \(state.settings.hotkey.displayString)"
            statusItem?.button?.image = NSImage(systemSymbolName: "mic.fill",
                                                accessibilityDescription: "Wisp2Mac")
        }
        statusItem?.button?.image?.isTemplate = true

        if state.lastTranscript.isEmpty {
            transcriptMenuItem?.title = "Нет записей"
        } else {
            let preview = String(state.lastTranscript.prefix(46))
            transcriptMenuItem?.title = preview.count < state.lastTranscript.count ? preview + "…" : preview
        }
    }

    // MARK: - Observe state changes

    private func observeStateChanges() {
        state.$isRecording
            .receive(on: RunLoop.main)
            .sink { [weak self] _ in self?.updateMenu() }
            .store(in: &cancellables)

        state.$lastTranscript
            .receive(on: RunLoop.main)
            .sink { [weak self] _ in self?.updateMenu() }
            .store(in: &cancellables)

        // Restart hotkey service when the hotkey setting changes
        state.$settings
            .map(\.hotkey)
            .removeDuplicates()
            .dropFirst()
            .receive(on: RunLoop.main)
            .sink { [weak self] newHotkey in
                self?.hotkeyService?.restart(config: newHotkey)
                self?.updateMenu()
            }
            .store(in: &cancellables)
    }

    // MARK: - Actions

    @objc private func openSettings() {
        NSApp.activate(ignoringOtherApps: true)
        NSApp.windows.first?.makeKeyAndOrderFront(nil)
    }

    @objc private func toggleDictation() {
        Task { @MainActor in await state.toggleDictation() }
    }

    @objc private func quit() {
        NSApp.terminate(nil)
    }
}

// MARK: - NSMenuDelegate

extension AppDelegate: NSMenuDelegate {
    nonisolated func menuNeedsUpdate(_ menu: NSMenu) {
        Task { @MainActor in updateMenu() }
    }
}
