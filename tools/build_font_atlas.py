from pathlib import Path
import argparse
import re
import sys

ROOT = Path(__file__).resolve().parent
sys.path.insert(0, str(ROOT))
from rp6l import RP6L

parser = argparse.ArgumentParser(description="Correct Ukrainian Є glyph slots in GUI font atlases")
parser.add_argument("--font-dir", required=True, type=Path,
                    help="Directory containing rajdhani_*_pc.scr definitions")
parser.add_argument("--baseline-rpack", required=True, type=Path,
                    help="Clean matching gui_common_pc.rpack baseline")
parser.add_argument("--output", required=True, type=Path,
                    help="Output RPACK path")
args = parser.parse_args()

W, H = 1024, 512

def pal(a, b):
    p = [a, b]
    if a > b:
        p += [(6*a+b)//7, (5*a+2*b)//7, (4*a+3*b)//7,
              (3*a+4*b)//7, (2*a+5*b)//7, (a+6*b)//7]
    else:
        p += [(4*a+b)//5, (3*a+2*b)//5, (2*a+3*b)//5,
              (a+4*b)//5, 0, 255]
    return p

def decode(raw):
    out = bytearray(W * H)
    pos = 0
    for by in range(H // 4):
        for bx in range(W // 4):
            a, b = raw[pos], raw[pos + 1]
            bits = int.from_bytes(raw[pos + 2:pos + 8], "little")
            palette = pal(a, b)
            for yy in range(4):
                for xx in range(4):
                    out[(by*4+yy)*W + bx*4+xx] = palette[(bits >> (3*(yy*4+xx))) & 7]
            pos += 8
    return out

def encode_block(values):
    candidates = sorted(set(values) | {0, 255})
    best = None
    for a in candidates:
        for b in candidates:
            palette = pal(a, b)
            bits = 0
            error = 0
            for i, value in enumerate(values):
                index = min(range(8), key=lambda j: abs(palette[j] - value))
                delta = palette[index] - value
                error += delta * delta
                bits |= index << (3*i)
            candidate = (error, a, b, bits)
            if best is None or candidate < best:
                best = candidate
    _, a, b, bits = best
    return bytes((a, b)) + bits.to_bytes(6, "little")

def patch_rect(payload, pixels, x, y, w, h):
    x0, y0 = x // 4 * 4, y // 4 * 4
    x1, y1 = (x+w+3)//4*4, (y+h+3)//4*4
    raw = bytearray(payload[80:])
    blocks_per_row = W // 4
    for block_y in range(y0//4, y1//4):
        for block_x in range(x0//4, x1//4):
            values = [pixels[(block_y*4+yy)*W + block_x*4+xx]
                      for yy in range(4) for xx in range(4)]
            offset = (block_y*blocks_per_row + block_x) * 8
            raw[offset:offset+8] = encode_block(values)
    return payload[:80] + bytes(raw)

font_dir = args.font_dir
groups = {}

for path in sorted(font_dir.glob("rajdhani_*_pc.scr")):
    active_texture = None
    found = None
    for line_number, line in enumerate(path.read_text(encoding="utf-8-sig").splitlines(), 1):
        texture_match = re.search(r'Texture\("common_fonts_(\d+)_PC_uif\.dds"\)', line, re.I)
        if texture_match:
            active_texture = int(texture_match.group(1))
        char_match = re.search(r'Char\(1028,\s*(\d+),\s*(\d+),\s*(\d+),\s*(\d+),', line)
        if char_match:
            assert active_texture is not None
            w, h, x, y = map(int, char_match.groups())
            found = (active_texture, w, h, x, y)
            print("MAPPING", path.name, "line", line_number, "->", found)
    assert found is not None, path
    groups.setdefault(found, []).append(path.name)

# baseline.rpack is the post-game-update resource with all earlier atlas
# experiments reversed. Never use the previously patched proper.rpack as input.
rp = RP6L(args.baseline_rpack)
payloads, decoded = {}, {}
for texture in sorted({key[0] for key in groups}):
    name = f"common_fonts_{texture}_pc_uif.dds"
    payloads[texture] = rp.extract(rp.index(name))
    decoded[texture] = decode(payloads[texture][80:])

changed = set()
for (texture, w, h, x, y), names in sorted(groups.items()):
    pixels = decoded[texture]
    band = max(1, w // 3)
    middle_rows = range(h // 3, max(h // 3 + 1, (2 * h) // 3))
    middle_left = sum(pixels[(y+yy)*W + x+xx]
                      for yy in middle_rows for xx in range(band))
    middle_right = sum(pixels[(y+yy)*W + x+xx]
                       for yy in middle_rows for xx in range(w-band, w))
    if middle_left > middle_right:
        print("KEEP CORRECT", (texture, w, h, x, y), names,
              "middle", middle_left, middle_right)
        continue
    original = [bytes(pixels[(y+yy)*W+x:(y+yy)*W+x+w]) for yy in range(h)]
    # The shipped U+0404/U+042D shared slot contains Russian Э. Mirror the
    # actual slot horizontally to produce Ukrainian Є. U+042D is removed from
    # the character map in data0.pak, so this slot is dedicated to U+0404.
    for yy in range(h):
        for xx in range(w):
            pixels[(y+yy)*W + x+xx] = original[yy][w-1-xx]
    changed.add((texture, w, h, x, y))
    print("PATCH WRONG", (texture, w, h, x, y), names,
          "middle", middle_left, middle_right)

for texture, payload in payloads.items():
    patched = payload
    for group_texture, w, h, x, y in sorted(changed):
        if group_texture == texture:
            patched = patch_rect(patched, decoded[texture], x, y, w, h)
    rp.replace_same_size(rp.index(f"common_fonts_{texture}_pc_uif.dds"), patched)

output = args.output
rp.save(output)
print("SAVED", output, output.stat().st_size, "unique groups", len(groups))
