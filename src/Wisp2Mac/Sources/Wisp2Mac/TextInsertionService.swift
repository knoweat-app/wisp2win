import AppKit

final class TextInsertionService {
    func insert(_ text: String, method: InsertMethod, target: TargetApplication?) throws {
        if let target {
            NSRunningApplication(processIdentifier: target.processIdentifier)?.activate(options: [.activateIgnoringOtherApps])
            Thread.sleep(forTimeInterval: 0.18)
        }

        switch method {
        case .auto:
            try paste(text)
        case .paste:
            try paste(text)
        case .typeText:
            try typeText(text)
        }
    }

    private func paste(_ text: String) throws {
        let pasteboard = NSPasteboard.general
        pasteboard.clearContents()
        pasteboard.setString(text, forType: .string)
        try sendModifiedKey(keyCode: 9, flags: .maskCommand)
        AppLog.info("insert", "Sent Cmd+V chars=\(text.count)")
    }

    private func typeText(_ text: String) throws {
        for scalar in text.unicodeScalars {
            var value = UniChar(truncatingIfNeeded: scalar.value)
            guard let down = CGEvent(keyboardEventSource: nil, virtualKey: 0, keyDown: true),
                  let up = CGEvent(keyboardEventSource: nil, virtualKey: 0, keyDown: false) else {
                throw WispError("Не удалось создать keyboard event")
            }

            down.keyboardSetUnicodeString(stringLength: 1, unicodeString: &value)
            up.keyboardSetUnicodeString(stringLength: 1, unicodeString: &value)
            down.post(tap: .cghidEventTap)
            up.post(tap: .cghidEventTap)
            Thread.sleep(forTimeInterval: 0.002)
        }
        AppLog.info("insert", "Typed chars=\(text.count)")
    }

    private func sendModifiedKey(keyCode: CGKeyCode, flags: CGEventFlags) throws {
        guard let down = CGEvent(keyboardEventSource: nil, virtualKey: keyCode, keyDown: true),
              let up = CGEvent(keyboardEventSource: nil, virtualKey: keyCode, keyDown: false) else {
            throw WispError("Не удалось создать keyboard event")
        }

        down.flags = flags
        up.flags = flags
        down.post(tap: .cghidEventTap)
        up.post(tap: .cghidEventTap)
    }
}
