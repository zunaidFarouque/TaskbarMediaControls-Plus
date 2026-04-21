# Release Helper (AI Runbook)

Use this guide to publish a new release quickly and consistently.

Scope:
- Portable ZIP release (primary distribution)
- Git tag + GitHub release
- Scoop manifest update in this repo

---

## 0) Inputs Required

Set these values first:

- `version`: semantic version without `v` (example: `1.0.3`)
- `repo`: GitHub repo for release commands (`zunaidFarouque/TaskbarMediaControls-Plus`)

PowerShell variables:

```powershell
$version = "1.0.3"
$tag = "v$version"
$repo = "zunaidFarouque/TaskbarMediaControls-Plus"
```

---

## 1) Preflight Checks

From repo root:

```powershell
git status --short --branch
git log --oneline -n 10
gh auth status
```

Requirements:
- Working tree is clean (or only intended release edits)
- Correct branch/commit selected
- GitHub CLI authenticated

---

## 2) Validate + Publish Build Output

```powershell
dotnet test "./TaskbarMediaControls.sln" -c Release
dotnet publish "./TaskbarMediaControls.csproj" -c Release
```

Publish output path:

```text
./bin/Release/net8.0-windows10.0.19041.0/publish
```

---

## 3) Build Portable ZIP + Compute SHA256

```powershell
$publishDir = "./bin/Release/net8.0-windows10.0.19041.0/publish"
Copy-Item "./scripts/shortcut-manager.bat" "$publishDir/shortcut-manager.bat" -Force
$zipPath = "./bin/Release/TaskbarMediaControls-Plus-v$version-portable.zip"
if (Test-Path $zipPath) { Remove-Item $zipPath -Force }
Compress-Archive -Path "$publishDir/*" -DestinationPath $zipPath
$hash = (Get-FileHash $zipPath -Algorithm SHA256).Hash.ToLower()
$zipPath
$hash
```

Expected ZIP naming:
- `TaskbarMediaControls-Plus-vX.Y.Z-portable.zip`

---

## 4) Create Tag + GitHub Release

Create and push tag:

```powershell
git tag -a $tag -m "Release $tag"
git push origin $tag
```

Create release and upload ZIP:

```powershell
gh release create $tag $zipPath `
  --repo $repo `
  --title "TaskbarMediaControls-plus $tag" `
  --notes "Portable release for $tag."
```

Important:
- Always pass `--repo $repo` so `gh` does not target upstream by mistake.

---

## 5) Update Scoop Manifest

File:
- `scoop/TaskbarMediaControls-Plus.json`

Update:
- `version` -> `X.Y.Z`
- `architecture.64bit.url` -> `https://github.com/zunaidFarouque/TaskbarMediaControls-Plus/releases/download/vX.Y.Z/TaskbarMediaControls-Plus-vX.Y.Z-portable.zip`
- `architecture.64bit.hash` -> SHA256 from step 3 (`$hash`)

---

## 6) Verify Release

```powershell
gh release view $tag --repo $repo --json url,tagName,assets
git status --short --branch
```

Check:
- Release URL exists
- Asset uploaded with expected filename
- Asset digest matches computed SHA256
- Git status only shows intended manifest change(s)

---

## 7) Optional Post-Release Commit (Scoop Manifest)

If you want this repo to track the new Scoop values:

```powershell
git add "./scoop/TaskbarMediaControls-Plus.json"
git commit -m "Update Scoop manifest for $tag portable release"
git push
```

---

## Quick One-Pass Script (Manual Use)

```powershell
$version = "1.0.3"; $tag = "v$version"; $repo = "zunaidFarouque/TaskbarMediaControls-Plus"
dotnet test "./TaskbarMediaControls.sln" -c Release
dotnet publish "./TaskbarMediaControls.csproj" -c Release
$publishDir = "./bin/Release/net8.0-windows10.0.19041.0/publish"
Copy-Item "./scripts/shortcut-manager.bat" "$publishDir/shortcut-manager.bat" -Force
$zipPath = "./bin/Release/TaskbarMediaControls-Plus-v$version-portable.zip"
if (Test-Path $zipPath) { Remove-Item $zipPath -Force }
Compress-Archive -Path "$publishDir/*" -DestinationPath $zipPath
$hash = (Get-FileHash $zipPath -Algorithm SHA256).Hash.ToLower()
git tag -a $tag -m "Release $tag"
git push origin $tag
gh release create $tag $zipPath --repo $repo --title "TaskbarMediaControls-plus $tag" --notes "Portable release for $tag."
$url = "https://github.com/zunaidFarouque/TaskbarMediaControls-Plus/releases/download/$tag/TaskbarMediaControls-Plus-$tag-portable.zip"
"ZIP: $zipPath"
"SHA256: $hash"
"Release tag: $tag"
```

