# Componentry

Componentry is an editor-only tool that puts what an object is made of at the top of the Inspector, instead of leaving it to be found by scrolling.

## The component bar

Select a GameObject and a row of chips appears between the header with the name, tag and layer and the Transform below it. There is **one chip per component the Inspector is drawing**, in the order it draws them, each with the component's own icon and type name. Hovering one gives the full type name, namespace included, which is the quick way to tell two same-named scripts apart.

The row wraps onto as many lines as it needs, up to six, and grows and shrinks with the Inspector as it is docked and resized.

**What is on the object but not in the Inspector is not in the bar either.** Components hidden with `HideFlags.HideInInspector` and the particle system renderer, which Unity folds into the particle system's own editor, are left out so the bar and the headers under it always agree. A component whose script cannot be loaded gets no chip of its own either, but it is not ignored: see **Missing scripts** below.

## Adding a component

**The plus at the very front of the bar** opens Unity's own Add Component search, the same one the button at the bottom of the Inspector opens, with the same list and the same "new script" flow at the end of it.

It is first on purpose, and stays first. At the other end it would sit after the last component and move every time one was added or removed, which is a button to be looked for each time rather than one that is simply there. At the front it is in the same place on every object, and the chips that come and go come and go behind it.

It adds to **the object the bar is showing**, which is not always the selected one: an Inspector that has been locked is showing what it was locked onto, and a component added from that bar belongs there.

**Add Component Button** in the settings takes it away.

An object with nothing on it but a Transform has no bar and so no plus, but the Inspector's own button is a line or two below on an object that empty, which is the case the bar was never needed for.

## The Transform

Right clicking the Transform chip, **on its own**, gives a menu of its own:

```
Copy Component          Paste Component Values
Copy World Transform    Paste World Transform
Copy Position           Paste Position
Copy Rotation           Paste Rotation
Copy Scale              Paste Scale
```

All nine in one list. The Inspector puts the same things behind a **Copy** folder and a **Paste** folder, which turns each of them into a hover and a second aim of the mouse for something that is one line of what somebody wanted.

**The paste entries are greyed by what is actually on the clipboard.** Copy a position and only Paste Position lights up. That is not this package guessing: it is the same check that greys out the Inspector's own entries, asked directly.

**Values copied before the last component are not offered at all.** The editor keeps components and values in two different places, so copying a Mesh Collider does not touch a position copied an hour earlier, and left alone the menu would still be offering to paste that position long after anybody stopped thinking about it. What is remembered instead is what the clipboard held when a component was last copied: if it still holds exactly that, nothing has been copied since and the values in it are the older of the two, so they are greyed.

Nothing is cleared to do that. Emptying the values clipboard means writing over what the machine has on its clipboard, which is very often a piece of text somebody wanted and would not expect a Unity package to take away.

**Paste Component Values is greyed when the last component copied from the bar was something other than a Transform.** Pasting a Mesh Filter's values onto a Transform is not a thing that can happen, and an entry that offers it and then answers with a dialog has lied.

It is the one entry here the editor cannot be asked about: its component clipboard is not readable, so what the package knows is only what was copied **through the bar**. After a copy made from a component's own header it knows nothing, and nothing means the entry is offered rather than refused, since an entry that might work is worth having and only one that certainly will not is worth taking away.

**World Transform is the interesting one.** Position, rotation and scale are the numbers as typed, which are relative to whatever the object is parented under. World placement is where the object actually is in the scene, so pasting it onto an object under a different parent works out the numbers that put it in the same place, rather than copying numbers across and sending it somewhere else. It is what to reach for when moving something between parents.

Every one of these is Unity's own call, so what is copied here pastes from the Transform's own header, into a Vector3 field, or anywhere else the editor accepts it. It is one clipboard, not a second one that happens to look the same.

The Transform keeps the ordinary menu when it is right clicked as **one of several picked chips**, since that is a question about the several.

## Missing scripts

A component whose script cannot be found leaves a box in the Inspector saying so, and on a crowded object that box is somewhere down the list where nobody is looking. **The bar puts a red chip at the front for it**, saying `Missing Script` or `3 Missing Scripts`, so an object with a broken component says so at the top before anything else is read.

**Click it** and the Inspector shows only those boxes. **Right click it** for **Remove Missing Scripts**, which asks first: it cannot be undone, and whatever was set on them goes with them. Somebody who has just moved a script file and is about to move it back would not thank us for doing that quietly.

