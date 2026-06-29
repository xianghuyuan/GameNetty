#!/usr/bin/env python3
"""Maintain BattleMainWindow layout assets and the single effect image.

The current workflow keeps one effect image per UI window, named after the
window itself. Intermediate previews, version suffixes, and rejected assets are
not preserved in the module output folder.
"""

from __future__ import annotations

import html
import json
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont


ROOT = Path(__file__).resolve().parents[2]
BUILD_DIR = ROOT / "spec/art/ui-auto-prefab/BattleMainWindow"
ASSET_DIR = BUILD_DIR / "assets"
LAYOUT_PATH = BUILD_DIR / "ui_layout.json"
STALE_PREVIEW_PATH = BUILD_DIR / "BattleMainWindow_preview.png"
STALE_EFFECT_SVG_PATH = BUILD_DIR / "BattleMainWindow_effect.svg"
EFFECT_IMAGE_PATH = BUILD_DIR / "BattleMainWindow.png"


DEMO = {
    "fill": "#2b2f3a",
    "fill2": "#3b4150",
    "stroke": "#d9d9d9",
    "accent": "#ff5a6a",
    "accent2": "#4cc9c0",
    "text": "#ffffff",
    "mask": "#111111",
}


ASSETS = {
    "hp_bar_bg": (416, 56, "bar_bg"),
    "hp_bar_fill": (352, 28, "fill_hp"),
    "hp_bar_frame": (416, 56, "bar_frame"),
    "boss_hp_bar_bg": (960, 72, "bar_bg"),
    "boss_hp_bar_fill": (880, 36, "fill_hp"),
    "boss_hp_bar_frame": (960, 72, "bar_frame"),
    "wave_panel_bg": (360, 96, "panel"),
    "hp_icon": (72, 72, "hp_icon"),
    "boss_name_plate": (360, 96, "label"),
    "emitter_rope": (760, 80, "line"),
    "emitter_card_bg": (160, 224, "card"),
    "emitter_card_selected": (176, 240, "card_selected"),
    "emitter_cooldown_mask": (128, 128, "cooldown"),
    "emitter_lock": (128, 128, "lock"),
    "buff_socket_small": (40, 40, "socket"),
    "buff_popup_bg": (380, 200, "panel"),
    "buff_socket_slot": (88, 88, "socket"),
    "buff_socket_add": (88, 88, "socket_add"),
    "buff_link_ribbon": (240, 48, "fill_energy"),
    "system_btn_plus": (72, 72, "btn_plus"),
    "system_btn_bookmark": (72, 72, "btn_bookmark"),
    "system_btn_pause": (72, 72, "btn_pause"),
}


def svg_rect(x: int, y: int, w: int, h: int, fill: str, stroke: str = "", sw: int = 0, rx: int = 0) -> str:
    stroke_part = f' stroke="{stroke}" stroke-width="{sw}"' if stroke and sw else ""
    return f'<rect x="{x}" y="{y}" width="{w}" height="{h}" rx="{rx}" fill="{fill}"{stroke_part}/>'


def svg_circle(cx: int, cy: int, r: int, fill: str, stroke: str = "", sw: int = 0) -> str:
    stroke_part = f' stroke="{stroke}" stroke-width="{sw}"' if stroke and sw else ""
    return f'<circle cx="{cx}" cy="{cy}" r="{r}" fill="{fill}"{stroke_part}/>'


def svg_line(x1: int, y1: int, x2: int, y2: int, stroke: str, sw: int = 2) -> str:
    return f'<line x1="{x1}" y1="{y1}" x2="{x2}" y2="{y2}" stroke="{stroke}" stroke-width="{sw}" stroke-linecap="round"/>'


def svg_text(x: float, y: float, text: str, size: int = 14, anchor: str = "middle") -> str:
    return (
        f'<text x="{x:.1f}" y="{y:.1f}" font-family="Arial, sans-serif" font-size="{size}" '
        f'text-anchor="{anchor}" dominant-baseline="middle" fill="{DEMO["text"]}">{html.escape(text)}</text>'
    )


