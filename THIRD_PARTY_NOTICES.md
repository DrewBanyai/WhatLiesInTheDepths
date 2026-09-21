# Third-Party Notices

**What Lies In The Depths** includes material owned by third parties. That material is
**not** covered by this project's [LICENSE](LICENSE) (source code) or
[LICENSE-ASSETS.md](LICENSE-ASSETS.md) (proprietary creative assets). Each item below is
used under its own license, and anyone reusing this repository must follow those terms
for it. See Section 6 of [LICENSE](LICENSE).

## Fonts

All four typefaces are licensed under the
[SIL Open Font License, Version 1.1](https://openfontlicense.org). The full license
text for each ships beside the font files.

| Typeface | Copyright | Files | License text |
|---|---|---|---|
| Karla | Copyright 2019 The Karla Project Authors (https://github.com/googlefonts/karla) | `Assets/Fonts/Karla-Regular.ttf`, `Karla-Medium.ttf`, `Karla-Bold.ttf` | `Assets/Fonts/OFL-Karla.txt` |
| Cormorant Garamond | Copyright 2015 the Cormorant Project Authors (github.com/CatharsisFonts/Cormorant) | `Assets/Fonts/CormorantGaramond-SemiBold.ttf`, `CormorantGaramond-SemiBoldItalic.ttf` | `Assets/Fonts/OFL-CormorantGaramond.txt` |
| IBM Plex Mono | Copyright © 2017 IBM Corp. with Reserved Font Name "Plex" | `Assets/Fonts/IBMPlexMono-Regular.ttf`, `IBMPlexMono-Medium.ttf`, `IBMPlexMono-Bold.ttf` | `Assets/Fonts/OFL-IBMPlexMono.txt` |
| Liberation Sans | Digitized data copyright (c) 2010 Google Corporation with Reserved Font Arimo, Tinos and Cousine; Copyright (c) 2012 Red Hat, Inc. with Reserved Font Name Liberation | `Assets/TextMesh Pro/Fonts/LiberationSans.ttf` (TextMesh Pro's default font) | `Assets/TextMesh Pro/Fonts/LiberationSans - OFL.txt` |

### Modified fonts

**Karla** (all three weights) is a Modified Version under the OFL. Its class-based kerning
(OpenType GPOS PairPos format 2) has been expanded into glyph-pair kerning (PairPos
format 1) so that Unity's TextCore can read all of it. Outlines, metrics and font names
are unchanged, and Karla declares no Reserved Font Name. The details are in
`Assets/Fonts/Karla - kerning note.txt`.

The other fonts are unmodified.

### Generated font assets

The TextMesh Pro font assets (`* SDF.asset`) beside each font are generated from these
fonts, contain their glyphs, and remain under the same OFL terms as the fonts they were
generated from.

## Unity engine and packages

The project is built with the Unity Editor and the Unity packages listed in
`Packages/manifest.json` (including TextMesh Pro via `com.unity.ugui`, the Universal
Render Pipeline, and the Input System). These are **not** redistributed as source in this
repository; they are fetched by Unity from its package registry and are governed by the
[Unity Terms of Service](https://unity.com/legal) and each package's own license
(generally the Unity Companion License).

The `Assets/TextMesh Pro/` folder holds TextMesh Pro's essential resources (shaders,
settings, style sheets and the Liberation Sans font above), which Unity copies into a
project when TextMesh Pro is first used. They remain subject to Unity's terms.

The `Assets/Settings/` render and input settings, and the sample scene came from Unity's 
2D project template and remain subject to Unity's terms.
