import sys
from PIL import Image, ImageDraw, ImageFont
root, S = sys.argv[1], sys.argv[2]
A = root + '/art/source/stage_label/'
rects = [(20,185),(225,152),(397,177),(594,170),(784,181),(985,185),(1190,172),(1382,172),(1574,172),(1766,176),(1962,150)]
gold = Image.open(A + 'stage_numbers_gold.png').convert('RGBA')
def label(text, scale):
    gs = [gold.crop((rects[10 if c == '-' else int(c)][0], 0, sum(rects[10 if c == '-' else int(c)]), 300)) for c in text]
    W = sum(g.size[0] for g in gs) - 18 * (len(gs) - 1)
    img = Image.new('RGBA', (W, 300)); x = 0
    for g in gs: img.alpha_composite(g, (x, 0)); x += g.size[0] - 18
    img = img.crop((0, 8, W, 298))
    return img.resize((int(img.size[0] * scale), int(img.size[1] * scale)), Image.LANCZOS)
def trimmed(path, h):
    im = Image.open(path).convert('RGBA'); im = im.crop(im.split()[3].point(lambda v: 255 if v > 16 else 0).getbbox())
    return im.resize((int(im.size[0] * h / im.size[1]), h), Image.LANCZOS)
def place(base, lab, cx, cy):
    base.alpha_composite(lab, (int(cx - lab.size[0] / 2), int(cy - lab.size[1] / 2)))
hdr = Image.open(A + 'header_blank.png').convert('RGBA')
plq = Image.open(root + '/Assets/Resources/Art/UI/Substage/intro_plaque.png').convert('RGBA')
f = ImageFont.truetype('C:/Windows/Fonts/malgunbd.ttf', 26)
cols = [('일반 구간 제목판', 0), ('보스 구간 제목판', 1), ('구간 시작 패 (보스)', 2)]
cw = [520, 520, 640]; rowh = 200
canvas = Image.new('RGBA', (sum(cw) + 40, 5 * rowh + 50), (58, 44, 66, 255)); d = ImageDraw.Draw(canvas)
x = 10
for name, _ in cols: d.text((x, 10), name, font=f, fill=(255, 240, 160)); x += cw[_] + 10
for s in range(1, 6):
    title = trimmed(A + f'title_{s}.png', 78)
    boss_h = trimmed(A + f'boss_name_{s}.png', 118)
    boss_p = trimmed(A + f'boss_name_{s}.png', 175)
    a = hdr.copy(); place(a, title, 768, 176)
    if s < 5: place(a, label(f'{s}-1', 0.50), 768, 363)
    else: a = None
    b = hdr.copy(); place(b, title, 768, 176); place(b, boss_h, 768, 363)
    c = plq.copy(); place(c, boss_p, 1086, 395)
    x = 10; y = 50 + (s - 1) * rowh
    for i, img in enumerate([a, b, c]):
        if img is not None:
            w = cw[i]; im = img.resize((w, int(w * img.size[1] / img.size[0])), Image.LANCZOS)
            canvas.alpha_composite(im, (x, y))
        else:
            d.text((x + 20, y + 70), '5스테이지는 보스 구간만', font=f, fill=(200, 200, 200))
        x += cw[i] + 10
canvas.save(S + '/stage_label_composite.png')
# number plaque samples
n = Image.new('RGBA', (1300, 230), (58, 44, 66, 255))
for i, t in enumerate(['1-1', '4-6']):
    c = plq.copy(); place(c, label(t, 0.78), 1086, 395)
    n.alpha_composite(c.resize((640, 213), Image.LANCZOS), (10 + i * 650, 8))
n.save(S + '/stage_number_plaque.png')
