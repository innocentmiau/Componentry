using System.Collections.Generic;
using UnityEngine;

namespace Componentry.UI
{
    /// <summary>
    /// Where a chip is being dragged from and to, and nothing else.
    /// </summary>
    public class ChipDrag
    {
        
        private int _pressed = -1;
        private Vector2 _pressedAt;
        private bool _active;
        private int _from = -1;
        private int _to = -1;
        
        public bool Active => _active;
        public int From => _from;
        public int To => _to;
        
        /// <summary>
        /// Remembered on the press, since a press is not yet a drag. Most of them turn out to be clicks, and a click has to go on picking the component the way it always did.
        /// </summary>
        /// <param name="index">Which chip was pressed.</param>
        /// <param name="mouse">Where the press landed, measured against later to tell a drag from a click.</param>
        public void Press(int index, Vector2 mouse)
        {
            _pressed = index;
            _pressedAt = mouse;
        }
        
        /// <summary>
        /// Whether the mouse has moved far enough from the press for this to count as a drag rather than a click.
        /// </summary>
        /// <param name="mouse">Where the mouse is now.</param>
        /// <param name="threshold">How far it has to have travelled, in pixels.</param>
        /// <returns>True once past the threshold, and false once the drag has already started.</returns>
        public bool ShouldStart(Vector2 mouse, float threshold)
        {
            if (_active || _pressed < 0) return false;
            return (mouse - _pressedAt).sqrMagnitude > threshold * threshold;
        }
        
        /// <summary>
        /// Turns the remembered press into a live drag, starting at the chip that was pressed.
        /// </summary>
        public void Start()
        {
            _active = true;
            _from = _pressed;
            _to = _pressed;
        }
        
        /// <summary>
        /// Moves the marker showing where the chip would land.
        /// </summary>
        /// <param name="slot">The slot it would drop into, or below zero for nowhere.</param>
        /// <returns>True only when the marker actually moved, so the caller repaints on a change and not on every mouse move.</returns>
        public bool MoveTo(int slot)
        {
            if (!_active || slot == _to || slot < 0) return false;
            _to = slot;
            return true;
        }
        
        /// <summary>
        /// Forgets the press and the drag both, leaving nothing in progress.
        /// </summary>
        public void Clear()
        {
            _pressed = -1;
            _active = false;
            _from = -1;
            _to = -1;
        }
        
        /// <summary>
        /// Ends the drag and says where it went. Cleared either way, so a drag that moved nothing leaves nothing behind.
        /// </summary>
        /// <param name="from">The slot the chip started in.</param>
        /// <param name="to">The slot it was dropped on.</param>
        /// <returns>True only when the component actually needs moving, which rules out a drop back onto its own slot.</returns>
        public bool Release(out int from, out int to)
        {
            from = _from;
            to = _to;
            
            bool moved = _active && _from >= 0 && _to >= 0 && _to != _from;
            
            Clear();
            return moved;
        }
        
        /// <summary>
        /// The order to draw the chips in while a drag is happening, with the dragged one lifted out and put back at the slot it is hovering over,
        /// so the row shows where it would land before it lands there. Plain order when nothing is being dragged.
        /// </summary>
        /// <param name="count">How many chips there are.</param>
        /// <param name="into">List filled with chip indices in draw order, cleared first.</param>
        public void Order(int count, List<int> into)
        {
            into.Clear();

            if (!_active || _from < 0 || _from >= count)
            {
                for (int i = 0; i < count; i++)
                    into.Add(i);
                return;
            }
            
            for (int i = 0; i < count; i++)
            {
                if (i == _from) continue;
                into.Add(i);
            }
            
            into.Insert(Mathf.Clamp(_to, 0, into.Count), _from);
        }
        
    }
}
