"""Decode PNG (stdlib only), analyze, and whiten the two Sumeru textures
into tintable near-white/grayscale bases (alpha and shading preserved).

Multiply-tinted blocks need a bright neutral base texture; a colored base
would turn muddy when tinted (color multiply).
"""
import struct
import zlib
import sys


def read_png(path):
    data = open(path, 'rb').read()
    assert data[:8] == b'\x89PNG\r\n\x1a\n', 'not a png'
    pos = 8
    width = height = bitdepth = colortype = None
    idat = b''
    while pos < len(data):
        length = struct.unpack('>I', data[pos:pos + 4])[0]
        tag = data[pos + 4:pos + 8]
        chunk = data[pos + 8:pos + 8 + length]
        if tag == b'IHDR':
            width, height, bitdepth, colortype, _, _, interlace = struct.unpack('>IIBBBBB', chunk)
            assert bitdepth == 8 and colortype == 6 and interlace == 0, f'unsupported: depth={bitdepth} type={colortype}'
        elif tag == b'IDAT':
            idat += chunk
        pos += 12 + length
    raw = zlib.decompress(idat)
    stride = width * 4
    pixels = bytearray(height * stride)
    prev = bytearray(stride)
    p = 0
    for y in range(height):
        ftype = raw[p]
        p += 1
        line = bytearray(raw[p:p + stride])
        p += stride
        if ftype == 1:  # sub
            for i in range(4, stride):
                line[i] = (line[i] + line[i - 4]) & 0xFF
        elif ftype == 2:  # up
            for i in range(stride):
                line[i] = (line[i] + prev[i]) & 0xFF
        elif ftype == 3:  # average
            for i in range(stride):
                left = line[i - 4] if i >= 4 else 0
                line[i] = (line[i] + ((left + prev[i]) >> 1)) & 0xFF
        elif ftype == 4:  # paeth
            for i in range(stride):
                a = line[i - 4] if i >= 4 else 0
                b = prev[i]
                c = prev[i - 4] if i >= 4 else 0
                pa, pb, pc = abs(b - c), abs(a - c), abs(a + b - 2 * c)
                pred = a if (pa <= pb and pa <= pc) else (b if pb <= pc else c)
                line[i] = (line[i] + pred) & 0xFF
        pixels[y * stride:(y + 1) * stride] = line
        prev = line
    return width, height, pixels


def write_png(path, width, height, pixels):
    raw = b''.join(b'\x00' + bytes(pixels[y * width * 4:(y + 1) * width * 4]) for y in range(height))

    def chunk(tag, d):
        return struct.pack('>I', len(d)) + tag + d + struct.pack('>I', zlib.crc32(tag + d) & 0xFFFFFFFF)

    ihdr = struct.pack('>IIBBBBB', width, height, 8, 6, 0, 0, 0)
    open(path, 'wb').write(b'\x89PNG\r\n\x1a\n' + chunk(b'IHDR', ihdr)
                           + chunk(b'IDAT', zlib.compress(raw, 9)) + chunk(b'IEND', b''))


def analyze(name, width, height, pixels):
    count = 0
    rsum = gsum = bsum = 0
    maxluma = 0
    for y in range(height):
        for x in range(width):
            o = (y * width + x) * 4
            r, g, b, a = pixels[o:o + 4]
            if a > 40:
                count += 1
                rsum += r
                gsum += g
                bsum += b
                luma = 0.299 * r + 0.587 * g + 0.114 * b
                maxluma = max(maxluma, luma)
    if count == 0:
        print(f'{name}: fully transparent!')
        return
    print(f'{name}: {count} opaque px, avg RGB=({rsum // count},{gsum // count},{bsum // count}), maxLuma={maxluma:.0f}')
    return maxluma


def whiten(name, width, height, pixels, maxluma, target=245):
    # 去色为灰度并按最亮像素归一化到 target，保留明暗细节与透明形状
    scale = target / max(1.0, maxluma)
    for y in range(height):
        for x in range(width):
            o = (y * width + x) * 4
            r, g, b, a = pixels[o:o + 4]
            if a == 0:
                continue
            luma = min(255.0, (0.299 * r + 0.587 * g + 0.114 * b) * scale)
            v = int(luma)
            pixels[o] = pixels[o + 1] = pixels[o + 2] = v
    write_png(f'Phytomana/Assets/Textures/PhytoMana/{name}.png', width, height, pixels)
    print(f'{name}: whitened (scale={scale:.2f})')


for name in ['SumeruFlower', 'SumeruPetal']:
    path = f'Phytomana/Assets/Textures/PhytoMana/{name}.png'
    w, h, px = read_png(path)
    maxluma = analyze(name, w, h, px)
    if maxluma is not None and maxluma < 235:
        whiten(name, w, h, px, maxluma)
    else:
        print(f'{name}: already bright, no change')
