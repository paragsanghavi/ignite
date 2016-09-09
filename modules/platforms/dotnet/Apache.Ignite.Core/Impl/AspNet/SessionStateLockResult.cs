﻿/*
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

namespace Apache.Ignite.Core.Impl.AspNet
{
    using System;
    using System.Diagnostics;
    using Apache.Ignite.Core.Binary;
    using Apache.Ignite.Core.Impl.Binary;

    /// <summary>
    /// Result of the session state lock processor.
    /// </summary>
    public class SessionStateLockResult : IBinaryWriteAware
    {
        /** Success flag. */
        private readonly bool _success;

        /** Session state data. */
        private readonly SessionStateData _data;

        /** Lock time. */
        private readonly DateTime? _lockTime;

        /// <summary>
        /// Initializes a new instance of the <see cref="SessionStateLockResult"/> class.
        /// </summary>
        /// <param name="reader">The reader.</param>
        public SessionStateLockResult(IBinaryRawReader reader)
        {
            _success = reader.ReadBoolean();
            _data = reader.ReadObject<SessionStateData>();
            _lockTime = reader.ReadTimestamp();

            Debug.Assert(_success ^ (_data == null));
            Debug.Assert(_success ^ (_lockTime != null));
        }

        /// <summary>
        /// Gets a value indicating whether lock succeeded.
        /// </summary>
        public bool Success
        {
            get { return _success; }
        }

        /// <summary>
        /// Gets the data. Null when <see cref="Success"/> is <c>false</c>.
        /// </summary>
        public SessionStateData Data
        {
            get { return _data; }
        }

        /// <summary>
        /// Gets the lock time. Null when <see cref="Success"/> is <c>true</c>.
        /// </summary>
        public DateTime? LockTime
        {
            get { return _lockTime; }
        }

        /// <summary>
        /// Returns a <see cref="string" /> that represents this instance.
        /// </summary>
        public override string ToString()
        {
            return string.Format("{0} [Success={1}]", GetType().Name, _success);
        }

        /// <summary>
        /// Writes this object to the given writer.
        /// </summary>
        public void WriteBinary(IBinaryWriter writer)
        {
            throw new NotSupportedException(GetType() + " is only written from native code.");
        }
    }
}