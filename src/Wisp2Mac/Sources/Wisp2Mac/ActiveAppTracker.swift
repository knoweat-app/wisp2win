import AppKit

final class ActiveAppTracker {
    func capture() -> TargetApplication? {
        guard let app = NSWorkspace.shared.frontmostApplication,
              app.bundleIdentifier != Bundle.main.bundleIdentifier else {
            return nil
        }

        let target = TargetApplication(
            processIdentifier: app.processIdentifier,
            bundleIdentifier: app.bundleIdentifier ?? "",
            localizedName: app.localizedName ?? ""
        )
        AppLog.info("target", "Captured pid=\(target.processIdentifier), bundle=\(target.bundleIdentifier), name=\(target.localizedName)")
        return target
    }
}
