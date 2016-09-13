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

namespace Apache.Ignite.AspNet
{
    using System;
    using System.Collections.Specialized;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Threading;
    using System.Web;
    using System.Web.SessionState;
    using Apache.Ignite.AspNet.Impl;
    using Apache.Ignite.Core;
    using Apache.Ignite.Core.Binary;
    using Apache.Ignite.Core.Cache;
    using Apache.Ignite.Core.Impl.Cache;
    using Apache.Ignite.Core.Log;

    /// <summary>
    /// ASP.NET Session-State Store Provider that uses Ignite distributed cache as an underlying storage.
    /// <para />
    /// You can either start Ignite yourself, and provide <c>gridName</c> attribute, 
    /// or provide <c>igniteConfigurationSectionName</c> attribute to start Ignite automatically from specified
    /// configuration section (see <see cref="IgniteConfigurationSection"/>) 
    /// using <c>igniteConfigurationSectionName</c>.
    /// <para />
    /// <c>cacheName</c> attribute specifies Ignite cache name to use for data storage. This attribute can be omitted 
    /// if cache name is null.
    /// <para />
    /// Optional <c>applicationId</c> attribute allows sharing a single Ignite cache between multiple web applications.
    /// </summary>
    public class IgniteSessionStateStoreProvider : SessionStateStoreProviderBase
    {
        /** Extension id  */
        private const int ExtensionId = 0;

        /// <summary>
        /// Op codes for <see cref="ICacheInternal.InvokeExtension{T}"/>.
        /// </summary>
        private enum Op
        {
            /** Lock the session data. */
            Lock = 1,

            /** Update and unlock the session data. */
            SetAndUnlock = 2,

            /** Get the data without lock. */
            Get = 3,

            /** Put the data without lock. */
            Put = 4,

            /** Remove the data without lock. */
            Remove = 5
        }

        /** Application id config parameter. */
        private const string ApplicationId = "applicationId";

        /** */
        private volatile string _applicationId;

        /** */
        private volatile ExpiryCacheHolder<string, IgniteSessionStateStoreData> _expiryCacheHolder;

        /** */
        private volatile ILogger _log;

        /** */
        private static long _lockId;

