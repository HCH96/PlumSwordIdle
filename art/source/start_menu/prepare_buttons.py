"""시작 화면 버튼 시안(buttons_concept.png)에서 게임용 버튼 그림을 만든다.

원본은 고치지 않는다. 프로젝트 폴더에서 실행한다 (Pillow, numpy 필요):
    python art/source/start_menu/prepare_buttons.py

만드는 것
- Assets/Resources/Art/StartMenu/button_start.png    : 위쪽 판 ("게임 시작"), 배경 투명, 그림자 반투명
- Assets/Resources/Art/StartMenu/button_settings.png : 아래쪽 판 ("설정")
- art/source/start_menu/button_blank.png              : 아래쪽 판에서 글자를 지운 빈 판 (작업 파일)
- Assets/Resources/Art/StartMenu/button_continue.png : 빈 판 + "이어하기" (Noto Serif KR Black, OFL. 임시 글자)
세 버튼은 모두 1436x262 로 맞춘다 (확대·축소 없이 투명 여백만 더함).
"""
import os
import sys
from collections import deque

import numpy as np
from PIL import Image, ImageDraw, ImageFilter, ImageFont

SRC = 'art/source/start_menu/buttons_concept.png'
GAME = 'Assets/Resources/Art/StartMenu'
WORK = 'art/source/start_menu'
FONT = os.environ.get('CONTINUE_FONT', 'C:/Windows/Fonts/NotoSerifKR-VF.ttf')
SIZE = (1436, 262)
im = np.asarray(Image.open(SRC).convert('RGB')).astype(float)
bg = np.array([60.0, 40.0, 60.0])

def cut(r0, r1, c0, c1):
    sub = im[r0:r1, c0:c1]
    h, w, _ = sub.shape
    k = (sub @ bg) / (bg @ bg)               # brightness relative to bg
    err = np.abs(sub - k[..., None] * bg).sum(axis=2)
    bglike = (err < 14) & (k > 0.5) & (k < 1.1)
    # flood fill from border
    seen = np.zeros((h, w), bool)
    q = deque()
    for y in range(h):
        for x in (0, w - 1):
            if bglike[y, x] and not seen[y, x]: seen[y, x] = True; q.append((y, x))
    for x in range(w):
        for y in (0, h - 1):
            if bglike[y, x] and not seen[y, x]: seen[y, x] = True; q.append((y, x))
    while q:
        y, x = q.popleft()
        for dy, dx in ((1,0),(-1,0),(0,1),(0,-1)):
            ny, nx = y + dy, x + dx
            if 0 <= ny < h and 0 <= nx < w and bglike[ny, nx] and not seen[ny, nx]:
                seen[ny, nx] = True; q.append((ny, nx))
    alpha = np.ones((h, w))
    shade = np.clip(1 - k, 0, 1)
    alpha[seen] = np.where(k[seen] > 0.93, 0, shade[seen])
    rgb = sub.copy()
    rgb[seen] = 0
    out = np.dstack([rgb, alpha * 255]).round().clip(0, 255).astype(np.uint8)
    # trim to non-transparent bbox
    ys, xs = np.where(out[..., 3] > 0)
    out = out[ys.min():ys.max() + 1, xs.min():xs.max() + 1]
    return out, (r0 + ys.min(), c0 + xs.min())

start, so = cut(140, 440, 150, 1625)
sett, eo = cut(480, 785, 150, 1625)
print('start', start.shape, so, 'settings', sett.shape, eo)


def pad(arr):
    img = Image.fromarray(arr, 'RGBA') if isinstance(arr, np.ndarray) else arr
    out = Image.new('RGBA', SIZE, (0, 0, 0, 0))
    out.alpha_composite(img, ((SIZE[0] - img.width) // 2, (SIZE[1] - img.height) // 2))
    return out


os.makedirs(GAME, exist_ok=True)
pad(start).save(f'{GAME}/button_start.png')
pad(sett).save(f'{GAME}/button_settings.png')

# blank plate: the settings plate with its text area (source rows 529..726, cols 692..1078) repainted
# by interpolating each row between the clean columns left and right of the text.
plate = sett.astype(float)
oy, ox = eo
y0, y1, x0, x1 = 529 - oy, 727 - oy, 692 - ox, 1078 - ox
rgb = plate[..., :3]
filled = rgb.copy()
L = np.median(np.stack([rgb[y0:y1, x0 - 8:x0]], 0)[0], axis=1)   # (rows, 3)
R = np.median(np.stack([rgb[y0:y1, x1:x1 + 8]], 0)[0], axis=1)
t = np.linspace(0, 1, x1 - x0)[None, :, None]
interp = L[:, None, :] * (1 - t) + R[:, None, :] * t
# smooth vertically a little so single noisy rows don't streak
k = np.ones(5) / 5
interp = np.apply_along_axis(lambda v: np.convolve(np.pad(v, 2, mode='edge'), k, 'valid'), 0, interp)
feather = np.clip(np.minimum(np.arange(x1 - x0), np.arange(x1 - x0)[::-1]) / 10.0, 0, 1)[None, :, None]
filled[y0:y1, x0:x1] = rgb[y0:y1, x0:x1] * (1 - feather) + interp * feather
blank = plate.copy(); blank[..., :3] = filled
blank_img = Image.fromarray(blank.round().clip(0, 255).astype(np.uint8), 'RGBA')
blank_img.save(f'{WORK}/button_blank.png')

# continue button: blank plate + text
plate = blank_img.copy()
font = ImageFont.truetype(FONT, 150)
try:
    font.set_variation_by_name('Black')
except Exception as e:
    print('variation', e)
text = '이어하기'
W, H = 1100, 320
mask = Image.new('L', (W, H), 0)
d = ImageDraw.Draw(mask)
bb = d.textbbox((0, 0), text, font=font)
d.text(((W - (bb[2]-bb[0]))/2 - bb[0], (H - (bb[3]-bb[1]))/2 - bb[1]), text, font=font, fill=255)
# slight forward lean like the brush titles
shear = 0.12
mask = mask.transform((W, H), Image.AFFINE, (1, shear, -shear*H/2, 0, 1, 0), resample=Image.BICUBIC)
layer = Image.new('RGBA', (W, H), (0, 0, 0, 0))
shadow = Image.new('RGBA', (W, H), (18, 8, 20, 0))
sm = mask.filter(ImageFilter.GaussianBlur(3)).point(lambda v: int(v * 0.85))
shadow.putalpha(sm)
layer.alpha_composite(shadow, (7, 9))
# slight vertical tint: lighter top, pinker bottom
grad = np.linspace(0, 1, H)[:, None]
arr = np.zeros((H, W, 4), np.uint8)
arr[..., 0] = 252; arr[..., 1] = (226 - 22 * grad).astype(np.uint8); arr[..., 2] = (222 - 20 * grad).astype(np.uint8)
arr[..., 3] = np.asarray(mask)
layer.alpha_composite(Image.fromarray(arr, 'RGBA'))
cx = plate.width // 2 - 8
cy = (529 + 727) // 2 - 500 + 2
plate.alpha_composite(layer, (cx - W // 2, cy - H // 2))
pad(plate).save(f'{GAME}/button_continue.png')
