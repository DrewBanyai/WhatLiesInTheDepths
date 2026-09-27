# What Lies In The Depths — the game

The game itself, from the first Journal entry to both endings, built on the reusable toolkit
in `Assets/Scripts/Ursine`. The interface is ported from the fourteen-file UI spec set
(`Game UI/`, outside the repository), following `00 READ ME FIRST.html` and
`_Whole Screen.html`, which is the authority.

## One-click build

1. Open the project in Unity.
2. **Tools ▸ What Lies In The Depths ▸ Build All UI**.
3. **Tools ▸ What Lies In The Depths ▸ Drop UI_Screen into the open scene** (only needed
   for a fresh scene; `Assets/Scenes/Game.unity` already holds it).

Everything under `Assets/Prefabs`, `Assets/Art/Generated` and
`Assets/Resources/WhatLiesInTheDepths` is generated. The build is re-runnable: edit a
builder in `Assets/Scripts/Editor/Build` and run it again, and the prefabs are rewritten in
place. Hand edits to a prefab will be lost on the next build — change the builder instead.

## Layout

| Path | Assembly | What |
|---|---|---|
| `Assets/Scripts/Ursine/Runtime` | `Ursine.Runtime` | The portable incremental toolkit. See its own README. |
| `Assets/Scripts/Ursine/Editor` | `Ursine.Editor` | Sprite generation, prefab plumbing, TMP font assets. |
| `Assets/Scripts/WhatLiesInTheDepths/Core` | `WhatLiesInTheDepths.Runtime` | Tokens, layout budget, routing, strings, settings, the road, art lookup. |
| `Assets/Scripts/WhatLiesInTheDepths/Data` | `WhatLiesInTheDepths.Runtime` | The dream: its state, its rules, its content and its save. |
| `Assets/Scripts/WhatLiesInTheDepths/UI` | `WhatLiesInTheDepths.Runtime` | One view per surface. Views draw state; they do not own it. |
| `Assets/Scripts/Editor` | `WhatLiesInTheDepths.Editor` | The prefab builders, the build menu and the Dream Debugger. |
| `Tools/Content` | — | The content source (`design.py`), its generator and the simulated player. See its README. |
| `Tools/Art` | — | The placeholder art generator. See its README. |

Ursine does not reference the game. That is enforced by the assembly definitions, so code
drifting back the wrong way fails to compile rather than going unnoticed.

## How the dream runs

`Data/Dream.cs` is the whole game as a plain class with no Unity lifetime, so it can be
played headlessly exactly as the scene plays it. `Data/GameState.cs` owns one, drives it from
Ursine's clocks, and forwards its surface so every view reads `GameState.I`.

Three rules hold the progression together:

1. **Unlocks only get set.** Nothing unsets one but a hard reset.
2. **Whether a thing is on screen is read from state, never pushed at a view.** A view asks
   `Shown(...)` whenever the state changes, and rebuilds when its answer changes.
3. **Appearing latches.** The first time a thing's requirements hold it is recorded as shown
   and wears its dot; it never disappears again except by being used up.

A requirement is a condition string read by `Dream.Holds`: an unlock id (`start`,
`rev:cold`, `built:altar`, `won:gate`, `max:silt`, `vision:door`) or a count compared with
`>=` (`done:absorb>=1`, `owned:hut>=2`, `parted>=20`, `fathoms>=15`, `held:dread>=25`).

Focus progress, the dive, Vision channelling and battles all run inside the dream, so
switching the center destination touches nothing else (spec section 15). Constructs,
Realizations, Visions and taken places apply real effects — rates, ceilings, gain, speed,
dive cost and yield, housing, unit and enemy strength, prices — and every card's effect line
is written from those effects, so a card cannot claim what its numbers do not do. Upgrades
(`Dream.upgrades`) change a thing in place (Warren → Abode, Echo → Whispers, each Sworn unit
→ its nightmare) while keeping its id, count and bindings.

## Content

The whole path is authored in `Tools/Content/design.py`: 18 resources, 17 Focus tasks,
22 constructs, the Realizations and Visions, 100 veils in 20 reaches across five chapters,
30 places on the road, the units, 31 Journal entries, the Achievements and both endings.
`Tools/Content/gen.py` turns it into:

- `Data/OpeningContent.cs` — generated; never edit it by hand.
- `Assets/Resources/WhatLiesInTheDepths/Strings.json` — every word the game shows, by key.

```
python3 Tools/Content/gen.py Assets
```

`Data/PlaceholderContent.cs` is the spec set's original mid-game snapshot. It is kept only as
a debug start (`GameState.startMidGame`), which shows everything at once and never loads or
writes a save.

Every number is a first-pass placeholder, tuned only far enough that the simulated player in
`Tools/Content/sim` walks the whole path (about 11½ hours for the bot on either ending).
After any change to `design.py`, rerun `Tools/Content/sim/report.py` and update the
*Simulated Playthrough* doc.

## Saving

- The dream saves every 120 s (`SaveClock`), on Exit, and when the application quits.
- `Data/DreamSave.cs` writes versioned JSON of only what the player has changed: unlocks,
  holdings, what is built, bound, realized, poured, mustered and taken, depth, and what has
  been read. Everything authored comes from the content on every start, so a save made before
  a balance change still loads. A saved id the content no longer has is skipped.
- Upgrades are not restored field by field; the dream re-applies them from its own content.
- `Ursine.SaveStore` decides where the save lives: a file with a `.bak` beside it on native
  builds, `PlayerPrefs` (IndexedDB) on the web. The slot is `dream`.
