"""시작 화면 꽃잎 원본(petals/petal_*.png)에서 게임용 꽃잎 그림을 만든다.

원본은 고치지 않는다. 프로젝트 폴더에서 실행한다 (Pillow, numpy 필요):
    python art/source/start_menu/prepare_petals.py

- 투명하지 않은 부분(알파 > 8)의 테두리 상자로 잘라 투명 여백 2px 을 남긴다.
- 긴 변이 64px 이 되도록 줄인다 (BOX). 원본 꽃잎의 픽셀 한 칸이 약 8px 이라 줄인 뒤 거의 1:1 이 된다.
  화면에서는 7~14px 로 보이고, 가져오기 설정은 시작 화면 그림과 같은 Trilinear·밉맵이다.
- Assets/Resources/Art/StartMenu/petal_open.png, petal_curled.png, petal_folded.png
"""
import os

import numpy as np
from PIL import Image

SRC = 'art/source/start_menu/petals'
GAME = 'Assets/Resources/Art/StartMenu'
NAMES = ('petal_open', 'petal_curled', 'petal_folded')
LONG_SIDE = 64
MARGIN = 2

os.makedirs(GAME, exist_ok=True)
for name in NAMES:
    image = Image.open(f'{SRC}/{name}.png').convert('RGBA')
    alpha = np.asarray(image)[..., 3]
    ys, xs = np.where(alpha > 8)
    petal = image.crop((xs.min(), ys.min(), xs.max() + 1, ys.max() + 1))
    scale = (LONG_SIDE - 2 * MARGIN) / max(petal.size)
    size = (max(1, round(petal.width * scale)), max(1, round(petal.height * scale)))
    # 알파를 곱한 색으로 줄여 투명한 가장자리에 검은 테두리가 생기지 않게 한다.
    small = petal.convert('RGBa').resize(size, Image.BOX).convert('RGBA')
    out = Image.new('RGBA', (size[0] + 2 * MARGIN, size[1] + 2 * MARGIN), (0, 0, 0, 0))
    out.alpha_composite(small, (MARGIN, MARGIN))
    out.save(f'{GAME}/{name}.png')
    print(name, image.size, '->', out.size)
