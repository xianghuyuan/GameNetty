# BattleMainWindow HUD Asset Generation

## Source Of Truth

This manifest is retained as a prompt and resource-size reference only:

```text
.codex/plans/battle_main_window_hud.asset-manifest.json
```

The prompts in this manifest must follow the approved visual reference:

```text
.Codex/artifacts/battle-ui/效果图/battle_main_cream_single_baseline_mockup.png
```

## Deprecated Script Flow

Do not use Python scripts to generate BattleMainWindow UI art. The former
`Tools/ImageGen/generate_ui_assets.py` flow has been removed.

Formal assets must be generated through Codex `imagegen` or produced manually in
an art tool, then imported according to the resource sizes in the manifest and
art spec.

## Prompt Rule

Do not treat each asset as an independent decorative illustration. Each prompt
should describe a compact reusable UI slice derived from the approved single-baseline HUD mockup:

- keep the target silhouette close to the HUD component in the mockup
- keep decoration density low
- preserve empty runtime areas for fills, icons, buffs, and text
- avoid large moons, clouds, leaves, flowers, ribbons, and hanging tags unless
  the component in the mockup actually needs that silhouette
- output must be prepared as a transparent PNG and resized to the manifest size
- do not create or restore Python image-generation scripts for this workflow
