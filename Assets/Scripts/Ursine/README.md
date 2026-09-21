# Ursine

A small toolkit for incremental games. Extracted from **What Lies In The Depths**, but it
knows nothing about that game and must not learn: `Ursine.Runtime` and `Ursine.Editor` do
not reference the game's assemblies, so a dependency in the wrong direction is a compile
error rather than a thing someone notices later.

## What is here

| | |
|---|---|
| `Runtime/Core/GameClock.cs`, `SaveClock.cs` | `GameClock` — one heartbeat everything accrues on, plus a per-frame event for things that are drawn rather than counted. `SaveClock` — an autosave interval and how stale the save is. Ursine keeps the clock; what a save *contains* is the game's business. |
| `Runtime/Core/Fmt.cs` | How an incremental game writes a number: no abbreviation, a sign and one decimal on every rate including zero, period rather than frequency. |
| `Runtime/Economy/Resource.cs` | `Res`, `Amount`, and `Refusal` — the difference between being short of something (time fixes it) and a cost above a ceiling (time never will). |
| `Runtime/Economy/Ledger.cs` | One purse: spending, granting, judging a cost, the single sentence a refusal is allowed, and tick accrual. Plus `WorkerPool`, for interchangeable workers bound to tasks. |
| `Runtime/Combat/Odds.cs` | Odds from a strength ratio, straight in log₂, so doubling an army is always worth the same amount of confidence. |
| `Runtime/Geometry/CubicPath.cs` | A path of SVG-style cubic segments (`C` and `S`), sampled by arc length, so a point can be placed a fraction of the way along it. |
| `Runtime/Theming/` | `Palette` and `Theme` — swap every colour at once at runtime. `ThemedGraphic` binds a graphic to a token so the swap reaches it. |
| `Runtime/Text/TypeKit.cs` | Type roles named for the job rather than the typeface, and `Typeset` for applying them: small caps, figures, and CSS-style line height included. |
| `Runtime/UI/` | One widget per file: `UiButton`, `FillBar`, `ProgressTrack`, `Stepper`, `CostPill`, `AttentionDot`, `SegmentedToggle`, `ToggleSwitch`, `VolumeBar`, `Fader`, `TintOnHover`, `FixedStage`, plus `PathLine` (an anti-aliased stroked path, solid or dashed), `PrefabRect` (a prefab root's design size) and `SpriteSet` (a name-to-sprite lookup). |
| `Editor/SpriteImport.cs` | Imports a folder of PNGs as sprites and indexes them by name. |
| `Editor/SpriteGen.cs` | Generated placeholder sprites: 9-sliced rounded rects and outlines, soft shadows, radial blooms, scrims, rings, discs, flat plates. |
| `Editor/Ui.cs` | Prefab construction plumbing — nodes, stretching, stacks, rows, grids, scroll views, saving and nesting prefabs. Geometry only, no opinions. |
| `Editor/FontAssetBuilder.cs` | TextMeshPro font assets from TTFs in the project; an asset whose TTF has changed is cleared so it relearns its glyphs. |

## The two hooks a game must install

Ursine cannot know what a game's tokens mean or where its assets live, so two things are
handed to it, once, at startup:

```csharp
Ursine.Theming.Theme.DefaultPalette = () => Resources.Load<Palette>("MyGame/Palette_Default");
Ursine.Theming.Theme.ContrastFilter = (token, level, colour) => /* what a contrast step does */;
Ursine.Text.TypeKit.Default        = () => Resources.Load<TypeKit>("MyGame/TypeKit");
```

See `Assets/Scripts/WhatLiesInTheDepths/Core/Tokens.cs` for a worked example: it declares
the game's token enum, wraps `Theme` so call sites read `Theme.Get(Tok.Ink)`, and installs
all three from a `[RuntimeInitializeOnLoadMethod]` that is also an `[InitializeOnLoadMethod]`
so editor-time prefab building gets them too.

## Two conventions worth keeping

**Tokens are indices.** A palette is an array; the names live in the game. This is what lets
one widget be dressed by any project, and it is why every widget takes its tokens as fields
rather than baking in a colour.

**Mouse only.** Nothing here is a `Selectable` and nothing sets up EventSystem navigation.
Hover, press and pointer enter/exit are the whole vocabulary. A project that needs keyboard
or gamepad support should add it deliberately, not bolt it onto these.

## Not yet portable, but close

Living in the game today and worth lifting if a second project wants them: the task model
(workers bound to a task, base time divided by workers, cost taken at completion, progress
that holds rather than resets), and the destination router with its one-underline rule.
