"""Generate a locally openable HTML catalog of mod items and machines."""
import csv
import json
import struct
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
TEXTURES = ROOT / "Textures" / "Items"
REFERENCES = ROOT / "ArtSource" / "References" / "native"
OUTPUT = ROOT / "item-catalog.html"

TYPE_LABELS = {
    "MATERIAL": "材料",
    "BASIC_MATERIAL": "基础材料",
    "HOUSEHOLD_GOOD": "生活用品",
    "CHEMICAL_SUPPLIES": "化学品",
    "MEDICAL_SUPPLIES": "医疗用品",
    "FARMING_SUPPLIES": "农业用品",
    "OXYGEN_SUPPLY": "助燃气体",
    "MODULE": "模块",
    "STORAGE": "收纳",
    "WATER_PURIFICATION_SUPPLY": "净水用品",
    "LUXURY_ITEM": "奢侈品",
}
HAZARD_LABELS = {
    "TOXIC": "有毒",
    "CORROSIVE": "腐蚀",
    "FLAMMABLE": "易燃",
    "RADIOACTIVE": "放射性",
}
COPY_SPRITES = {
    "furnace_module_junk": "long_module3.png",
    "water_tablet": "water_tablet.png",
}


def read_json(path):
    return json.loads(path.read_text(encoding="utf-8"))


def read_locale(path):
    values = {}
    with path.open(encoding="utf-8-sig", newline="") as stream:
        for row in csv.reader(stream):
            if len(row) >= 2 and row[0] and not row[0].lstrip().startswith("#"):
                values[row[0]] = row[1]
    return values


def localized(values, key):
    return values.get(key, key)


def image_info(sprite, copy_sprite=None):
    if sprite:
        path = TEXTURES / f"{sprite}.png"
        relative = path.relative_to(ROOT).as_posix()
    else:
        reference = COPY_SPRITES.get(copy_sprite)
        if not reference:
            raise ValueError(f"No catalog image mapping for copied sprite {copy_sprite!r}")
        path = REFERENCES / reference
        relative = path.relative_to(ROOT).as_posix()
    width, height = struct.unpack(">II", path.read_bytes()[16:24])
    if width % 16 or height % 16:
        raise ValueError(f"{path.name} is not aligned to the 16 px inventory grid")
    return relative, f"{width // 16}×{height // 16}"


def item_category(item):
    types = item.get("itemTypes", [])
    if "STORAGE" in types:
        return "收纳"
    if "MODULE" in types:
        return "模块"
    if "LUXURY_ITEM" in types:
        return "奢侈品"
    if "WATER_PURIFICATION_SUPPLY" in types:
        return "净水用品"
    if "CHEMICAL_SUPPLIES" in types and "MEDICAL_SUPPLIES" in types:
        return "化学品 / 医疗"
    if "CHEMICAL_SUPPLIES" in types:
        return "化学品"
    if "MEDICAL_SUPPLIES" in types:
        return "医疗用品"
    if "FARMING_SUPPLIES" in types:
        return "农业用品"
    return "材料"


def item_details(item):
    details = []
    if item.get("gas"):
        details.extend((f"气体 {item['gas']}", "新购含量 100 克", "同种气体可堆叠合并"))
    if item.get("chemLiquid"):
        details.append(f"溶液包 {item.get('chemML', 0)} 毫升")
    if item.get("chemicalProductIngredient"):
        details.append("纯度随原料品质变化")
    if item.get("stackable") and not item.get("gas"):
        details.append("可堆叠")
    if item.get("ingot"):
        details.append("实际价值随纯度变化")
    if item.get("canSpawnInLoot"):
        details.append("可在物资中找到")
    if item.get("hazards"):
        details.extend(HAZARD_LABELS.get(hazard, hazard) for hazard in item["hazards"])
    if item["id"] == "elem_laboratory_bay":
        details.extend(("14×18 储物格", "12 个独立药品格", "药品格内的同类化学品可堆叠至 99 件"))
    return details


