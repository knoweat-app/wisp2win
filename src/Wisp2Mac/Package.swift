// swift-tools-version: 5.9

import PackageDescription

let package = Package(
    name: "Wisp2Mac",
    platforms: [.macOS(.v13)],
    products: [
        .executable(name: "Wisp2Mac", targets: ["Wisp2Mac"])
    ],
    targets: [
        .executableTarget(
            name: "Wisp2Mac",
            path: "Sources/Wisp2Mac"
        )
    ]
)
