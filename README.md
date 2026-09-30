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

Exporting now requests a Unity organization signature and requires Unity 6.6 or
later. Sign in to Unity as a member of the intended organization. The exporter
uses `CloudProjectSettings.organizationKey`, or the explicit automation argument
`-vidaSigningOrganization <organization-id>`. A missing organization stops the
export; it never silently selects an unsigned export. Existing output files are
preserved, so choose a new path. Importing these packages remains compatible with
older supported Editors.

For Unity 6.6, the signing ID is the organization's legacy numeric ID
(`genesisId` in `unity cloud org list`), matching `organizationKey`; do not pass
the Cloud organization's UUID. Use only the organization authorized for the
release. A GUI Editor session must be signed in; a CLI login alone does not
establish that Editor session.

Unity itself can produce an unsigned file when signing fails. The export log
therefore reports `signingRequested=true signatureReadbackRequired=true`, not a
verified signature. The exporter rejects files without Unity 6.6's nonempty
`package/.attestation.p7m` entry; it does not rely on Console callbacks, which can
miss native signing errors. Presence alone does not verify the signature.
Before release, inspect the resulting signature during import
in a disposable Unity 6.6 project. A file or successful process exit alone is not
proof. Recompute the artifact's size and SHA-256 after signing; publication is a
separate action.

To re-export an existing Starter asset set, use
`Vida.Framework.Editor.FrameworkPackageExporter.ExportAssetListForAutomation`
with `-vidaPackageAssetList <json>`, `-vidaPackageOutput <new.unitypackage>` and
`-vidaSigningOrganization <organization-id>`. The JSON contains an `assets` array
of `{ "path": "Assets/...", "folder": false, "sha256": "...", "metaSha256": "..." }`
records derived from the selected original `.unitypackage`. Folder records use
`folder: true` and still require their metadata hash. Native plugin bundle members
that have no individual `.meta` in the original archive use an empty metadata hash;
the exporter checks that no metadata file has appeared for those members. It verifies every
asset and `.meta` hash before export, rejecting local source drift instead of
silently signing a different Starter. It exports only listed paths, with no
recursive or dependency expansion. Use a dedicated staging project containing
those exact source assets when the working project has unrelated edits.

Unity reference: [asset-package signatures](https://docs.unity.com/en-us/engine/6000.6/manual/assets-and-media/asset-packages/signatures).

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

Use **Güncellemeleri kontrol et** above the connection status to update a bootstrap
installation directly from the signed-in Framework Store catalog. Only newer
versions are installed, after size and SHA-256 verification, without an import
selection dialog. Unity must be idle and satisfy the package's minimum version.
Git/UPM installations remain managed through Package Manager to avoid creating a
second Framework installation in Assets.

## Codex bridge

Choose **Vida > Framework > Install or Repair Codex Bridge** once after importing
Framework. The explicit installer copies the packaged local MCP connector to the
current user's `.vida/framework-mcp` directory and adds the `vida-framework` STDIO
server to the user's Codex `config.toml`. Restart Codex after installation.

With the intended Unity project open, Codex can then check the Framework session,
start secure Vida sign-in in the system browser, list the latest visible packages,
and import the latest release for an exact catalog package ID. A valid Editor-session
login is reused. Package history and arbitrary file or URL imports are deliberately
not exposed. Import operations require Unity to be idle and outside Play Mode; the
Framework package itself continues to use **Güncellemeleri kontrol et** in
**Vida > Menu**.

The connector communicates only through generated files under
`Library/VidaFramework/CodexBridge`. Session handles remain inside Unity and are
never written to the bridge directory or returned to Codex.
