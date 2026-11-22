import sys
from PIL import Image, ImageDraw, ImageFilter, ImageChops
root, out = sys.argv[1], sys.argv[2]
src = Image.open(root + '/Assets/Resources/Art/UI/damage_numbers.png').convert('RGBA')
L = [33,232,403,587,770,957,1150,1342,1518,1705]
R = [217,383,579,756,950,1141,1321,1513,1689,1880]
TOP, H = 214, 300
glyphs = [src.crop((l, TOP, r+1, TOP+H)) for l, r in zip(L, R)]

def make_hyphen():
    W = 150
    m = Image.new('L', (W, H), 0); d = ImageDraw.Draw(m)
    # slanted brush bar, thick left end tapering right, centered at body middle
    cy = 150
    d.polygon([(28, cy-6), (40, cy-22), (122, cy-30), (128, cy-18), (118, cy+8), (36, cy+18), (26, cy+10)], fill=255)
    m = m.filter(ImageFilter.GaussianBlur(2)).point(lambda v: 255 if v > 110 else 0)
    outline = m.filter(ImageFilter.MaxFilter(27))
    shadow = ImageChops.offset(outline, 7, 7)
    img = Image.new('RGBA', (W, H), (0,0,0,0))
    img.paste((89,48,13,254), (0,0), shadow)
    img.paste((0,0,0,253), (0,0), outline)
    img.paste((253,243,220,254), (0,0), m)
    # faint highlight on top edge like digits
    hl = ImageChops.subtract(m, ImageChops.offset(m, 0, 4))
    img.paste((255,250,232,254), (0,0), hl)
    return img
glyphs.append(make_hyphen())

def gold(g):
    g = g.copy(); px = g.load(); w, h = g.size
    for y in range(h):
        t = min(1, max(0, (y - 70) / 170))
        top, bot = (255, 240, 196), (226, 160, 72)
        col = tuple(int(top[i] + (bot[i]-top[i]) * t) for i in range(3))
        for x in range(w):
            r, gg, b, a = px[x, y]
            if a > 100 and r > 150 and gg > 140:
                k = (r + gg + b) / (3 * 238)
                px[x, y] = tuple(min(255, int(c * k)) for c in col) + (a,)
    return g

def sheet(gs, name):
    gap = 20; W = sum(g.size[0] for g in gs) + gap * (len(gs) + 1)
    s = Image.new('RGBA', (W, H), (0,0,0,0)); x = gap; rects = []
    for g in gs:
        s.alpha_composite(g, (x, 0)); rects.append((x, g.size[0])); x += g.size[0] + gap
    s.save(f'{out}/{name}.png'); return rects

r1 = sheet(glyphs, 'stage_numbers_ivory')
r2 = sheet([gold(g) for g in glyphs], 'stage_numbers_gold')
print('rects (x,width), order 0-9,-:', r1)
