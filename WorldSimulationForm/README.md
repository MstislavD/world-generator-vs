# WorldSimulationForm

WinForms app (net9.0-windows) that visualizes and simulates worlds from two world generators.
The app starts with the **legacy generator** form; press **F12** to switch to the **new generator** form (and back).

## Running

```
dotnet run --project WorldSimulationForm
```

(or open `WorldSimulator.sln` and run the `WorldSimulationForm` project)

Entry point: [Program.cs](Program.cs) → `LegacyWorldSimulatorForm`.

## Main window layout

- **Parameters panel** — left side, ~5% of window width, top-down flow.
- **Map area** — the rest of the window (5 px margin), where the world bitmap is drawn.
- The window opens maximized and double-buffered; all UI is built in code (no designer files).

## Forms

### LegacyWorldSimulatorForm (default)

Full-featured form for `WorldGeneratorLegacy`: multi-level hex grids → subregions/regions,
history simulation (races, pops, events), plus the Log and Paedia side windows.

### NewWorldSimulatorForm

Minimal form for the new `WorldGenerator`: layered hex grids only. Map mode is fixed to
Elevation; there are no subregions, regions or history — those members throw in
[GeneratorAdapter.cs](GeneratorAdapter.cs) and are not exposed by this form's UI.

## Parameters panel

### Legacy form (top → bottom)

1. **Start** (button) — generates a new world with a fresh random seed; also clears the Log
   window and re-initializes Paedia. Same as pressing `R`.
2. **Grid level** (combo, 0…`GridLevels`, default finest) — re-renders the map at that grid
   resolution; does not regenerate.
3. **Map mode** (combo, default *Biomes*) — Elevation / Height / Temperature / Precipitation /
   Biomes / Pops / Cells / Landmasses; re-renders only.
4. **SRegion borders** (checkbox, off) — draws subregion borders (plus vertex dots above 4× zoom).
5. **Texture** (combo) — *Color* / *Texture* / *Texture Imp* biome rendering style for Biomes mode.
6. **New seed** (checkbox, on) — when a generator parameter changes: on → regenerate with a fresh
   random seed; off → re-run generation with the same seed.
7. **Generator parameters** — any change regenerates the world (subject to *New seed*):
   - *Seeds* (read-only text boxes, click = new random value): Main Seed, Subregion Seed,
     Deformation Seed, Height Seed, Precipitation Seed
   - *Deformation*: Frequency Min/Max (1–500), Strength Min/Max, Number of Deformations (1–8),
     Detailed, Detalization (1–50)
   - *Sea/land balance*: Deep Sea % (0.3–0.9), Rise Elevation %, Lower Elevation %, Island %,
     Ridge %, Ridge Clear %
   - *Regions*: Uniform Subregion Size, Region Smoothing, Deform (checkboxes)
   - *Climate & precipitation*: Temp Smoothing, Precipitation Smoothing Steps (0–4),
     Precipitation Smoothing Inertia (1–8), "Precipiation Swaps %" (sic)
   - *World shape*: Map Script (Random / One / Two / Three continents), Land Size
     (Tiny…Colossal), Hemispheres (Two / North / South), Climate (Balanced / Dry / Wet)
   - *Rivers*: River %, Tributary Threshold
8. **Log** (button) — toggles the Log window.
9. **Paedia** (button) — toggles the Paedia window.
10. **Next Event** (button) — advances history by 1 event (Ctrl = 10, Shift = 100, Alt = 1000);
    label becomes `Next (<turn>)` after the first event; disabled when the history is finished.
    In Pops mode the current event is drawn as a migration arrow (or a red border around the
    origin region when it has no destination).
11. **Test** (button) — draws a one-shot developer test image in the map area (see below).
12. **Info** (label) — mouse readout: cursor coordinates + hovered-region details.

### New form (top → bottom)

Start → **Grid level** (combo, 0…`GridLevels-1`, default finest) → **New seed** (checkbox, on)
→ **Sea to land** (checkbox, on — re-runs the sea-to-land pass during generation) → Test → Info.

## Map rendering

The map is rendered once per state change into a cached bitmap (`HexGridRenderer`) and blitted in
`OnPaint`; hover overlays are drawn on top and disposed after each paint.

Map modes (legacy form):

- **Elevation** — color per elevation band (deep ocean → shallow ocean → lowland → upland →
  highland → mountain); region borders or coastline; ridges in dark red.
- **Height** — continuous height/depth color interpolation with rivers; the finest grid level is
  rendered per subregion.
- **Temperature** — rainbow interpolation by temperature; sea blue; ridges dark red.
- **Precipitation** — humidity zones (dry = red, seasonal = yellow, otherwise green); sea blue;
  ridges dark red.
- **Biomes** (default) — one of 18 biomes as flat color or texture (*Texture* / *Texture Imp*);
  mountain sprites along ridges below 4× zoom; rivers.
- **Pops** — regions colored by their pops' races (white = unpopulated, black = ridge, blue = sea);
  draws the current event (see Next Event).
- **Cells** — developer view: a 1600-column raster of cells filled with per-region random colors
  (built in parallel, 4 threads).
- **Landmasses** — flat color per landmass, center markers, and neighbor links between landmasses.