They have no chip each, since a missing script has no name and no icon to be told apart by. One chip stands for all of them.

### What this fixed on the way

A missing script has a box in the Inspector but is not a component the bar can draw, and the bar finds out which box belongs to which component **by counting along them**. Counting the chips rather than the boxes was out by one for every missing script above, so on an object with one, narrowing the Inspector to a component hid the wrong one.

The counting is now done against everything the Inspector drew, missing scripts included, and only the drawing is done against the chips. An object with a broken script filters correctly whether or not the chip for it is ever used.

## Carrying components to another object

Pick the chips you want, press the **copy button** at the left of the search field, select another object, and press the **paste button** beside it. Everything picked arrives on the other object, in the order it sat in on the first one.

The same two are in the right click menu. **Copy Mesh Collider** on a chip on its own, **Copy 5 Components** when the right click lands on one of five picked, and **Paste N Components Here** whenever something is waiting. It is one entry saying a different number rather than two different things, so copying one component and copying five are the same gesture.

Copying **one** component also fills the Inspector's own clipboard, so the **Paste Component Values** and **Paste Component As New** on any component's header work with it afterwards. Copying several does not: that clipboard holds one component, and there would be no honest answer to which of them it should be.

### The two pastes

They are different things and the menu says which is which.

**Paste 1 Component As New** puts another component on the object. Copy a collider, select something else, and it now has a collider it did not have before.

**Paste Mesh Collider Values** pours what was copied into a component the object **already has**, leaving it as the one component it was. Copy a collider that is set up the way you want, right click the collider on another object, and that one now matches it.

The values entry is only there when **one** component was copied, since values come from one thing, and only on chips of that **same type**, which is Unity's rule about pasting values. Where it would do nothing it is absent rather than greyed, so the menu never offers something that cannot happen.

With several chips picked and the right click landing on one of them, values go into every picked component of that type at once, and the entry says how many.

**What is carried is a copy taken at the moment of copying**, not the components that were copied. Copy a collider, change it, paste, and what lands is the collider **as it was when it was copied**, which is what a clipboard is for and what the Inspector's own Copy Component does. Remembering which components they were would paste whatever they had become by then, which is not copying at all.

The copy is made by handing each component to Unity's own copy and paste, onto a GameObject nobody can see. So it is a real component of the same type, made the way the Inspector makes one, and every kind of field that survives Copy Component survives this: object references, awkward types, whatever a type does with its own serialization. None of it is this package's business.

That hidden object lives in a **preview scene**, Unity's own idea of a scene that is not part of the project. Nothing in it is saved, nothing is drawn, and copying never makes the scene you are working in dirty.

Three things follow.

The whole paste is **one entry in the undo history**, however many components it put down.

Pasting **leaves Unity's own single component clipboard** holding the last thing pasted, since that clipboard is what carries each component across.

The clipboard is **emptied by a script recompiling or by play mode starting**, since nothing in a preview scene is meant to outlive the moment, and by **Forget Copied Components** in the right click menu.

Transforms are never carried. Everything has one already, so a Transform among the picked chips is left behind rather than counted and then refused.

## Dragging components out of the bar

Dragging a chip **within the bar** reorders the components. Dragging one **out of the bar and into the Hierarchy** carries it there.

**Dropped on an object**, the components land on that object. It is the copy and the paste said as one gesture instead of three.

**Dropped on nothing**, on the empty space below the objects or between two of them, an empty GameObject is made and they land on that, in whatever the drop was inside. It is the quickest way there is to lift a working set of components out of one object and into a new one of its own, and it undoes as one thing.

Either way the object that was dropped on, or made, becomes the selection, so the Inspector is showing what just happened.

**Dragging one of several picked chips carries all of them**, the same way right clicking one of them acts on all of them. Dragging a chip that is not picked is about that chip alone. A Transform is never carried, since everything has one.

Nothing leaves the bar until the cursor does: while the drag is inside the bar it is still a reorder, and stepping outside is what turns it into a carry.

## Reordering components

**Drag a chip and the components reorder.** The bar is already the components in the order the Inspector draws them, so moving one along the bar is the same thing as moving it up or down the Inspector, and it is a shorter distance to drag.

The chip being carried is **outlined in yellow** for as long as it is held, which is the colour these packages use for something that has been set up and has not happened yet. That is exactly what a chip in the middle of a drag is, and it is also what says which chip will move rather than whichever one the cursor is passing over.

