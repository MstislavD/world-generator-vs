# Code Review: WorldSimulatorForm

_Saved 2026-09-03 for reference in future coding sessions._

**Status update (P0-completion session):** all three P0 items are **fixed and compile-verified** — #1 GDI leak (`HANDOVER.md` §1), #2 stale race highlight and #3 stale click position (`HANDOVER.md` §1, "P0 completion"). Full open-work list (now P1/P2 only), do-not-touch rules, and sandbox build recipes: `HANDOVER.md`. Paedia window: audited and fixed in the paedia-fix session — see bug item 6; its high-DPI label overlap was fixed in the paedia-highdpi session (`HANDOVER.md` §11).

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

2. **Stale race highlight. (FIXED in the P0-completion session.)** Pre-fix: `RaceHoverBegin` set `_highlightedArea`, but `RegionHoverBegin` never cleared it — after hovering a race in the paedia, moving the cursor over sea called `RegionHoverBegin(null)` and OnPaint kept drawing the old red outline forever. Fix: `RegionHoverBegin` resets `_highlightedArea` to `[]` when `region == null`.

3. **Click uses stale mouse position. (FIXED in the P0-completion session.)** Pre-fix: `WorldSimulatorForm_MouseClick` hit-tested against `_mouse` (last `MouseMove`) instead of `e.Location` — wrong for a first click or fast movement. Fix: the click handler now uses `e.Location`; `_mouse` is kept only for MouseMove's move-detection.

4. **Non-legacy mode is a landmine** (dormant while `_legacy = true`, but it will bite when the flag flips):
    - `GeneratorAdapter.cs:200`: `Regenerate(int seed)` calls `_gen.Regenerate()` — **the seed is silently discarded**, even though `WorldGenerator.Regenerate(int newSeed)` exists and honors it.
    - `initialization()` (`WorldSimulatorForm.cs:140-155`) never wires `MouseMove`/`MouseClick`/`KeyDown`, so hover/zoom/pan don't work; and most `MapMode`s throw `NotImplementedException` through the adapter (`Height` → `GetCountByElevation`; `Temperature`/`Biomes`/`Pops`/`Cells`/`Landmasses` → `SubregionGraph`). Only `Elevation` actually renders.

5. **ComboBox external-update bug. (FIXED in the P1-bugfix session.)** Pre-fix, `ParametersPanel.cs:114` assigned the *Parameter object* instead of `parameter.Current`, so any programmatic update cleared the combo's selection rather than reflecting it. Now assigns `parameter.Current`.

