# Code Review: WorldSimulatorForm

_Saved 2026-09-03 for reference in future coding sessions._

**Status update (GDI-fix session):** P0 item 1 (GDI leak) is **fixed and compile-verified** — see `HANDOVER.md` §1. Remaining P0: items 2 and 3 below. Full open-work list, do-not-touch rules, and sandbox build recipes: `HANDOVER.md`.

Scope: the form's two partial files — `WorldSimulationForm/WorldSimulatorForm.cs` and `WorldSimulationForm/WorldSimulatorForm.MapRender.cs` — plus everything they depend on (`GeneratorAdapter`, `ParametersPanel`, `HexGridRenderer`, `PaediaForm`, `LogForm`, the `Parameter` framework, `WorldGeneratorLegacy`/`HistorySimulator`, spatial index).

**Verification done at review time:** all 23 project sources compiled directly with Roslyn `csc` against the net9.0-windows ref assemblies and the project's own DLLs → **exit 0, warnings only**. (`dotnet build` itself fails in a sandboxed environment — MSBuild's nested nodes can't open named pipes, producing "build failed" with 0 errors; environmental, not a code problem — working recipes in `HANDOVER.md` §4.) All 39 texture names used by `_biomesImage` are present in `Resources.resx`.

## Architecture (brief)

The form is a map viewer: a left `ParametersPanel` drives an `IGenerator` (legacy/new behind a hardcoded `_legacy = true` flag), and every parameter change or generation completion funnels into `_renderMap` → `Invalidate()` → `OnPaint`, which builds a `RenderObjects` description per `MapMode` and rasterizes it through `HexGridRenderer` into a cached `_image` bitmap. Zoom/pan is done via `_multiplier`/`_origin` re-rendering; hover highlights are drawn as an overlay. The design is coherent, but several pieces leak resources or hold stale state across renders.

## Bugs (correctness)

1. **GDI handle leaks — the big one. (FIXED in the GDI-fix session.)** Pre-fix inventory — all items below were addressed by the per-render ownership model in `RenderObjects` + caller-side dispose (details in `HANDOVER.md` §1). Disposable GDI objects used to be created on every render and never disposed:
   - `HexGridRenderer.cs:46`: `Graphics.FromImage(_image)` is never disposed — one leaked GDI context per render.
   - `WorldSimulatorForm.MapRender.cs:51,98`: `_image` is nulled/replaced without `Dispose()` on every parameter change or zoom keypress.
   - `WorldSimulatorForm.MapRender.cs:104-106`: the hover **overlay** bitmap is rebuilt on *every* subregion entered and never disposed — leaks continuously just by moving the mouse.
   - Per-render churn in the render methods: ~19 `TextureBrush`es plus mirrored mountain bitmaps in `_biomesImage` (the mirrors leak too, and `mountains[i].MakeTransparent(Color.White)` **mutates shared cached resource bitmaps**), a fresh `SolidBrush` per subregion in `_temperatureImage`, ~140 brushes from all `KnownColor`s in `_cellsImage`, race/landmass brushes in `_popImage`/`_landmassImage`, and many `Pen`s. Long sessions will exhaust GDI handles (the classic "system has run out of resources" crash).
   - Applied fix: per-render owned brushes/pens registered in `RenderObjects` (disposed via `objects.Dispose()` after each render) plus explicit disposal of `Graphics`, `_image`, hover overlays, and transient bitmaps. Chosen over cached static fields so shared resource bitmaps are never mutated.

2. **Stale race highlight.** `WorldSimulatorForm.cs:176-182`: `RaceHoverBegin` sets `_highlightedArea`, but `RegionHoverBegin` never clears it. After hovering a race in the paedia, moving the cursor over sea (no subregion) calls `RegionHoverBegin(null)` → `OnPaint` falls into the `else if (_highlightedArea != null)` branch and keeps drawing the old red outline forever.

3. **Click uses stale mouse position.** `WorldSimulatorForm.cs:246-251`: `WorldSimulatorForm_MouseClick` hit-tests against `_mouse` (last `MouseMove`) instead of `e.Location`. Usually equal, but wrong for a first click or fast movement.

4. **Non-legacy mode is a landmine** (dormant while `_legacy = true`, but it will bite when the flag flips):
   - `GeneratorAdapter.cs:200`: `Regenerate(int seed)` calls `_gen.Regenerate()` — **the seed is silently discarded**, even though `WorldGenerator.Regenerate(int newSeed)` exists and honors it.
   - `initialization()` (`WorldSimulatorForm.cs:140-155`) never wires `MouseMove`/`MouseClick`/`KeyDown`, so hover/zoom/pan don't work; and most `MapMode`s throw `NotImplementedException` through the adapter (`Height` → `GetCountByElevation`; `Temperature`/`Biomes`/`Pops`/`Cells`/`Landmasses` → `SubregionGraph`). Only `Elevation` actually renders.

5. **ComboBox external-update bug.** `ParametersPanel.cs:114`: `parameter.OnUpdate += (s, e) => combo.SelectedItem = parameter;` — assigns the *Parameter object* instead of `parameter.Current`, so any programmatic update clears the combo's selection rather than reflecting it.

6. **Fragile paedia code:**
   - `PaediaForm.cs:145`: `info.Substring(0, info.Length - 2)` throws `ArgumentOutOfRangeException` if a race has zero tags. Today every race gets ≥2 tags only because `PREFERABLE_BELTS`/`PREFERABLE_HUMIDITY` subscribe to `RaceCreated` before any race exists — remove or reorder those traits and `_showRaces` crashes on show.
   - `PaediaForm.cs:149`: `RaceHoverEnd?.Invoke(race, e)` passes arguments swapped relative to the `EventHandler<Race>` convention; the mismatch is masked because `RaceHoverEnd` is declared as a plain `EventHandler` (inconsistent with `RegionHoverEnd`).
   - `PaediaForm.cs:189-192`: pop labels in `OnRegionSelected` are created but never made clickable — unfinished feature.

