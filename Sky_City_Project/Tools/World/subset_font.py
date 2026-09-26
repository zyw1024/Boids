"""Build the small, redistributable menu font from Google Fonts Noto Sans SC.

Run with fonttools installed; pass the downloaded NotoSansSC[wght].ttf path.
Keep OFL.txt alongside the generated font. Add UI text to the source before rerunning.
"""
from pathlib import Path
import sys
from fontTools.ttLib import TTFont
from fontTools.varLib.instancer import instantiateVariableFont
from fontTools import subset

root = Path(__file__).resolve().parents[2]
sources = [root / "Assets/SkyCity/Runtime/UI" / name for name in ("SkyCityFlockMenu.cs", "SkyCityWfcMenu.cs", "SkyCityGardenMenu.cs")]
font = instantiateVariableFont(TTFont(sys.argv[1]), {"wght": 400}, inplace=True)
options = subset.Options()
options.name_IDs = [0, 1, 2, 3, 4, 5, 6, 13, 14]
subsetter = subset.Subsetter(options=options)
subsetter.populate(text="".join(source.read_text(encoding="utf-8") for source in sources) + "".join(chr(i) for i in range(32, 127)))
subsetter.subset(font)
for record in font["name"].names:
    names = {1: "Sky City UI", 2: "Regular", 3: "SkyCityUI-Regular", 4: "Sky City UI Regular", 6: "SkyCityUI-Regular"}
    if record.nameID in names:
        record.string = names[record.nameID].encode(record.getEncoding())
output = root / "Assets/SkyCity/Content/World/SkyCityUI.ttf"
font.save(output)
print(f"{output}: {output.stat().st_size:,} bytes")
