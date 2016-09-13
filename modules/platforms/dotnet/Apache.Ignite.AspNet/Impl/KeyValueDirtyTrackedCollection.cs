/*
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

namespace Apache.Ignite.AspNet.Impl
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.Linq;
    using Apache.Ignite.Core.Binary;
    using Apache.Ignite.Core.Impl.Common;

    /// <summary>
    /// Binarizable key-value collection with dirty item tracking.
    /// </summary>
    internal class KeyValueDirtyTrackedCollection
    {
        /** */
        private readonly Dictionary<string, int> _dict;

        /** */
        private readonly List<Entry> _list;

        /** Indicates where this is a new collection, not a deserialized old one. */
        private readonly bool _isNew;

        /** Indicates that this instance is a diff. */
        private readonly bool _isDiff;

        /** Removed keys. Hash set because keys can be removed multiple times. */
        private HashSet<string> _removedKeys;

        /** Indicates that entire collection is dirty and can't be written as a diff. */
        private bool _dirtyAll;

        /// <summary>
        /// Initializes a new instance of the <see cref="KeyValueDirtyTrackedCollection"/> class.
        /// </summary>
        /// <param name="reader">The binary reader.</param>
        internal KeyValueDirtyTrackedCollection(IBinaryRawReader reader)
        {
            Debug.Assert(reader != null);

            _isDiff = reader.ReadBoolean();

            var count = reader.ReadInt();

            _dict = new Dictionary<string, int>(count);
            _list = new List<Entry>(count);

            for (var i = 0; i < count; i++)
            {
                var key = reader.ReadString();

                var valBytes = reader.ReadByteArray();

                if (valBytes != null)
                {
                    var entry = new Entry(key, true, valBytes);

                    _dict[key] = _list.Count;

                    _list.Add(entry);
                }
                else
                    AddRemovedKey(key);
            }

            _isNew = false;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="KeyValueDirtyTrackedCollection"/> class.
        /// </summary>
        public KeyValueDirtyTrackedCollection()
        {
            _dict = new Dictionary<string, int>();
            _list = new List<Entry>();
            _isNew = true;
        }

        /// <summary>
        /// Gets the number of elements contained in the <see cref="T:System.Collections.ICollection" />.
        /// </summary>
        public int Count
        {
            get { return _dict.Count; }
        }

        /// <summary>
        /// Gets or sets the value with the specified key.
        /// </summary>
        public object this[string key]
        {
            get
            {
                var entry = GetEntry(key);

                if (entry == null)
                    return null;

                SetDirtyOnRead(entry);

                return entry.Value;
            }
            set
            {
                var entry = GetOrCreateDirtyEntry(key);

                entry.Value = value;
            }
        }

        /// <summary>
        /// Gets or sets the value at the specified index.
        /// </summary>
        public object this[int index]
        {
            get
            {
                var entry = _list[index];

                SetDirtyOnRead(entry);

                return entry.Value;
            }
            set
            {
                var entry = _list[index];

                entry.IsDirty = true;

                entry.Value = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is dirty.
        /// </summary>
        public bool IsDirty
        {
            get { return _dirtyAll || _list.Any(x => x.IsDirty); }
            set { _dirtyAll = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether only dirty changed things should be serialized.
        /// </summary>
        public bool WriteChangesOnly { get; set; }

        /// <summary>
        /// Gets the keys.
        /// </summary>
        public IEnumerable<string> GetKeys()
        {
            return _list.Select(x => x.Key);
        }

        /// <summary>
        /// Writes this object to the given writer.
        /// </summary>
        /// <param name="writer">Writer.</param>
        public void WriteBinary(IBinaryRawWriter writer)
        {
            IgniteArgumentCheck.NotNull(writer, "writer");

            if (_isDiff)
                throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture,
                    "Cannot serialize incomplete {0}.", GetType()));

            if (_isNew || _dirtyAll || !WriteChangesOnly || (_removedKeys == null && _list.All(x => x.IsDirty)))
            {
                // Write in full mode.
                writer.WriteBoolean(false);
                writer.WriteInt(_list.Count);

                foreach (var entry in _list)
                {
                    writer.WriteString(entry.Key);

                    // Write as byte array to enable partial deserialization.
                    writer.WriteByteArray(entry.GetBytes());
                }
            }
            else
            {
                // Write in diff mode.
                writer.WriteBoolean(true);

                var removed = GetRemovedKeys();

                var count = _list.Count + (removed == null ? 0 : removed.Count);

                writer.WriteInt(count);  // reserve count

                // Write removed keys as [key + null].
                if (removed != null)
                {
                    foreach (var removedKey in removed)
                    {
                        writer.WriteString(removedKey);
                        writer.WriteByteArray(null);

                        count++;
                    }
                }

                // Write dirty items.
                foreach (var entry in _list)
                {
                    if (!entry.IsDirty)
                        continue;

                    writer.WriteString(entry.Key);

                    // Write as byte array to enable partial deserialization.
                    writer.WriteByteArray(entry.GetBytes());

                    count++;
                }
            }
        }

        private ICollection<string> GetRemovedKeys()
        {
            if (_removedKeys == null)
                return null;

            // Filter out existing keys.
            var removed = new HashSet<string>(_removedKeys);

            foreach (var entry in _list)
                removed.Remove(entry.Key);

            return removed;
        }

        /// <summary>
        /// Removes the specified key.
        /// </summary>
        public void Remove(string key)
        {
            var index = GetIndex(key);

            if (index < 0)
                return;

            var entry = _list[index];
            Debug.Assert(key == entry.Key);

            _list.RemoveAt(index);
            _dict.Remove(key);

            // Update all indexes.
            for (var i = 0; i < _list.Count; i++)
                _dict[_list[i].Key] = i;

            if (entry.IsInitial)
                AddRemovedKey(key);
        }

        /// <summary>
        /// Removes at specified index.
        /// </summary>
        public void RemoveAt(int index)
        {
            var entry = _list[index];

            _list.RemoveAt(index);
            _dict.Remove(entry.Key);

            if (entry.IsInitial)
                AddRemovedKey(entry.Key);
        }

        /// <summary>
        /// Clears this instance.
        /// </summary>
        public void Clear()
        {
            foreach (var entry in _list)
            {
                if (entry.IsInitial)
                    AddRemovedKey(entry.Key);
            }

            _list.Clear();
            _dict.Clear();

            _dirtyAll = true;
        }

        /// <summary>
        /// Applies the changes.
        /// </summary>
        public void ApplyChanges(KeyValueDirtyTrackedCollection changes)
        {
            var removed = changes._removedKeys;

            if (removed != null)
            {
                foreach (var key in removed)
                    Remove(key);
            }
            else if (!changes._isDiff)
            {
                // Not a diff: replace all.
                Clear();
            }

            foreach (var changedEntry in changes._list)
            {
                var entry = GetOrCreateDirtyEntry(changedEntry.Key);

                // Copy without deserialization.
                changedEntry.CopyTo(entry);
            }
        }

        /// <summary>
        /// Adds the removed key.
        /// </summary>
        private void AddRemovedKey(string key)
        {
            Debug.Assert(!_isNew);

            if (_removedKeys == null)
                _removedKeys = new HashSet<string>();

            _removedKeys.Add(key);
        }

        /// <summary>
        /// Gets or creates an entry.
        /// </summary>
        private Entry GetOrCreateDirtyEntry(string key)
        {
            var entry = GetEntry(key);

            if (entry == null)
            {
                entry = new Entry(key, false, null);

                _dict[key] = _list.Count;
                _list.Add(entry);
            }

            entry.IsDirty = true;

            return entry;
        }

        /// <summary>
        /// Gets the entry.
        /// </summary>
        private Entry GetEntry(string key)
        {
            IgniteArgumentCheck.NotNull(key, "key");

            int index;

            return !_dict.TryGetValue(key, out index) ? null : _list[index];
        }

        /// <summary>
        /// Gets the index.
        /// </summary>
        private int GetIndex(string key)
        {
            int index;

            return !_dict.TryGetValue(key, out index) ? -1 : index;
        }

        /// <summary>
        /// Sets the dirty on read.
        /// </summary>
        private static void SetDirtyOnRead(Entry entry)
        {
            var type = entry.Value.GetType();

            if (IsImmutable(type))
                return;

            entry.IsDirty = true;
        }

        /// <summary>
        /// Determines whether the specified type is immutable.
        /// </summary>
        private static bool IsImmutable(Type type)
        {
            type = Nullable.GetUnderlyingType(type) ?? type;  // Unwrap nullable.

            if (type.IsPrimitive)
                return true;

            if (type == typeof(string) || type == typeof(DateTime) || type == typeof(Guid) || type == typeof(decimal))
                return true;

            return false;
        }

        /// <summary>
        /// Inner entry.
        /// </summary>
        private class Entry
        {
            /** */
            public readonly bool IsInitial;

            /** */
            public readonly string Key;

            /** */
            public bool IsDirty;

            /** */
            private object _value;

            /** */
            private bool _isDeserialized;

            /// <summary>
            /// Initializes a new instance of the <see cref="Entry"/> class.
            /// </summary>
            public Entry(string key, bool isInitial, object value)
            {
                Debug.Assert(key != null);

                Key = key;
                IsInitial = isInitial;
                _isDeserialized = !isInitial;
                _value = value;
            }

            /// <summary>
            /// Gets or sets the value.
            /// </summary>
            public object Value
            {
                get
                {
                    if (!_isDeserialized)
                    {
                        // TODO: BinaryFormatter
                        //_value = _marsh.Unmarshal<object>((byte[]) _value);
                        _isDeserialized = true;
                    }

                    return _value;
                }
                set
                {
                    _value = value;
                    _isDeserialized = true;
                }
            }

            /// <summary>
            /// Copies contents to another entry.
            /// </summary>
            public void CopyTo(Entry entry)
            {
                Debug.Assert(entry != null);

                entry._isDeserialized = _isDeserialized;
                entry._value = _value;
            }

            /// <summary>
            /// Gets the bytes.
            /// </summary>
            public byte[] GetBytes()
            {
                if (!_isDeserialized)
                    return (byte[]) _value;

                // TODO: BinaryFormatter
                //return marsh.Marshal(_value);
                return null;
            }
        }
    }
}
