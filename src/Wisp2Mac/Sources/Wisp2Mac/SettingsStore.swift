import Foundation

struct SettingsStore {
    func load() -> AppSettings {
        do {
            try AppPaths.ensure()
            guard FileManager.default.fileExists(atPath: AppPaths.settingsURL.path) else {
                return AppSettings()
            }

            let data = try Data(contentsOf: AppPaths.settingsURL)
            return try JSONDecoder().decode(AppSettings.self, from: data)
        } catch {
            AppLog.error("settings", error)
            return AppSettings()
        }
    }

    func save(_ settings: AppSettings) {
        do {
            try AppPaths.ensure()
            let data = try JSONEncoder().encode(settings)
            try data.write(to: AppPaths.settingsURL, options: .atomic)
        } catch {
            AppLog.error("settings", error)
        }
    }
}
