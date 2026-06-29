#!/usr/bin/env python3
"""Analyze a flat UI mockup image and emit a Unity prefab draft layout.

The output is intentionally conservative: it detects visible regions that differ
from the corner background color, draws each region as an independent local PNG
asset, and lets a human-provided override file assign UI semantics such as
buttons and text.
"""

from __future__ import annotations

import argparse
import json
from collections import deque
from pathlib import Path
from typing import Any

from PIL import Image


DEFAULT_MIN_COMPONENT_AREA = 24
DEFAULT_COLOR_DISTANCE = 18


def analyze_mockup(
    source_path: str | Path,
    output_dir: str | Path,
    module: str,
    window_name: str,
    override_path: str | Path | None = None,
    min_component_area: int = DEFAULT_MIN_COMPONENT_AREA,
    color_distance: int = DEFAULT_COLOR_DISTANCE,
) -> dict[str, Any]:
    source = Path(source_path)
    out_dir = Path(output_dir)
    assets_dir = out_dir / "assets"
    assets_dir.mkdir(parents=True, exist_ok=True)

    image = Image.open(source).convert("RGBA")
    width, height = image.size
    background = _estimate_background(image)
    mask = _build_foreground_mask(image, background, color_distance)
    components = _find_connected_components(mask, width, height, min_component_area)

    nodes: list[dict[str, Any]] = []
    for index, component_region in enumerate(sorted(components, key=lambda c: (c["box"][1], c["box"][0])), start=1):
        box = component_region["box"]
        left, top, right, bottom = box
        node_id = f"node_{index:03d}"
        asset_width = right - left + 1
        asset_height = bottom - top + 1
        fill = _dominant_color(image, component_region["pixels"])
        runs = _build_local_runs(component_region["pixels"], width, left, top)
        sprite_name = f"assets/{node_id}.png"
        _save_png_asset(out_dir / sprite_name, asset_width, asset_height, runs, fill)
        nodes.append(
            {
                "id": node_id,
                "name": f"auto_img{index:03d}",
                "component": "Image",
                "bind": False,
                "nineSlice": False,
                "assetType": "Png",
                "sprite": sprite_name,
                "slice": sprite_name,
                "fullCanvasLayer": False,
                "rect": {
                    "x": left,
                    "y": top,
                    "width": asset_width,
                    "height": asset_height,
                },
            }
        )

    layout: dict[str, Any] = {
        "version": 1,
        "source": str(source),
        "module": module,
        "windowName": window_name,
        "canvas": {"width": width, "height": height},
        "prefabPath": f"Assets/AssetRaw/UI/{module}/{window_name}.prefab",
        "spriteOutputPath": f"Assets/AssetRaw/UIRaw/Auto/{module}/{window_name}",
        "nodes": nodes,
    }

    if override_path:
        _apply_override(layout, Path(override_path))

    (out_dir / "ui_layout.json").write_text(
        json.dumps(layout, ensure_ascii=False, indent=2),
        encoding="utf-8",
    )
    return layout


def _estimate_background(image: Image.Image) -> tuple[int, int, int, int]:
    width, height = image.size
    points = [
        image.getpixel((0, 0)),
        image.getpixel((width - 1, 0)),
        image.getpixel((0, height - 1)),
        image.getpixel((width - 1, height - 1)),
    ]
    return tuple(round(sum(pixel[channel] for pixel in points) / len(points)) for channel in range(4))


def _build_foreground_mask(
    image: Image.Image,
    background: tuple[int, int, int, int],
    color_distance: int,
) -> list[bool]:
    get_pixels = getattr(image, "get_flattened_data", image.getdata)
    pixels = list(get_pixels())
    threshold_sq = color_distance * color_distance
    mask: list[bool] = []
    for pixel in pixels:
        if pixel[3] == 0:
            mask.append(False)
            continue
        distance_sq = sum((pixel[channel] - background[channel]) ** 2 for channel in range(4))
        mask.append(distance_sq > threshold_sq)
    return mask


def _find_connected_components(
    mask: list[bool],
    width: int,
    height: int,
    min_area: int,
) -> list[dict[str, Any]]:
    visited = [False] * len(mask)
    components: list[dict[str, Any]] = []

    for start, is_foreground in enumerate(mask):
        if not is_foreground or visited[start]:
            continue

        queue: deque[int] = deque([start])
        visited[start] = True
        area = 0
        min_x = width
        min_y = height
        max_x = 0
        max_y = 0
        pixels: list[int] = []

        while queue:
            current = queue.popleft()
            x = current % width
            y = current // width
            area += 1
            pixels.append(current)
            min_x = min(min_x, x)
            min_y = min(min_y, y)
            max_x = max(max_x, x)
            max_y = max(max_y, y)

            for nx, ny in ((x - 1, y), (x + 1, y), (x, y - 1), (x, y + 1)):
                if nx < 0 or ny < 0 or nx >= width or ny >= height:
                    continue
                neighbor = ny * width + nx
                if mask[neighbor] and not visited[neighbor]:
                    visited[neighbor] = True
                    queue.append(neighbor)

        if area >= min_area:
            components.append({"box": (min_x, min_y, max_x, max_y), "pixels": pixels})

    return components


