# Parallax_PaperTheaterForest

GPT image 2 generated parallax background assets for the side-scrolling battle scene.

All current PNG layers are normalized to `1920x1080` for the project target resolution. The source generations were proportionally cover-scaled and horizontally center-cropped, with no non-uniform stretching.

Transparency is processed for runtime compositing:

- `L0` remains opaque and should be the base sky/fog layer.
- `L1-L5` contain alpha channels for parallax stacking.
- Legacy opaque source backups were previously kept outside the Unity asset tree by the old image-generation workflow. This README is historical generated-scene metadata.

The layer intent follows `docs/功能设计/横版战斗背景视差设计.md`:

- `L0` sky and fog, `0.05x`
- `L1` distant backdrop, `0.15x`
- `L2` mid cardboard flats, `0.35x`
- `L3` near stage frame, `0.65x`
- `L4` battle runway, `1.00x`
- `L5` foreground edges, `1.20x-1.50x`

Current delivery contains `loop_01` for each layer. Add `loop_02` and `loop_03` after checking in-game seam quality and combat readability.