6. **Fragile paedia code:**
    - ~~`PaediaForm.cs:145`: `info.Substring(0, info.Length - 2)` throws `ArgumentOutOfRangeException` if a race has zero tags.~~ **(FIXED)** — the trim is now guarded by `info.Length > 0`. (Context kept: today every race gets ≥2 tags only because `PREFERABLE_BELTS`/`PREFERABLE_HUMIDITY` subscribe to `RaceCreated` before any race exists.)
    - ~~`PaediaForm.cs:149`: `RaceHoverEnd?.Invoke(race, e)` passes arguments swapped relative to the `EventHandler<Race>` convention.~~ **(FIXED)** — `RaceHoverEnd` is now declared `EventHandler<Race>` (consistent with `RaceHoverBegin`/`RegionHoverEnd`) and invoked as `(this, race)`.
    - ~~Pop labels in `OnRegionSelected` were created but never made clickable — unfinished feature.~~ **(FIXED, paedia-fix session)** — pop labels are now selectable and click through to the race view.
    - **(paedia-fix session, all FIXED)** `_showRaces()` dereferenced `_generator.History.Races` while both were null before the first Start (the Paedia button is available immediately) → NRE crash on first show; now guarded with a "press Start" hint label. Stale map highlight: hover-*End* events were never fired on navigation/hide and had no subscribers in the main form (`RegionHoverEnd` was CS0067) — the red outline survived leaving a race/region view, hiding the window, or regeneration; paedia now fires the End events when leaving a detail view, on hide, and in `InitializeHistory`, and the main form subscribes to both. GDI font leak: `_selectableLabel` allocated a fresh `Font` on every MouseEnter/MouseLeave (never disposed — 2 HFONT handles per hover cycle) and `_header` leaked one per navigation; now one shared bold font per form, disposed in `Dispose(bool)`. Layout: window opened at the default ~300×300 with no title/position and labels were hardcoded 1000px wide → permanent horizontal scrollbar; now a 420×620 CenterParent window with MinimumSize and labels that track panel width on resize. Navigation: back button never disabled at root, and "Races" while already in Races pushed a duplicate history entry — both fixed.
    - **(paedia-highdpi session, FIXED)** Entry labels overlapped and were unreadable on high-DPI displays (~240–250% scaling): `Label`'s default 23px height is a hardcoded 96-DPI value that never scales, so rows stayed 23px tall while text needs ~47px/line there. `_label()` now fits each label's height to its wrapped text (new `_fitHeight` helper), re-fitting on resize (`_applyLabelWidths`) and header uppercasing/bold (`_header`).
    - **(paedia-highdpi session, FIXED)** Hover flicker between multi-line labels: the shared bold font was built from `SystemFonts.DefaultFont`, which resolves to a *smaller* font than the labels' DPI-scaled default (line height 31px vs 40px at ~250% scaling — the "smaller gap" in bold), so every hover shrank the label, shifted the rows below under the cursor, and re-triggered Enter/Leave storms. `_boldFont` is now derived from a sample `Label`'s actual default font (identical line metrics at any DPI, no GDI cost), and `_fitHeight` measures with the bold (wider) font so a label's height is identical in both hover states — the hover handlers now swap the font only and change no geometry. Details + probe verification: `HANDOVER.md` §11.
    - **(paedia-stale-races session, FIXED)** After a second Start with the window open, the race list stayed stale (empty) and neither clicking "Races" nor re-showing the window refreshed it: the duplicate-prevention guard in `_showRaces()` (`if (_currentMode == ViewMode.Races) return;`) skipped not just the history push but the re-render too, and `VisibleChanged` only refreshed in `Init` mode. `_showRaces()` now always re-renders from current data (skipping only the duplicate history entry), the race list refreshes on show when the mode is `Init` or `Races`, an empty list shows a hint label, and `_clearItems()`/`_hoveredRace` drop the map highlight of a hovered race label when its list is destroyed. Details: `HANDOVER.md` §13.
    - **(paedia-live-refresh session, FIXED)** The open window's race list stayed on the "no races yet" hint after event batches created new races (refresh only happened on "Races" click / re-show). `PaediaForm.RefreshRaces()` now re-renders the list when visible and in Races mode, called from `BtnNextEvent_Click` after each event batch. Details: `HANDOVER.md` §15.

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

1. **P0** — ~~dispose `Graphics`/bitmaps/brushes~~, ~~clear `_highlightedArea` in `RegionHoverBegin`~~, ~~use `e.Location` in the click handler~~ — **ALL DONE**.
2. **P1** — fix the non-legacy adapter (seed passthrough) or gate the flag until it's complete; ~~`combo.SelectedItem = parameter.Current`~~, ~~guard `_showRaces` against empty tags~~ **(both FIXED)**; add resize handling; bound zoom and swallow camera keys when a child control has focus.
3. **P2** — move generation off the UI thread with proper marshaling; cache brushes/pens as static fields; strip dead code, junk usings, duplicate resource embedding; rename `LogForm.Update`.

## Overall

The structure (parameter-driven re-render, adapter over two generators, renderer separated from data) is sound and the code compiles cleanly; the main risks are the accumulated GDI leaks, the stale-highlight state machine, and the dormant-but-broken non-legacy path.
