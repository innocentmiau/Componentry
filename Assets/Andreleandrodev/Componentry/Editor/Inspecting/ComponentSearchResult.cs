using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Componentry.Inspecting
{
    /*
     * The SerializedObject is kept rather than made again each time the results are drawn,
     * because the properties are handles into it and would be worthless without it,
     * and because building one is the expensive half of searching.
     */
    /// <summary>
    /// One component a search matched, and the properties of it that matched.
    /// </summary>
    public class ComponentSearchResult : IDisposable
    {

        private readonly Component _component;
        private readonly SerializedObject _serialized;
        private readonly List<SerializedProperty> _properties;

        public Component Component => _component;
        public SerializedObject Serialized => _serialized;
        public List<SerializedProperty> Properties => _properties;

        /// <summary>
        /// A component deleted while its result was on screen. Everything here is then a handle to something that is gone, and the search has to be run again.
        /// </summary>
        public bool IsStale => !_component;

        /// <summary>
        /// Takes ownership of the SerializedObject, which is disposed along with this.
        /// </summary>
        /// <param name="component">The component that matched.</param>
        /// <param name="serialized">The SerializedObject the properties were read from.</param>
        /// <param name="properties">The properties inside it that matched, as handles into that same SerializedObject.</param>
        public ComponentSearchResult(Component component, SerializedObject serialized, List<SerializedProperty> properties)
        {
            _component = component;
            _serialized = serialized;
            _properties = properties;
        }

        /// <summary>
        /// Drops the properties and the SerializedObject behind them. Nothing here is usable afterwards.
        /// </summary>
        public void Dispose()
        {
            _properties.Clear();
            _serialized?.Dispose();
        }

    }
}