**Nothing moves until you let go.** While the mouse is down the only thing changing is the order the chips are drawn in, so the bar answers immediately and the object is not touched. On release the component is moved once, which is one edit and one entry in the undo history however far it travelled and however many chips it passed on the way. Dropping it back where it started is not an edit at all and does nothing.

**Escape** during a drag puts it back.

A press only becomes a drag once the mouse has moved a little way, so clicking a chip still picks it. That distance is `DRAG_THRESHOLD` in `Constants.cs`.

The **Transform** cannot be dragged and nothing can be dropped above it, which is Unity's rule about the Transform rather than one of this package's.

## Right clicking a chip

Right clicking a chip opens a short menu of the things the bar can do to a component, with the component's own Inspector menu at the bottom of it.

## Switching components off

**Disable Mesh Renderer** is the first thing in that menu, and it does what the checkbox on the component's header does. **A switched off component's chip fades**, so the bar says which components are doing anything as well as which ones are there.

With several chips picked, and the right click landing on one of them, the entry becomes **Enable 5 Components** and **Disable 5 Components** and acts on all of them at once, as a single entry in the undo history. Both are offered rather than one that guesses: a set with some on and some off has no single opposite. Right clicking a chip that is **not** picked is asking about that chip, so it acts on that one alone.

Components with **no checkbox on their header**, a Transform or a Mesh Filter, are left out of this rather than offered something that cannot be done. Whether a component has one is asked of Unity, not worked out from its type: `Behaviour`, `Renderer` and `Collider` all have an `enabled` and share no base that does, so a list kept here would be wrong the first time somebody used a type that was not on it.

## The component's own menu

The chip's menu holds what the bar can do that the Inspector cannot, and nothing that it can. **Reset, Remove Component, Move Up and Move Down, Copy Component and the two pastes stay on the component's own header**, where they already were.

They are not far. Left clicking a chip narrows the Inspector to that component, which puts its header at the top of the window, and right clicking a header is what it always was.

This is worth doing rather than rebuilding, and the pastes are the reason. Whether **Paste Component As New** and **Paste Component Values** are offered depends on what is on the component clipboard, and **nothing outside Unity can find that out**. That menu lives in Unity's native code, labels and all; the managed side has no way to ask what was copied. A menu built by this package could call the pastes but could never know whether to grey them, which is exactly the difference anybody would notice first.

Asking for Unity's menu means there is nothing here deciding what any of it does, so nothing here to be wrong about undo, about prefabs, about which components may be removed, or about what may be pasted onto what.

The menu is always about the one chip that was right clicked, never about whichever chips happen to be picked. Picking is about what is on screen; this is about the component.

The call behind it is not public API. If a future Unity moves it, the chips fall back to a smaller menu of this package's own, with Remove and Copy and the two pastes always offered, which is worse in exactly the way described above but is still a working right click.

The menu is always about the one chip that was right clicked, never about whichever chips happen to be picked. Picking is about what is on screen; this is about the component.

**Remove Component is greyed out when the Inspector would have refused it too.** The Transform cannot be taken away, and neither can a component another one says it needs, which says which component is in the way when it happens. A requirement that two components both meet does not stop either of them going, since the one left over still meets it.

It is also greyed out for a prefab opened from the Project window, where removing a component is not the same operation as removing one in a scene. Open the prefab and the chip there will do it. Copy works in both.

## Narrowing the Inspector down

**Click a chip and the Inspector shows that component and nothing else.** Click it again and everything comes back. Click a second chip and both are shown, a third and all three are, so a Rigidbody can be read next to the collider it is fighting with, with the twenty properties between them out of the way.

**Shift clicking takes everything between.** Click one chip, then shift click another, and every chip from one to the other joins the selection. The range is measured from the last chip clicked **without** shift, so that chip is the anchor until another plain click moves it.

The range is added to what is already picked rather than replacing it, since a plain click here is a toggle and building a selection out of several clicks is how the bar already works. A shift click is that gesture with the middle filled in, not a different one.

A picked chip is drawn pressed, with a blue line along its bottom edge, so what the Inspector is showing is readable from the bar without scrolling to find out.

