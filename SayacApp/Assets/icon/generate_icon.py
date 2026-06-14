#!/usr/bin/env python3
"""Generate the Soonish app icon: a playful gradient squircle with a white hourglass.

A rounded-square badge filled with a teal→indigo diagonal gradient and a clean white
hourglass glyph ("time / happening soon"). Each size is rendered with supersampling for
smooth edges, then downsampled.

Outputs (into ../ , i.e. SayacApp/Assets/):
  - soonish.png   1024x1024 master
  - soonish.ico   Windows multi-res (16,24,32,48,64,128,256)
  - soonish.icns  macOS icon (via a generated .iconset + iconutil)

Run:  python3 generate_icon.py
Requires Pillow; iconutil (macOS) is used for the .icns step (skipped if absent).
"""

import os
import shutil
import subprocess
import tempfile

from PIL import Image, ImageDraw

TEAL = (45, 212, 191, 255)     # #2DD4BF
INDIGO = (129, 140, 248, 255)  # #818CF8
WHITE = (255, 255, 255, 255)

SS = 4  # supersample factor


def _mix(a, b, t):
    return tuple(round(a[i] + (b[i] - a[i]) * t) for i in range(4))


def render(size: int) -> Image.Image:
    s = size * SS
    img = Image.new("RGBA", (s, s), (0, 0, 0, 0))

    # Diagonal gradient via a 2x2 seed (TL=teal, BR=indigo, corners=midpoint) upscaled.
    mid = _mix(TEAL, INDIGO, 0.5)
    seed = Image.new("RGBA", (2, 2))
    seed.putpixel((0, 0), TEAL)
    seed.putpixel((1, 0), mid)
    seed.putpixel((0, 1), mid)
    seed.putpixel((1, 1), INDIGO)
    gradient = seed.resize((s, s), Image.BILINEAR)

    # Squircle mask (rounded rectangle).
    margin = round(0.05 * s)
    radius = round(0.26 * s)
    mask = Image.new("L", (s, s), 0)
    ImageDraw.Draw(mask).rounded_rectangle(
        [margin, margin, s - margin, s - margin], radius=radius, fill=255)
    img.paste(gradient, (0, 0), mask)

    # White hourglass glyph, centered.
    d = ImageDraw.Draw(img)
    cx = cy = s / 2.0
    hw = 0.165 * s       # half width of the frame bars
    hh = 0.215 * s       # half height
    bar = 0.05 * s       # cap bar thickness
    hw2 = 0.135 * s      # half width where the bulbs meet the bars
    r = round(0.018 * s)  # rounded bar corners

    top_bar = [cx - hw, cy - hh, cx + hw, cy - hh + bar]
    bot_bar = [cx - hw, cy + hh - bar, cx + hw, cy + hh]
    d.rounded_rectangle(top_bar, radius=r, fill=WHITE)
    d.rounded_rectangle(bot_bar, radius=r, fill=WHITE)
    # Upper bulb (wide at top, meeting at the center) and lower bulb (mirror).
    d.polygon([(cx - hw2, cy - hh + bar), (cx + hw2, cy - hh + bar), (cx, cy)], fill=WHITE)
    d.polygon([(cx - hw2, cy + hh - bar), (cx + hw2, cy + hh - bar), (cx, cy)], fill=WHITE)

    return img.resize((size, size), Image.LANCZOS)


def main() -> None:
    here = os.path.dirname(os.path.abspath(__file__))
    assets = os.path.normpath(os.path.join(here, ".."))

    master = render(1024)
    master.save(os.path.join(assets, "soonish.png"))
    print("wrote soonish.png (1024x1024)")

    ico_sizes = [256, 128, 64, 48, 32, 24, 16]
    ico_imgs = [render(n) for n in ico_sizes]
    ico_imgs[0].save(
        os.path.join(assets, "soonish.ico"),
        format="ICO",
        append_images=ico_imgs[1:],
    )
    print("wrote soonish.ico (%s)" % ", ".join(str(n) for n in sorted(ico_sizes)))

    if shutil.which("iconutil"):
        with tempfile.TemporaryDirectory() as tmp:
            iconset = os.path.join(tmp, "soonish.iconset")
            os.makedirs(iconset)
            entries = [
                ("icon_16x16.png", 16), ("icon_16x16@2x.png", 32),
                ("icon_32x32.png", 32), ("icon_32x32@2x.png", 64),
                ("icon_128x128.png", 128), ("icon_128x128@2x.png", 256),
                ("icon_256x256.png", 256), ("icon_256x256@2x.png", 512),
                ("icon_512x512.png", 512), ("icon_512x512@2x.png", 1024),
            ]
            cache: dict[int, Image.Image] = {}
            for name, px in entries:
                if px not in cache:
                    cache[px] = render(px)
                cache[px].save(os.path.join(iconset, name))
            subprocess.run(
                ["iconutil", "-c", "icns", iconset,
                 "-o", os.path.join(assets, "soonish.icns")],
                check=True,
            )
        print("wrote soonish.icns")
    else:
        print("iconutil not found — skipping soonish.icns")


if __name__ == "__main__":
    main()
