# HANDOVER — world-generator-vs / WorldSimulationForm

Self-contained state log so the project can be picked up in a fresh session or on another machine.
Companion to `CodeReview-WorldSimulatorForm.md` (the full review + plan, saved 2026-09-03).
Last updated: after the GDI-fix session.

## TL;DR

- **Done & compile-verified:** the GDI handle leak (review bug #1, P0) is fixed — explicit `Dispose` everywhere, ownership model documented on `RenderObjects`.
- **Remaining P0 (two small items):** clear `_highlightedArea` in `RegionHoverBegin`; use `e.Location` in the click handler.
- Everything else open is tracked in the review doc (P1/P2).
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

## 2. Open work

### P0 remaining (small)

- **Stale race highlight** — `WorldSimulatorForm.cs:176-182`: `RaceHoverBegin` sets `_highlightedArea`, but `RegionHoverBegin` never clears it. Hover a race in the paedia, then move over sea → `RegionHoverBegin(null)` → OnPaint keeps drawing the old red outline forever. Fix: clear `_highlightedArea` when no subregion is hovered.
- **Click uses stale mouse position** — `WorldSimulatorForm.cs:246-251`: hit-tests against `_mouse` (last MouseMove) instead of `e.Location`.

### P1 (from the review)

- Non-legacy adapter landmines (dormant while `_legacy = true` is hardcoded): `GeneratorAdapter.cs:200` discards the seed in `Regenerate(int)`; `initialization()` never wires MouseMove/MouseClick/KeyDown; most MapModes throw `NotImplementedException` through the adapter (only Elevation renders).
- `ParametersPanel.cs:114`: `combo.SelectedItem = parameter` should be `parameter.Current`.
- `PaediaForm.cs:145`: `Substring(0, info.Length - 2)` crashes with zero tags; `PaediaForm.cs:149`: `RaceHoverEnd` args swapped.
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
