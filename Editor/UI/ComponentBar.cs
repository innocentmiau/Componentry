using System;
using System.Collections.Generic;
using Componentry.Core;
using Componentry.Inspecting;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Componentry.UI
{
    /*
     * One of these per Inspector window, not one for the editor. A locked Inspector is showing something the others are not,
     * so each bar follows its own window and asks that window what it is locked onto rather than reading the selection.
     *
     * The bar is an IMGUIContainer put into the Inspector's own UIElements tree, directly above the list of component editors.
     * Filtering is then a matter of hiding elements in that list rather than drawing anything: the editors stay Unity's,
     * so a custom editor is still drawn by its custom editor and a Transform still looks like a Transform.
     * Hiding the wrong element is the whole risk here, which is why the index mapping goes through the drawn list,
     * counting the boxes the Inspector puts up including the ones for missing scripts, rather than counting chips.
     *
     * Nothing is recomputed per frame. The component list, the icons, the measured widths and the filter each have their own dirty flag,
     * set by the events that can actually change them, and a repaint that changes nothing costs a few comparisons.
     * The styles are static and shared by every bar, rebuilt only when the size setting or the skin moves,
     * since a GUIStyle per bar per repaint was the most expensive thing here before it was measured.
     */
    /// <summary>
    /// The row of component chips drawn above the Transform in one Inspector, and the filtering, searching and reordering done from it.
    /// </summary>
    public class ComponentBar
    {
        
        private const string EDITORS_LIST_CLASS = "unity-inspector-editors-list";
        private const string BAR_NAME = "Componentry Bar";
        private const string RESULTS_NAME = "Componentry Search Results";
        private const string SEARCH_CONTROL = "ComponentrySearch";
        
        private static readonly int DRAG_HASH = "ComponentryChipDrag".GetHashCode();
        
        private static GUIStyle _chipStyle;
        private static GUIStyle _labelStyle;
        private static GUIStyle _pickedLabelStyle;
        private static GUIStyle _searchStyle;
        private static GUIStyle _placeholderStyle;
        private static float _styleScale;
        private static bool _styleProSkin;
        
        private static int _styleGeneration;
        
        private readonly EditorWindow _window;
        private readonly Func<bool> _isLocked;
        private readonly List<Component> _drawn = new List<Component>();
        
        private readonly List<Component> _components = new List<Component>();
        private readonly List<Texture> _icons = new List<Texture>();
        private readonly List<bool> _labelled = new List<bool>();
        private readonly List<float> _widths = new List<float>();
        private readonly ChipLabelResolver _labels = new ChipLabelResolver();
        private readonly GUIContent _chipContent = new GUIContent();
        private readonly GUIContent _measureContent = new GUIContent();
        
        private readonly HashSet<int> _picked = new HashSet<int>();
        
        private readonly List<ComponentSearchResult> _results = new List<ComponentSearchResult>();
        
        private readonly HashSet<int> _matched = new HashSet<int>();
        
        private readonly ChipDrag _drag = new ChipDrag();
        private readonly List<int> _order = new List<int>();
        private readonly List<Rect> _slots = new List<Rect>();
        private readonly List<Component> _others = new List<Component>();
        private readonly List<Component> _selection = new List<Component>();
        
        private VisualElement _editorsList;
        private IMGUIContainer _bar;
        private IMGUIContainer _resultsPanel;
        private string _query = string.Empty;
        private string _searchedQuery;
        private double _queryTypedAt;
        private bool _queryDirty;
        private InspectedTarget _target = InspectedTarget.NONE;
        private Object _followed;
        private int _appliedRows;
        
        private int _componentCount = -1;
        private ChipLabelMode _labelMode;
        private bool _contentDirty = true;
        private bool _filterDirty = true;
        private int _widthsGeneration = -1;
        private float _measuredWidth = float.NaN;
        private float _showAllWidth;
        private bool _showAllLabelled;
        private bool _measuredShowAll;
        
        private int _missing;
        private float _missingWidth;
        private bool _pickedMissing;
        
        private int _pivot;
        
        private float _addWidth;
        
        public EditorWindow Window => _window;
        
        private static float Scale => ComponentrySettings.Scale;
        private static float ChipHeight => Constants.CHIP_HEIGHT * Scale;
        private static float ChipPadding => Constants.CHIP_PADDING * Scale;
        private static float ChipSpacing => Constants.CHIP_SPACING * Scale;
        private static float RowSpacing => Constants.ROW_SPACING * Scale;
        private static float IconSize => Constants.ICON_SIZE * Scale;
        private static float IconGap => Constants.ICON_GAP * Scale;
        private static float BarMargin => Constants.BAR_MARGIN * Scale;
        
        private static float SearchHeight => Constants.SEARCH_HEIGHT * Scale;
        private static float SearchGap => Constants.SEARCH_GAP * Scale;
        private static float ClearSize => Constants.CLEAR_SIZE * Scale;
        
        private int ComponentStartIndex => _target.BarIndex + 2;

        private bool TransformOnly => _missing == 0 && _components.Count == 1 && _components[0] is Transform;

        private bool Hidden => ComponentrySettings.HideOnTransformOnly && TransformOnly && ComponentClipboard.Count == 0;

        private bool ToolbarOnly => ComponentrySettings.HideOnTransformOnly && TransformOnly;

        private bool Searching => !string.IsNullOrWhiteSpace(_query);

        private bool SearchSettled => Searching && _searchedQuery == _query.Trim();

        private bool SearchPending => _queryDirty && Searching;

        private bool NeedsResultsPanel => SearchSettled && (ComponentrySettings.SearchProperties || _matched.Count == 0);

        private HashSet<int> ShownComponents => ShowingMatches ? _matched : _picked;

        private bool ShowingMissing => _pickedMissing && !ShowingMatches;

        private bool ShowingMatches => !ComponentrySettings.SearchProperties && _searchedQuery != null && (SearchSettled || SearchPending);

        private bool ShowingShowAll => Narrowing || !ComponentrySettings.ShowAllWhenNeeded;

        private bool Narrowing => _picked.Count > 0 || _pickedMissing || Searching;

        /// <summary>
        /// Nothing is attached here. The bar goes into the window on the first Update that finds the Inspector's editor list built.
        /// </summary>
        /// <param name="window">The Inspector this bar belongs to, and the only one it will ever draw into.</param>
        public ComponentBar(EditorWindow window)
        {
            _window = window;
            _isLocked = InspectorWindowAccess.LockedGetter(window);
        }

        /// <summary>
        /// Points the bar at whatever is selected, unless its window is locked, in which case it stays on what it was locked onto.
        /// </summary>
        /// <param name="selected">The active object, or null when the selection is empty or more than one thing.</param>
        public void Follow(Object selected)
        {
            if (_isLocked != null && _isLocked()) return;
            if (ReferenceEquals(selected, _followed)) return;
            _followed = selected;
            SetTarget(selected);
        }

        private void SetTarget(Object inspected)
        {
            InspectedTarget target = InspectedTarget.Of(inspected);

            if (target.IsSameAs(_target)) return;
            _target = target;

            _picked.Clear();
            _pickedMissing = false;
            _pivot = 0;
            PropertySearch.Clear(_results);
            _matched.Clear();
            _query = string.Empty;
            _searchedQuery = null;
            _queryDirty = false;

            Detach();
            MarkDirty();
        }

        private void MenuChanged()
        {
            MarkDirty();
            _bar?.MarkDirtyRepaint();
            _window?.Repaint();
        }

        /// <summary>
        /// Says the components may have changed, so the list and the filter are both worked out again on the next update.
        /// </summary>
        public void MarkDirty()
        {
            _contentDirty = true;
            _filterDirty = true;
        }

        /// <summary>
        /// One pass of the bar, run once a frame. Attaches it when the Inspector is ready for it, rebuilds only what is dirty, and takes it away when there is nothing to show.
        /// </summary>
        public void Update()
        {
            if (!_window) return;

            if (!_target.IsValid)
            {
                Detach();
                return;
            }

            if (_editorsList?.panel == null)
            {
                _editorsList = _window.rootVisualElement?.Q(null, EDITORS_LIST_CLASS);
                MarkDirty();
            }

            if (_editorsList == null) return;

            if (_contentDirty || NeedsRebuild()) Rebuild();

            if (_components.Count == 0 || Hidden)
            {
                Detach();
                return;
            }

            Attach();

            if (!InPlace(_bar, _target.BarIndex) || !InPlace(_resultsPanel, _target.BarIndex + 1)) return;

            UpdateSearch();

            if (!_filterDirty) return;
            if (SearchPending) return;

            _filterDirty = false;

            if (SearchSettled && (ComponentrySettings.SearchProperties || _matched.Count == 0))
            {
                EditorElements.HideComponents(_editorsList, ComponentStartIndex, _drawn);
                return;
            }

            EditorElements.Apply(_editorsList, ComponentStartIndex, _drawn, ShownComponents, ShowingMissing);
        }

        private void UpdateSearch()
        {
            if (_queryDirty && (!Searching || EditorApplication.timeSinceStartup - _queryTypedAt >= Constants.SEARCH_DELAY)) RunSearch();
            if (!_queryDirty && Searching && ComponentrySettings.SearchProperties && PropertySearch.AnyStale(_results)) RunSearch();
            UpdateResultsVisibility();
        }

        private void RunSearch()
        {
            _queryDirty = false;
            _searchedQuery = _query.Trim();

            if (ComponentrySettings.SearchProperties)
            {
                _matched.Clear();
                PropertySearch.Run(_components, _query, _results);
            }
            else
            {
                PropertySearch.Clear(_results);
                ComponentNameSearch.Run(_components, _query, _matched);
            }

            _filterDirty = true;
            _resultsPanel?.MarkDirtyRepaint();
            _window.Repaint();
        }

        private void SetQuery(string query)
        {
            if (query == _query) return;
            _query = query;
            _queryTypedAt = EditorApplication.timeSinceStartup;
            _queryDirty = true;
            _filterDirty = true;
        }

        private void UpdateResultsVisibility()
        {
            if (_resultsPanel == null) return;
            if (SearchPending) return;

            DisplayStyle wanted = NeedsResultsPanel ? DisplayStyle.Flex : DisplayStyle.None;

            if (_resultsPanel.style.display.value == wanted) return;

            _resultsPanel.style.display = wanted;
            _filterDirty = true;
        }

        private void DrawResults()
        {
            if (string.IsNullOrEmpty(_searchedQuery)) return;
            SearchResultsView.Draw(_results, _searchedQuery, ComponentrySettings.SearchProperties);
        }

        private bool NeedsRebuild()
        {
            if (_labelMode != ComponentrySettings.Labels) return true;
            return _target.GameObject.GetComponentCount() != _componentCount;
        }

        private void Rebuild()
        {
            _contentDirty = false;
            _filterDirty = true;
            _componentCount = _target.GameObject.GetComponentCount();
            _labelMode = ComponentrySettings.Labels;

            VisibleComponents.CollectDrawn(_target.GameObject, _drawn);
            VisibleComponents.CollectChips(_drawn, _components);

            _missing = VisibleComponents.MissingIn(_drawn);

            if (_missing == 0) _pickedMissing = false;
            
            _icons.Clear();

            foreach (Component component in _components)
                _icons.Add(ChipLabelResolver.IconOf(component));

            _labels.Resolve(_icons, _labelMode, _labelled);

            PrunePicked();

            if (_query.Length > 0) _queryDirty = true;
            
            _widthsGeneration = -1;
        }

        /// <summary>
        /// Takes the bar out of the Inspector and puts every component editor back on screen, so nothing stays hidden by a bar that is no longer there.
        /// </summary>
        public void Detach()
        {
            EditorElements.ShowAll(_editorsList);
            
            _resultsPanel?.RemoveFromHierarchy();
            _resultsPanel = null;
            
            _bar?.RemoveFromHierarchy();
            _bar = null;
            _appliedRows = 0;
            _filterDirty = true;
        }

        /// <summary>
        /// Throws the bar away and builds it again. Every measurement is taken at build time, so this is what a size or label setting change comes through.
        /// </summary>
        public void Refresh()
        {
            Detach();
            MarkDirty();
            _window?.Repaint();
        }
        
        private void PrunePicked()
        {
            if (_picked.Count == 0) return;
            _picked.RemoveWhere(NotOnObject);
        }

        private bool NotOnObject(int instanceId)
        {
            foreach (Component component in _components)
                if (component.GetInstanceID() == instanceId) return false;
            return true;
        }

        private void ClickChip(int index)
        {
            if (_query.Length > 0)
            {
                SetQuery(string.Empty);
                GUI.FocusControl(null);
            }

            int pivot = PivotIndex();

            if (Event.current.shift && pivot >= 0)
            {
                PickRange(pivot, index);
                return;
            }

            _pivot = _components[index].GetInstanceID();
            
            TogglePicked(_components[index]);
        }

        private void PickRange(int from, int to)
        {
            int start = Mathf.Min(from, to);
            int end = Mathf.Max(from, to);

            for (int i = start; i <= end; i++)
                _picked.Add(_components[i].GetInstanceID());

            _filterDirty = true;
            _window.Repaint();
        }

        private int PivotIndex()
        {
            if (_pivot == 0) return -1;
            
            for (int i = 0; i < _components.Count; i++)
                if (_components[i].GetInstanceID() == _pivot) return i;
            
            return -1;
        }

        private void TogglePicked(Component component)
        {
            int id = component.GetInstanceID();

            if (!_picked.Remove(id)) _picked.Add(id);

            _filterDirty = true;
            _window.Repaint();
        }

        private bool InPlace(VisualElement element, int index)
        {
            return element != null && element.parent == _editorsList && _editorsList.IndexOf(element) == index;
        }

        private void Attach()
        {
            int index = _target.BarIndex;

            if (_editorsList.childCount <= index) return;
            if (InPlace(_bar, index) && InPlace(_resultsPanel, index + 1)) return;

            Detach();

            _editorsList.Q(BAR_NAME)?.RemoveFromHierarchy();
            _editorsList.Q(RESULTS_NAME)?.RemoveFromHierarchy();

            float margin = BarMargin;

            _bar = new IMGUIContainer(Draw) { name = BAR_NAME };
            _bar.style.marginTop = margin;
            _bar.style.marginBottom = margin;
            _bar.style.marginLeft = margin;
            _bar.style.marginRight = margin;
            _bar.style.height = HeightOf(1);

            _editorsList.Insert(index, _bar);

            _resultsPanel = new IMGUIContainer(DrawResults) { name = RESULTS_NAME };
            _resultsPanel.style.marginLeft = margin;
            _resultsPanel.style.marginRight = margin;
            _resultsPanel.style.display = DisplayStyle.None;
            _resultsPanel.style.height = new StyleLength(StyleKeyword.Auto);

            _editorsList.Insert(index + 1, _resultsPanel);

            _filterDirty = true;
        }

        private void Draw()
        {
            if (!_target.IsValid || _components.Count == 0) return;
            if (_labelled.Count != _components.Count) return;

            EnsureStyles();

            if (_widthsGeneration != _styleGeneration) MeasureWidths();

            Rect area = _bar.contentRect;

            if (float.IsNaN(area.width) || area.width <= 0f) return;

            DrawToolbar(new Rect(area.x, area.y, area.width, SearchHeight));

            if (ToolbarOnly)
            {
                ApplyHeight(0);
                return;
            }

            area.yMin += SearchHeight + SearchGap;

            int dragControl = GUIUtility.GetControlID(DRAG_HASH, FocusType.Passive);

            HandleDrag(dragControl);

            _drag.Order(_components.Count, _order);

            bool showAdd = ComponentrySettings.AddComponent && AddComponentWindowAccess.Available;
            bool showAll = ShowingShowAll;
            bool showMissing = _missing > 0;

            if (showAll != _measuredShowAll)
            {
                _measuredShowAll = showAll;
                _measuredWidth = float.NaN;
            }

            if (!Mathf.Approximately(area.width, _measuredWidth))
            {
                _measuredWidth = area.width;
                ApplyHeight(MeasureRows(area.width, LeadingWidth(showAdd, showAll, showMissing)));
            }

            float height = ChipHeight;
            float spacing = ChipSpacing;
            float x = area.x;
            float y = area.y;
            int row = 1;

            int lead = 0;
            int addSlot = showAdd ? lead++ : -1;
            int allSlot = showAll ? lead++ : -1;
            int missingSlot = showMissing ? lead++ : -1;
            int offset = lead;
            int chips = _order.Count + offset;

            _slots.Clear();

            for (int i = 0; i < chips; i++)
            {
                bool isAdd = i == addSlot;
                bool isShowAll = i == allSlot;
                bool isMissing = i == missingSlot;
                int component = i < offset ? -1 : _order[i - offset];
                float width = isAdd ? _addWidth : isShowAll ? _showAllWidth : isMissing ? _missingWidth : _widths[component];

                if (x > area.x && x + width > area.xMax)
                {
                    if (row >= ComponentrySettings.MaxRows) return;

                    x = area.x;
                    y += height + RowSpacing;
                    row++;
                }

                Rect rect = new Rect(x, y, width, height);

                if (isAdd)
                {
                    DrawAdd(rect);
                }
                else if (isShowAll)
                {
                    DrawShowAll(rect);
                }
                else if (isMissing)
                {
                    DrawMissing(rect);
                }
                else
                {
                    _slots.Add(rect);
                    DrawChip(rect, component, dragControl);
                }

                x += width + spacing;
            }
        }

        private void DrawToolbar(Rect row)
        {
            float size = row.height;
            float spacing = ChipSpacing;

            Rect copy = new Rect(row.x, row.y, size, size);
            Rect paste = new Rect(copy.xMax + spacing, row.y, size, size);

            DrawCarryButton(copy, true);
            DrawCarryButton(paste, false);

            Rect field = row;
            field.xMin = paste.xMax + spacing;

            DrawSearchField(field);
        }

        private void DrawCarryButton(Rect rect, bool copying)
        {
            int carried = ComponentClipboard.Count;
            bool enabled = copying ? _picked.Count > 0 : carried > 0;

            _chipContent.text = string.Empty;
            _chipContent.image = null;
            _chipContent.tooltip = CarryTooltip(copying, carried);

            ConsumeRightButton(rect);

            using (new EditorGUI.DisabledScope(!enabled))
            {
                if (GUI.Button(rect, _chipContent, _chipStyle) && enabled)
                {
                    if (copying)
                        CopyPicked();
                    else
                        PasteCarried();
                }
            }

            if (Event.current.type != EventType.Repaint) return;

            Texture icon = copying ? ComponentryIcons.Custom("Copy") ?? ComponentryIcons.First("TreeEditor.Duplicate", "SaveAs") : ComponentryIcons.Custom("Paste") ?? ComponentryIcons.First("Clipboard", "Import");

            if (!icon) return;

            float size = IconSize;
            Rect iconRect = new Rect(rect.center.x - size * .5f, rect.center.y - size * .5f, size, size);

            GUI.DrawTexture(iconRect, icon, ScaleMode.ScaleToFit);
        }

        private string CarryTooltip(bool copying, int carried)
        {
            if (copying)
            {
                return _picked.Count > 0 ? $"Copy the {_picked.Count} picked component(s), to be pasted onto another object" 
                    : "Pick the components to copy first, by clicking their chips";
            }

            return carried > 0 ? $"Paste the {carried} copied component(s) onto this object" : "No components have been copied";
        }

        private void CollectPicked()
        {
            _selection.Clear();

            foreach (Component component in _components)
                if (_picked.Contains(component.GetInstanceID()))
                    _selection.Add(component);
        }

        private void CopyPicked()
        {
            CollectPicked();
            ComponentClipboard.Copy(_selection);
            _window.Repaint();
        }

        private void PasteCarried()
        {
            if (!_target.IsValid) return;

            _selection.Clear();
            ComponentClipboard.Fill(_selection);

            if (ComponentActions.PasteAll(_target.GameObject, _selection) > 0) MarkDirty();

            _window.Repaint();
        }

        private void DrawSearchField(Rect rect)
        {
            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape && GUI.GetNameOfFocusedControl() == SEARCH_CONTROL && _query.Length > 0)
            {
                SetQuery(string.Empty);
                GUI.FocusControl(null);
                Event.current.Use();
                _window.Repaint();
                return;
            }

            bool hasText = _query.Length > 0;
            Rect clear = ClearRect(rect);

            _chipContent.text = string.Empty;
            _chipContent.image = null;
            _chipContent.tooltip = "Clear the search";

            if (hasText) ConsumeRightButton(clear);

            bool cleared = GUI.Button(hasText ? clear : Rect.zero, _chipContent, GUIStyle.none) && hasText;

            GUI.SetNextControlName(SEARCH_CONTROL);
            string typed = GUI.TextField(rect, _query, _searchStyle);

            if (cleared)
            {
                SetQuery(string.Empty);
                GUI.FocusControl(null);
                _window.Repaint();
                return;
            }

            SetQuery(typed);

            if (Event.current.type != EventType.Repaint) return;

            DrawSearchIcon(rect);

            if (hasText)
            {
                Texture icon = ClearIcon();
                if (icon) GUI.DrawTexture(clear, icon, ScaleMode.ScaleToFit);
                return;
            }

            Rect hint = rect;
            hint.xMin += _searchStyle.padding.left;

            GUI.Label(hint, ComponentrySettings.SearchProperties ? "Search components and properties" : "Search components", _placeholderStyle);
        }

        private static Rect ClearRect(Rect field)
        {
            float size = ClearSize;
            return new Rect(field.xMax - size - ChipPadding, field.y + (field.height - size) * .5f, size, size);
        }

        private static Texture ClearIcon() => ComponentryIcons.First("CrossIcon", "clear");

        private static void DrawSearchIcon(Rect field)
        {
            Texture icon = ComponentryIcons.First("Search Icon", "SearchWindow", "ViewToolZoom");

            if (!icon) return;

            float size = Constants.SEARCH_ICON_SIZE * Scale;
            Rect rect = new Rect(field.x + ChipPadding, field.y + (field.height - size) * .5f, size, size);

            GUI.DrawTexture(rect, icon, ScaleMode.ScaleToFit);
        }

        private void DrawAdd(Rect rect)
        {
            ConsumeRightButton(rect);

            _chipContent.text = string.Empty;
            _chipContent.image = null;
            _chipContent.tooltip = $"Add a component to {_target.GameObject.name}";

            if (GUI.Button(rect, _chipContent, _chipStyle) && AddComponentWindowAccess.Open(AddAnchor(rect), _target.GameObject)) GUIUtility.ExitGUI();

            if (Event.current.type != EventType.Repaint) return;

            Texture icon = ComponentryIcons.First("Toolbar Plus", "CreateAddNew");

            if (!icon) return;

            float size = IconSize;
            Rect iconRect = new Rect(rect.center.x - size * .5f, rect.center.y - size * .5f, size, size);

            GUI.DrawTexture(iconRect, icon, ScaleMode.ScaleToFit);
        }

        private Rect AddAnchor(Rect chip)
        {
            Rect area = _bar.contentRect;
            return new Rect(area.x, chip.y, Mathf.Max(area.width, chip.width), chip.height);
        }

        private string MissingLabel => _missing == 1 ? "Missing Script" : $"{_missing} Missing Scripts";

        private void DrawMissing(Rect rect)
        {
            if (Event.current.type == EventType.MouseDown && Event.current.button == 1 && rect.Contains(Event.current.mousePosition))
            {
                ShowMissingMenu();
                Event.current.Use();
            }

            ConsumeRightButton(rect);

            _chipContent.text = string.Empty;
            _chipContent.image = null;
            _chipContent.tooltip = _missing == 1 ? "A component here has a script that cannot be found\n\nClick to show only it\nRight click to remove it" : $"{_missing} components here have scripts that cannot be found\n\nClick to show only them\nRight click to remove them";

            if (GUI.Toggle(rect, _pickedMissing, _chipContent, _chipStyle) != _pickedMissing)
            {
                _pickedMissing = !_pickedMissing;
                _filterDirty = true;
                _window.Repaint();
            }

            if (_pickedMissing && Event.current.type == EventType.Repaint)
            {
                float accentHeight = Constants.ACCENT_HEIGHT * Scale;
                EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - accentHeight, rect.width, accentHeight), Constants.MISSING);
            }

            float iconSize = IconSize;
            Texture icon = ComponentryIcons.First("console.warnicon.sml", "console.warnicon", "CrossIcon");
            Rect iconRect = new Rect(rect.x + ChipPadding, rect.y + (rect.height - iconSize) * .5f, iconSize, iconSize);

            if (icon) GUI.DrawTexture(iconRect, icon, ScaleMode.ScaleToFit);

            Color previous = GUI.color;
            GUI.color = Constants.MISSING;

            Rect labelRect = new Rect(iconRect.xMax + IconGap, rect.y, rect.xMax - iconRect.xMax - IconGap - ChipPadding, rect.height);
            GUI.Label(labelRect, MissingLabel, _labelStyle);

            GUI.color = previous;
        }

        private void ShowMissingMenu()
        {
            GameObject owner = _target.GameObject;
            int missing = _missing;

            GenericMenu menu = new GenericMenu();

            menu.AddItem(new GUIContent(missing == 1 ? "Remove Missing Script" : $"Remove {missing} Missing Scripts"), false, () =>
            {
                if (ComponentActions.RemoveMissingScripts(owner, missing) > 0)
                    MenuChanged();
            });

            menu.ShowAsContext();
        }

        private void DrawShowAll(Rect rect)
        {
            ConsumeRightButton(rect);

            _chipContent.text = string.Empty;
            _chipContent.image = null;
            _chipContent.tooltip = Narrowing ? "Show every component again" : "Nothing is being narrowed down";

            using (new EditorGUI.DisabledScope(!Narrowing))
            {
                if (GUI.Button(rect, _chipContent, _chipStyle)) ShowEverything();
            }

            Texture icon = ShowAllIcon();
            float iconSize = IconSize;
            float iconX = _showAllLabelled ? rect.x + ChipPadding : rect.center.x - iconSize * .5f;
            Rect iconRect = new Rect(iconX, rect.y + (rect.height - iconSize) * .5f, iconSize, iconSize);

            if (icon) GUI.DrawTexture(iconRect, icon, ScaleMode.ScaleToFit);

            if (!_showAllLabelled) return;

            Rect labelRect = new Rect(iconRect.xMax + IconGap, rect.y, rect.xMax - iconRect.xMax - IconGap - ChipPadding, rect.height);
            GUI.Label(labelRect, Constants.SHOW_ALL_NAME, _labelStyle);
        }

        private static Texture ShowAllIcon() => ComponentryIcons.First("GridLayoutGroup Icon", "animationvisibilitytoggleon", "clear");

        private void ShowEverything()
        {
            if (!Narrowing) return;

            _picked.Clear();
            SetQuery(string.Empty);
            GUI.FocusControl(null);

            _filterDirty = true;
            _window.Repaint();
        }

        private void HandleDrag(int control)
        {
            Event current = Event.current;

            switch (current.type)
            {
                case EventType.MouseDrag:
                    if (_drag.ShouldStart(current.mousePosition, Constants.DRAG_THRESHOLD * Scale))
                    {
                        _drag.Start();
                        GUIUtility.hotControl = control;
                        _measuredWidth = float.NaN;
                    }

                    if (!_drag.Active) return;

                    if (!_bar.contentRect.Contains(current.mousePosition))
                    {
                        CarryOut();
                        return;
                    }

                    if (_drag.MoveTo(SlotAt(current.mousePosition)))
                        _measuredWidth = float.NaN;

                    current.Use();
                    _window.Repaint();
                    return;

                case EventType.MouseUp:
                    if (_drag.Active)
                    {
                        CompleteDrag();
                        GUIUtility.hotControl = 0;
                        current.Use();
                        _window.Repaint();
                        return;
                    }

                    _drag.Clear();
                    return;

                case EventType.KeyDown:
                    if (!_drag.Active || current.keyCode != KeyCode.Escape) return;

                    _drag.Clear();
                    GUIUtility.hotControl = 0;
                    _measuredWidth = float.NaN;
                    current.Use();
                    _window.Repaint();
                    return;
            }
        }

        private void CarryOut()
        {
            int from = _drag.From;

            _drag.Clear();
            GUIUtility.hotControl = 0;
            _measuredWidth = float.NaN;

            if (from < 0 || from >= _components.Count) return;

            Component dragged = _components[from];

            CollectPicked();

            if (!_selection.Contains(dragged))
            {
                _selection.Clear();
                _selection.Add(dragged);
            }

            ChipHierarchyDrop.Begin(_selection);

            Event.current.Use();
            _window.Repaint();
        }

        private int SlotAt(Vector2 mouse)
        {
            for (int i = 0; i < _slots.Count; i++)
                if (_slots[i].Contains(mouse)) return Mathf.Max(i, FirstMovableSlot);
            return _drag.To;
        }

        private int FirstMovableSlot => _components.Count > 0 && _components[0] is Transform ? 1 : 0;

        private void CompleteDrag()
        {
            if (!_drag.Release(out int from, out int to)) return;
            if (from < 0 || from >= _components.Count) return;

            Component moving = _components[from];
            
            _others.Clear();

            for (int i = 0; i < _components.Count; i++) 
                if (i != from) _others.Add(_components[i]);

            if (ComponentActions.Reorder(moving, _others, to)) MarkDirty();
        }

        private void DrawChip(Rect rect, int index, int dragControl)
        {
            Component component = _components[index];

            bool picked = ShownComponents.Contains(component.GetInstanceID());

            HandleChipContext(rect, component);

            _chipContent.text = string.Empty;
            _chipContent.image = null;
            _chipContent.tooltip = TooltipFor(component, picked);

            if (Event.current.type == EventType.MouseDown && Event.current.button == 0 
                                                          && rect.Contains(Event.current.mousePosition) && ComponentActions.CanReorder(component))
                _drag.Press(index, Event.current.mousePosition);

            if (GUI.Toggle(rect, picked, _chipContent, _chipStyle) != picked && GUIUtility.hotControl != dragControl) ClickChip(index);

            if (picked && Event.current.type == EventType.Repaint)
            {
                float accentHeight = Constants.ACCENT_HEIGHT * Scale;
                EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - accentHeight, rect.width, accentHeight), Constants.ACCENT);
            }

            if (_drag.Active && _drag.From == index && Event.current.type == EventType.Repaint) 
                DrawOutline(rect, Constants.DRAG_OUTLINE * Scale, Constants.DRAGGING);

            float iconSize = IconSize;
            Texture icon = _icons[index];
            float iconX = _labelled[index] ? rect.x + ChipPadding : rect.center.x - iconSize * .5f;
            Rect iconRect = new Rect(iconX, rect.y + (rect.height - iconSize) * .5f, iconSize, iconSize);

            Color previous = GUI.color;
            bool off = !ComponentActions.IsEnabled(component);

            if (off) GUI.color = new Color(previous.r, previous.g, previous.b, previous.a * Constants.DISABLED_FADE);

            if (icon) GUI.DrawTexture(iconRect, icon, ScaleMode.ScaleToFit);

            if (_labelled[index])
            {
                Rect labelRect = new Rect(iconRect.xMax + IconGap, rect.y, rect.xMax - iconRect.xMax - IconGap - ChipPadding, rect.height);
                GUI.Label(labelRect, component.GetType().Name, picked ? _pickedLabelStyle : _labelStyle);
            }

            GUI.color = previous;
        }

        private void HandleChipContext(Rect rect, Component component)
        {
            Event current = Event.current;

            if (!rect.Contains(current.mousePosition)) return;

            bool pressed = current.type == EventType.MouseDown && current.button == 1;

            if (!pressed && current.type != EventType.ContextClick) return;

            CollectPicked();

            ChipContextMenu.Show(component, _selection, MenuChanged);
            current.Use();
        }

        private static void ConsumeRightButton(Rect rect)
        {
            Event current = Event.current;
            if (current.type != EventType.MouseDown || current.button != 1) return;
            if (rect.Contains(current.mousePosition)) current.Use();
        }

        private static void DrawOutline(Rect rect, float thickness, Color colour)
        {
            EditorGUI.DrawRect(new Rect(rect.x - thickness, rect.y - thickness, rect.width + thickness * 2f, thickness), colour);
            EditorGUI.DrawRect(new Rect(rect.x - thickness, rect.yMax, rect.width + thickness * 2f, thickness), colour);
            EditorGUI.DrawRect(new Rect(rect.x - thickness, rect.y, thickness, rect.height), colour);
            EditorGUI.DrawRect(new Rect(rect.xMax, rect.y, thickness, rect.height), colour);
        }

        private static string TooltipFor(Component component, bool picked)
        {
            string name = component.GetType().FullName;
            string click = picked ? "Click to stop showing only this" : "Click to show only this";
            return $"{name}\n\n{click}\nShift click to take everything between\nRight click for more, drag to move or carry it";
        }

        private float LeadingWidth(bool showAdd, bool showAll, bool showMissing)
        {
            float spacing = ChipSpacing;
            float leading = 0f;

            if (showAdd) leading += _addWidth + spacing;
            if (showAll) leading += _showAllWidth + spacing;
            if (showMissing) leading += _missingWidth + spacing;

            return leading;
        }

        private int MeasureRows(float width, float leading)
        {
            int rows = 1;
            float x = leading;
            float spacing = ChipSpacing;

            foreach (int index in _order)
            {
                if (x > 0f && x + _widths[index] > width)
                {
                    x = 0f;
                    rows++;
                }

                x += _widths[index] + spacing;
            }

            return Mathf.Min(rows, ComponentrySettings.MaxRows);
        }

        private void MeasureWidths()
        {
            _widthsGeneration = _styleGeneration;
            _widths.Clear();

            float padding = ChipPadding;
            float iconSize = IconSize;
            float gap = IconGap;

            _showAllLabelled = _labelMode == ChipLabelMode.ALWAYS || (_labelMode == ChipLabelMode.WHEN_NEEDED && !ShowAllIcon());

            _measureContent.text = Constants.SHOW_ALL_NAME;
            _showAllWidth = _showAllLabelled ? padding + iconSize + gap + _labelStyle.CalcSize(_measureContent).x + padding : padding + iconSize + padding;

            _measureContent.text = MissingLabel;
            _missingWidth = padding + iconSize + gap + _labelStyle.CalcSize(_measureContent).x + padding;

            _addWidth = padding + iconSize + padding;

            for (int i = 0; i < _components.Count; i++)
            {
                if (!_labelled[i])
                {
                    _widths.Add(padding + iconSize + padding);
                    continue;
                }

                _measureContent.text = _components[i].GetType().Name;
                _widths.Add(padding + iconSize + gap + _labelStyle.CalcSize(_measureContent).x + padding);
            }

            _measuredWidth = float.NaN;
        }

        private void ApplyHeight(int rows)
        {
            if (rows == _appliedRows) return;

            _appliedRows = rows;
            _bar.style.height = HeightOf(rows);
            _window.Repaint();
        }

        private float HeightOf(int rows)
        {
            float height = rows * ChipHeight + (rows - 1) * RowSpacing;
            if (ToolbarOnly) return SearchHeight;
            return height + SearchHeight + SearchGap;
        }

        private static void EnsureStyles()
        {
            float scale = Scale;
            
            bool proSkin = EditorGUIUtility.isProSkin;

            if (_chipStyle != null && proSkin == _styleProSkin && Mathf.Approximately(scale, _styleScale)) return;

            _styleScale = scale;
            _styleProSkin = proSkin;
            _styleGeneration++;

            _chipStyle = new GUIStyle(EditorStyles.miniButton) { fixedHeight = 0f };
            _labelStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(),
                fontSize = Mathf.Max(1, Mathf.RoundToInt(Constants.FONT_SIZE * scale))
            };

            _pickedLabelStyle = new GUIStyle(_labelStyle);

            Color pickedText = EditorStyles.miniButton.onNormal.textColor;

            if (pickedText.a > 0f) _pickedLabelStyle.normal.textColor = pickedText;

            _searchStyle = new GUIStyle(EditorStyles.textField)
            {
                fixedHeight = 0f,
                alignment = TextAnchor.MiddleLeft,
                fontSize = Mathf.Max(1, Mathf.RoundToInt(Constants.FONT_SIZE * scale))
            };

            _searchStyle.padding.left = Mathf.RoundToInt((Constants.SEARCH_ICON_SIZE + Constants.CHIP_PADDING * 2f) * scale);
            _searchStyle.padding.right = Mathf.RoundToInt((Constants.CLEAR_SIZE + Constants.CHIP_PADDING * 2f) * scale);

            _placeholderStyle = new GUIStyle(_labelStyle);
            _placeholderStyle.normal.textColor = new Color(_labelStyle.normal.textColor.r, _labelStyle.normal.textColor.g, _labelStyle.normal.textColor.b, .5f);
        }

    }
}
