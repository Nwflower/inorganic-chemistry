from pathlib import Path
import re

path = Path(r"E:\SteamLibrary\steamapps\common\Probably Stolen\Probably Stolen_Data\Managed\Assembly-CSharp.dll")
data = path.read_bytes()
strings = set()
strings.update(s.decode("ascii", "ignore") for s in re.findall(rb"[ -~]{4,}", data))
strings.update(s.decode("utf-16le", "ignore") for s in re.findall(rb"(?:[ -~]\x00){4,}", data))
for value in sorted(strings):
    low = value.lower()
    if any(term in low for term in ("acid", "reducing", "reducer", "chem_base", "chemitem", "chemical")):
        if len(value) < 240:
            print(value)