**A Show All chip appears at the front of the bar** as soon as anything is picked, drawn with a grid icon, and one click on it drops the filter however many chips are picked. It is not there while nothing is filtered, so the bar stays as narrow as it can. **Show All Only When Needed** in the settings turns that off, and the chip then sits at the front of the bar at all times, greyed out until there is a filter to drop.

**Nothing is being hidden from the object, only from the view.** The editors are still there and still built; they have been taken out of the layout, the way a collapsed foldout is. Nothing is changed on the object, nothing is saved anywhere, and there is no state to get stuck in: picking nothing shows everything.

The filter belongs to the object it was set on and is dropped when the Inspector moves to another one, so selecting something never shows you a part of it because of a decision made while looking at something else. It does survive the things that rebuild an Inspector without changing what it is showing: adding a component, entering play mode, a script  recompiling.

Picking nothing is not the same as picking everything. With nothing picked, a component added to the object turns up in the Inspector as it always would, rather than being the one thing hidden.

### Materials

A GameObject with a renderer on it has the renderer's material drawn under the components, as an editor of its own. It is not a component and no chip stands for it, so it is treated as part of the renderer it belongs to: **picking the renderer shows the material with it, and picking anything else puts it away** along with the renderer.

The Add Component button and anything else the Inspector keeps at the bottom of itself are left alone.

## Searching

Above the chips is a row with two buttons and a search field, always. The buttons are for carrying components to another object and are described above; they are greyed out when there is nothing picked to carry or nothing waiting to be put down.

**Typing a few letters of a component's name narrows the Inspector to the components called that**, and their chips light up as though they had been picked by hand. It is the same narrowing a click on a chip does, so what is left on screen is the Inspector's own editors: a custom editor is still a custom editor and a Transform
still looks like a Transform.

Nothing is opened or read to answer that. The search is against the type's name, matched against both `MeshRenderer` and the `Mesh Renderer` Unity would nicify it into, so it can be typed either way.

A search that matches nothing says so in place of the Inspector, rather than leaving everything on screen as though what was typed had been ignored.

The **cross at the end of the field** empties it. So does **Escape**, so does **Show All**, and so does clicking any chip, which is a different question being asked and takes over rather than queueing up behind the search.

The search runs a moment after the last key rather than on every letter, so typing a word at speed is one search and not one per letter of it. That moment is `SEARCH_DELAY` in `Constants.cs`.

**Between one letter and the next, nothing moves.** What the previous letter found stays on screen, chips and all, until the next answer is ready to replace it. The alternative, letting go of the old answer the instant a key is pressed, would mean the whole object coming back and going away again between every letter of a word, which is a great deal of flashing to watch and a great deal of laying out to do for something nobody was going to read. Emptying the field is the exception and is not waited on: there is nothing to work out, so the Inspector comes straight back.


### Searching inside components

**Search Inside Properties** in the settings, off to begin with, makes a search look at the fields inside components as well as at their names.

With it on, two kinds of thing match. A **component** still matches by its type name, and the whole of it comes back. A **property** matches by the name the Inspector puts beside it, and only the properties that matched come back, so typing `mass` is asking where mass is, wherever that turns out to be, and what arrives is the one field under whichever components have one.

What it finds is drawn in place of the Inspector rather than by filtering it, and this is the part worth knowing before turning it on: **those are the serialized fields behind a component, which is the same view the Inspector's own Debug mode gives.** A component with a custom editor is drawn plainly rather than the way its editor would have drawn it, so a Transform arrives as the fields it stores rather than as Position, Rotation and Scale.

That is not a shortcoming to be fixed, it is what searching inside a component means. A custom editor is code that draws; it does not answer questions about what it drew, so there is nothing to search but the serialized data underneath. It is genuinely useful when the field you want is one a custom editor does not show, which is otherwise a trip through Debug mode to reach.

Only the top level of that data, though. A search does not go down into arrays and nested structs: the answer would be a path nobody asked about rather than a field, and finding it would mean walking the whole object on every keystroke.

Editing anything found works as it always does, undo included. Nothing is a copy.

### When it will not filter

The bar works out which editor belongs to which component by counting, since the Inspector builds them in the order the components are on the object. If that count ever fails to line up, on some future Unity that puts something else in among them, **the bar stops filtering rather than guessing**: chips still light up, the Inspector keeps showing everything. Hiding nothing is a bar that is not doing its job, which is a nuisance. Hiding the wrong thing would be components disappearing for no reason anybody could work out, which is worse.

### Objects with only a Transform

