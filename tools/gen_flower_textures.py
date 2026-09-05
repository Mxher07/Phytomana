"""Generate 32x32 RGBA flower textures for the three new Phytomana generating flowers.

Pure stdlib (zlib+struct) PNG writer, pixel art composed from circles/stems.
"""
import zlib
import struct
import random

SIZE = 32


def write_png(path, pixels):
    """pixels: list of SIZE rows, each row a list of (r, g, b, a)."""
    raw = b''.join(b'\x00' + bytes(v for px in row for v in px) for row in pixels)

    def chunk(tag, data):
        return (struct.pack('>I', len(data)) + tag + data
                + struct.pack('>I', zlib.crc32(tag + data) & 0xFFFFFFFF))

    ihdr = struct.pack('>IIBBBBB', SIZE, SIZE, 8, 6, 0, 0, 0)
    png = (b'\x89PNG\r\n\x1a\n' + chunk(b'IHDR', ihdr)
           + chunk(b'IDAT', zlib.compress(raw, 9)) + chunk(b'IEND', b''))
    with open(path, 'wb') as f:
        f.write(png)


def blank():
    return [[(0, 0, 0, 0) for _ in range(SIZE)] for _ in range(SIZE)]


def put(px, x, y, color):
    if 0 <= x < SIZE and 0 <= y < SIZE:
        if len(color) == 3:
            color = (*color, 255)
        r, g, b, a = color
        if a == 0:
            return
        px[y][x] = (r, g, b, min(255, a))


def disc(px, cx, cy, r, color, jitter=6, seed=1):
    rng = random.Random(seed)
    rr = r * r
    for y in range(int(cy - r) - 1, int(cy + r) + 2):
        for x in range(int(cx - r) - 1, int(cx + r) + 2):
            d2 = (x - cx) ** 2 + (y - cy) ** 2
            if d2 <= rr:
                shade = 1.0 + rng.uniform(-jitter, jitter) / 100.0
                c = (max(0, min(255, int(color[0] * shade))),
                     max(0, min(255, int(color[1] * shade))),
                     max(0, min(255, int(color[2] * shade))), 255)
                put(px, x, y, c)


def stem(px, x0, y0, y1, color=(52, 118, 48)):
    for y in range(y0, y1 + 1):
        w = 1 if y < (y0 + y1) // 2 else 2
        for x in range(x0 - w + 1, x0 + w):
            put(px, x, y, color)


def leaf(px, cx, cy, rx, ry, color=(60, 140, 52)):
    for y in range(cy - ry, cy + ry + 1):
        for x in range(cx - rx, cx + rx + 1):
            if ((x - cx) / rx) ** 2 + ((y - cy) / ry) ** 2 <= 1.0:
                put(px, x, y, color)


def petals(px, cx, cy, ring, pr, color, count=6, seed=2):
    import math
    for i in range(count):
        ang = 2 * math.pi * i / count - math.pi / 2
        px_ = cx + ring * math.cos(ang)
        py_ = cy + ring * math.sin(ang)
        disc(px, px_, py_, pr, color, seed=seed + i)


def base_flower(head_cy, petal_color, center_color, stem_color=(52, 118, 48), seed=2):
    px = blank()
    stem(px, 16, head_cy + 2, 31, stem_color)
    leaf(px, 11, 24, 4, 2)
    leaf(px, 21, 27, 4, 2)
    return px


def nightshade():
    # 夜影花：暗紫花瓣 + 淡蓝花芯，深色调
    px = base_flower(12, (0, 0, 0), (0, 0, 0), seed=3)
    petals(px, 16, 11, 5, 4, (86, 48, 138), seed=21)
    disc(px, 16, 11, 3, (168, 214, 255), jitter=8, seed=30)
    return px


def gourmaryllis():
    # 暴食花：橙红大头 + 黄圈深口，像张嘴的食虫花
    px = base_flower(13, (0, 0, 0), (0, 0, 0), seed=4)
    disc(px, 16, 12, 8, (226, 118, 38), jitter=7, seed=40)
    disc(px, 16, 12, 5, (250, 196, 60), jitter=6, seed=41)
    disc(px, 16, 12, 3, (122, 34, 26), jitter=5, seed=42)
    return px


def thermalily():
    # 热力百合：熔岩红花瓣 + 亮黄花芯
    px = base_flower(12, (0, 0, 0), (0, 0, 0), stem_color=(96, 70, 40), seed=5)
    petals(px, 16, 11, 5, 4, (198, 58, 28), seed=51)
    disc(px, 16, 11, 3, (255, 178, 40), jitter=8, seed=52)
    return px


out = 'Phytomana/Assets/Textures/PhytoMana/'
write_png(out + 'Nightshade.png', nightshade())
write_png(out + 'Gourmaryllis.png', gourmaryllis())
write_png(out + 'Thermalily.png', thermalily())
print('textures written')
