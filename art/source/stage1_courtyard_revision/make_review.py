from pathlib import Path
import sys
from PIL import Image, ImageDraw, ImageOps

root = Path(__file__).parent
version = sys.argv[1] if len(sys.argv) > 1 else 'v1'
img = Image.open(root / f'concept_{version}.png').convert('RGB')
half = img.resize((img.width // 2, img.height // 2), Image.Resampling.NEAREST)
strip = Image.new('RGB', (half.width * 3, half.height))
for i, tile in enumerate((ImageOps.mirror(half), half, ImageOps.mirror(half))):
    strip.paste(tile, (i * half.width, 0))
strip.save(root / f'mirror_check_{version}.png')

# CombatView: background height 18 units, center y=2.9; camera height 11.25.
height = 576
background = img.resize((round(img.width * height / img.height), height), Image.Resampling.NEAREST)
frame = Image.new('RGB', (640, 360))
frame.paste(background, ((640 - background.width) // 2, -201))
frame = frame.convert('RGBA')
refs = root / 'references'
for name, width, pivot_x, pivot_y, x in [
    ('disciple_idle.png', 76, 38, 12, 120),
    ('jongnam_male_idle.png', 68, 34, 11, 260),
    ('jongnam_female_idle.png', 76, 38, 12, 370),
]:
    sprite = Image.open(refs / name).convert('RGBA').crop((0, 0, width, 76))
    sprite = sprite.resize((round(width * 1.5), 114), Image.Resampling.NEAREST)
    frame.alpha_composite(sprite, (round(x - pivot_x * 1.5), round(228 - (76 - pivot_y) * 1.5)))
    draw = ImageDraw.Draw(frame)
    color = (51, 204, 77) if name.startswith('disciple') else (217, 51, 51)
    draw.rectangle((x - 16, 140, x + 15, 143), fill=(38, 38, 38))
    draw.rectangle((x - 15, 141, x + 14, 142), fill=color)
frame.convert('RGB').resize((1280, 720), Image.Resampling.NEAREST).save(root / f'combat_mockup_{version}.png')
