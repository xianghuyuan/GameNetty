import fs from "node:fs/promises";
import path from "node:path";
import { createRequire } from "node:module";

const require = createRequire(import.meta.url);
const sharp = require("/Users/gxx/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/sharp");

const outDir = "Unity/Assets/AssetRaw/UIRaw/Atlas/Battle/battle_main";

const style = `
  .panel { fill: #f6efd9; stroke: #c7aa64; stroke-width: 3; }
  .panel-soft { fill: #e9f3ef; stroke: #72a58c; stroke-width: 3; }
  .dark { fill: #2f3f49; stroke: #d6b35a; stroke-width: 3; }
  .danger { fill: #ffe1df; stroke: #d85d58; stroke-width: 3; }
  .bar-bg { fill: #51313a; }
  .hp { fill: #d75245; }
  .energy { fill: #5fa4e8; }
  .muted { fill: #e8dcc0; stroke: #b98b36; stroke-width: 3; }
`;

const assets = [
  {
    name: "hp_bar_bg",
    width: 416,
    height: 56,
    body: `<rect class="bar-bg" x="0" y="0" width="416" height="56" rx="28"/>`,
  },
  {
    name: "hp_bar_fill",
    width: 352,
    height: 28,
    body: `<rect class="hp" x="0" y="0" width="352" height="28" rx="14"/>`,
  },
  {
    name: "hp_bar_frame",
    width: 416,
    height: 56,
    body: `
      <rect class="panel" x="1.5" y="1.5" width="413" height="53" rx="18" fill="none"/>
      <circle cx="56" cy="28" r="22" fill="#2f3f49" stroke="#c7aa64" stroke-width="3"/>
    `,
  },
  {
    name: "boss_hp_bar_bg",
    width: 960,
    height: 72,
    body: `<rect class="bar-bg" x="0" y="0" width="960" height="72" rx="36"/>`,
  },
  {
    name: "boss_hp_bar_fill",
    width: 880,
    height: 36,
    body: `<rect class="hp" x="0" y="0" width="880" height="36" rx="18"/>`,
  },
  {
    name: "boss_hp_bar_frame",
    width: 960,
    height: 72,
    body: `
      <rect class="danger" x="1.5" y="1.5" width="957" height="69" rx="18" fill="none"/>
      <rect x="365" y="6" width="230" height="24" rx="12" fill="#f6efd9" stroke="#c7aa64" stroke-width="2"/>
    `,
  },
  {
    name: "wave_panel_bg",
    width: 360,
    height: 96,
    body: `
      <rect class="panel" x="1.5" y="1.5" width="357" height="93" rx="16"/>
      <rect x="30" y="28" width="82" height="40" rx="10" fill="#2f3f49" stroke="#c7aa64" stroke-width="2"/>
      <rect x="138" y="31" width="160" height="13" rx="6.5" fill="#d8caa8"/>
      <rect x="138" y="54" width="118" height="11" rx="5.5" fill="#d8caa8"/>
    `,
  },
  {
    name: "player_status_panel",
    width: 392,
    height: 132,
    body: `
      <rect class="panel" x="16" y="16" width="360" height="100" rx="14"/>
      <circle cx="64" cy="66" r="30" fill="#2f3f49" stroke="#c7aa64" stroke-width="3"/>
      <rect x="108" y="45" width="230" height="20" rx="10" fill="#d8caa8" opacity="0.55"/>
      <rect x="108" y="76" width="230" height="14" rx="7" fill="#d8caa8" opacity="0.45"/>
    `,
  },
  {
    name: "energy_bar_fill",
    width: 352,
    height: 20,
    body: `<rect class="energy" x="0" y="0" width="352" height="20" rx="10"/>`,
  },
  {
    name: "auto_battle_button",
    width: 232,
    height: 100,
    body: `
      <rect class="muted" x="16" y="16" width="200" height="68" rx="16"/>
      <circle cx="58" cy="50" r="18" fill="#2f3f49"/>
      <rect x="94" y="41" width="88" height="18" rx="9" fill="#d8caa8"/>
    `,
  },
  {
    name: "pause_button",
    width: 112,
    height: 100,
    body: `
      <rect class="muted" x="16" y="16" width="80" height="68" rx="14"/>
      <rect x="44" y="38" width="7" height="24" rx="3" fill="#25313a"/>
      <rect x="61" y="38" width="7" height="24" rx="3" fill="#25313a"/>
    `,
  },
  {
    name: "system_btn_pause",
    width: 80,
    height: 80,
    body: `
      <circle cx="40" cy="40" r="34" fill="#dfc17a" stroke="#59381d" stroke-width="5"/>
      <rect x="31" y="25" width="7" height="30" rx="3" fill="#332315"/>
      <rect x="43" y="25" width="7" height="30" rx="3" fill="#332315"/>
    `,
  },
  {
    name: "system_btn_plus",
    width: 80,
    height: 80,
    body: `
      <circle cx="40" cy="40" r="34" fill="#dfc17a" stroke="#59381d" stroke-width="5"/>
      <rect x="23" y="36" width="34" height="8" rx="4" fill="#332315"/>
      <rect x="36" y="23" width="8" height="34" rx="4" fill="#332315"/>
    `,
  },
  {
    name: "system_btn_bookmark",
    width: 80,
    height: 80,
    body: `
      <circle cx="40" cy="40" r="34" fill="#dfc17a" stroke="#59381d" stroke-width="5"/>
      <path d="M27 24 H53 V58 L40 49 L27 58 Z" fill="#332315"/>
    `,
  },
  {
    name: "skill_slot",
    width: 136,
    height: 136,
    body: `
      <circle class="dark" cx="68" cy="68" r="48"/>
      <circle cx="68" cy="68" r="32" fill="#22313a" stroke="#516672" stroke-width="2"/>
      <path d="M68 20 A48 48 0 0 1 116 68 L68 68 Z" fill="#000000" opacity="0.35"/>
    `,
  },
  {
    name: "buff_socket",
    width: 112,
    height: 112,
    body: `
      <rect class="dark" x="16" y="16" width="80" height="80" rx="16"/>
      <circle cx="56" cy="56" r="24" fill="#22313a" stroke="#516672" stroke-width="2"/>
    `,
  },
  {
    name: "secondary_popup_panel",
    width: 392,
    height: 142,
    body: `
      <rect class="panel-soft" x="16" y="16" width="360" height="110" rx="16"/>
      <rect x="40" y="40" width="82" height="62" rx="12" fill="#f6efd9" stroke="#72a58c" stroke-width="2"/>
      <rect x="155" y="40" width="82" height="62" rx="12" fill="#f6efd9" stroke="#72a58c" stroke-width="2"/>
      <rect x="270" y="40" width="82" height="62" rx="12" fill="#f6efd9" stroke="#72a58c" stroke-width="2"/>
    `,
  },
  {
    name: "danger_warning_banner",
    width: 442,
    height: 112,
    body: `
      <rect class="danger" x="16" y="16" width="410" height="80" rx="16"/>
      <rect x="44" y="41" width="354" height="30" rx="15" fill="#d85d58" opacity="0.35"/>
    `,
  },
];

await fs.mkdir(outDir, { recursive: true });

for (const asset of assets) {
  const svg = `
    <svg width="${asset.width}" height="${asset.height}" viewBox="0 0 ${asset.width} ${asset.height}" xmlns="http://www.w3.org/2000/svg">
      <defs><style>${style}</style></defs>
      ${asset.body}
    </svg>
  `;

  await sharp(Buffer.from(svg)).png().toFile(path.join(outDir, `${asset.name}.png`));
}

console.log(`Exported ${assets.length} battle_main HUD PNG assets to ${outDir}`);