- Options are kept in a separate `settings` slot (`Core/GameSettings.cs`), so a hard reset
  starts the dream over without resetting volumes, palette or contrast.
- **Exit** saves and quits (on the web it only saves; a page cannot close itself).
  **Hard reset** deletes the save and reloads the scene into a new dream.

## Where the rules live

- **Tokens** — `Core/Tokens.cs`. The twenty-eight names, the three contrast rules, and the
  installation of Ursine's theming hooks. A color written as a literal is a bug: it will
  not follow a palette change.
- **Type** — the roles are Ursine's; which face fills each is `Editor/AssetFactory.cs`.
  The rule is absolute: Cormorant for anything named or written, IBM Plex Mono for anything
  that counts, Karla for labels only.
- **Words** — `Core/Strings.cs` installs `Strings.json` as Ursine's `Loc` source. Only an
  `en` section exists today.
- **Layout budget** — `Core/Layout.cs`. The only numbers in the project that are fixed.
- **The underline rule** — `Core/Router.cs`, with `UI/DestinationBinder.cs` and
  `UI/UtilityBinder.cs`. The underline lives where the button is, not where the content is,
  and exactly one exists at any moment.
- **One purse** — `Data/Dream.cs`, over `Ursine.Economy.Ledger`. Every menu that spends
  spends out of the ledger the left column draws.

## What is generated

| Path | What |
|---|---|
| `Assets/Prefabs/UI_Screen.prefab` | The composite: three columns, three bars, every destination, both utility pages and the ending |
| `Assets/Prefabs/UI_*.prefab` | 43 surface and leaf prefabs |
| `Assets/Art/Generated/*.png` | Placeholder sprites |
| `Assets/Resources/WhatLiesInTheDepths/Palette_*.asset` | Dream, Dusk, Parchment — whole token sets of 28 |
| `Assets/Resources/WhatLiesInTheDepths/TypeKit.asset` | The eight TMP font assets |
| `Assets/Resources/WhatLiesInTheDepths/Art.asset` | The art index, by id (`Core/Art.cs`) |
| `Assets/Fonts/*.ttf` | Cormorant Garamond 600 + italic, IBM Plex Mono 400/500/700, Karla 400/500/700, all OFL |

## Art

The spec set's own inline SVG, rasterized at 2x, plus everything since drawn in the same
templates by `Tools/Art`. All of it is indexed into `Art.asset` so a view can find the art
for a thing by its id.

| Folder | What | Drawn |
|---|---|---|
| `Art/Glyphs/Resource` | 18 resource icons | white, tinted by token |
| `Art/Glyphs/Tab` | 13 bar glyphs: six destinations, three utilities, three social links, the outward arrow | white, tinted |
| `Art/Glyphs/Focus` | 16 Focus task glyphs | white, tinted |
| `Art/Glyphs/Sigil`, `Vision` | 9 Revelation sigils, 17 Vision glyphs | white, tinted |
| `Art/Glyphs/Construct`, `Place`, `Unit` | 22, 31 and 16 glyphs the plates, pins and portraits are composited from | white, tinted |
| `Art/Glyphs/Source`, `Ui`, `Achievement` | the Journal's 4 source marks, 7 interface marks, 1 achievement mark | white, tinted |
| `Art/Spec` | construct plates, place art, unit portraits (nightmares on a dusk ground), the veil plate, the mass, the eye, the road, the palace grounds, the ending | own colors |
| `Art/Spec/Map` | Mind Palace forms, pale fills and a ground line | own colors |

Glyphs are white so a token can tint them — a glyph is always its label's color. Artwork
keeps its own colors, because artwork does not follow the palette. The finale's places and
The Night Kiln use a night tone, so the dark path and the last road read differently.

The spec calls all of this placeholder too. Replacing a PNG with real art of the same name
is the whole hand-off; re-run the build to re-index.

## Music

Five tracks in `Assets/Music`, played by Ursine's `Jukebox`. `Editor/Build/MusicFactory.cs`
imports them streamed rather than decompressed into memory. The foot of the left column
(`UI/NowPlayingView.cs`) names the track playing.

## Editor tools

Under **Tools ▸ What Lies In The Depths**:

| Item | What |
|---|---|
| Build All UI | Rebuilds every prefab, palette, type kit and the art index. |
| Rebuild Placeholder Sprites / Palettes and Type Kit / Reimport Music | One part of the above. |
| Dream Debugger (Ctrl+Shift+D) | Play mode only. Reaches into the running dream to add fathoms, set or take back unlocks and jump ahead, through the same `Dirty()` the game's own buttons use. |
| Capture Game View (Ctrl+Shift+F12) | Saves the game view to `Captures/`. |
| Sync Version From version.json | Copies the repository's `version.json` into Player Settings (also runs on every script reload). |
| Find Missing Scripts | Lists components whose script is gone. |

## Not done yet

- **Achievements.** Eight story marks (the first veil parted, each chapter, each ending),
  not the spec's thirty-two. All share one glyph. There are no lifetime counters or
  watchers yet, which most of the spec's marks need.
- **Sound effects.** Options sets master, music and effects volumes and a mute; only the
  music exists, so the effects volume does nothing yet.
- **Localization.** The mechanism is in place; only English is written.
- **Accessibility.** No reduced-motion path and no text scaling. The Dusk and Parchment
  palettes are unaudited placeholder values.
- **Offline progress.** Time away does not accrue.
- **Social links.** Discord is live; Reddit and Twitter have no address and stay inert.
- **Some Doors Stay Shut** does not yet cut the price of The Beacon.
- **Keyboard and gamepad:** not supported, by decision. Mouse only throughout.
