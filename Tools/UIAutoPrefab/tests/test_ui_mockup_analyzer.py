import json
import tempfile
import unittest
from pathlib import Path

from PIL import Image, ImageDraw

from ui_mockup_analyzer import analyze_mockup


class UiMockupAnalyzerTests(unittest.TestCase):
    def test_analyze_mockup_exports_independent_png_assets(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            source = root / "RewardWindow.png"
            image = Image.new("RGBA", (100, 80), (20, 30, 40, 255))
            draw = ImageDraw.Draw(image)
            draw.rectangle((10, 12, 45, 31), fill=(200, 40, 40, 255))
            draw.rectangle((60, 50, 75, 65), fill=(40, 200, 80, 255))
            image.save(source)

            layout = analyze_mockup(
                source_path=source,
                output_dir=root / "out",
                module="Battle",
                window_name="RewardWindow",
            )

            self.assertEqual(layout["canvas"], {"width": 100, "height": 80})
            self.assertEqual(layout["module"], "Battle")
            self.assertEqual(layout["windowName"], "RewardWindow")
            self.assertEqual(len(layout["nodes"]), 2)
            self.assertEqual(layout["nodes"][0]["rect"], {"x": 10, "y": 12, "width": 36, "height": 20})
            self.assertEqual(layout["nodes"][0]["component"], "Image")
            self.assertEqual(layout["nodes"][0]["assetType"], "Png")
            self.assertFalse(layout["nodes"][0]["fullCanvasLayer"])
            sprite_path = root / "out" / layout["nodes"][0]["sprite"]
            self.assertTrue(sprite_path.exists())
            sprite = Image.open(sprite_path).convert("RGBA")
            self.assertEqual(sprite.size, (36, 20))
            self.assertEqual(sprite.getpixel((0, 0)), (200, 40, 40, 255))
            self.assertTrue((root / "out" / "ui_layout.json").exists())

    def test_analyze_mockup_applies_override_by_node_id(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            source = root / "RewardWindow.png"
            image = Image.new("RGBA", (80, 60), (0, 0, 0, 255))
            draw = ImageDraw.Draw(image)
            draw.rectangle((8, 8, 35, 25), fill=(255, 255, 255, 255))
            image.save(source)
            override = root / "ui_layout.override.json"
            override.write_text(
                json.dumps(
                    {
                        "nodes": {
                            "node_001": {
                                "name": "m_btnClaim",
                                "component": "UIButton",
                                "bind": True,
                                "nineSlice": True,
                            }
                        }
                    }
                ),
                encoding="utf-8",
            )

            layout = analyze_mockup(
                source_path=source,
                output_dir=root / "out",
                module="Battle",
                window_name="RewardWindow",
                override_path=override,
            )

            node = layout["nodes"][0]
            self.assertEqual(node["name"], "m_btnClaim")
            self.assertEqual(node["component"], "UIButton")
            self.assertTrue(node["bind"])
            self.assertTrue(node["nineSlice"])


if __name__ == "__main__":
    unittest.main()