def item_entry(item, locale, module_names):
    image, footprint = image_info(item.get("sprite"), item.get("copySpriteFrom"))
    item_id = item["id"]
    price = f"每 100 克 {item['value']}" if item.get("gas") else f"{item['value']}"
    variants = []
    if item_id == "elem_laboratory_bay":
        variants = [
            {"image": f"Textures/Items/{item_id if state == 0 else item_id + '_' + str(state)}.png",
             "label": f"{state} 件" if state < 4 else "4+ 件"}
            for state in range(5)
        ]
    return {
        "kind": "物品",
        "id": item_id,
        "name": localized(locale, f"item.{item_id}.name"),
        "effect": localized(locale, f"item.{item_id}.desc"),
        "category": item_category(item),
        "types": [TYPE_LABELS.get(value, value) for value in item.get("itemTypes", [])],
        "module": module_names.get(item.get("module", ""), item.get("module", "")),
        "price": price,
        "priceValue": item["value"],
        "footprint": footprint,
        "image": image,
        "details": item_details(item),
        "variants": variants,
    }


def machine_details(machine, recipes):
    details = []
    if machine["archetype"] == "fuel_cell":
        details.extend(("初级违禁品", "氢燃料槽 · 可选氧化剂槽 · 产水容器槽"))
        return details

    recipe_ids = machine.get("recipeIds", [])
    costs = sorted({recipe["power"] for recipe in recipes if recipe["id"] in recipe_ids})
    if costs:
        cost = str(costs[0]) if len(costs) == 1 else f"{costs[0]}–{costs[-1]}"
        details.append(f"配方耗电 {cost} 点 / 批")
    if machine.get("inputWidth") and machine.get("inputHeight"):
        details.append(f"原料槽 {machine['inputWidth']}×{machine['inputHeight']}")
    if machine.get("outputWidth") and machine.get("outputHeight"):
        details.append(f"产物槽 {machine['outputWidth']}×{machine['outputHeight']}")
    if machine.get("waterSlot"):
        details.append("水槽")
    if machine.get("batterySlot"):
        details.append("电池槽")
    if machine.get("moduleBay"):
        details.append("模组槽")
    return details


def machine_entry(machine, locale, module_names, recipes):
    image, footprint = image_info(machine["sprite"])
    machine_id = machine["id"]
    manual = locale.get(f"machine.{machine_id}.manual", "").replace("\\n", "\n")
    return {
        "kind": "设备",
        "id": machine_id,
        "name": localized(locale, f"machine.{machine_id}.name"),
        "effect": localized(locale, f"machine.{machine_id}.desc"),
        "manual": manual,
        "category": "能源电池" if machine["archetype"] == "fuel_cell" else "机器",
        "types": ["能源" if machine["archetype"] == "fuel_cell" else "生产设备"],
        "module": module_names.get(machine.get("module", ""), machine.get("module", "")),
        "price": str(machine["value"]),
        "priceValue": machine["value"],
        "footprint": footprint,
        "image": image,
        "details": machine_details(machine, recipes),
        "variants": [],
    }


def render_page(entries):
    data = json.dumps(entries, ensure_ascii=False).replace("</", "<\\/")
    return PAGE.replace("__CATALOG_DATA__", data)


