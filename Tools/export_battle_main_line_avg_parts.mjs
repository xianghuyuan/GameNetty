import fs from "node:fs/promises";
import path from "node:path";
import { createRequire } from "node:module";

const require = createRequire(import.meta.url);
const sharp = require("/Users/gxx/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/sharp");

const outDir = "Unity/Assets/AssetRaw/UIRaw/Atlas/Battle/battle_main";

const style = `
  .panel { fill: rgba(13, 20, 24, 0.72); stroke: #d8bd72; stroke-width: 2; }
  .panelSoft { fill: rgba(13, 20, 24, 0.58); stroke: #6f8791; stroke-width: 1.5; }
  .hpBg { fill: rgba(82, 39, 48, 0.9); }
  .hp { fill: #d84d45; }
  .energyBg { fill: rgba(39, 60, 76, 0.9); }
  .energy { fill: #5da5e8; }
  .button { fill: rgba(16, 25, 31, 0.82); stroke: #d8bd72; stroke-width: 2; }
  .danger { fill: rgba(160, 58, 54, 0.72); stroke: #ffd8cc; stroke-width: 1.5; }
  .nameplate { fill: rgba(216,189,114,0.9); }
`;

const assets = [
  {
    name: "line_player_panel",
    width: 250,
    height: 58,
    body: `<rect class="panelSoft" x="1" y="1" width="248" height="56" rx="9"/>`,
  },
  {
    name: "line_boss_panel",
    width: 500,
    height: 54,
    body: `<rect class="panel" x="1" y="1" width="498" height="52" rx="13"/>`,
  },
  {
    name: "line_wave_panel",
    width: 230,
    height: 58,
    body: `<rect class="panelSoft" x="1" y="1" width="228" height="56" rx="9"/>`,
  },
  {
    name: "line_danger_panel",
    width: 280,
    height: 32,
    body: `<rect class="danger" x="1" y="1" width="278" height="30" rx="8"/>`,
  },
  {
    name: "line_command_panel",
    width: 1204,
    height: 138,
    body: `<rect class="panel" x="1" y="1" width="1202" height="136" rx="15"/>`,
  },
  {
    name: "line_command_nameplate",
    width: 132,
    height: 34,
    body: `<rect class="nameplate" x="0" y="0" width="132" height="34" rx="8"/>`,
  },
  {
    name: "line_auto_button",
    width: 92,
    height: 44,
    body: `<rect class="panelSoft" x="1" y="1" width="90" height="42" rx="10"/>`,
  },
  {
    name: "line_pause_button",
    width: 58,
    height: 44,
    body: `<rect class="panelSoft" x="1" y="1" width="56" height="42" rx="10"/>`,
  },
  {
    name: "line_skill_slot",
    width: 72,
    height: 72,
    body: `<circle class="button" cx="36" cy="36" r="34"/>`,
  },
  {
    name: "line_hp_bar_bg",
    width: 130,
    height: 9,
    body: `<rect class="hpBg" x="0" y="0" width="130" height="9" rx="4.5"/>`,
  },
  {
    name: "line_hp_bar_fill",
    width: 130,
    height: 9,
    body: `<rect class="hp" x="0" y="0" width="130" height="9" rx="4.5"/>`,
  },
  {
    name: "line_boss_hp_bar_bg",
    width: 388,
    height: 9,
    body: `<rect class="hpBg" x="0" y="0" width="388" height="9" rx="4.5"/>`,
  },
  {
    name: "line_boss_hp_bar_fill",
    width: 388,
    height: 9,
    body: `<rect class="hp" x="0" y="0" width="388" height="9" rx="4.5"/>`,
  },
  {
    name: "line_energy_bar_bg",
    width: 130,
    height: 7,
    body: `<rect class="energyBg" x="0" y="0" width="130" height="7" rx="3.5"/>`,
  },
  {
    name: "line_energy_bar_fill",
    width: 130,
    height: 7,
    body: `<rect class="energy" x="0" y="0" width="130" height="7" rx="3.5"/>`,
  },
];

function svg(width, height, body) {
  return `
<svg width="${width}" height="${height}" viewBox="0 0 ${width} ${height}" xmlns="http://www.w3.org/2000/svg">
  <defs><style>${style}</style></defs>
  ${body}
</svg>`;
}

await fs.mkdir(outDir, { recursive: true });

for (const asset of assets) {
  const outPath = path.join(outDir, `${asset.name}.png`);
  await sharp(Buffer.from(svg(asset.width, asset.height, asset.body))).png().toFile(outPath);
  console.log(outPath);
}
