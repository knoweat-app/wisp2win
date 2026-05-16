import Foundation

enum AppPaths {
    static let appName = "Wisp2Win"

    static var applicationSupport: URL {
        let base = FileManager.default.urls(for: .applicationSupportDirectory, in: .userDomainMask)[0]
        return base.appendingPathComponent(appName, isDirectory: true)
    }

    static var modelsDirectory: URL {
        applicationSupport.appendingPathComponent("Models", isDirectory: true)
    }

    static var settingsURL: URL {
        applicationSupport.appendingPathComponent("settings.json")
    }

    static var logsDirectory: URL {
        FileManager.default.urls(for: .libraryDirectory, in: .userDomainMask)[0]
            .appendingPathComponent("Logs", isDirectory: true)
            .appendingPathComponent(appName, isDirectory: true)
    }

    static var logURL: URL {
        logsDirectory.appendingPathComponent("wisp2win.log")
    }

    static var tempDirectory: URL {
        applicationSupport.appendingPathComponent("Temp", isDirectory: true)
    }

    static func ensure() throws {
        try FileManager.default.createDirectory(at: applicationSupport, withIntermediateDirectories: true)
        try FileManager.default.createDirectory(at: modelsDirectory, withIntermediateDirectories: true)
        try FileManager.default.createDirectory(at: logsDirectory, withIntermediateDirectories: true)
        try FileManager.default.createDirectory(at: tempDirectory, withIntermediateDirectories: true)
    }
}
