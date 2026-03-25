from PIL import Image
img = Image.open(r"D:\RemakeSoF\Assets\Art\Textures\gfx\misc\jk_drop.png")
print("Mode:", img.mode, "Size:", img.size)
w, h = img.size
print("Center:", img.getpixel((w // 2, h // 2)))
print("Corner:", img.getpixel((0, 0)))
has_alpha = "A" in img.mode
print("HasAlpha:", has_alpha)
if has_alpha:
    alpha = img.split()[-1]
    print("Alpha range:", alpha.getextrema())

img2 = Image.open(r"D:\RemakeSoF\Assets\Art\Textures\gfx\misc\jk_dirt_grey.png")
print("\njk_dirt_grey:")
print("Mode:", img2.mode, "Size:", img2.size)
w2, h2 = img2.size
print("Center:", img2.getpixel((w2 // 2, h2 // 2)))
print("Corner:", img2.getpixel((0, 0)))
has_alpha2 = "A" in img2.mode
print("HasAlpha:", has_alpha2)
if has_alpha2:
    alpha2 = img2.split()[-1]
    print("Alpha range:", alpha2.getextrema())
