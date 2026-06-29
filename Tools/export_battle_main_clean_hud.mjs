import fs from "node:fs/promises";
import { createRequire } from "node:module";

const require = createRequire(import.meta.url);
const sharp = require("/Users/gxx/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/sharp");

const svgPath = "docs/art/ui/BattleMainWindow_CleanHUD.svg";
const outPath = "Unity/Assets/AssetRaw/UIRaw/Atlas/Battle/battle_main/battle_main_clean_hud.png";

const svg = await fs.readFile(svgPath);
await sharp(svg).resize(1280, 720).png().toFile(outPath);
console.log(outPath);
