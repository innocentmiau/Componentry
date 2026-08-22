# Componentry

An editor-only Unity tool that puts a row of buttons at the top of the Inspector, one for every component on the selected object.

Scrolling a long Inspector to find one component is slow, and you have to remember what is on the object to know where to look. The bar shows all of it at once, above the Transform, and each button does something useful. 

![The component bar sitting above the Transform, with one chip per component on the selected object](https://github.com/innocentmiau/Componentry/blob/main/Documentation~/images/componentDock.png)

## What you can do

- **See every component at a glance**, with its icon and name.
- **Click one to show only it**, so the Inspector shows the component you are working on and nothing else.
- **Search for a component by name**, or for a field by name.
- **Drag to reorder** components, or drag one into the Hierarchy to copy it onto another object.
- **Copy and paste components** between objects, several at a time.
- **Switch components off** without scrolling to their checkbox.
- **Spot missing scripts** straight away instead of finding them buried in the Inspector.
- **Add a component** from the top of the Inspector instead of the bottom.

## Requirements

- Unity **6.3** (`6000.3`) or newer
- Editor-only: no runtime code, no dependencies, nothing shipped in your build

## Installation

In Unity, open **Window > Package Manager**, click **+ > Add package from git URL**, and paste:

```
https://github.com/innocentmiau/Componentry.git
```

To pin a version, add a tag: `...Componentry.git#v1.0.0`

Or add it to `Packages/manifest.json` yourself:

```json
"com.andreleandrodev.componentry": "https://github.com/innocentmiau/Componentry.git"
```

There is nothing to set up. Select a GameObject and the bar is there.

## Using it

### What a chip does

| Do this to a chip | What happens |
| --- | --- |
| **Click** | The Inspector shows only that component |
| **Click it again** | Everything comes back |
| **Click another** | That one is shown too |
| **Shift click** | Every chip between the two is taken as well |
| **Right click** | A menu: switch off, copy, paste |
| **Drag along the bar** | The component moves up or down the Inspector |
| **Drag into the Hierarchy** | The component is copied onto another object |
| **Hover** | The full type name, so two same-named scripts can be told apart |

![A chip clicked in the bar, with the Inspector below showing only that component](https://github.com/innocentmiau/Componentry/blob/main/Documentation~/images/showComponentsSelected.png)

Nothing is changed on your object when you click a chip. The components are only hidden from view, the way a closed foldout is, and they come back when you clear the filter.

### The buttons that are not components

| Button | What it is |
| --- | --- |
| **+** | Opens Unity's Add Component search. Always first, so it never moves |
| **All** | Clears the filter. Appears when something is filtered |
| **Missing Script** | Red, and only there when a script cannot be found. Click to show it, right click to remove it |

### Searching

Type in the field above the chips and the Inspector narrows to the components whose name matches. **Escape** or the cross at the end of the field clears it.

Turn on **Search Inside Properties** in the settings and it searches the fields inside components too, so typing `mass` finds it wherever it lives. Fields found this way are shown plainly, the way the Inspector's Debug mode shows them, which is why it is off to start with.

### Copying components to another object

1. Click the chips you want.
2. Press the **copy** button, at the left of the search field.
3. Select the other object.
4. Press the **paste** button.

![Right clicking one of several picked chips, showing the menu entries to copy or paste them all at once](https://github.com/innocentmiau/Componentry/blob/main/Documentation~/images/copyOrPasteMultiComponents.png)

The same two are in the right click menu. What you copy is a snapshot, so changing a component afterwards does not change what gets pasted.

### The Transform

Right clicking the Transform on its own gives it a menu of its own, because there is more than one thing you might mean:

- Copy or paste the **whole component**
- Copy or paste the **world transform**, which keeps the object in the same place in the scene even when the new parent is different
- Copy or paste just the **position**, **rotation** or **scale**

All in one list, instead of the submenus Unity puts them behind. Paste entries are greyed out when there is nothing of that kind to paste.

These use Unity's own copy and paste, so anything you copy here can be pasted from the Transform's own header too, or into any Vector3 field.

### When there is no bar

- **Objects with only a Transform** get no bar, since there would be nothing to say. Turn that off with **Hide When Only A Transform**.
- **With several objects selected**, the bar hides itself, because the Inspector then only shows the components those objects have in common.
- A **locked** Inspector keeps showing the object it was locked to, and so does its bar.

## Settings

**Tools > Componentry > Settings**, or **Edit > Preferences > Componentry**. Both are the same page. Settings are saved per person, not per project, so they follow you between projects.

| Setting | Default | What it does |
| --- | --- | --- |
| **Component Bar** | On | The whole thing, on or off |
| **Add Component Button** | On | The **+** at the front of the bar |
| **Hide When Only A Transform** | On | No bar on empty objects |
| **Size** | Default | Compact, Small, Default, Large or Extra Large |
| **Rows At Most** | 6 | How tall the bar may grow before leaving chips out |
| **Names** | Only when needed | Whether chips show the component name next to the icon |
| **Show All Only When Needed** | On | Whether the **All** button is always there |
| **Search Inside Properties** | Off | Search the fields inside components as well as names |

## Changing how it looks

- **Sizes and spacing**: every measurement is a number in [`Editor/Core/Constants.cs`](Editor/Core/Constants.cs), including the multiplier behind each size preset. Change one, let Unity recompile, done.
- **Starting values**: what each setting is before anyone changes it lives in [`Editor/Core/Defaults.cs`](Editor/Core/Defaults.cs).
- **Icons**: the copy and paste icons are in [`Editor/Icons`](Editor/Icons), one file per theme. Paint over a `.png` and the button changes.

## More detail

[the full documentation](https://github.com/innocentmiau/Componentry/blob/main/Documentation~/Componentry.md) explains how each part works and why it behaves the way it does.

## License

[MIT](LICENSE.md) (c) André Leandro