7. **Keyboard handling hazards.** `WorldSimulatorForm.cs:347-394`: with `KeyPreview = true` and no `e.Handled`, WASD pan / R regenerate fire even while a child control (e.g. a `NumericUpDown`) has focus — typing in the panel also moves the camera. `_multiplier` has no lower bound (`Subtract` at 0 zooms *out* indefinitely).

8. **No resize handling.** `_imageRect`, `panel.Width`, and the cached `_image` are computed once in the constructor from `ClientSize`; there is no `OnResize`/`OnLoad` override in either partial file. Restore→maximize leaves the map at its initial size and misaligned. (The startup path only works because `WindowState = Maximized` on line 61 resizes before line 66 reads `ClientSize` — fragile ordering.)

9. **Smaller items:**
   - `WorldSimulatorForm.MapRender.cs:88`: `_ => throw new Exception()` — bare exception in the paint path; make the switch exhaustive or use `NotImplementedException`.
   - Compiler-confirmed CS8602 at `WorldSimulatorForm.cs:260` and `300`: `graph.SpatialIndex` is nullable (`ISpatialIndex<TSubregion>?`) and dereferenced without a check. In practice it's always set, but note `GenerateSpatialIndex`'s `while (true)` + bare `catch` retry loop in `WorldSimulation/SubregionGraph.cs:178-190` is the only thing guaranteeing that.
   - `WorldSimulatorForm.cs:204,206-212`: `sender == null` check is dead (Click always passes the button); the `ModifierKeys` switch misses combined modifiers (Ctrl+Alt falls through to 1); `_trackedEvents` is hardcoded `true`, so the `NextEvents(eventsCount)` branch and the whole modifier-key scheme are dead code.
   - `WorldSimulatorForm.cs:157-167`: in `Panel_OnParameterUpdate`, the `else if (_regenerate)` branch keys off the *checkbox value* for any parameter outside the two lists — i.e. every generator-parameter tweak triggers a full (re)generation. Probably intended, but it reads like a bug and, combined with synchronous generation (below), means constant UI freezes.

## Performance / responsiveness

- **Generation is fully synchronous on the UI thread.** `WorldGeneratorLegacy.Generate()` runs to completion on the caller's stack and then raises `OnGenerationComplete` (`WorldSimulation/WorldGeneratorLegacy.cs:159-160`). Every Start click, R press, or parameter change freezes the form (grid generation, spatial index, full history simulation) with no progress feedback. Move generation to a background task and marshal `OnGenerationComplete` back to the UI thread.
- `_cellsImage` additionally blocks the UI thread running a `Parallel.For` (4 threads) over ~1600×900 quadtree point queries — the query path itself is read-only, so it's safe, but it stutters the form for seconds on every switch to Cells mode.
- `_addBordersToSubregionImage` visits every internal edge twice (once per neighboring subregion), adding duplicate segments and doubling the work.
- Cosmetic: `HexGridRenderer` sizes its bitmap from `grid.BoundingBox` with `scale = min(xScale, yScale)`, so when the window aspect differs from the map aspect the image is smaller than `_imageRect` and anchored top-left (gap on right/bottom).

## Dead code & hygiene

- `_printLog` is always `false` → `Generator_LogUpdated` never does anything; `drawBelts = false` makes `brushByBelt` in `_temperatureImage` dead; `drawZones = true` makes the interpolation branch of `_precipitationImage` (and its `colorByTemperature`) dead.
- Commented-out blocks: three `btnTest.Click` variants (`WorldSimulatorForm.cs:87-89`), the whole subregion loop in `_popImage` (MapRender.cs:430-441).
- Junk usings: `System.CodeDom`, `System.Reflection.Metadata.Ecma335` (WorldSimulatorForm.cs:1,5).
- Naming: project/folder is **WorldSimulation**Form but the class/file is **WorldSimulator**Form; label typo `"SRegion borders"` (`WorldSimulatorForm.cs:50`); local `_outlinedRegions` with field-style underscore (MapRender.cs:708).
- `LogForm.Update()` hides `Control.Update()` (CS0108) — rename it.
- `Utilities/RandomExt.cs`: `NextDouble()` isn't overridden, so it draws from the *base* `Random`'s state while all `Next*` overloads draw from `_random` — two independent streams seeded with the same value. Deterministic, but surprising; it's what drives mountain placement in `_biomesImage`.
- `WorldSimulationForm.csproj`: every texture PNG is embedded **twice** — once via the resx (ResXFileRef) and again as standalone `<EmbeddedResource>` items (lines 63-113) — bloating the exe; the `None Remove` list is also out of sync with the `EmbeddedResource Include` list.

## Suggested priority order

1. **P0** — ~~dispose `Graphics`/bitmaps/brushes~~ **DONE**; clear `_highlightedArea` in `RegionHoverBegin`; use `e.Location` in the click handler.
2. **P1** — fix the non-legacy adapter (seed passthrough) or gate the flag until it's complete; `combo.SelectedItem = parameter.Current`; guard `_showRaces` against empty tags; add resize handling; bound zoom and swallow camera keys when a child control has focus.
3. **P2** — move generation off the UI thread with proper marshaling; cache brushes/pens as static fields; strip dead code, junk usings, duplicate resource embedding; rename `LogForm.Update`.

## Overall

The structure (parameter-driven re-render, adapter over two generators, renderer separated from data) is sound and the code compiles cleanly; the main risks are the accumulated GDI leaks, the stale-highlight state machine, and the dormant-but-broken non-legacy path.
