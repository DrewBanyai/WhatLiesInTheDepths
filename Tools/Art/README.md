# Placeholder art generator

Draws the chapter 1–5 placeholder art in the spec set's own idiom and rasterizes it the way
the existing PNGs under `Assets/Art` were made (checked pixel-for-pixel against them).

| File | What |
|---|---|
| `extract.js` | Pulls the spec's existing glyph dictionaries out of `Game UI/_Whole Screen.html` into `existing.json`. |
| `lib.js` | The spec's templates: line glyph, construct plate (432×150 @2x), place art (236×150 @2x), unit portrait (96×150 @2x), palace map form (48 grid @4x). Adds a night tone `n` for the plate and place art, and a dusk portrait for nightmares. |
| `new.js` | Every new drawing: Focus, resource, construct (glyph + map form), Vision, unit and place glyphs. |
| `build.js` | Turns `new.js` into render jobs with the right grid, stroke and size per folder. |
| `render.js` | Renders the jobs with Playwright's Chromium to transparent PNGs. |

```
node extract.js "<path to Game UI/_Whole Screen.html>"
node build.js
node render.js jobs.json ../../Assets/Art
```

Then **Tools ▸ What Lies In The Depths ▸ Build All UI** re-indexes the art.
