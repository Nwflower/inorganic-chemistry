from pathlib import Path
import UnityPy

GAME = Path(r"E:\SteamLibrary\steamapps\common\Probably Stolen\Probably Stolen_Data")
OUT = Path(__file__).resolve().parents[3] / "References" / "native"
TERMS = ("acid", "reducing", "reducer", "chemical", "chem_", "bottle")
for asset in ("resources.assets", "sharedassets0.assets", "sharedassets1.assets"):
    env = UnityPy.load(str(GAME / asset))
    for obj in env.objects:
        if obj.type.name not in ("Sprite", "Texture2D"):
            continue
        data = obj.read()
        name = getattr(data, "m_Name", "")
        if not any(term in name.lower() for term in TERMS):
            continue
        print(asset, obj.type.name, name, obj.path_id)
        if obj.type.name == "Sprite":
            image = data.image
            target = OUT / f"{name}.png"
            image.save(target)
            print("saved", target, image.size)
