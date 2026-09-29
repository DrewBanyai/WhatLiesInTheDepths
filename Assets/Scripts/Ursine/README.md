# Ursine

A small toolkit for incremental games. Extracted from **What Lies In The Depths**, but it
knows nothing about that game and must not learn: `Ursine.Runtime` and `Ursine.Editor` do
not reference the game's assemblies, so a dependency in the wrong direction is a compile
error rather than a thing someone notices later.

## What is here

### Runtime

| | |
|---|---|
| `Core/GameClock.cs`, `SaveClock.cs` | `GameClock` — one heartbeat everything accrues on, plus a per-frame event for things that are drawn rather than counted. `SaveClock` — an autosave interval (120 s by default) and how stale the save is. Ursine keeps the clock; what a save *contains* is the game's business. |
| `Core/BackgroundPacing.cs` | Keeps the game running when it is not in front: switches Run In Background on, and paces a minimized window down to a low frame rate (10 fps by default) so it does not draw flat out with nothing to sync to, restoring the old pacing when the window returns. On Windows it asks whether the window is minimized; other desktops slow down while unfocused; the web is left alone, since a browser stops a hidden tab itself. `BackgroundPacing.Install(fps)` sets it up. |
| `Core/SaveStore.cs` | Where a save lives on each platform. Native builds write a file under `Application.persistentDataPath`, through a temporary file and with the previous save kept as `.bak`, so a crash mid-save leaves the last good one. WebGL writes to the browser's `localStorage` under a fixed key, `SaveStore.WebPrefix` + slot (the product name by default), with the previous save as `.bak`: not `PlayerPrefs`, whose IndexedDB folder is named for the page's address, so a host that gives every upload a new address (itch.io) lost the save on every upload. A save still in `PlayerPrefs` is read once and carried over; if the browser refuses localStorage, `PlayerPrefs` is used. It stores and returns text under a slot name and never looks inside. |
| `Core/WebBridge.cs`, `UrsineWeb.jslib` | The few things a WebGL build needs from the page: localStorage get/set/remove, a text file download, and the browser's file picker (its result comes back through `SendMessage`). Off the web every call reports unavailable, so callers fall back. |
| `Core/SaveTransfer.cs` | A save the player can hold. `Export(fileName, text)` downloads it (web), asks where with the Editor's dialog (Editor), or writes it to the player's Downloads folder (desktop builds; the save folder's `Exports` if there is none). `Import(prefix, done)` opens the browser's file picker (web) or the Editor's (Editor), or reads the newest `prefix*.json` in that same folder (desktop builds, which have no picker of their own). Moves text only; the game decides whether it is a save. |
| `Core/Unlocks.cs` | The set of things that have happened. An unlock is a string; setting one is the only way anything opens up, and nothing unsets one but starting over. What a screen shows is a question asked of this set, so a load needs no catch-up code. |
| `Core/Fmt.cs` | How an incremental game writes a number: no abbreviation, a sign and one decimal on every rate including zero, period rather than frequency, whole units held (9.6 held is 9), and no floating-point dust in fractional amounts. |
| `Economy/Resource.cs` | `Res`, `Amount`, and `Refusal` — the difference between being short of something (time fixes it) and a cost above a ceiling (time never will). |
| `Economy/Ledger.cs` | One purse: spending, granting, judging a cost, the single sentence a refusal is allowed, and tick accrual. Plus `WorkerPool`, for interchangeable workers bound to tasks. |
| `Combat/Odds.cs` | Odds from a strength ratio, straight in log₂, so doubling an army is always worth the same amount of confidence, and what a loss costs. |
| `Geometry/CubicPath.cs` | A path of SVG-style cubic segments (`C` and `S`), sampled by arc length, so a point can be placed a fraction of the way along it. |
| `Theming/` | `Palette` and `Theme` — swap every color at once at runtime, with a contrast step. `ThemedGraphic` binds a graphic to a token so the swap reaches it. |
| `Text/TypeKit.cs` | Type roles named for the job rather than the typeface, and `Typeset` for applying them: small caps, figures, and CSS-style line height included. |
| `Text/Loc.cs` | Every word a player reads, looked up by key from one strings file with a section per language. A key missing from the current language falls back to English; one missing from English shows as `⟦key⟧` so it cannot go unnoticed. |
| `Text/MiniJson.cs` | A small JSON reader and writer that keeps object keys in written order. Unity's `JsonUtility` cannot read a dictionary, and a strings file or a save is mostly dictionaries. |
| `Audio/Jukebox.cs` | The music: a set of tracks played endlessly in a random order that never repeats recently played tracks, each dissolving into the next over two alternating sources. |
| `UI/` | One widget per file: `UiButton`, `FillBar`, `ProgressTrack`, `Stepper`, `CostPill`, `AttentionDot`, `SegmentedToggle`, `ToggleSwitch`, `VolumeBar`, `Fader`, `TintOnHover`, `FixedStage`, `Marquee` (a line that stands still when it fits and makes one unhurried pass when it does not), `ResourceHover` / `ResourceHint` (anything showing a cost names its resource; while the pointer rests on it `ResourceHint.Current` is that resource, held a moment after leaving so a pill rebuilt under a still pointer does not flicker it off, and a ledger can light the matching row), `LocalizedText` (a label that fills itself from the strings file and follows a language change), `PolygonGraphic` (filled convex shapes with an optional radial gradient and an anti-aliased outline, hit only inside the shape itself), plus `PathLine` (an anti-aliased stroked path, solid or dashed), `PrefabRect` (a prefab root's design size) and `SpriteSet` (a name-to-sprite lookup). |

### Editor

| | |
|---|---|
| `SpriteImport.cs` | Imports a folder of PNGs as sprites and indexes them by name. |
| `SpriteGen.cs` | Generated placeholder sprites: 9-sliced rounded rects and outlines, soft shadows, radial blooms, scrims, rings, discs, flat plates. A PNG whose pixels have not changed is not rewritten. |
| `Ui.cs` | Prefab construction plumbing — nodes, stretching, stacks, rows, grids, scroll views, saving and nesting prefabs. Geometry only, no opinions. Saving skips a prefab whose content has not changed. |
| `PrefabDiff.cs` | Compares a freshly built hierarchy with its saved prefab by content — objects, components and serialized values, with internal references compared by hierarchy position rather than file id — so an unchanged prefab is not rewritten with new file ids. |
| `FontAssetBuilder.cs` | TextMeshPro font assets from TTFs in the project. Given a character set, it teaches the asset every glyph up front, so play mode adds nothing and the file stays put; it relearns only when the TTF, the set or the atlas settings change (a hash kept in the asset's .meta). |

## The hooks a game installs

Ursine cannot know what a game's tokens mean, where its assets live or where its words are
kept, so a few things are handed to it, once, at startup:

```csharp
Ursine.Theming.Theme.DefaultPalette = () => Resources.Load<Palette>("MyGame/Palette_Default");
Ursine.Theming.Theme.ContrastFilter = (token, level, color) => /* what a contrast step does */;
Ursine.Text.TypeKit.Default        = () => Resources.Load<TypeKit>("MyGame/TypeKit");
Ursine.Text.Loc.Source             = () => Resources.Load<TextAsset>("MyGame/Strings").text;
Ursine.Text.Loc.Warn               = message => Debug.LogWarning(message);   // optional
```

`SaveStore.Warn` has a default and can be replaced the same way.

See `Assets/Scripts/WhatLiesInTheDepths/Core/Tokens.cs` and `Core/Strings.cs` for worked
examples. `Tokens.cs` declares the game's token enum, wraps `Theme` so call sites read
`Theme.Get(Tok.Ink)`, and installs the theming and type hooks; `Strings.cs` installs `Loc`.
Both do it from a `[RuntimeInitializeOnLoadMethod]` that is also an `[InitializeOnLoadMethod]`,
so editor-time prefab building gets them too.

## Two conventions worth keeping

**Tokens are indices.** A palette is an array; the names live in the game. This is what lets
one widget be dressed by any project, and it is why every widget takes its tokens as fields
rather than baking in a color.

**Mouse only.** Nothing here is a `Selectable` and nothing sets up EventSystem navigation.
Hover, press and pointer enter/exit are the whole vocabulary. A project that needs keyboard
or gamepad support should add it deliberately, not bolt it onto these.

## Not yet portable, but close

Living in the game today and worth lifting if a second project wants them:

- **The condition grammar** (`Dream.Holds`): an unlock id, or a count compared with `>=`
  (`done:absorb>=10`, `owned:hut>=2`, `held:silt>=40`). It is general apart from the names of
  the counts it knows.
- **The task model**: workers bound to a task, base time divided by workers, cost taken at
  completion, progress that holds rather than resets.
- **The destination router** with its one-underline rule (`Core/Router.cs`).
- **The save shape** (`Data/DreamSave.cs`): versioned JSON of only what the player changed,
  keyed by id, so a save survives a content change.