An object with nothing on it but a Transform **gets no bar**, which is how the package starts. A bar there would be a row saying the object has the one component every object has, and a scene is full of empties used as parents, as markers and as folders.

**Except while components are waiting to be pasted.** An empty object is exactly the kind of thing components get carried to, and the paste button lives in the bar, so hiding the bar there would hide the one thing that object was selected for. In that case the row stays and the chips do not: the useful half and none of the noise. Put the components down, or forget them, and the bar goes away again.

**Hide When Only A Transform** in the settings turns this off, and then every object has a bar, Transform or not.

### Following the selection

The bar follows the selection like the rest of the Inspector, and **every open Inspector gets its own**, so a second one docked elsewhere is not left showing the wrong object.

A **locked** Inspector keeps the bar of whatever it was locked onto.

With **more than one object selected** the bar hides itself. The Inspector in that case draws only the components the selected objects have in common, and a bar listing everything on the active one would be pointing at headers that are not there.

Prefabs opened from the Project window get the bar as well. Models do not: they are drawn by their importer rather than by the usual component editors, so there is no component list under them to sit above.

## The icons

Almost every picture the bar draws is Unity's own: a component's icon is the one the editor gives that type, so a chip looks like the header it stands for and there is nothing to keep up to date.

The **copy and paste buttons are the exception**, and their pictures live in **`Editor/Icons`** inside the package. Nothing the editor ships for copying and pasting is a picture of copying and pasting; they are all borrowed from somewhere else, and the nearest one, its clipboard, has no dark theme version at all, so it comes out as a smudge on the theme most people work in.

There are two files for each, following the same `d_` prefix Unity uses: `Copy.png` for the light theme and `d_Copy.png` for the dark one. **Painting over either file changes the button**, with no script to touch and nothing to restart. Deleting one falls back to the other, and deleting both falls back to Unity's nearest icon, so a missing file is a borrowed picture rather than a blank button.

The vector originals are in **`Editor/Icons/Source~`**, with a note beside them on redrawing them. That folder ends with a `~`, so Unity never imports it and never sees it, which is what keeps a half finished drawing out of your project. Editing an SVG there does nothing on its own: export it over the PNG above at 64 x 64 to see it.

Unity can import SVG directly as of 6.3, through its vector graphics module, but only as a sprite and only with the right import settings, which is a good deal of machinery to hang on two small pictures that never change at runtime. PNG is what the bar draws.

## Settings

**Tools > Componentry > Settings** opens them. The same page is under **Edit > Preferences > Componentry** for anyone who looks for editor settings
there, and the two are the same controls writing the same values, so it does not matter which one you reach for. Beside Settings in the menu is a **Component Bar** toggle, for turning the bar off without opening anything at all.

Everything is kept in `EditorPrefs`, so it is one person's preference and follows them into every project rather than being saved with any one of them and turning up in somebody else's checkout.

### Names

**Names** decides whether a chip carries the component's type name beside its icon, and it has three settings.

**Only when needed**, which is how it starts, is the one worth understanding. It asks not whether the *component* is unique on the object but whether its *icon* is, and those are not the same question. A Transform, a Mesh Renderer and a Rigidbody each have an icon nobody else on the object is using, so the name beside it is a word spent repeating what the picture already said, and it goes. Scripts do not: a MonoBehaviour without an icon of its own is drawn with the same default script icon as every other one, so two scripts would be two identical chips, and both keep their names. The same holds for two of the same component, two Box Colliders say, which keep their names as well.

The result on a typical object is a short row of icons with names only where a name is carrying information.

**Always** is the old behaviour, every chip named. **Never** is icons and nothing else, which is the most compact the bar gets.

Whichever is chosen, **hovering a chip names the component**, namespace included, so nothing is ever unidentifiable.

### Size

The bar comes in several, from **Compact** up to **Extra Large**. Each one is a multiplier over a single set of measurements rather than a set of its own, so the chips, their icons, the search field, its magnifier, the text and the spacing all grow together and the bar keeps its proportions at every size.

These sizes are not everybody's sizes. Every measurement the bar is drawn with is a number in **`Editor/Core/Constants.cs`**, including the multiplier behind each preset, and it is the only file in the package that holds one. For a size the presets do not offer, or for chips a little tighter or a little roomier than any of them, edit a number there and let Unity recompile. Because the presets multiply those same numbers, changing one changes it at every size.