PAGE = r'''<!doctype html>
<html lang="zh-CN">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <title>无机化学基础 | 物品目录</title>
  <style>
    :root { color-scheme: light; --ink:#202b29; --muted:#687570; --line:#d9e0dd; --paper:#f2f5f3; --white:#fff; --green:#276c5d; --blue:#32758b; --amber:#a86a16; --red:#a7433e; }
    * { box-sizing:border-box; }
    body { margin:0; background:var(--paper); color:var(--ink); font:14px/1.5 "Segoe UI", "Microsoft YaHei", sans-serif; }
    header { background:var(--white); border-bottom:1px solid var(--line); }
    .header-inner, main { max-width:1480px; margin:0 auto; padding:18px 24px; }
    .header-inner { display:flex; align-items:end; justify-content:space-between; gap:18px; }
    h1 { margin:0; font-size:23px; line-height:1.25; font-weight:650; }
    .subtitle { color:var(--muted); margin-top:5px; }
    .totals { color:var(--muted); white-space:nowrap; font-variant-numeric:tabular-nums; }
    .controls { display:grid; grid-template-columns:minmax(200px,1fr) 170px 190px 190px; gap:10px; padding:14px 0 16px; }
    input, select { min-width:0; height:40px; border:1px solid #cbd5d1; border-radius:5px; padding:0 11px; background:white; color:var(--ink); font:inherit; }
    input:focus, select:focus { outline:2px solid #73aa9b; outline-offset:1px; }
    .result-line { display:flex; align-items:center; justify-content:space-between; margin:0 0 12px; color:var(--muted); }
    .catalog { display:grid; grid-template-columns:repeat(auto-fill,minmax(285px,1fr)); gap:12px; align-items:stretch; }
    .entry { min-width:0; background:var(--white); border:1px solid var(--line); border-radius:6px; overflow:hidden; }
    .entry-top { min-height:99px; display:grid; grid-template-columns:80px minmax(0,1fr); align-items:center; gap:13px; padding:12px; border-bottom:1px solid #e7ece9; }
    .image-stage { width:80px; height:74px; display:flex; align-items:center; justify-content:center; border:1px solid #e0e5e2; border-radius:4px; background-color:#f5f6f4; background-image:linear-gradient(45deg,#e8ece9 25%,transparent 25%),linear-gradient(-45deg,#e8ece9 25%,transparent 25%),linear-gradient(45deg,transparent 75%,#e8ece9 75%),linear-gradient(-45deg,transparent 75%,#e8ece9 75%); background-position:0 0,0 6px,6px -6px,-6px 0; background-size:12px 12px; }
    .image-stage img { width:64px; height:64px; object-fit:contain; image-rendering:pixelated; }
    .title-line { display:flex; align-items:start; justify-content:space-between; gap:8px; }
    h2 { margin:0; font-size:16px; line-height:1.3; font-weight:650; }
    .kind { flex:none; color:var(--green); background:#e8f2ee; border-radius:3px; padding:2px 6px; font-size:11px; }
    .entry.machine .kind { color:var(--blue); background:#eaf2f4; }
    code { display:block; margin-top:4px; color:#66726f; font:11px/1.3 Consolas, monospace; overflow-wrap:anywhere; }
    .tags { display:flex; flex-wrap:wrap; gap:5px; margin-top:7px; }
    .tag { border:1px solid #e1e6e3; border-radius:3px; padding:1px 5px; color:#596661; font-size:11px; }
    .entry-body { padding:11px 13px 13px; }
    .facts { display:flex; flex-wrap:wrap; gap:6px 13px; margin-bottom:8px; font-size:12px; }
    .facts b { color:var(--muted); font-weight:500; margin-right:3px; }
    .effect { margin:0; color:#303c38; }
    .details { display:flex; flex-wrap:wrap; gap:5px; margin-top:9px; }
    .detail { display:inline-block; border-radius:3px; padding:2px 6px; background:#f0f3f1; color:#4d5b56; font-size:11px; }
    .manual { margin-top:10px; border-top:1px solid #e7ece9; padding-top:8px; }
    .manual summary { color:var(--blue); cursor:pointer; font-size:12px; }
    .manual-text { margin:7px 0 0; color:#46534e; font-size:12px; white-space:pre-wrap; overflow-wrap:anywhere; }
    .detail.warning { background:#fbefeb; color:var(--red); }
    .variants { display:flex; gap:3px; align-items:center; margin:8px -1px 0; padding-top:8px; border-top:1px solid #e7ece9; }
    .variant { display:flex; min-width:0; flex:1; flex-direction:column; align-items:center; gap:1px; color:var(--muted); font-size:10px; }
    .variant img { width:42px; height:42px; object-fit:contain; image-rendering:pixelated; }
    .empty { grid-column:1/-1; padding:40px; text-align:center; color:var(--muted); background:var(--white); border:1px solid var(--line); border-radius:6px; }
    @media (max-width:850px) { .controls { grid-template-columns:1fr 1fr; } }
    @media (max-width:560px) { .header-inner, main { padding-left:14px; padding-right:14px; } .header-inner { align-items:start; flex-direction:column; gap:5px; } .controls { grid-template-columns:1fr; } .catalog { grid-template-columns:1fr; } }
  </style>
</head>
<body>
  <header>
    <div class="header-inner">
      <div><h1>无机化学基础</h1><div class="subtitle">物品与设备目录</div></div>
      <div class="totals" id="totals"></div>
    </div>
  </header>
  <main>
    <div class="controls">
      <input id="search" type="search" placeholder="搜索名称、编号或效果" aria-label="搜索名称、编号或效果">
      <select id="kind" aria-label="条目类型"><option value="">全部条目</option><option>物品</option><option>设备</option></select>
      <select id="category" aria-label="物品类别"><option value="">全部类别</option></select>
      <select id="sort" aria-label="排序"><option value="name">按名称</option><option value="price">价值从高到低</option><option value="footprint">占格从大到小</option></select>
    </div>
    <div class="result-line"><span id="result-count"></span><span>标价为基础物品价值；气体按 100 克计</span></div>
    <section id="catalog" class="catalog" aria-live="polite"></section>
  </main>
  <script>
    const entries = __CATALOG_DATA__;
    const catalog = document.getElementById("catalog");
    const search = document.getElementById("search");
    const kind = document.getElementById("kind");
    const category = document.getElementById("category");
    const sort = document.getElementById("sort");
    const totals = document.getElementById("totals");
    const resultCount = document.getElementById("result-count");
    const uniqueCategories = [...new Set(entries.map(entry => entry.category))].sort((a, b) => a.localeCompare(b, "zh-CN"));
    for (const label of uniqueCategories) {
      const option = document.createElement("option");
      option.value = label;
      option.textContent = label;
      category.append(option);
    }
    totals.textContent = `${entries.filter(entry => entry.kind === "物品").length} 件物品 · ${entries.filter(entry => entry.kind === "设备").length} 台设备`;

    function makeEntry(entry) {
      const card = document.createElement("article");
      card.className = `entry ${entry.kind === "设备" ? "machine" : "item"}`;
      const top = document.createElement("div");
      top.className = "entry-top";
      const stage = document.createElement("div");
      stage.className = "image-stage";
      const picture = document.createElement("img");
      picture.src = entry.image;
      picture.alt = `${entry.name} 贴图`;
      picture.loading = "lazy";
      stage.append(picture);
      const heading = document.createElement("div");
      const titleLine = document.createElement("div");
      titleLine.className = "title-line";
      const title = document.createElement("h2");
      title.textContent = entry.name;
      const kindBadge = document.createElement("span");
      kindBadge.className = "kind";
      kindBadge.textContent = entry.kind;
      titleLine.append(title, kindBadge);
      const id = document.createElement("code");
      id.textContent = entry.id;
      const tags = document.createElement("div");
      tags.className = "tags";
      for (const label of [...entry.types, entry.module]) {
        const tag = document.createElement("span");
        tag.className = "tag";
        tag.textContent = label;
        tags.append(tag);
      }
      heading.append(titleLine, id, tags);
      top.append(stage, heading);

      const body = document.createElement("div");
      body.className = "entry-body";
      const facts = document.createElement("div");
      facts.className = "facts";
      for (const [label, value] of [["价值", entry.price], ["占格", entry.footprint]]) {
        const fact = document.createElement("span");
        const name = document.createElement("b");
        name.textContent = `${label}：`;
        fact.append(name, document.createTextNode(value));
        facts.append(fact);
      }
      const effect = document.createElement("p");
      effect.className = "effect";
      effect.textContent = entry.effect;
      const details = document.createElement("div");
      details.className = "details";
      for (const label of entry.details) {
        const detail = document.createElement("span");
        detail.className = "detail" + (label.includes("违禁") || label.includes("有毒") || label.includes("腐蚀") ? " warning" : "");
        detail.textContent = label;
        details.append(detail);
      }
      body.append(facts, effect, details);
      if (entry.manual) {
        const manual = document.createElement("details");
        manual.className = "manual";
        const summary = document.createElement("summary");
        summary.textContent = "设备说明书";
        const text = document.createElement("p");
        text.className = "manual-text";
        text.textContent = entry.manual;
        manual.append(summary, text);
        body.append(manual);
      }
      if (entry.variants.length) {
        const variants = document.createElement("div");
        variants.className = "variants";
        for (const variant of entry.variants) {
          const cell = document.createElement("div");
          cell.className = "variant";
          const image = document.createElement("img");
          image.src = variant.image;
          image.alt = `${entry.name}：${variant.label}`;
          image.loading = "lazy";
          const caption = document.createElement("span");
          caption.textContent = variant.label;
          cell.append(image, caption);
          variants.append(cell);
        }
        body.append(variants);
      }
      card.append(top, body);
      return card;
    }

    function render() {
      const query = search.value.trim().toLocaleLowerCase("zh-CN");
      let visible = entries.filter(entry => {
        const matchesQuery = !query || [entry.name, entry.id, entry.category, entry.module, entry.effect, ...entry.types].join(" ").toLocaleLowerCase("zh-CN").includes(query);
        return matchesQuery && (!kind.value || entry.kind === kind.value) && (!category.value || entry.category === category.value);
      });
      if (sort.value === "price") visible.sort((a, b) => b.priceValue - a.priceValue || a.name.localeCompare(b.name, "zh-CN"));
      else if (sort.value === "footprint") visible.sort((a, b) => area(b.footprint) - area(a.footprint) || a.name.localeCompare(b.name, "zh-CN"));
      else visible.sort((a, b) => a.name.localeCompare(b.name, "zh-CN"));
      catalog.replaceChildren();
      resultCount.textContent = `显示 ${visible.length} / ${entries.length} 项`;
      if (!visible.length) {
        const empty = document.createElement("div");
        empty.className = "empty";
        empty.textContent = "没有匹配条目";
        catalog.append(empty);
      } else {
        catalog.append(...visible.map(makeEntry));
      }
    }

    function area(value) {
      const [width, height] = value.split("×").map(Number);
      return width * height;
    }
    search.addEventListener("input", render);
    kind.addEventListener("change", render);
    category.addEventListener("change", render);
    sort.addEventListener("change", render);
    render();
  </script>
</body>
</html>
'''


def main():
    locale = read_locale(ROOT / "Localization" / "zh.csv")
    modules = read_json(ROOT / "Data" / "modules.json")["modules"]
    module_names = {module["id"]: localized(locale, module["nameKey"]) for module in modules}
    items = read_json(ROOT / "Data" / "items.json")["items"]
    machines = read_json(ROOT / "Data" / "machines.json")["machines"]
    recipes = read_json(ROOT / "Data" / "recipes.json")["recipes"]
    entries = [item_entry(item, locale, module_names) for item in items]
    entries.extend(machine_entry(machine, locale, module_names, recipes) for machine in machines)
    OUTPUT.write_text(render_page(entries), encoding="utf-8", newline="\n")
    print(f"Wrote {len(items)} items and {len(machines)} machines to {OUTPUT}")


if __name__ == "__main__":
    main()