def _dominant_color(image: Image.Image, pixels: list[int]) -> tuple[int, int, int, int]:
    source_pixels = image.load()
    width, _ = image.size
    counts: dict[tuple[int, int, int, int], int] = {}
    for index in pixels:
        x = index % width
        y = index // width
        color = source_pixels[x, y]
        counts[color] = counts.get(color, 0) + 1
    return max(counts.items(), key=lambda item: item[1])[0]


def _build_local_runs(
    pixels: list[int],
    image_width: int,
    left: int,
    top: int,
) -> list[tuple[int, int, int]]:
    rows: dict[int, list[int]] = {}
    for index in pixels:
        x = index % image_width
        y = index // image_width
        rows.setdefault(y - top, []).append(x - left)

    runs: list[tuple[int, int, int]] = []
    for y in sorted(rows):
        xs = sorted(rows[y])
        start = xs[0]
        previous = xs[0]
        for x in xs[1:]:
            if x == previous + 1:
                previous = x
                continue
            runs.append((y, start, previous - start + 1))
            start = x
            previous = x
        runs.append((y, start, previous - start + 1))
    return runs


def _save_png_asset(
    output_path: Path,
    width: int,
    height: int,
    runs: list[tuple[int, int, int]],
    fill: tuple[int, int, int, int],
) -> None:
    asset = Image.new("RGBA", (width, height), (0, 0, 0, 0))
    pixels = asset.load()
    for y, x, run_width in runs:
        for dx in range(run_width):
            pixels[x + dx, y] = fill
    asset.save(output_path)


def _apply_override(layout: dict[str, Any], override_path: Path) -> None:
    override = json.loads(override_path.read_text(encoding="utf-8"))

    for key in ("module", "windowName", "prefabPath", "spriteOutputPath"):
        if key in override:
            layout[key] = override[key]

    node_overrides = override.get("nodes", {})
    if isinstance(node_overrides, list):
        node_overrides = {node["id"]: node for node in node_overrides if "id" in node}

    for node in layout["nodes"]:
        patch = node_overrides.get(node["id"]) or node_overrides.get(node["name"])
        if not patch:
            continue
        for key, value in patch.items():
            if key != "id":
                node[key] = value
        if node.get("bind") and "name" not in patch and str(node.get("name", "")).startswith("auto_"):
            node["name"] = _default_bound_name(node)


def _default_bound_name(node: dict[str, Any]) -> str:
    suffix = str(node.get("id", "node")).replace("_", "").replace("-", "")
    component = node.get("component", "Image")
    if component == "UIButton":
        return f"m_btn{suffix}"
    if component in ("UIText", "UITextPlaceholder", "TextMeshProUGUI"):
        return f"m_tmp{suffix}"
    if component in ("Container", "List", "RectTransform"):
        return f"m_tf{suffix}"
    return f"m_img{suffix}"


def main() -> int:
    parser = argparse.ArgumentParser(description="Analyze a flat UI PNG into ui_layout.json.")
    parser.add_argument("source", help="Source mockup PNG.")
    parser.add_argument("--output", required=True, help="Directory for ui_layout.json and generated PNG assets.")
    parser.add_argument("--module", required=True, help="UI module name, e.g. Battle.")
    parser.add_argument("--window", required=True, help="Window or widget class/prefab name.")
    parser.add_argument("--override", help="Optional ui_layout.override.json path.")
    parser.add_argument("--min-area", type=int, default=DEFAULT_MIN_COMPONENT_AREA)
    parser.add_argument("--color-distance", type=int, default=DEFAULT_COLOR_DISTANCE)
    args = parser.parse_args()

    layout = analyze_mockup(
        source_path=args.source,
        output_dir=args.output,
        module=args.module,
        window_name=args.window,
        override_path=args.override,
        min_component_area=args.min_area,
        color_distance=args.color_distance,
    )
    print(json.dumps({"layout": str(Path(args.output) / "ui_layout.json"), "nodes": len(layout["nodes"])}, ensure_ascii=False))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
