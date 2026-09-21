# HANDOVER — world-generator-vs / WorldSimulationForm

Self-contained state log so the project can be picked up in a fresh session or on another machine.
Companion to `CodeReview-WorldSimulatorForm.md` (the full review + plan, saved 2026-09-03).
Last updated: after the paedia-live-refresh session (the open Paedia now updates its race list as events create new races — see §15).

## TL;DR

- **Done & compile-verified:** all three P0 items — the GDI handle leak (review bug #1), the stale race highlight (bug #2), and the stale click position (bug #3). See §1.
- Everything else open is tracked in the review doc (P1/P2).
- **Paedia window fixed (paedia-fix session):** pre-Start NRE crash, stale map highlights (hover-end events fired + subscribed), per-hover Font/GDI leak, 1000px-label / default-size layout, back-button state, clickable pop labels — compile-verified, see §9.
- **Paedia entry labels on high-DPI fixed (paedia-highdpi session):** rows were stuck at the unscaled 23px `Label` default height while text needs ~47px/line at ~250% scaling → entries overlapped and were unreadable; labels now fit their wrapped-text height, see §11. Hover flicker between multi-line labels fixed too: the shared bold font was derived from `SystemFonts.DefaultFont` (a smaller, non-DPI-scaled font), so hovering shrank text lines and shifted rows under the cursor — it is now derived from the labels' actual font, and label heights are pre-fitted to bold metrics so hover changes no geometry.
- **Paedia stale race list fixed (paedia-stale-races session):** after a second Start with the window open, the race list stayed empty and neither "Races" nor re-showing refreshed it; `_showRaces()` now always re-renders from current data, see §13. The open window's race list also live-refreshes after each event batch (paedia-live-refresh session, §15).
- **Agents working inside the DSH sandbox: read §4 before running any build** — plain `dotnet build` fails under the default sandbox mode; working recipes are there.

## 1. What the GDI-fix session did

### Per-file changes (all compile-verified, see below)

| File | Change |
|---|---|
| `WorldSimulationForm/RenderObjects.cs` | Rewritten as `IDisposable` (internal class). Owns every pen/brush in its lists + a `Disposables` list for transient bitmaps; `Dispose()` disposes all and clears the lists. Carries the ownership contract as doc comments. |
| `WorldSimulationForm/HexGridRenderer.cs` | `using Graphics g = Graphics.FromImage(_image);` — per-render Graphics disposed (was the single biggest leak: one GDI context per render). |
| `WorldSimulationForm/WorldSimulatorForm.MapRender.cs` | Every render path allocates its brushes/pens per render and disposes them via `objects.Dispose()` after drawing: `_renderMap` (disposes+nulls `_image`), `_elevationImage`, `_heightImage`, `_temperatureImage` (dead `brushByBelt`/`drawBelts` removed; one owned brush per subregion), `_precipitationImage`, `_biomesImage` (TextureBrush dict only in Texture mode, SolidBrushes only in Color mode; fixed a `; ;` typo), mirror bitmaps registered in `objects.Disposables`, `_popImage`, `_landmassImage`, `_addBordersToSubregionImage`. |
| `WorldSimulationForm/WorldSimulatorForm.cs` | OnPaint disposes `_testImage` before nulling it and disposes main-render + overlay `RenderObjects`; form-level `Dispose(bool)` releases `_image`/`_testImage`. |
| `WorldSimulationForm/Tests/LayerGridTest.cs` | Per-render pen + `objects.Dispose()` after render. |

### Ownership rule (now enforced in code)

- A `RenderObjects` instance **owns** every pen/brush placed into its lists, plus everything in `Disposables`. The caller disposes it after rendering completes.
- Pens/brushes may be shared *between entries of one instance* (`Dispose` is idempotent), but shared statics (`Brushes.*`/`Pens.*`) must **never** be added to an instance that will be disposed.
- `ImageData.Image` is **not** owned (entries may reference shared resource bitmaps); register owned images (transient copies) in `Disposables`.

### Behavior-preservation notes (deliberately NOT changed)

- Sea→Blue / ridge→DarkRed color precedence in the temperature/precipitation lambdas kept identical.
- `_biomesImage` still calls `MakeTransparent(Color.White)` on shared resource bitmaps — idempotent, left as-is (the review flagged it as mutation of shared state; fixing it is a P2 hygiene item).
- `Utilities/RandomExt.NextDouble()` draws from the *base* Random's state (drives mountain placement) — untouched.

### Compile verification

All 20 form sources compiled with Roslyn csc: **exit 0, 0 errors**. Warnings are all pre-existing: 27× CS8632 (nullable annotations outside #nullable context), 1× CS0108 (`LogForm.Update` hides inherited member), 1× CS0067 (`PaediaForm.RegionHoverEnd` never used — the dormant hover path).

### P0 completion (second session)

| File | Change |
|---|---|
| `WorldSimulationForm/WorldSimulatorForm.cs` | `RegionHoverBegin`: when `region == null` (cursor over sea), `_highlightedArea` is reset to `[]`, so a race highlight set by the paedia no longer survives as a stale red outline. Non-null callers (paedia region selection) are unaffected — OnPaint already prioritizes `_highlightedRegion` there. |
| `WorldSimulationForm/WorldSimulatorForm.cs` | `WorldSimulatorForm_MouseClick`: hit-test and coordinate mapping now use `e.Location` instead of the cached `_mouse` field (which is still kept for MouseMove's move-detection). Fixes first-click / fast-movement misses. |

Compile verification: full `dotnet build WorldSimulator.sln` → **Ошибок: 0, exit 0** (197 warnings, all pre-existing nullable/CS0067 categories — the two known CS8602s at `graph.SpatialIndex` just shifted +2 lines).

## 2. Open work

### P0 — complete

All three P0 items are fixed and compile-verified (GDI leak: above; stale highlight + click position: "P0 completion" table above).

### P1 (from the review)

- Non-legacy adapter landmines (dormant while `_legacy = true` is hardcoded): `GeneratorAdapter.cs:200` discards the seed in `Regenerate(int)`; `initialization()` never wires MouseMove/MouseClick/KeyDown; most MapModes throw `NotImplementedException` through the adapter (only Elevation renders).
- ~~`ParametersPanel.cs:114`: `combo.SelectedItem = parameter` should be `parameter.Current`.~~ **FIXED** — now assigns `parameter.Current` (programmatic combo updates reflect instead of clearing selection; no loop risk since `SelectionChangeCommitted` only fires on user input).
- ~~`PaediaForm.cs:145`: `Substring(0, info.Length - 2)` crashes with zero tags; `PaediaForm.cs:149`: `RaceHoverEnd` args swapped.~~ **FIXED** — the trim is now guarded by `info.Length > 0`; `RaceHoverEnd` is redeclared as `EventHandler<Race>` (consistent with `RaceHoverBegin`/`RegionHoverEnd`) and invoked as `(this, race)`. Verified no subscribers existed before the signature change.
- No resize handling (no OnResize/OnLoad; startup only works because Maximized precedes the ClientSize read).
- Zoom has no lower bound; WASD pan / R regenerate fire while child controls have focus (`KeyPreview = true`, no `e.Handled`).

### P2

- Generation is fully synchronous on the UI thread (every Start/R/parameter change freezes the form) → background task + marshal back.
- **Vector2 allocation churn** (perf, not a leak): `_cellsImage` allocates 5 Vector2s per cell (~7M per Cells render, MapRender.cs:547-556); `QuadTreeSpatialIndex.FindPolygonContainingPoint` allocates per query (QuadTree.cs:138); `Region.Center`/`Landmass.Center` getters allocate per call (Region.cs:50,71; Landmass.cs:40).
- Dead code & hygiene from the review: junk usings (`System.CodeDom`, `Ecma335`), commented-out blocks, `_printLog` always false, `LogForm.Update` rename, and **csproj embeds every texture PNG twice** (resx ResXFileRef + standalone EmbeddedResource lines 63-113) → exe bloat.

### Do-not-touch list

- **`Topology.Vector2` must stay a class.** It is a heap type by design: "one vector is shared by three hexagons" (Vector2.cs:7-8); `HexCell.GetVertex()` returns the stored reference; edges pull vertices from neighbor cells (HexGrid.cs:148-151). Converting to struct would ~3× retained memory.
- `_legacy = true` is hardcoded — the non-legacy path is dormant; don't half-fix it.
- **No built-in WinForms GDI-handle logging switch exists in .NET 9.0.19** (verified by DLL string scan) — do not recommend one.

## 3. Verifying the GDI fix at runtime (user side)

Background: before the fix, VS diagnostics showed a classic finalizer-based sawtooth — Bitmap/Graphics/Brush objects accumulate per render and drop only when GC finalizers run; Task Manager's "GDI Objects" column climbed while hovering the map (per-process GDI quota is ~10,000).

After the fix, expect:
1. VS diagnostics: flat lines for Bitmap/Graphics/Brush (no sawtooth).
2. Task Manager → Details → **GDI Objects** column stays flat while moving the mouse over the map.
3. If programmatic polling is ever needed again: P/Invoke `GetGuiResources($proc.Handle, 0)` / `(…, 1)` returns a process's GDI/User object counts (a throwaway poller was used during the leak hunt and removed after the fix).

## 4. Environment & build recipes (agent / sandbox notes)

### Machine facts

- Windows, **Russian locale** — all tool output is localized: "Ошибка сборки" = build failed, "Сборка успешно завершена" = build succeeded, "Ошибок: 0" = 0 errors.
- .NET SDK **9.0.317** at `C:\Program Files\dotnet`; target `net9.0-windows`; solution `WorldSimulator.sln`; WinForms app with sibling projects PerlinNoise / Topology / Utilities / WorldSimulation (ProjectReferences).
- This machine's ref packs are **non-standard**: `Microsoft.NETCore.App.Ref\9.0.19` has **no** `System.Private.CoreLib.dll` (it ships `mscorlib.dll` + `netstandard.dll` instead) → for raw csc, reference the *shared framework* DLLs, not the ref packs.
- The user builds/runs normally outside any sandbox (VS or plain dotnet). The restrictions below only apply to commands executed inside the DSH agent sandbox.

### `dotnet build` under the DSH sandbox (verified matrix)

| Mode | Result |
|---|---|
| workspace-write (default), plain `dotnet build` | **Fails.** Two layers: (1) CLI first-run configurator writes a sentinel to `%USERPROFILE%\.dotnet` → denied outside the workspace → `UnauthorizedAccessException`; (2) after redirecting USERPROFILE, MSBuild dies on **blocked named pipes** (worker-node reuse + VBCSCompiler server) → generic "Ошибка сборки" with 0 warnings / 0 errors, exit 1. |
| workspace-write + flags | **Works:** set `$env:USERPROFILE` to `<workspace>\.dotnet-home`, then `dotnet build <proj> -nodeReuse:false -m:1 /p:UseSharedCompilation=false` (verified: Ошибок: 0, exit 0). Caveat: this creates stray `NuGet/` + `WorldSimulationForm/NuGet/` folders in the workspace — delete them after. |
| danger-full-access (user-approved; approval policy is "ask", so each elevated command prompts) | **Works:** plain `dotnet build` as-is (verified: Сборка успешно завершена, exit 0). Preferred when approved. |

Sandbox mechanics worth knowing: confined modes (read-only, workspace-write) block named pipes entirely — any program that opens one dies with EPERM; Node `child_process` with default piped stdio fails the same way (`stdio: 'inherit'` works). Read-only pwsh runs in ConstrainedLanguage mode; workspace-write stays FullLanguage.

### Raw csc recipe (fallback when no MSBuild is available)

- Compiler: `C:\Program Files\dotnet\sdk\9.0.317\Roslyn\bincore\csc.dll` run via `dotnet.exe` (the host still triggers first-run config → set USERPROFILE to a workspace dir).
- `/nostdlib /target:winexe`; **quote every argument** — paths contain spaces ("Git Projects"); an unquoted `/out:` splits and produces CS2001 with a doubled path.
- References = all **managed** DLLs in `C:\Program Files\dotnet\shared\Microsoft.NETCore.App\9.0.19` + `...\Microsoft.WindowsDesktop.App\9.0.19`, filtered by try/catch on `[System.Reflection.AssemblyName]::GetAssemblyName()` (native DLLs like `*_cor3.dll`, `msquic.dll`, `System.IO.Compression.Native.dll` cause CS0009 if referenced) + the four project DLLs from `WorldSimulationForm\bin\Debug\net9.0-windows` (PerlinNoise, Topology, Utilities, WorldSimulation — **not** WorldSimulationForm.dll).
- Sources = all `.cs` under `WorldSimulationForm` excluding obj/bin, **plus a generated GlobalUsings.g.cs**. The project has `<ImplicitUsings>enable</ImplicitUsings>`, so files without explicit usings need the SDK's implicit set — as **`global using`** directives: System, System.Collections.Generic, System.Drawing, System.IO, System.Linq, System.Threading.Tasks, System.Windows.Forms.
  - **Gotcha that cost a whole debugging session:** plain top-level `using X;` in a separate file applies only to that file (compilation unit), NOT the whole compilation — you must write `global using X;`. Symptom when wrong: CS0246 for EventHandler/Label/List etc. only in files lacking explicit usings, while other files compile fine.

## 5. Files added by these sessions

- `CodeReview-WorldSimulatorForm.md` — full review (9 bug categories, perf, dead code) + P0/P1/P2 plan. P0 item 1 (GDI) is now done; the doc is updated to say so.
- `HANDOVER.md` — this file.

## 6. Git state at end of the GDI-fix session

- Modified: `WorldSimulationForm/HexGridRenderer.cs`, `RenderObjects.cs`, `Tests/LayerGridTest.cs`, `WorldSimulatorForm.MapRender.cs`, `WorldSimulatorForm.cs`
- Untracked: `CodeReview-WorldSimulatorForm.md`, `HANDOVER.md`
- Scratch dirs (.probe/, .dotnet-home/, stray NuGet/) were created during verification and **removed**; bin/obj updates are normal gitignored build output.

## 7. Git state at end of the P0-completion session

- Modified (on top of §6): `WorldSimulationForm/WorldSimulatorForm.cs` (two P0 fixes), `HANDOVER.md`, `CodeReview-WorldSimulatorForm.md`
- Nothing new to commit from scratch dirs; bin/obj updates are gitignored build output.

## 8. Git state at end of the P1-bugfix session

- Modified (on top of §7): `WorldSimulationForm/ParametersPanel.cs` (combo.SelectedItem fix), `WorldSimulationForm/PaediaForm.cs` (zero-tag guard + RaceHoverEnd → EventHandler<Race>), plus both docs.
- Compile-verified: full `dotnet build WorldSimulator.sln` → Сборка успешно завершена, Ошибок: 0, exit 0.

## 9. Paedia window fix session

### Per-file changes (compile-verified, see below)

| File | Change |
|---|---|
| `WorldSimulationForm/PaediaForm.cs` | (1) `_showRaces()` guards `_generator?.History == null` with a "press Start" hint label — before, clicking Paedia before the first Start crashed with NRE (`History` is only created during generation). (2) Hover-end events now fire when leaving a detail view (`_pushCurrentMode`), on window hide (`PaediaForm_VisibleChanged`), and in `InitializeHistory`, which also nulls `_currentRace`/`_currentRegion` (now nullable) and refreshes the visible window to the new race list. (3) One shared bold `Font` per form, disposed in `Dispose(bool)`, replaces the per-hover `new Font(...)` allocations in `_selectableLabel`/`_header` (2 HFONT GDI handles leaked per hover cycle). (4) Window: `Text="Paedia"`, `CenterParent`, 420×620, MinimumSize 320×240; labels track panel width on resize instead of fixed 1000px (permanent horizontal scrollbar before). (5) Back button tracks the history stack (disabled at root); "Races" while already in Races no longer pushes a duplicate entry. (6) Pop labels in region view are clickable → race view (completes the unfinished feature from the review). Hygiene: `LogForm_FormClosing` renamed to `PaediaForm_FormClosing`, invariant-culture case conversions, stable tag/trait ordering. |
| `WorldSimulationForm/WorldSimulatorForm.cs` | Subscribes to `RaceHoverEnd` (clears `_highlightedArea`) and `RegionHoverEnd` (clears `_highlightedRegion`) in `initializationLegacy`. |

### Compile verification

Full `dotnet build WorldSimulator.sln --no-incremental` → exit 0, 0 errors, **194 warnings** = baseline 197 - 1 (CS0067 `PaediaForm.RegionHoverEnd` never used — now subscribed) - 2 (CS8618 for `_currentRace`/`_currentRegion`, now nullable). No new warnings; the two known CS8602s at `graph.SpatialIndex` are still there, shifted down by the added wiring (now `WorldSimulatorForm.cs:278/318`).

## 10. Git state at end of the paedia-fix session

- Modified (on top of §8): `WorldSimulationForm/PaediaForm.cs` (session rewrite), `WorldSimulationForm/WorldSimulatorForm.cs` (hover-end wiring + handlers), plus both docs.
- Probe files (.probe-build.log, .probe-warnings.txt) used for warning diffing were removed after verification; bin/obj updates are gitignored build output.

## 11. Paedia high-DPI label overlap session

### Symptom

In the Paedia window (Races view), entry labels overlapped each other and were unreadable on the user's laptop (~240–250% display scaling; app is PerMonitorV2). Fine at 100% scaling.

### Root cause (verified empirically with a headless layout probe)

`Label`'s default height is **23px — a hardcoded 96-DPI value that is not scaled to the display DPI** (`new Label().Size == (100, 23)` even inside a ~240dpi process context; `SystemInformation.VerticalScrollBarWidth` was 43px there). `_label()` set only `Width`, so every entry row stayed 23px tall while one line of text needs ~47px at that scaling (long names wrap to 2 lines → ~87px). Rows stacked at a 23px pitch under 47–87px-tall glyphs → adjacent entries overlapped by roughly 2×. The paedia-fix session (§9) fixed label *width* tracking but never addressed height.

### Follow-up (same session): hover flicker between multi-line labels

**Symptom:** moving the mouse between multi-line entries made them flicker — an unhovered neighbor would change size and grab the hover, then immediately revert. The user also noticed the bold font has a *smaller* line gap than regular.

**Root cause (probe-verified):** `_boldFont` was built from `SystemFonts.DefaultFont`, which on this display resolves to a **smaller font than the labels' actual DPI-scaled default** — line height 31px vs 40px at ~250% scaling (that is exactly the "smaller gap" in bold). Hovering swapped to that smaller font and re-fitted the height, so every hover shrank the label and yanked the rows below up/down under the cursor; each row shift re-triggered Enter/Leave on the labels under the moving mouse. Probe simulation of mouse movement (exact control-tree replica): current behavior → 17 enter/leave transitions with **7 rapid re-entry storms** on a slow sweep, 16 row-shifts wobbling across one boundary; fixed behavior → 5 clean transitions, **0 row-shifts**.

### Final fix (`WorldSimulationForm/PaediaForm.cs`)

- `_boldFont` is now derived from a sample `Label`'s actual default font — `new Font(new Label().Font, FontStyle.Bold)` — so bold has identical line metrics to regular at any DPI (the sample label creates no handle → no GDI cost; still one shared font, disposed in `Dispose(bool)`).
- New `_fitHeight(Label)` fits the row height to the wrapped text **measured with the bold (wider) font** (via a throwaway label's `GetPreferredSize(new Size(width, int.MaxValue))`, 0-guarded): regular text wraps to no more lines than the same text in bold, so the height is identical before and after the hover font swap.
- `_fitHeight` called from: `_label()` (creation), `_applyLabelWidths()` (form resize — wrapping changes with width), and `_header()` (uppercasing + bold can change the wrap count). The `_selectableLabel()` MouseEnter/MouseLeave handlers now **swap the font only** — no re-fit, so hover changes no geometry at all and cannot re-trigger Enter/Leave under the cursor.

### Verification

- Headless layout probe (~240dpi context): pre-fix row pitch 23px vs text needing 47/87px (glyph overlap); post-fix rows at 0/47/94/141 with 0 overlaps.
- Headless hover simulation: current code shrinks a hovered multi-line label (h 47→37, rows below jump) and produces enter/leave storms; fixed code changes no geometry on hover (h 87→87, font lineH 40→40) and produces zero row-shifts. Note: window handles could not be created in this session (no interactive desktop for the probe process), so verification is by layout math, not pixel render — a visual check on the laptop is still worthwhile.
- `dotnet build WorldSimulationForm.csproj` → **0 errors, 194 warnings** = unchanged from the §9 baseline (no new warnings). Full-solution builds only fail to *copy* outputs while the user's running app instance locks `bin\Debug\net9.0-windows\*.dll` (MSB3027) — environmental; close the app and rebuild.

## 12. Git state at end of the paedia-highdpi session

- Modified (on top of §10): `WorldSimulationForm/PaediaForm.cs` (`_boldFont` derivation, `_fitHeight` bold-metric sizing, hover handlers font-only), plus both docs.
- The throwaway probe projects (`C:\temp-paedia-probe`, `C:\temp-paedia-probe2`) and temp build output (`C:\temp-paedia-out`) were removed after verification; bin/obj updates are gitignored build output.

## 13. Paedia stale race list session

### Symptom (user-reported repro)

Start → Next event (races generated) → open Paedia (races shown) → **Start again** → Next event (new races) → the still-open Paedia window shows no races; clicking "Races" doesn't help; hiding and re-showing the window doesn't help either.

### Root cause

The duplicate-prevention guard added in the paedia-fix session (§9, item 5) conflated two things: `_showRaces()` began with `if (_currentMode == ViewMode.Races) return;` — "don't push a duplicate history entry" had become "don't do anything at all". After a regeneration (`InitializeHistory`) the window is visible, so it re-renders immediately — but the new world has no races yet (races are created during events, `HistorySimulator.CreateRace`), so the list renders empty and `_currentMode` becomes `Races`. When the first event of the new world creates races, nothing re-renders:

- clicking "Races" → early return in `_showRaces()` → stale empty list;
- hide + re-show → `PaediaForm_VisibleChanged` only refreshed when `_currentMode == ViewMode.Init`, which is no longer the case.

### Fix (`WorldSimulationForm/PaediaForm.cs`)

- `_showRaces()` now **always re-renders from current data**; it only skips pushing a history entry when already in Races mode (the original intent of the guard).
- `PaediaForm_VisibleChanged` refreshes the race list on show when the mode is `Init` **or** `Races` (re-showing may reveal a world that changed while hidden); detail views keep their place.
- An empty race list now shows a hint label ("No races in this world yet — run some events.") instead of a blank window — right after Start, before any event, the new world legitimately has zero races (`History.Races` is `IEnumerable<Race>`, so the check is `.Any()`).
- New `_clearItems()` helper + `_hoveredRace` field: when the list is cleared while a race label is hovered (regeneration with the cursor over the list), the destroyed label's MouseLeave never fires — `_clearItems()` drops that map highlight manually. Used in `_showRaces()`, `_pushCurrentMode()`, and `InitializeHistory()`.

### Verification

- All 7 repro steps traced through the new code paths: after Start #2 the open window shows the empty-world hint; after Next event, clicking "Races" re-renders the new race list (no duplicate history entry, back-button state unchanged); hide + re-show also refreshes.
- `dotnet build WorldSimulationForm.csproj` → **0 errors, 194 warnings** = baseline (full-solution builds only fail at the output-copy step while a running app instance locks `bin\Debug\net9.0-windows\*.dll` — environmental; close the app and rebuild).
- Known limitation (pre-existing, not addressed): detail views (race/region) are not refreshed when re-shown after events ran while hidden — they can show stale pop counts.

## 14. Git state at end of the paedia-stale-races session

- Modified (on top of §12): `WorldSimulationForm/PaediaForm.cs` (`_showRaces` always re-renders, refresh-on-show for Init/Races, empty-list hint, `_clearItems` + `_hoveredRace`), plus both docs.
- Temp build output (`C:\temp-paedia-out`) removed after verification; bin/obj updates are gitignored build output.

## 15. Paedia live-refresh session

### Symptom (user-reported)

After a second Start with the window open, the "No races in this world yet" hint kept showing even after Next Event generated new races — the user expected the open window to update as events ran (the §13 fix only refreshed on "Races" click / re-show).

### Root cause

Nothing told the Paedia that the world changed: `History.Simulate()` runs zero turns during `Generate()` (races are created later, by event resolution — `HistorySimulator.CreateRace`), so right after Start the hint is correct; but subsequent event batches add races to the same live `History` object and no re-render was triggered until the user clicked "Races" or re-showed the window.

### Fix

- `PaediaForm.RefreshRaces()` (new public method): re-renders via `_showRaces()` when the window is visible and in Races mode (no history-stack change, back-button state preserved).
- `WorldSimulatorForm.BtnNextEvent_Click` calls it after each event batch — one refresh per user action, even for 10/100/1000-event batches.

### Verification

- Full scenario traced: Start #2 with window open → hint; Next Event → race list replaces the hint automatically; further events keep it current. Window-closed path unchanged (refresh is a no-op when hidden; re-show renders current data).
- `dotnet build WorldSimulationForm.csproj` → **0 errors, 194 warnings** = baseline.
- Known limitation (unchanged): detail views (race/region) still don't refresh after events — they can show stale pop counts until navigated away and back.

## 16. Git state at end of the paedia-live-refresh session

- Modified (on top of §14): `WorldSimulationForm/PaediaForm.cs` (`RefreshRaces`), `WorldSimulationForm/WorldSimulatorForm.cs` (call after event batch), plus both docs.
- Temp build output (`C:\temp-paedia-out`) removed after verification; bin/obj updates are gitignored build output.
