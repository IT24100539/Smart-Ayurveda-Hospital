"""Compress oversized PNGs and draw the leaf brand mark. No network."""

from __future__ import annotations

from pathlib import Path

from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[1]
MAX_KB = 200
MAX_WIDTH = 1280
IMAGE_DIRS = [
    ROOT / "mobile-patient" / "assets" / "images",
    ROOT / "web-staff" / "public" / "images",
]
BRAND_DIRS = [
    ROOT / "mobile-patient" / "assets" / "brand",
    ROOT / "web-staff" / "public" / "brand",
]


def compress_png(path: Path) -> None:
    size_kb = path.stat().st_size / 1024
    if size_kb <= MAX_KB:
        return
    image = Image.open(path)
    if image.width > MAX_WIDTH:
        height = int(image.height * MAX_WIDTH / image.width)
        image = image.resize((MAX_WIDTH, height), Image.Resampling.LANCZOS)
    if image.mode in {"P", "LA"}:
        image = image.convert("RGBA")
    quality = 80
    while quality >= 45:
        image.convert("RGB").save(path, format="JPEG", quality=quality, optimize=True)
        # Keep the .png filename so asset paths stay the same.
        if path.stat().st_size / 1024 <= MAX_KB:
            print(f"compressed {path.name} -> {path.stat().st_size / 1024:.1f} KB (q={quality})")
            return
        quality -= 8
    print(f"still large {path.name} -> {path.stat().st_size / 1024:.1f} KB")


def draw_leaf(path: Path, dark: bool) -> None:
    size = 256
    image = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    draw = ImageDraw.Draw(image)
    bg = (11, 21, 18, 255) if dark else (31, 111, 95, 255)
    leaf = (79, 179, 170, 255) if dark else (247, 243, 232, 255)
    vein = (6, 32, 28, 255) if dark else (31, 111, 95, 255)
    draw.rounded_rectangle((0, 0, size - 1, size - 1), radius=56, fill=bg)
    draw.ellipse((48, 36, 208, 236), fill=leaf)
    draw.line((128, 56, 128, 214), fill=vein, width=8)
    draw.arc((64, 72, 128, 150), start=200, end=340, fill=vein, width=6)
    draw.arc((128, 110, 196, 188), start=20, end=160, fill=vein, width=6)
    path.parent.mkdir(parents=True, exist_ok=True)
    image.save(path, format="PNG", optimize=True)
    print(f"wrote {path}")


def main() -> None:
    for folder in IMAGE_DIRS:
        if not folder.exists():
            continue
        for path in sorted(folder.glob("*.png")):
            compress_png(path)
    for folder in BRAND_DIRS:
        draw_leaf(folder / "leaf-mark.png", dark=False)
        draw_leaf(folder / "leaf-mark-dark.png", dark=True)


if __name__ == "__main__":
    main()