        /// <summary>
        /// Initializes the provider.
        /// </summary>
        /// <param name="name">The friendly name of the provider.</param>
        /// <param name="config">A collection of the name/value pairs representing the provider-specific attributes 
        /// specified in the configuration for this provider.</param>
        [SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods")]
        public override void Initialize(string name, NameValueCollection config)
        {
            base.Initialize(name, config);

            var cache = ConfigUtil.InitializeCache<string, IgniteSessionStateStoreData>(config, GetType());

            _expiryCacheHolder = new ExpiryCacheHolder<string, IgniteSessionStateStoreData>(cache);

            _applicationId = config[ApplicationId];

            var log = cache.Ignite.Logger;

            if (log.IsEnabled(LogLevel.Trace))
                _log = log.GetLogger(GetType().FullName);

            Log("{0} initialized: gridName={1}, cacheName={2}, applicationId={3}", GetType(), cache.Ignite.Name, 
                cache.Name, _applicationId);
        }


        /// <summary>
        /// Releases all resources used by the <see cref="T:System.Web.SessionState.SessionStateStoreProviderBase" /> 
        /// implementation.
        /// </summary>
        public override void Dispose()
        {
            // No-op.
        }

        /// <summary>
        /// Sets a reference to the <see cref="T:System.Web.SessionState.SessionStateItemExpireCallback" /> 
        /// delegate for the Session_OnEnd event defined in the Global.asax file.
        /// </summary>
        /// <param name="expireCallback">The <see cref="T:System.Web.SessionState.SessionStateItemExpireCallback" />  
        /// delegate for the Session_OnEnd event defined in the Global.asax file.</param>
        /// <returns>
        /// true if the session-state store provider supports calling the Session_OnEnd event; otherwise, false.
        /// </returns>
        public override bool SetItemExpireCallback(SessionStateItemExpireCallback expireCallback)
        {
            // Expiration events not supported for now.
            return false;
        }

        /// <summary>
        /// Called by the <see cref="T:System.Web.SessionState.SessionStateModule" /> object 
        /// for per-request initialization.
        /// </summary>
        /// <param name="context">The <see cref="T:System.Web.HttpContext" /> for the current request.</param>
        public override void InitializeRequest(HttpContext context)
        {
            // No-op.
        }

        /// <summary>
        /// Returns read-only session-state data from the session data store.
        /// </summary>
        /// <param name="context">The <see cref="T:System.Web.HttpContext" /> for the current request.</param>
        /// <param name="id">The <see cref="P:System.Web.SessionState.HttpSessionState.SessionID" /> for the 
        /// current request.</param>
        /// <param name="locked">When this method returns, contains a Boolean value that is set to true if the 
        /// requested session item is locked at the session data store; otherwise, false.</param>
        /// <param name="lockAge">When this method returns, contains a <see cref="T:System.TimeSpan" /> object that 
        /// is set to the amount of time that an item in the session data store has been locked.</param>
        /// <param name="lockId">When this method returns, contains an object that is set to the lock identifier 
        /// for the current request. For details on the lock identifier, see "Locking Session-Store Data" 
        /// in the <see cref="T:System.Web.SessionState.SessionStateStoreProviderBase" /> class summary.</param>
        /// <param name="actions">When this method returns, contains one of the 
        /// <see cref="T:System.Web.SessionState.SessionStateActions" /> values, indicating whether the current 
        /// session is an uninitialized, cookieless session.</param>
        /// <returns>
        /// A <see cref="T:System.Web.SessionState.SessionStateStoreData" /> populated with session values and 
        /// information from the session data store.
        /// </returns>
        public override SessionStateStoreData GetItem(HttpContext context, string id, out bool locked,
            out TimeSpan lockAge, out object lockId,
            out SessionStateActions actions)
        {
            Log("GetItem(id={0})", id);

            actions = SessionStateActions.None;
            lockId = null;
            lockAge = TimeSpan.Zero;
            locked = false;

            var key = GetKey(id);
            var data = GetItem(key);

            if (data != null)
            {
                locked = data.LockNodeId != null;

                if (!locked)
                {
                    Log("GetItem session store data found", id, context);

                    return data;
                }

                Log("GetItem session store data locked", id, context);

                Debug.Assert(data.LockTime != null);

                lockAge = DateTime.UtcNow - data.LockTime.Value;

                lockId = data.LockId;

                return null;
            }

            Log("GetItem session store data not found", id, context);

            return null;
        }

        /// <summary>
        /// Returns read-only session-state data from the session data store.
        /// </summary>
        /// <param name="context">The <see cref="T:System.Web.HttpContext" /> for the current request.</param>
        /// <param name="id">The <see cref="P:System.Web.SessionState.HttpSessionState.SessionID" /> for the current 
        /// request.</param>
        /// <param name="locked">When this method returns, contains a Boolean value that is set to true if a lock 
        /// is successfully obtained; otherwise, false.</param>
        /// <param name="lockAge">When this method returns, contains a <see cref="T:System.TimeSpan" /> object that 
        /// is set to the amount of time that an item in the session data store has been locked.</param>
        /// <param name="lockId">When this method returns, contains an object that is set to the lock identifier 
        /// for the current request. For details on the lock identifier, see "Locking Session-Store Data" in 
        /// the <see cref="T:System.Web.SessionState.SessionStateStoreProviderBase" /> class summary.</param>
        /// <param name="actions">When this method returns, contains one of the 
        /// <see cref="T:System.Web.SessionState.SessionStateActions" /> values, indicating whether the current 
        /// session is an uninitialized, cookieless session.</param>
        /// <returns>
        /// A <see cref="T:System.Web.SessionState.SessionStateStoreData" /> populated with session values 
        /// and information from the session data store.
        /// </returns>
        public override SessionStateStoreData GetItemExclusive(HttpContext context, string id, out bool locked,
            out TimeSpan lockAge,
            out object lockId, out SessionStateActions actions)
        {
            Log("GetItemExclusive", id, context);

            actions = SessionStateActions.None;  // Our items never need initialization.
            lockAge = TimeSpan.Zero;

            var lockId0 = Interlocked.Increment(ref _lockId);
            lockId = lockId0;

            var key = GetKey(id);

            var lockResult = LockItem(key, lockId0);

            if (lockResult == null)
            {
                Log("GetItemExclusive session store data not found", id, context);

                locked = false;

                return null;
            }

            if (!lockResult.Success)
            {
                // Already locked.
                Log("GetItemExclusive already locked", id, context);

                locked = true;

                Debug.Assert(lockResult.LockTime != null);

                lockAge = DateTime.UtcNow - lockResult.LockTime.Value;

                return null;
            }

            Log("GetItemExclusive session store data found", id, context);

            locked = false;

            return lockResult.Data;
        }

        /// <summary>
        /// Releases a lock on an item in the session data store.
        /// </summary>
        /// <param name="context">The <see cref="T:System.Web.HttpContext" /> for the current request.</param>
        /// <param name="id">The session identifier for the current request.</param>
        /// <param name="lockId">The lock identifier for the current request.</param>
        public override void ReleaseItemExclusive(HttpContext context, string id, object lockId)
        {
            Log("ReleaseItemExclusive", id, context);

            UnlockItem(GetKey(id), (long) lockId);
        }

        /// <summary>
        /// Updates the session-item information in the session-state data store with values from the current request, 
        /// and clears the lock on the data.
        /// </summary>
        /// <param name="context">The <see cref="T:System.Web.HttpContext" /> for the current request.</param>
        /// <param name="id">The session identifier for the current request.</param>
        /// <param name="item">The <see cref="T:System.Web.SessionState.SessionStateStoreData" /> object that 
        /// contains the current session values to be stored.</param>
        /// <param name="lockId">The lock identifier for the current request.</param>
        /// <param name="newItem">true to identify the session item as a new item; false to identify the session 
        /// item as an existing item.</param>
        [SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods")]
        public override void SetAndReleaseItemExclusive(HttpContext context, string id, SessionStateStoreData item,
            object lockId, bool newItem)
        {
            Log("SetAndReleaseItemExclusive", id, context);

            Debug.Assert(item != null);

            var key = GetKey(id);

            SetAndUnlockItem(key, (IgniteSessionStateStoreData) item);
        }

        /// <summary>
        /// Deletes item data from the session data store.
        /// </summary>
        /// <param name="context">The <see cref="T:System.Web.HttpContext" /> for the current request.</param>
        /// <param name="id">The session identifier for the current request.</param>
        /// <param name="lockId">The lock identifier for the current request.</param>
        /// <param name="item">The <see cref="T:System.Web.SessionState.SessionStateStoreData" /> that represents 
        /// the item to delete from the data store.</param>
        public override void RemoveItem(HttpContext context, string id, object lockId, SessionStateStoreData item)
        {
            Log("RemoveItem", id, context);

            RemoveItem(GetKey(id));
        }

        /// <summary>
        /// Updates the expiration date and time of an item in the session data store.
        /// </summary>
        /// <param name="context">The <see cref="T:System.Web.HttpContext" /> for the current request.</param>
        /// <param name="id">The session identifier for the current request.</param>
        public override void ResetItemTimeout(HttpContext context, string id)
        {
            // No-op.

            // This is not necessary since ResetItemTimeout is called right after SetAndReleaseItemExclusive,
            // which itself resets the timeout when the item is inserted into cache.
        }

        /// <summary>
        /// Creates a new <see cref="T:System.Web.SessionState.SessionStateStoreData" /> object to be used 
        /// for the current request.
        /// </summary>
        /// <param name="context">The <see cref="T:System.Web.HttpContext" /> for the current request.</param>
        /// <param name="timeout">The session-state <see cref="P:System.Web.SessionState.HttpSessionState.Timeout" /> 
        /// value for the new <see cref="T:System.Web.SessionState.SessionStateStoreData" />.</param>
        /// <returns>
        /// A new <see cref="T:System.Web.SessionState.SessionStateStoreData" /> for the current request.
        /// </returns>
        public override SessionStateStoreData CreateNewStoreData(HttpContext context, int timeout)
        {
            Log("CreateNewStoreData", "", context, timeout);

            return new IgniteSessionStateStoreData(SessionStateUtility.GetSessionStaticObjects(context), timeout);
        }

        /// <summary>
        /// Adds a new session-state item to the data store.
        /// </summary>
        /// <param name="context">The <see cref="T:System.Web.HttpContext" /> for the current request.</param>
        /// <param name="id">The <see cref="P:System.Web.SessionState.HttpSessionState.SessionID" /> 
        /// for the current request.</param>
        /// <param name="timeout">The session <see cref="P:System.Web.SessionState.HttpSessionState.Timeout" /> 
        /// for the current request.</param>
        public override void CreateUninitializedItem(HttpContext context, string id, int timeout)
        {
            Log("CreateUninitializedItem", id, context, timeout);

            var cache = _expiryCacheHolder.GetCacheWithExpiry((long) timeout * 60);

            var key = GetKey(id);

            var data = new IgniteSessionStateStoreData(SessionStateUtility.GetSessionStaticObjects(context), timeout);

            PutItem(key, data, cache);
        }

        /// <summary>
        /// Called by the <see cref="T:System.Web.SessionState.SessionStateModule" /> object at the end of a request.
        /// </summary>
        /// <param name="context">The <see cref="T:System.Web.HttpContext" /> for the current request.</param>
        public override void EndRequest(HttpContext context)
        {
            // No-op.
        }

        /// <summary>
        /// Gets the cache.
        /// </summary>
        private ICache<string, IgniteSessionStateStoreData> Cache
        {
            get
            {
                var holder = _expiryCacheHolder;

                if (holder == null)
                    throw new InvalidOperationException(GetType() + " has not been initialized.");

                return holder.Cache;
            }
        }

        /// <summary>
        /// Gets the key.
        /// </summary>
        private string GetKey(string sessionId)
        {
            return _applicationId == null ? sessionId : ApplicationId + "." + sessionId;
        }

        /// <summary>
        /// Writes the lock info.
        /// </summary>
        private void WriteLockInfo(IBinaryRawWriter writer, long lockId, bool writeTime = false)
        {
            writer.WriteGuid(Cache.Ignite.GetCluster().GetLocalNode().Id);
            writer.WriteLong(lockId);

            if (writeTime)
                writer.WriteTimestamp(DateTime.UtcNow);
        }

        /// <summary>
        /// Logs the specified message to the trace log, if enabled.
        /// </summary>
        private void Log(string msg, params object[] args)
        {
            var log = _log;

            if (log == null)
                return;

            log.Trace(msg, args);
        }

        /// <summary>
        /// Logs the specified method call to the trace log, if enabled.
        /// </summary>
        private void Log(string method, string id, HttpContext ctx, int timeout = 0)
        {
            var log = _log;

            if (log == null)
                return;

            log.Trace("{0}: id={1}, url={2}, timeout={3}", method, id, ctx.Request.Path, timeout);
        }

        /// <summary>
        /// Locks the item.
        /// </summary>
        private SessionStateLockResult LockItem(string key, long lockId)
        {
            return OutInOp(Op.Lock,
                w =>
                {
                    w.WriteString(key);
                    WriteLockInfo(w, lockId, true);
                }, 
                r => new SessionStateLockResult(r));
        }

        /// <summary>
        /// Unlocks the item.
        /// </summary>
        private void UnlockItem(string key, long lockId)
        {
            OutOp(Op.SetAndUnlock,
                w =>
                {
                    w.WriteString(key);
                    w.WriteBoolean(false); // Only unlock.
                    WriteLockInfo(w, lockId);
                });
        }

        /// <summary>
        /// Sets and unlocks the item.
        /// </summary>
        private void SetAndUnlockItem(string key, IgniteSessionStateStoreData data)
        {
            var cache = _expiryCacheHolder.GetCacheWithExpiry(data.Timeout * 60);

            OutOp(Op.SetAndUnlock, w =>
            {
                w.WriteString(key);
                w.WriteBoolean(true); // Unlock and update.
                data.WriteBinary(w, true);
            }, cache);
        }

        /// <summary>
        /// Puts the item.
        /// </summary>
        private void PutItem(string key, IgniteSessionStateStoreData data, ICache<string, IgniteSessionStateStoreData> cache)
        {
            OutOp(Op.Put, w =>
            {
                w.WriteString(key);
                data.WriteBinary(w, false);
            }, cache);
        }

        /// <summary>
        /// Gets the item.
        /// </summary>
        private IgniteSessionStateStoreData GetItem(string key)
        {
            return OutInOp(Op.Get, w => w.WriteString(key), r => new IgniteSessionStateStoreData(r));
        }

        /// <summary>
        /// Removes the item.
        /// </summary>
        private void RemoveItem(string key)
        {
            OutOp(Op.Remove, w => w.WriteString(key));
        }

        /// <summary>
        /// Invokes the extension operation.
        /// </summary>
        private void OutOp(Op op, Action<IBinaryRawWriter> writeAction, 
            ICache<string, IgniteSessionStateStoreData> cache = null)
        {
            OutInOp<object>(op, writeAction, null, cache);
        }

        /// <summary>
        /// Invokes the extension operation.
        /// </summary>
        private T OutInOp<T>(Op op, Action<IBinaryRawWriter> writeAction, Func<IBinaryRawReader, T> readFunc, 
            ICache<string, IgniteSessionStateStoreData> cache = null)
        {
            cache = cache ?? Cache;

            return ((ICacheInternal) cache).InvokeExtension(ExtensionId, (int) op, writeAction, readFunc);
        }
    }
}