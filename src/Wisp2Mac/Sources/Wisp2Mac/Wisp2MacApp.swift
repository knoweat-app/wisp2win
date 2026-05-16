import SwiftUI

@main
struct Wisp2MacApp: App {
    @NSApplicationDelegateAdaptor(AppDelegate.self) private var appDelegate

    var body: some Scene {
        WindowGroup("Wisp2Mac") {
            SettingsView(state: appDelegate.state)
                .frame(minWidth: 560, minHeight: 640)
        }
        .windowStyle(.hiddenTitleBar)
        .commands {
            CommandGroup(replacing: .appInfo) {
                Button("О Wisp2Mac") {
                    NSApplication.shared.orderFrontStandardAboutPanel()
                }
            }
        }
    }
}
