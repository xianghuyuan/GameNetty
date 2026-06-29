# Image Generation Rules

本目录不再提供项目正式生图脚本。

## 禁止项

- 禁止使用 Python 脚本生成正式 UI、背景、角色、怪物或其他美术素材。
- 禁止用代码绘制方式替代 imagegen 生图。
- 禁止恢复 `generate_ui_assets.py`、`generate_battle_formal_assets.py`、`generate_battle_scene_loop_assets.py`、`postprocess_battle_scene_imagegen_layers.py` 或同类脚本作为正式美术生产入口。
- 禁止用脚本批量请求图片接口并直接产出正式资源。

## 正式流程

正式美术素材必须通过 Codex `imagegen` 能力生成或由美术工具人工制作，然后再进入项目资源目录。

允许的脚本能力只限于非创作性处理，例如尺寸检查、文件命名检查、Unity `.meta` 检查、透明通道检查、纯色抠图底转 alpha 和资源引用检查。此类脚本不能绘制、合成或生成美术内容。