def asset_svg(name: str, size: tuple[int, int], kind: str) -> str:
    w, h = size
    parts = [
        f'<svg xmlns="http://www.w3.org/2000/svg" width="{w}" height="{h}" viewBox="0 0 {w} {h}">',
        "<title>" + html.escape(name) + "</title>",
        '<rect width="100%" height="100%" fill="none"/>',
    ]

    if kind == "bar_bg":
        parts.append(svg_rect(4, 8, w - 8, h - 16, DEMO["fill"], DEMO["stroke"], 2, h // 4))
        parts.append(svg_rect(20, h // 2 - 9, w - 40, 18, "#111111", "", 0, 9))
    elif kind == "bar_frame":
        parts.append(svg_rect(4, 8, w - 8, h - 16, "none", DEMO["stroke"], 3, h // 4))
        parts.append(svg_line(20, h // 2, w - 20, h // 2, DEMO["stroke"], 1))
    elif kind == "fill_hp":
        parts.append(svg_rect(0, 0, w, h, DEMO["accent"], "", 0, h // 2))
    elif kind == "fill_energy":
        parts.append(svg_rect(0, 0, w, h, DEMO["accent2"], "", 0, h // 2))
    elif kind in {"panel", "label"}:
        parts.append(svg_rect(4, 4, w - 8, h - 8, DEMO["fill"], DEMO["stroke"], 2, 10))
        if kind == "label":
            parts.append(svg_line(36, h - 24, w - 36, h - 24, DEMO["stroke"], 1))
    elif kind == "hp_icon":
        parts.append(svg_circle(w // 2, h // 2, min(w, h) // 2 - 6, DEMO["fill"], DEMO["stroke"], 2))
        parts.append(svg_rect(w // 2 - 6, 18, 12, 36, DEMO["accent"], "", 0, 4))
        parts.append(svg_rect(18, h // 2 - 6, 36, 12, DEMO["accent"], "", 0, 4))
    elif kind == "line":
        parts.append(svg_line(20, h // 2, w - 20, h // 2, DEMO["stroke"], 4))
        for x in range(54, w, 86):
            parts.append(svg_circle(x, h // 2, 7, DEMO["stroke"]))
    elif kind in {"card", "card_selected"}:
        sw = 4 if kind == "card_selected" else 2
        parts.append(svg_rect(8, 8, w - 16, h - 16, DEMO["fill"], DEMO["stroke"], sw, 10))
        parts.append(svg_rect(22, 28, w - 44, h - 82, DEMO["fill2"], DEMO["stroke"], 1, 8))
    elif kind == "cooldown":
        parts.append(f'<path d="M {w // 2} {h // 2} L {w // 2} 4 A {w // 2 - 4} {h // 2 - 4} 0 1 1 4 {h // 2} Z" fill="{DEMO["mask"]}" opacity="0.65"/>')
    elif kind == "lock":
        parts.append(svg_rect(30, 56, 68, 52, DEMO["fill"], DEMO["stroke"], 3, 8))
        parts.append(f'<path d="M 42 58 V 42 A 22 22 0 0 1 86 42 V 58" fill="none" stroke="{DEMO["stroke"]}" stroke-width="8"/>')
    elif kind in {"socket", "socket_add"}:
        parts.append(svg_rect(6, 6, w - 12, h - 12, DEMO["fill"], DEMO["stroke"], 2, 6))
        if kind == "socket_add":
            parts.append(svg_line(w // 2, 24, w // 2, h - 24, DEMO["stroke"], 5))
            parts.append(svg_line(24, h // 2, w - 24, h // 2, DEMO["stroke"], 5))
    elif kind.startswith("btn_"):
        parts.append(svg_circle(w // 2, h // 2, min(w, h) // 2 - 6, DEMO["fill"], DEMO["stroke"], 2))
        if kind == "btn_plus":
            parts.append(svg_line(w // 2, 22, w // 2, h - 22, DEMO["stroke"], 6))
            parts.append(svg_line(22, h // 2, w - 22, h // 2, DEMO["stroke"], 6))
        elif kind == "btn_bookmark":
            parts.append(f'<path d="M 25 20 H 47 V 54 L 36 45 L 25 54 Z" fill="{DEMO["stroke"]}"/>')
        else:
            parts.append(svg_rect(24, 22, 8, 28, DEMO["stroke"], "", 0, 2))
            parts.append(svg_rect(40, 22, 8, 28, DEMO["stroke"], "", 0, 2))

    parts.append("</svg>")
    return "\n".join(parts)


def hex_rgb(value: str) -> tuple[int, int, int, int]:
    value = value.lstrip("#")
    alpha = int(value[6:8], 16) if len(value) >= 8 else 255
    return int(value[0:2], 16), int(value[2:4], 16), int(value[4:6], 16), alpha


def rounded_rect(draw: ImageDraw.ImageDraw, xy, radius: int, fill: str, outline: str = "", width: int = 1) -> None:
    draw.rounded_rectangle(xy, radius=radius, fill=hex_rgb(fill) if fill else None, outline=hex_rgb(outline) if outline else None, width=width)


def draw_asset_png(size: tuple[int, int], kind: str) -> Image.Image:
    w, h = size
    image = Image.new("RGBA", size, (0, 0, 0, 0))
    d = ImageDraw.Draw(image)

    if kind == "bar_bg":
        rounded_rect(d, (4, 8, w - 4, h - 8), h // 4, DEMO["fill"], DEMO["stroke"], 2)
        rounded_rect(d, (20, h // 2 - 9, w - 20, h // 2 + 9), 9, "#111111")
    elif kind == "bar_frame":
        rounded_rect(d, (4, 8, w - 4, h - 8), h // 4, "#00000000", DEMO["stroke"], 3)
        d.line((20, h // 2, w - 20, h // 2), fill=hex_rgb(DEMO["stroke"]), width=1)
    elif kind == "fill_hp":
        rounded_rect(d, (0, 0, w, h), h // 2, DEMO["accent"])
    elif kind == "fill_energy":
        rounded_rect(d, (0, 0, w, h), h // 2, DEMO["accent2"])
    elif kind in {"panel", "label"}:
        rounded_rect(d, (4, 4, w - 4, h - 4), 10, DEMO["fill"], DEMO["stroke"], 2)
        if kind == "label":
            d.line((36, h - 24, w - 36, h - 24), fill=hex_rgb(DEMO["stroke"]), width=1)
    elif kind == "hp_icon":
        d.ellipse((6, 6, w - 6, h - 6), fill=hex_rgb(DEMO["fill"]), outline=hex_rgb(DEMO["stroke"]), width=2)
        rounded_rect(d, (w // 2 - 6, 18, w // 2 + 6, 54), 4, DEMO["accent"])
        rounded_rect(d, (18, h // 2 - 6, 54, h // 2 + 6), 4, DEMO["accent"])
    elif kind == "line":
        d.line((20, h // 2, w - 20, h // 2), fill=hex_rgb(DEMO["stroke"]), width=4)
        for x in range(54, w, 86):
            d.ellipse((x - 7, h // 2 - 7, x + 7, h // 2 + 7), fill=hex_rgb(DEMO["stroke"]))
    elif kind in {"card", "card_selected"}:
        sw = 4 if kind == "card_selected" else 2
        rounded_rect(d, (8, 8, w - 8, h - 8), 10, DEMO["fill"], DEMO["stroke"], sw)
        rounded_rect(d, (22, 28, w - 22, h - 82), 8, DEMO["fill2"], DEMO["stroke"], 1)
    elif kind == "cooldown":
        d.pieslice((4, 4, w - 4, h - 4), 90, 360, fill=(17, 17, 17, 166))
    elif kind == "lock":
        rounded_rect(d, (30, 56, 98, 108), 8, DEMO["fill"], DEMO["stroke"], 3)
        d.arc((42, 20, 86, 72), 180, 360, fill=hex_rgb(DEMO["stroke"]), width=8)
    elif kind in {"socket", "socket_add"}:
        rounded_rect(d, (6, 6, w - 6, h - 6), 6, DEMO["fill"], DEMO["stroke"], 2)
        if kind == "socket_add":
            d.line((w // 2, 24, w // 2, h - 24), fill=hex_rgb(DEMO["stroke"]), width=5)
            d.line((24, h // 2, w - 24, h // 2), fill=hex_rgb(DEMO["stroke"]), width=5)
    elif kind.startswith("btn_"):
        d.ellipse((6, 6, w - 6, h - 6), fill=hex_rgb(DEMO["fill"]), outline=hex_rgb(DEMO["stroke"]), width=2)
        if kind == "btn_plus":
            d.line((w // 2, 22, w // 2, h - 22), fill=hex_rgb(DEMO["stroke"]), width=6)
            d.line((22, h // 2, w - 22, h // 2), fill=hex_rgb(DEMO["stroke"]), width=6)
        elif kind == "btn_bookmark":
            d.polygon([(25, 20), (47, 20), (47, 54), (36, 45), (25, 54)], fill=hex_rgb(DEMO["stroke"]))
        else:
            rounded_rect(d, (24, 22, 32, 50), 2, DEMO["stroke"])
            rounded_rect(d, (40, 22, 48, 50), 2, DEMO["stroke"])

    return image


def save_assets() -> None:
    ASSET_DIR.mkdir(parents=True, exist_ok=True)

    for name, (w, h, kind) in ASSETS.items():
        draw_asset_png((w, h), kind).save(ASSET_DIR / f"{name}.png")


def image_node(node_id, name, sprite, x, y, w, h, parent="", bind=False, nine=False, image_type="", fill=1.0, component="Image"):
    node = {
        "id": node_id,
        "parent": parent,
        "name": name,
        "component": component,
        "bind": bind,
        "nineSlice": nine,
        "assetType": "Png",
        "sprite": f"assets/{sprite}.png",
        "rect": {"x": x, "y": y, "width": w, "height": h},
    }
    if image_type:
        node["imageType"] = image_type
        node["fillAmount"] = fill
    return node


def text_node(node_id, name, x, y, w, h, parent, text, font_size=24):
    return {
        "id": node_id,
        "parent": parent,
        "name": name,
        "component": "UIText",
        "bind": True,
        "text": text,
        "fontSize": font_size,
        "rect": {"x": x, "y": y, "width": w, "height": h},
    }


def container_node(node_id, name, x, y, w, h, parent=""):
    return {
        "id": node_id,
        "parent": parent,
        "name": name,
        "component": "Container",
        "bind": name.startswith("m_"),
        "rect": {"x": x, "y": y, "width": w, "height": h},
    }


def build_layout() -> dict:
    nodes = [
        container_node("playerstatus", "playerstatus", 32, 140, 520, 136),
        image_node("hp_icon", "hp_icon", "hp_icon", 0, 8, 72, 72, "playerstatus"),
        image_node("player_hp_bg", "player_hp_bg", "hp_bar_bg", 84, 8, 416, 56, "playerstatus", nine=True),
        image_node("m_imgPlayerHp", "m_imgPlayerHp", "hp_bar_fill", 116, 22, 352, 28, "playerstatus", bind=True, image_type="Filled"),
        image_node("player_hp_frame", "player_hp_frame", "hp_bar_frame", 84, 8, 416, 56, "playerstatus", nine=True),
        text_node("m_tmpPlayerHp", "m_tmpPlayerHp", 156, 66, 230, 30, "playerstatus", "1200/1200", 22),
        image_node("m_imgPlayerEnergy", "m_imgPlayerEnergy", "buff_link_ribbon", 128, 100, 220, 24, "playerstatus", bind=True, image_type="Filled"),
        text_node("m_tmpControlMode", "m_tmpControlMode", 368, 96, 110, 32, "playerstatus", "Auto", 20),

        container_node("TopCenterStatus", "TopCenterStatus", 160, 20, 960, 156),
        image_node("top_long_bar", "top_long_bar", "boss_hp_bar_bg", 0, 64, 960, 72, "TopCenterStatus", nine=True),
        image_node("m_imgBossHp", "m_imgBossHp", "boss_hp_bar_fill", 40, 18, 880, 36, "top_long_bar", bind=True, image_type="Filled"),
        image_node("boss_hp_bar_frame", "boss_hp_bar_frame", "boss_hp_bar_frame", 0, 0, 960, 72, "top_long_bar", nine=True),
        image_node("boss_name_plate", "boss_name_plate", "boss_name_plate", 300, -60, 360, 54, "top_long_bar", nine=True),
        text_node("m_tmpBossName", "m_tmpBossName", 360, -48, 240, 28, "top_long_bar", "Boss Name", 24),
        text_node("m_tmpBossHp", "m_tmpBossHp", 378, 78, 220, 28, "top_long_bar", "5000/5000", 22),

        image_node("wave_panel_bg", "wave_panel_bg", "wave_panel_bg", 860, 196, 360, 96, "", nine=True),
        text_node("m_tmpWave", "m_tmpWave", 70, 32, 220, 32, "wave_panel_bg", "Wave 1/5", 28),

        container_node("m_tfEmitterBar", "m_tfEmitterBar", 280, 540, 720, 166),
        image_node("m_itemEmitterSlot", "m_itemEmitterSlot", "emitter_card_selected", 0, 0, 112, 150, "m_tfEmitterBar", bind=True, component="UIButton"),
        image_node("emitter_slot_02", "emitter_slot_02", "emitter_card_bg", 150, 10, 100, 140, "m_tfEmitterBar", component="UIButton"),
        image_node("emitter_slot_03", "emitter_slot_03", "emitter_card_bg", 300, 10, 100, 140, "m_tfEmitterBar", component="UIButton"),
        image_node("emitter_slot_04", "emitter_slot_04", "emitter_card_bg", 450, 10, 100, 140, "m_tfEmitterBar", component="UIButton"),
        image_node("emitter_slot_05", "emitter_slot_05", "emitter_card_bg", 600, 10, 100, 140, "m_tfEmitterBar", component="UIButton"),
        image_node("m_imgCooldown05", "m_imgCooldown05", "emitter_cooldown_mask", 618, 34, 64, 64, "m_tfEmitterBar", bind=True, image_type="Filled", fill=0.68),
        text_node("m_tmpCooldown05", "m_tmpCooldown05", 628, 52, 44, 28, "m_tfEmitterBar", "6.8", 18),
        image_node("emitter_rope", "emitter_rope", "emitter_rope", 0, 118, 720, 48, "m_tfEmitterBar", nine=True),
        image_node("buff_socket_01", "buff_socket_01", "buff_socket_small", 42, 136, 28, 28, "m_tfEmitterBar"),
        image_node("buff_socket_02", "buff_socket_02", "buff_socket_small", 186, 136, 28, 28, "m_tfEmitterBar"),
        image_node("buff_socket_03", "buff_socket_03", "buff_socket_small", 336, 136, 28, 28, "m_tfEmitterBar"),
        image_node("buff_socket_04", "buff_socket_04", "buff_socket_small", 486, 136, 28, 28, "m_tfEmitterBar"),
        image_node("buff_socket_05", "buff_socket_05", "buff_socket_small", 636, 136, 28, 28, "m_tfEmitterBar"),

        image_node("m_btnGear", "m_btnGear", "system_btn_plus", 1176, 34, 72, 72, "", bind=True, component="UIButton"),
        image_node("m_btnBookmark", "m_btnBookmark", "system_btn_bookmark", 1096, 34, 72, 72, "", bind=True, component="UIButton"),
        image_node("m_btnPause", "m_btnPause", "system_btn_pause", 1016, 34, 72, 72, "", bind=True, component="UIButton"),
    ]

    return {
        "version": 1,
        "source": "spec/art/战斗主界面资源需求.md",
        "module": "Battle",
        "windowName": "BattleMainWindow",
        "stage": "demo-svg",
        "canvas": {"width": 1280, "height": 720},
        "prefabPath": "Assets/AssetRaw/UI/Battle/BattleMainWindow.prefab",
        "spriteOutputPath": "Assets/AssetRaw/UIRaw/Atlas/Battle/battle_main",
        "nodes": nodes,
    }


def absolute_rects(layout: dict) -> dict[str, dict]:
    nodes = {node["id"]: node for node in layout["nodes"]}
    cache: dict[str, dict] = {}

    def resolve(node_id: str) -> dict:
        if node_id in cache:
            return cache[node_id]
        node = nodes[node_id]
        rect = dict(node["rect"])
        parent = node.get("parent") or ""
        if parent:
            parent_rect = resolve(parent)
            rect["x"] += parent_rect["x"]
            rect["y"] += parent_rect["y"]
        cache[node_id] = rect
        return rect

    for item_id in nodes:
        resolve(item_id)
    return cache


def load_font(size: int) -> ImageFont.ImageFont:
    for path in (
        "/System/Library/Fonts/PingFang.ttc",
        "/System/Library/Fonts/STHeiti Light.ttc",
        "/System/Library/Fonts/Supplemental/Arial Unicode.ttf",
    ):
        if Path(path).exists():
            return ImageFont.truetype(path, size)
    return ImageFont.load_default()


def draw_effect_image_text(draw: ImageDraw.ImageDraw, rect: dict, text: str, font_size: int) -> None:
    font = load_font(font_size)
    bbox = draw.textbbox((0, 0), text, font=font)
    width = bbox[2] - bbox[0]
    height = bbox[3] - bbox[1]
    x = rect["x"] + max(0, (rect["width"] - width) // 2)
    y = rect["y"] + max(0, (rect["height"] - height) // 2)
    draw.text((x, y), text, fill=hex_rgb(DEMO["text"]), font=font)


def generate_effect_image(layout: dict) -> None:
    canvas = layout["canvas"]
    image = Image.new("RGBA", (canvas["width"], canvas["height"]), (18, 18, 18, 255))
    rects = absolute_rects(layout)
    text_nodes = []

    for node in layout["nodes"]:
        component = node.get("component")
        if component == "Container":
            continue
        rect = rects[node["id"]]
        if component == "UIText":
            text_nodes.append((node, rect))
            continue
        sprite = node.get("sprite")
        if not sprite:
            continue
        asset = Image.open(BUILD_DIR / sprite).convert("RGBA").resize((rect["width"], rect["height"]), Image.Resampling.NEAREST)
        image.alpha_composite(asset, (rect["x"], rect["y"]))

    draw = ImageDraw.Draw(image)
    for node, rect in text_nodes:
        draw_effect_image_text(draw, rect, node.get("text", node["name"]), int(node.get("fontSize", 24)))

    image.convert("RGB").save(EFFECT_IMAGE_PATH)


def referenced_asset_names(layout: dict) -> set[str]:
    names: set[str] = set()
    for node in layout.get("nodes", []):
        sprite = node.get("sprite") or node.get("slice")
        if sprite:
            names.add(Path(sprite).name)
    return names


def clean_unreferenced_outputs(layout: dict) -> None:
    referenced = referenced_asset_names(layout)
    if ASSET_DIR.exists():
        for path in ASSET_DIR.glob("*.png"):
            if path.name not in referenced:
                path.unlink()

    for stale in (
        STALE_PREVIEW_PATH,
        BUILD_DIR / "BattleMainWindow_asset_preview.png",
        STALE_EFFECT_SVG_PATH,
    ):
        if stale.exists():
            stale.unlink()


def main() -> int:
    BUILD_DIR.mkdir(parents=True, exist_ok=True)
    if not LAYOUT_PATH.exists():
        raise FileNotFoundError("BattleMainWindow ui_layout.json is required: " + str(LAYOUT_PATH))
    if not EFFECT_IMAGE_PATH.exists():
        raise FileNotFoundError("BattleMainWindow baseline image is required: " + str(EFFECT_IMAGE_PATH))

    layout = json.loads(LAYOUT_PATH.read_text(encoding="utf-8"))
    clean_unreferenced_outputs(layout)
    print(json.dumps({
        "layout": str(LAYOUT_PATH),
        "effect_image": str(EFFECT_IMAGE_PATH),
        "png_assets": len(referenced_asset_names(layout)),
        "stage": layout.get("stage", "imagegen-png-assets"),
    }, ensure_ascii=False))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
