# What Lies In The Depths

[![Dev version](https://img.shields.io/badge/dynamic/json?url=https%3A%2F%2Fraw.githubusercontent.com%2FDrewBanyai%2FWhatLiesInTheDepths%2Fdevelop%2Fversion.json&query=%24.version&label=dev%20version&prefix=v)](https://github.com/DrewBanyai/WhatLiesInTheDepths/tree/develop)<br/>
[![Live version](https://img.shields.io/badge/dynamic/json?url=https%3A%2F%2Fraw.githubusercontent.com%2FDrewBanyai%2FWhatLiesInTheDepths%2Fmain%2Fversion.json&query=%24.version&label=live%20version&prefix=v)](https://github.com/DrewBanyai/WhatLiesInTheDepths/releases)<br/>
[![Build status](https://img.shields.io/github/actions/workflow/status/DrewBanyai/WhatLiesInTheDepths/build.yml?branch=main&label=build&logo=github)](https://github.com/DrewBanyai/WhatLiesInTheDepths/actions/workflows/build.yml)<br/>
[![Discord](https://img.shields.io/discord/1550284623790088302?color=7289DA&label=Discord&logo=discord)](https://discord.gg/PRSR6RxjU)

A short incremental game about delving into a dream world to defeat a nightmare.

## Overview

You are a dreamer who has gone down into your own sleep and not come back up. The dream
has layers, called veils, and the way through each one is to sink: fathom by fathom, dive
by dive, until the floor gives and the next, stranger shore opens beneath it. Every veil
is a place with its own story, written into a journal a paragraph at a time as you go
deeper, and every veil is closer to the thing waiting at the bottom: a nightmare that has
been growing in the dark while you were busy dreaming, and that you will have to face.

The descent is an incremental game. You gather what the dream offers (reverie, silt,
moonsilver, whispers), bind wisps to the work of gathering it, and put your own attention
wherever it counts most. Along the way you:

- **Focus** your effort on tasks that turn one resource into another.
- **Build constructs** in a Mind Palace that hold and produce more of it.
- **Come to realizations** that change how the dream works.
- **Channel visions** that reshape the descent. The golden ones are taken into the eye
  and never leave.
- **March an army** down a road of nightmare-held places, taking them one at a time,
  because what waits below will not be talked to.

What waits below has had a long time to grow, feeding on the dark while the dream went 
unwatched. It will not come up to meet you, and it will not go away on its own. It must be 
found, and it must be destroyed.

## The game and the engine

The code is split into two parts, and the split is deliberate.

**Ursine** (`Assets/Scripts/Ursine`) is a small, reusable toolkit for incremental games:
resources and a ledger that spends, grants and refuses; workers bound to tasks; a game
clock and autosave interval; combat odds; a palette-and-token theming system; type roles;
and a set of UI widgets (buttons, steppers, cost pills, progress bars, switches, stroked
paths) plus editor tooling for building prefabs, generating placeholder sprites and making
TextMeshPro font assets. It knows nothing about this game. Its assemblies
(`Ursine.Runtime`, `Ursine.Editor`) do not reference the game's assemblies, so a
dependency in the wrong direction is a compile error rather than something noticed later.
It is meant to be lifted out and dropped into the next incremental game as it stands. See
[its README](Assets/Scripts/Ursine/README.md) for what is there and the two hooks a game
installs at startup.

**What Lies In The Depths** (`Assets/Scripts/WhatLiesInTheDepths`, with its editor code in
`Assets/Scripts/Editor`) is everything specific to this game: its colour tokens and
palettes, its screens and their views, its data and placeholder content, and the builders
that turn the interface specification into prefabs. Where something could be written
generally, it went into Ursine and the game supplies only the particulars: the game's
`Tok` names give meaning to Ursine's numbered tokens, the game's type kit fills Ursine's
type roles, and the game's road is a path Ursine's line renderer knows how to draw.

The rule of thumb for a new file: if another incremental game could use it unchanged, it
belongs in Ursine; if it needs to know what a veil, a revelation or a wisp is, it belongs
to the game.

## License

This project uses a **split license**: the source code is reusable, the creative
work is not.

### Source code

The source code is available under the
[What Lies In The Depths Source Code License, Version 1.0](LICENSE).

- **Commercial use is permitted.** You may use the code in commercial products
  and sell those products, with no royalty or fee.
- **Modification, forking, and redistribution are permitted**, subject to the
  terms of the license.
- **Your game does not have to be open source.** The license has no copyleft and
  no source-disclosure requirement — you may keep your own code and your product
  entirely proprietary, and you may sublicense your own work as you see fit.

### Creative assets — not licensed

The project's **artwork, sprites, textures, models, animations, music, sound
effects, voice recordings, story, dialogue, lore, characters and character
designs, logos, branding, and the game's title** remain the proprietary
intellectual property of the copyright holder and are **not** covered by the
code license. Unity scenes, prefabs, and other serialized design data are
treated as creative assets as well. Storing them in this repository grants no
rights to them. See [LICENSE-ASSETS.md](LICENSE-ASSETS.md) for the full list and
Section 3 of [LICENSE](LICENSE) for the terms.

If you build on this code, bring your own art, audio, story, and branding.

### Required attribution

If you distribute a game or other product that incorporates this code, you must
display this attribution in the finished product:

```
What Lies In The Depths - Code by Drew Banyai
```

It belongs somewhere a player can actually find it — the credits, an
acknowledgements screen, an "About" section, or a third-party licenses screen —
and must be reachable without completing the game or making a purchase. Section
4 of [LICENSE](LICENSE) sets out the exact minimum standard. Attribution is
required only when the code is actually shipped in something you distribute; it
is not required for private use, study, or forking.

### A note on the license itself

This is a custom, source-available license, not MIT, Apache-2.0, or any other
standard license, and it is **not** OSI-approved or FSF-certified. Standard
permissive licenses require only that a notice be preserved in the source;
this project requires visible credit in the shipped product, which is why a
custom license is used.
