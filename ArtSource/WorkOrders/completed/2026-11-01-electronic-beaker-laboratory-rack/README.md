# Electronic Beaker and Laboratory Rack

The transparent masters in this work order were generated for the game's inventory art:

- `electronic-beaker-master.png`: an electrode-equipped glass beaker on a compact electronic base. It replaces the chemical-reactor appearance while keeping the existing machine ID and recipes.
- `laboratory-rack-0-items-master.png` through `laboratory-rack-3-items-master.png`: the empty rack and the 1-, 2-, and 3-item states.
- `laboratory-rack-master.png`: the current four-piece glassware composition, used for the 4+ item state.

The rack states keep the same 4x4 frame and are selected by direct inventory-entry count, matching the vanilla machine-bay state behavior. The runtime generator thresholds alpha, applies a shared crop so each state stays aligned, scales to 64x64, limits the palette, and writes exact 16x nearest-neighbor sources under `ArtSource/Items/16x/`.
