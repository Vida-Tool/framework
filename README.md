# vida-framework

## Getting Started

Vida Framework requires Unity 6 (6000.0) or later. Download the published bootstrap
`.unitypackage` from `packages.vida.games`, then import it with **Assets > Import
Package > Custom Package**. This is the canonical first install path.

Open `Vida > Menu` and sign in through the system browser. The Unity Editor keeps the
opaque Framework session only for the current Editor session. Framework, starter, SDK,
and general packages come from the authenticated catalog. Open a package to
inspect its description, release notes, and published versions before downloading.
Downloads are imported only after their published size and SHA-256 digest have been
verified.

Code snippets remain on the legacy source and are not part of Framework Store v1.

Framework maintainers may instead install the repository through Package Manager by
Git URL. In that development route, Unity installs the dependencies declared in
`package.json` automatically. The `.unitypackage` catalog and download tools compile
using Unity 6 built-in APIs; they do not silently modify the importing project's
Package Manager dependencies.

The Framework uses the new Input System. For projects that use its input features,
accept Unity's prompt to enable the new input backend, or set **Edit > Project Settings
> Player > Other Settings > Active Input Handling** to **Input System Package (New)**,
then restart the Editor. Existing projects must migrate legacy `UnityEngine.Input`
calls and UI input modules before using New-only mode.

## Bootstrap export

Use `Vida > Framework > Export Bootstrap Package` to create the distributable
`.unitypackage`. The export contains Framework assets under `Assets/framework`, excluding local agent
instructions and generated code maps, and deliberately
does not use Unity's `IncludeDependencies` option, which would copy unrelated project
assets. The exporter enforces the Framework Store's exact 100,000,000-byte artifact
limit.

## Possible Errors

If Unity reports that no `git` executable was found while using the optional Git
Package Manager route, install Git and restart Unity. The bootstrap `.unitypackage`
route does not require Git.

## Editor appearance

The Editor uses a warm dark palette, rounded controls, quiet header actions, and
spacious package rows. Home, Starter, SDK, Codes, and Packages share the same visual
language; the empty Templates and Settings tabs are hidden. Package details, sign-in,
code snippets, and download progress use the same styling. Narrow windows keep the
compact icon sidebar and move package categories into row subtitles.