The new form always renders **Elevation**.

Zoom/pan state (`_multiplier`, `_origin`) is applied to the render transform: `+` zooms in ×2,
`-` zooms out (unbounded), `Home` resets. The same transform applies to hover overlays and hit-testing.

## Keyboard

| Key | Action |
| --- | --- |
| **F12** | Switch to the other generator's form (see [Form switching](#form-switching-f12)) |
| **+ / −** | Zoom in / out by ×2 |
| **Home** | Reset zoom and pan |
| **W A S D** | Pan up / left / down / right (step = world size ÷ 2^(zoom+2)) |
| **R** | Regenerate with a fresh seed (same as Start) |

The form has `KeyPreview = true`, so these fire even when a panel control has focus — WASD pans
the map while a combo box is focused. Pan/zoom/hover require a generated world; F12 and R work at any time.

## Mouse (legacy form)

- **Hover** over the map: the Info label shows cursor coordinates + hovered-region details
  (name, biome, size, height, water level, traits, pops); the region is outlined on the map.
- **Click** a subregion: opens/focuses Paedia on that region's detail view.

The new form has no subregion graph, so hover and click are inert there (the Info label stays empty).

## Side windows

### Log (legacy)

800×800; one line per history event (`T<turn>: <info>`), alternating white/lavender rows.
Auto-scrolls to the bottom after each render while visible; cleared on Start. The close button
**hides** the window — the Log/Paedia panel buttons toggle it.

### Paedia (legacy)

420×620 (min 320×240), centered over the main window; a browsable encyclopedia of the world's history:

- **Races view** — all races by name with trait tags; hovering a race highlights its regions on
  the map (red/black area outline); clicking opens the race detail.
- **Race detail** — tags, traits, pop count.
- **Region view** — center, climate (belt + humidity), biome (ridge marker), clickable pops →
  race detail; while this view is open, hovering the window outlines that region on the map.
- `<` navigates back through the view history; **Races** returns to the list (re-rendered from live data).
- The close button hides it; re-showing refreshes the race list.

## Test image

The **Test** button draws a one-shot developer image in place of the map on the next paint: a 50×35
parent hex grid subdivided by `ChildGridGenerator` into a child grid, each parent cell painted a
random color with black child-cell borders — a visual check of grid subdivision and rendering.
It is unrelated to any generated world and is disposed after being drawn once; the map reappears
on the next render.

## Form switching (F12)

- Opens the other generator's form (maximized), or re-shows it if it was already opened; hides the current form.
- Hidden forms stay alive: each keeps its generated world, parameters, zoom/pan position, and Log/Paedia
  state — so you can generate in one, switch to the other, and flip back without regenerating.
- Closing either window (X / Alt+F4) exits the app, even if the other form is still open (hidden).

## File map

| File | Contents |
| --- | --- |
| [Program.cs](Program.cs) | Entry point — runs `LegacyWorldSimulatorForm` |
| [WorldSimulatorBaseForm.cs](WorldSimulatorBaseForm.cs) | Abstract shared form: layout, panel wiring, parameter handling, mouse/keyboard, F12 switch, highlight state |
| [WorldSimulatorBaseForm.MapRender.cs](WorldSimulatorBaseForm.MapRender.cs) | Shared render pipeline: `OnPaint`, all map-mode image builders, rivers/borders/outlines |
| [LegacyWorldSimulatorForm.cs](LegacyWorldSimulatorForm.cs) | Legacy generator form: parameters, Log/Paedia/Next-Event buttons, history wiring |
| [NewWorldSimulatorForm.cs](NewWorldSimulatorForm.cs) | New generator form: grid level + Sea to land |
| [GeneratorAdapter.cs](GeneratorAdapter.cs) | `IGenerator` interface + adapters for both generators (the new adapter throws `NotImplementedException` for legacy-only features) |
| [ParametersPanel.cs](ParametersPanel.cs) | Left panel; binds `Parameter` objects to controls (combo/checkbox/numeric-up/read-only seed box) and raises `OnParameterUpdate` |
| [PaediaForm.cs](PaediaForm.cs) / [LogForm.cs](LogForm.cs) | Side windows |
| [HexGridRenderer.cs](HexGridRenderer.cs), [RenderObjects.cs](RenderObjects.cs), PolygonData/SegmentData/ImageData/VertexData/HexCellData, [ColorConverter.cs](ColorConverter.cs) | Rendering primitives (polygons, segments, images, vertices; GDI resource disposal) |
| [SmoothHeightRender.cs](SmoothHeightRender.cs) | Standalone demo renderer — not wired into any UI path |
| [Tests/LayerGridTest.cs](Tests/LayerGridTest.cs) | Test image for the Test button |

## Known limitations

- **New generator form**: Elevation mode only. No subregions/regions/history — `GeneratorAdapter`
  throws `NotImplementedException` for those members; the form's UI does not expose them (hover/click
  are inert, keyboard pan steps come from grid dimensions).
- **KeyPreview hazard**: WASD/R/+/- fire even when a panel control has focus.
- The legacy *Next Event* modifier switch only handles single modifiers (Ctrl+Alt together → 1 event).
- `-` zoom is unbounded below (zooms out past the world).
