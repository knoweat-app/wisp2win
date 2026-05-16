import Foundation

struct TranscriptPostProcessor {
    func polish(_ text: String, language: String) -> String {
        var result = text.trimmingCharacters(in: .whitespacesAndNewlines)
        guard !result.isEmpty else {
            return ""
        }

        result = replaceSpokenPunctuation(result, language: language)
        result = normalizePunctuationSpacing(result)
        result = capitalizeSentences(result)
        result = ensureFinalPunctuation(result)
        AppLog.info("polish", "Polished chars \(text.count)->\(result.count)")
        return result
    }

    private func replaceSpokenPunctuation(_ text: String, language: String) -> String {
        var result = text
        let russian = language == "ru" || language == "auto"
        let replacements: [(String, String)] = russian
            ? [
                ("точка с запятой", ";"),
                ("вопросительный знак", "?"),
                ("восклицательный знак", "!"),
                ("новая строка", "\n"),
                ("с новой строки", "\n"),
                ("новый абзац", "\n\n"),
                ("абзац", "\n\n"),
                ("двоеточие", ":"),
                ("запятая", ","),
                ("точка", "."),
                ("тире", "-")
            ]
            : [
                ("semicolon", ";"),
                ("question mark", "?"),
                ("exclamation mark", "!"),
                ("new paragraph", "\n\n"),
                ("new line", "\n"),
                ("colon", ":"),
                ("comma", ","),
                ("period", "."),
                ("full stop", "."),
                ("dash", "-")
            ]

        for (phrase, replacement) in replacements {
            result = result.replacingOccurrences(of: phrase, with: replacement, options: [.caseInsensitive])
        }
        return result
    }

    private func normalizePunctuationSpacing(_ text: String) -> String {
        var result = text.replacingOccurrences(of: "\r\n", with: "\n").replacingOccurrences(of: "\r", with: "\n")
        result = result.replacingOccurrences(of: #"[ \t]+"#, with: " ", options: .regularExpression)
        result = result.replacingOccurrences(of: #"\s+([,.;:!?])"#, with: "$1", options: .regularExpression)
        result = result.replacingOccurrences(of: #"([,.;:!?])(?=\S)"#, with: "$1 ", options: .regularExpression)
        result = result.replacingOccurrences(of: #"[ \t]*\n[ \t]*"#, with: "\n", options: .regularExpression)
        result = result.replacingOccurrences(of: #"\n{3,}"#, with: "\n\n", options: .regularExpression)
        return result.trimmingCharacters(in: .whitespacesAndNewlines)
    }

    private func capitalizeSentences(_ text: String) -> String {
        var result = ""
        var sentenceStart = true
        for char in text {
            if sentenceStart && char.isLetter {
                result.append(String(char).uppercased())
                sentenceStart = false
            } else {
                result.append(char)
                if char.isLetter || char.isNumber {
                    sentenceStart = false
                } else if ".!?\n".contains(char) {
                    sentenceStart = true
                }
            }
        }
        return result
    }

    private func ensureFinalPunctuation(_ text: String) -> String {
        guard let last = text.last else { return text }
        return ".,;:!?)]\"'".contains(last) ? text : text + "."
    }
}
