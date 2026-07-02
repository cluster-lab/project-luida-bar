# Packaging LUIDA as a Unity package

This folder (`Assets/ClusterMetaverseLab/LuidaExpTemplate/`) is the intended **package root**
(it's what `LuidaPaths.PackageRoot` resolves to). The goal is to ship it as a redistributable
Unity package via **git URL or release `.tgz`** (which installs read-only into
`Library/PackageCache/`).

## Already done (the package-readiness refactor)

- **All hardcoded asset paths go through `LuidaPaths`** (`Editor/Scripts/LuidaPaths.cs`). Package-internal
  assets resolve relative to a dynamically discovered package root, so they work under both `Assets/` and
  `Packages/`. User output stays under `Assets/_Experiment_/` (the consuming project, kept writable).
- **All KaomoLab CSCombiner access goes through `LuidaCombiner`** (`Editor/Scripts/LuidaCombiner.cs`).
- **KaomoLab is optional at compile time**: `LuidaDependencies` (`Editor/Scripts/`, `[InitializeOnLoad]`)
  toggles `LUIDA_HAS_CSCOMBINER` / `LUIDA_HAS_CSEMULATOR`; the KaomoLab-typed files are `#if`-guarded, so
  LUIDA compiles even when KaomoLab is absent. CSEmulator is fully optional.
- **Stray assets relocated into the package tree**: `Doc/`, `Fonts/`, and `Runtime/Resources/` (moved from
  `Assets/Doc`, `Assets/Fonts`, `Assets/Resources`; `Resources.Load` keys unchanged).
- **`LUIDA > Validate installation`** reports missing package assets + dependency status.

See `CLAUDE.md` → *Architecture → Packaging* for how those pieces fit together.

## Remaining manual steps

Do these in order; steps 1–2 require Unity and iteration (the headless tooling can't compile-check them).

### 1. Resolve the KaomoLab assembly situation (gating decision)
LUIDA's editor asmdef (step 2) cannot reference code in the predefined `Assembly-CSharp-Editor`, and
`ScriptableClusterScriptCombiner : CSCombiner` is a hard inheritance (can't be reflection-decoupled). So
KaomoLab's CSCombiner **must live in its own assembly definition**.
- In Unity, inspect `Assets/KaomoLab/CSCombiner/` and `Assets/KaomoLab/CSEmulator/`.
  - **If they ship `.asmdef`s** → note the assembly names; reference them in step 2. Done.
  - **If they don't** → add `.asmdef`s to the KaomoLab folders (a runtime + an editor one per tool), or
    keep a documented setup step instructing users to do so. Without this, LUIDA-in-an-asmdef won't compile
    when `LUIDA_HAS_CSCOMBINER` is on.

### 2. Add assembly definitions
- `Runtime/Luida.ExpTemplate.Runtime.asmdef`
- `Editor/Luida.ExpTemplate.Editor.asmdef` (`includePlatforms: ["Editor"]`, references the Runtime asmdef)
- References to add: CCK asmdefs `ClusterVR.CreatorKit.Item.Implements` (runtime — home of `JavaScriptAsset`),
  `ClusterVR.CreatorKit.Editor` + `ClusterVR.CreatorKit.Editor.EditorEvents` (editor); plus KaomoLab from step 1.
- ⚠️ Expect iteration: several `Runtime/Scripts/CSharp/` files use `AssetDatabase`/`EditorUtility` under
  `#if UNITY_EDITOR`. A runtime asmdef does not auto-reference `UnityEditor`, so move the editor-only ones
  (e.g. the guarded combiner files, gimmick editor logic) into the Editor asmdef and compile-check until clean.
- Adding asmdefs also makes `PackageInfo.FindForAssembly` (in `LuidaPaths`) return the real package path.

### 3. Add `package.json` (at this folder)
```jsonc
{
  "name": "com.cluster-lab.luida-exp-template",
  "version": "0.1.0",
  "displayName": "LUIDA Experiment Template",
  "description": "No-code/low-code editor tooling for building LUIDA Experiment Worlds on Cluster.",
  "unity": "2021.3",
  "dependencies": { "mu.cluster.cluster-creator-kit": "2.35.0" }
}
```
(CCK resolves via the `mu.cluster` scoped registry already in `Packages/manifest.json`. KaomoLab is **not** a
UPM package — document it as a manual prerequisite import from Booth.pm.)

### 4. Separate generated content & secrets
- Exclude `Assets/_Experiment_/` from the package — it's per-experiment **user output**, not payload.
- Move demo content (sample scenes/avatars/models) into a `Samples~` folder so it's opt-in, not always installed.
- Ship only a **blank** `ExpIdentifiers.js` template — never a filled-in one (it holds the verify token / endpoint ID).
- `LUIDA-AvatarSpawner.prefab` is intentionally **not shipped** (built from scratch at runtime); no action needed.

### 5. Distribute & test-install
- Distribute via git URL or a release `.tgz`.
- Install into a **clean** project (with CCK + KaomoLab) and run the verification below.

## Verification checklist
1. Open in Unity 2021.3.4f1 → allow the one-time recompile (the detector sets the defines) → Console clean →
   `LUIDA > Validate installation` passes (AvatarSpawner shows as *optional*).
2. Exercise generators (new/duplicate scene, state machine save, data collector save Builder+Code, assign-avatar,
   questionnaire, create state-listening item) → output lands in `Assets/_Experiment_/`, combine works.
3. Remove `Assets/KaomoLab/` → project still **compiles**, Validate reports "CSCombiner missing"; re-add → recovers.
4. Move/install the package under `Packages/` → repeat 1–2 to confirm dynamic root resolution off `Assets/`.
5. Confirm no writes land inside the package (read-only install): all generated output goes to `Assets/_Experiment_/`.
