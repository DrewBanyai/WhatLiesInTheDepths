# What Lies In The Depths — UI build

Ported from the fourteen-file spec set in `Hypnic Empire/Game UI`, following
`00 READ ME FIRST.html` and `_Whole Screen.html`, which is the authority.

> The game is **What Lies In The Depths**, in the spec set and in the code alike. (The spec
> files once called it *Deeper Still*; they have been updated.)

## One-click build

1. Open the project in Unity.
2. **Tools ▸ What Lies In The Depths ▸ Build All UI**.
3. **Tools ▸ What Lies In The Depths ▸ Drop UI_Screen into the open scene**.

Everything under `Assets/Prefabs`, `Assets/Art/Generated` and
`Assets/Resources/WhatLiesInTheDepths` is generated. The build is re-runnable: edit a
builder in `Assets/Scripts/Editor/Build` and run it again, and the prefabs are rewritten in
place. Hand edits to a prefab will be lost on the next build — change the builder instead.

## Layout

| Path | Assembly | What |
|---|---|---|
| `Assets/Scripts/Ursine/Runtime` | `Ursine.Runtime` | The portable incremental toolkit. See its own README. |
| `Assets/Scripts/Ursine/Editor` | `Ursine.Editor` | Sprite generation, prefab plumbing, TMP font assets. |
| `Assets/Scripts/WhatLiesInTheDepths` | `WhatLiesInTheDepths.Runtime` | This game: tokens, layout budget, routing, data, and every surface. |
| `Assets/Scripts/Editor` | `WhatLiesInTheDepths.Editor` | The prefab builders and the build menu. |

Ursine does not reference the game. That is enforced by the assembly definitions, so code
drifting back the wrong way fails to compile rather than going unnoticed.

## What is generated

| Path | What |
|---|---|
| `Assets/Prefabs/UI_Screen.prefab` | The composite: three columns, three bars, every destination, both utility pages and the ending |
| `Assets/Prefabs/UI_*.prefab` | 37 surface and leaf prefabs |
| `Assets/Art/Generated/*.png` | Placeholder sprites |
| `Assets/Resources/WhatLiesInTheDepths/Palette_*.asset` | Dream, Dusk, Parchment — whole token sets of 28 |
| `Assets/Resources/WhatLiesInTheDepths/TypeKit.asset` | The eight TMP font assets |
| `Assets/Fonts/*.ttf` | Cormorant Garamond 600 + italic, IBM Plex Mono 400/500/700, Karla 400/500/700, all OFL |

## Where the rules live

- **Tokens** — `Core/Tokens.cs`. The twenty-eight names, the three contrast rules, and the
  installation of Ursine's theming hooks. A color written as a literal is a bug: it will
  not follow a palette change.
- **Type** — the roles are Ursine's; which face fills each is `Editor/AssetFactory.cs`.
  The rule is absolute: Cormorant for anything named or written, IBM Plex Mono for anything
  that counts, Karla for labels only.
- **Layout budget** — `Core/Layout.cs`. The only numbers in the project that are fixed.
- **The underline rule** — `Core/Router.cs` and `UI/BarBinders.cs`. The underline lives
  where the button is, not where the content is, and exactly one exists at any moment.
- **One purse** — `Data/GameState.cs`, over `Ursine.Economy.Ledger`. Every menu that spends
  spends out of the ledger the left column draws.

## Every number here is a placeholder

`Data/PlaceholderContent.cs` carries the demo content from the spec set. Costs, rates,
ceilings, enemy strengths, unit powers, Oneiri counts, dream-day counts and percentages were
chosen to make the interface legible at a representative mid-game state. None of it is
balanced and none of it was ever meant to be. Names, blurbs, veil entries and journal prose
are first-draft placeholder copy; rewrite freely, nothing depends on the words except their
length.

## Art

Every glyph and plate is converted from the spec set's own inline SVG, rasterized at 2x
as section 16 asks, and indexed into `Resources/WhatLiesInTheDepths/Art.asset` so a view
can find the art for a thing by its id (`Core/Art.cs`).

| Folder | What | Drawn |
|---|---|---|
| `Art/Glyphs/Resource` | 15 resource icons | white, tinted by token |
| `Art/Glyphs/Tab` | 9 tab glyphs, plus the outward arrow | white, tinted |
| `Art/Glyphs/Focus` | 7 Focus task glyphs | white, tinted |
| `Art/Glyphs/Sigil` | 9 Revelation sigils | white, tinted |
| `Art/Glyphs/Vision` | 9 Vision glyphs | white, tinted |
| `Art/Glyphs/Source` | the Journal's 4 source marks | white, tinted |
| `Art/Glyphs/Construct`, `Place`, `Unit` | the glyphs the plates are composited from | white, tinted |
| `Art/Spec/Map` | 11 Mind Palace forms, pale fills and a ground line | own colors |
| `Art/Spec` | per-construct plates, per-place art, per-unit portraits, the veil plate, the mass, the eye, the road, the palace grounds, the ending | own colors |

Glyphs are white so a token can tint them — a glyph is always its label's color. Artwork
keeps its own colors, because artwork does not follow the palette.

The spec calls all of this placeholder too. Replacing a PNG with real art of the same name
is the whole hand-off; re-run the build to re-index.

**Two tab glyphs were never drawn.** Section 16 asks for eleven — six destinations, three
social links, two utilities — and the set draws nine. Journal and Discord have none, so
their bar items show the label alone rather than a stand-in shape beside real glyphs.

## Not done, and deliberately so

The spec's own open questions (brief section 9), not omissions:

- No save format. The 120s clock is real; `SaveClock.Saved` is the hook and nothing is written.
- Exit and Hard reset are drawn and asked; neither answer does anything yet.
- No sound. Options sets three volumes and a mute, and nothing anywhere says what makes a sound.
- No reduced-motion path, no text scaling, and the Dusk and Parchment palettes are
  unaudited placeholder values.
- The Journal needs 30–40 authored entries; six are here as examples.
- No keyboard or gamepad support, by decision. Mouse only throughout.
