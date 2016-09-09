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

package org.apache.ignite.internal.processors.platform.websession;

import org.apache.ignite.binary.BinaryObjectException;
import org.apache.ignite.binary.BinaryRawReader;
import org.apache.ignite.binary.BinaryRawWriter;
import org.apache.ignite.binary.BinaryReader;
import org.apache.ignite.binary.BinaryWriter;
import org.apache.ignite.binary.Binarylizable;
import org.apache.ignite.internal.util.tostring.GridToStringExclude;
import org.apache.ignite.internal.util.typedef.internal.S;

import java.sql.Timestamp;
import java.util.Map;
import java.util.UUID;

/**
 * Web session state data.
 */
@SuppressWarnings({"ReturnOfDateField", "AssignmentToDateFieldFromParameter"})
public class PlatformDotnetSessionData implements Binarylizable {
    /** Items. */
    private Map<String, byte[]> items;

    /** Static objects. */
    @GridToStringExclude
    private byte[] staticObjects;

    /** Timeout. */
    private int timeout;

    /** Lock ID. */
    private long lockId;

    /** Lock node ID. */
    private UUID lockNodeId;

    /** Lock time. */
    private Timestamp lockTime;

    /**
     * @return Items.
     */
    public Map<String, byte[]> items() {
        return items;
    }

    /**
     * @return Static objects.
     */
    public byte[] staticObjects() {
        return staticObjects;
    }

    /**
     * @return Timeout.
     */
    public int timeout() {
        return timeout;
    }

    /**
     * @return Lock ID.
     */
    public long lockId() {
        return lockId;
    }

    /**
     * @return Lock node ID.
     */
    public UUID lockNodeId() {
        return lockNodeId;
    }

    /**
     * @return Lock time.
     */
    public Timestamp lockTime() {
        return lockTime;
    }

    /**
     * @return {@code True} if locked.
     */
    public boolean isLocked() {
        return lockTime != null;
    }

    public PlatformDotnetSessionData lock(UUID lockNodeId, long lockId, Timestamp lockTime) {
        assert !isLocked();

        PlatformDotnetSessionData res = copyWithoutLockInfo();

        res.lockId = lockId;
        res.lockNodeId = lockNodeId;
        res.lockTime = lockTime;

        return res;
    }

    /**
     * Unlock session state data.
     *
     * @param lockNodeId Lock node ID.
     * @param lockId Lock ID.
     * @return Unlocked data.
     */
    public PlatformDotnetSessionData unlock(UUID lockNodeId, long lockId) {
        assert isLocked();

        if (!this.lockNodeId.equals(lockNodeId))
            throw new IllegalStateException("Can not unlock session data: lock node id check failed.");

        if (this.lockId != lockId)
            throw new IllegalStateException("Can not unlock session data: lock id check failed.");

        return copyWithoutLockInfo();
    }

    /**
     * Update session state and release the lock.
     *
     * @param lockNodeId Lock node ID.
     * @param lockId Lock ID.
     * @param items Items.
     * @param staticObjects Static objects.
     * @param timeout Timeout.
     * @return Result.
     */
    public PlatformDotnetSessionData updateAndUnlock(UUID lockNodeId, long lockId, Map<String, byte[]> items,
        boolean isDiff, byte[] staticObjects, int timeout) {
        assert items != null;

        PlatformDotnetSessionData res = unlock(lockNodeId, lockId);

        if (!isDiff) {
            // Not a diff: remove all
            this.items.clear();
        }

        for (Map.Entry<String, byte[]> e : items.entrySet()) {
            String key = e.getKey();
            byte[] value = e.getValue();

            if (value != null)
                this.items.put(key, value);
            else
                this.items.remove(key);   // Null value indicates removed key.
        }

        res.staticObjects = staticObjects;
        res.timeout = timeout;

        return res;
    }

    /**
     * Gets a copy of this instance with non-lock properties set.
     *
     * @return Copied state data.
     */
    private PlatformDotnetSessionData copyWithoutLockInfo() {
        PlatformDotnetSessionData res = new PlatformDotnetSessionData();

        res.staticObjects = staticObjects;
        res.items = items;
        res.timeout = timeout;

        return res;
    }

    /** {@inheritDoc} */
    @Override public void writeBinary(BinaryWriter writer) throws BinaryObjectException {
        BinaryRawWriter raw = writer.rawWriter();

        raw.writeInt(timeout);
        raw.writeUuid(lockNodeId);
        raw.writeLong(lockId);
        raw.writeTimestamp(lockTime);
        raw.writeObject(items);
        raw.writeByteArray(staticObjects);
    }

    /** {@inheritDoc} */
    @Override public void readBinary(BinaryReader reader) throws BinaryObjectException {
        BinaryRawReader raw = reader.rawReader();

        timeout = raw.readInt();
        lockNodeId = raw.readUuid();
        lockId = raw.readLong();
        lockTime = raw.readTimestamp();
        items = raw.readObject();
        staticObjects = raw.readByteArray();
    }

    /** {@inheritDoc} */
    @Override public String toString() {
        return S.toString(PlatformDotnetSessionData.class, this);
    }
}
