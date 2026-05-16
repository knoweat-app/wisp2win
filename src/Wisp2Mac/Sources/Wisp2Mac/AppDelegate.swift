import AppKit

final class AppDelegate: NSObject, NSApplicationDelegate {
    let state = AppState()
    private var statusItem: NSStatusItem?
    private var hotkeyService: HotkeyService?

    func applicationDidFinishLaunching(_ notification: Notification) {
        AppLog.info("app", "Started")
        NSApp.setActivationPolicy(.accessory)
        configureStatusItem()

        hotkeyService = HotkeyService {
            Task { @MainActor in
                await self.state.toggleDictation()
            }
        }
        hotkeyService?.start()
    }

    func applicationWillTerminate(_ notification: Notification) {
        hotkeyService?.stop()
    }

    private func configureStatusItem() {
        let item = NSStatusBar.system.statusItem(withLength: NSStatusItem.variableLength)
        item.button?.image = NSImage(systemSymbolName: "mic.fill", accessibilityDescription: "Wisp2Mac")
        item.button?.image?.isTemplate = true

        let menu = NSMenu()
        menu.addItem(NSMenuItem(title: "Открыть", action: #selector(openWindow), keyEquivalent: ""))
        menu.addItem(NSMenuItem(title: "Диктовка", action: #selector(toggleDictation), keyEquivalent: ""))
        menu.addItem(.separator())
        menu.addItem(NSMenuItem(title: "Выход", action: #selector(quit), keyEquivalent: "q"))
        item.menu = menu
        statusItem = item
    }

    @objc private func openWindow() {
        NSApp.activate(ignoringOtherApps: true)
        NSApp.windows.first?.makeKeyAndOrderFront(nil)
    }

    @objc private func toggleDictation() {
        Task { @MainActor in
            await state.toggleDictation()
        }
    }

    @objc private func quit() {
        NSApp.terminate(nil)
    }
}
