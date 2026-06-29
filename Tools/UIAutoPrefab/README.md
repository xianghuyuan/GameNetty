# UI Auto Prefab

This workflow turns a flat PNG UI mockup into a Unity UI Prefab draft for
GameNetty. It is intentionally semi-automatic: the analyzer finds visual
regions, draws each region into an independent PNG asset, then
`ui_layout.override.json` supplies UI semantics that a flat PNG cannot contain.

## 1. Analyze a PNG

```bash
python3 Tools/UIAutoPrefab/ui_mockup_analyzer.py mockup.png \
  --output /tmp/BattleRewardWindowLayout \
  --module Battle \
  --window BattleRewardWindow
```

The analyzer writes:

- `/tmp/BattleRewardWindowLayout/ui_layout.json`
- `/tmp/BattleRewardWindowLayout/assets/*.png`

Each generated PNG is an independent local asset with its own pixel bounds.
The layout `rect` records where that asset should be placed in the final Unity
Prefab.

Detected nodes default to `auto_` names, so they do not become generated C#
fields unless an override marks them as bound.

## 2. Add Semantic Overrides

Create a `ui_layout.override.json` next to the mockup or layout work directory,
then rerun the analyzer with `--override`.

```json
{
  "nodes": {
    "node_001": {
      "component": "UIButton",
      "name": "m_btnClaim",
      "bind": true,
      "nineSlice": true
    },
    "node_002": {
      "component": "UITextPlaceholder",
      "name": "m_tmpTitle",
      "bind": true,
      "text": "奖励"
    },
    "node_003": {
      "component": "List",
      "name": "m_tfRewardItems",
      "bind": true
    }
  }
}
```

Supported component values:

- `Image`
- `UIButton`
- `UIText`, `UITextPlaceholder`, `TextMeshProUGUI`
- `Container`, `List`, `RectTransform`

## 3. Import in Unity

Open Unity and choose:

`Tools/GameNetty/UI Auto Prefab/Import Layout`

Select the generated `ui_layout.json`. The importer will:

- remove stale PNG assets from the configured sprite output folder
- copy current layout PNG assets to the configured sprite output folder
- create the Prefab at `Assets/AssetRaw/UI/<Module>/<WindowName>.prefab`
- create `RectTransform`, `Image`, `GameLogic.UIButton`, `GameLogic.UIText`
- fill `UIBindComponent` through the existing UIScriptGenerator path
- optionally generate binding code under the configured UI gen path

## Notes

- Single PNG input cannot reliably identify buttons, text, lists, or nine-slice
  rules. Use the override file for those decisions.
- Generated buttons use `GameLogic.UIButton`; generated click wiring uses
  `UIButton.SetClick(...)`.
- Business code should only bind data/events and reuse list item widgets. Do not
  create UI hierarchy in runtime code.

## BattleMainWindow Spec Generation

BattleMainWindow uses one approved PNG effect image as the visual baseline.
The deterministic script is only allowed to validate the layout workspace and
clean stale outputs; it must not generate or overwrite the effect image.

```bash
python3 Tools/UIAutoPrefab/generate_battle_main_spec_assets.py
```

The module output under `spec/art` is:

- `spec/art/ui-auto-prefab/BattleMainWindow/ui_layout.json`
- `spec/art/ui-auto-prefab/BattleMainWindow/assets/*.png`
- `spec/art/ui-auto-prefab/BattleMainWindow/BattleMainWindow.png`

Each UI window keeps exactly one effect image in its module folder. The file
name must match the window name, for example `BattleMainWindow.png`. Do not keep
`preview`, `asset_preview`, `effect`, `v2`, `v3`, or similar suffixed effect
images for the same UI.
`BattleMainWindow.png` is replaced only when a new effect image is approved.
Prefab layout, component structure, and referenced PNG sprites must then be
updated against that same baseline.

Current BattleMainWindow PNG assets are produced as project-local sprites under:

- `spec/art/ui-auto-prefab/BattleMainWindow/assets/`

Do not put deterministic generator outputs under
`Unity/Assets/AssetRaw/UIRaw/_Incoming`, and do not store generated resources
under `Tools/`. `_Incoming` is reserved for art production intake/review, while
`Tools/` is source code for generators. For BattleMainWindow, the Unity importer
copies PNG assets into the formal atlas folder
`Unity/Assets/AssetRaw/UIRaw/Atlas/Battle/battle_main/`.

That folder is current-layout output, not an archive. Process images, rejected
variants, old mockups, and stale sprites must not stay there. When the effect
image changes, update `ui_layout.json`, regenerate or replace the referenced
PNGs, rerun the importer, and let the importer remove PNG files that are no
longer referenced by the layout.
