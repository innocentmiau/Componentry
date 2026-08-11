# Icon sources

The vector originals of the icons in the folder above. **Unity never looks in here**: the
folder ends with a `~`, which is Unity's way of saying "not an asset", so nothing in it is
imported, and nothing in it is what the bar draws.

## Changing an icon

The bar draws the **PNGs** in the folder above, so those are what have to change. Two ways:

- Open the `.png` in any image editor and paint over it. Nothing else needs doing; Unity
  reimports it and the bar is drawing the new one within a second or two.
- Or edit the `.svg` here, export it to PNG at **64 x 64** with a transparent background, and
  replace the matching file above. Keep the source in step with the PNG so the next person
  editing it starts from what is actually on screen.

## Why there are two of each

`Copy.png` is for the light theme and `d_Copy.png` for the dark one, following the `d_`
prefix Unity uses for the same reason. They are the same drawing in two colours: dark grey
on light, light grey on dark. An icon with only one of them looks like a smudge on the theme
it was not drawn for, which is exactly what happened with the editor's own clipboard icon
and is why these exist at all.

The colour is the only difference, so an SVG here has no colour of its own: it is drawn with
`currentColor` and coloured on export.

## Sizes

Drawn at 64 x 64 and shown between about 12 and 28 pixels, depending on the bar's size
setting. That is why the strokes are thick in the source: a stroke of 6 at 64 comes out at
roughly one pixel at the smallest size, which is what keeps the shape readable there.

If an icon of yours goes muddy when it is small, it is almost always the strokes being too
thin rather than the size being too small.
