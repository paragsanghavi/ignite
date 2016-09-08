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
import org.apache.ignite.binary.BinaryReader;
import org.apache.ignite.binary.BinaryWriter;
import org.apache.ignite.binary.Binarylizable;
import org.apache.ignite.cache.CacheEntryProcessor;
import org.apache.ignite.internal.util.typedef.internal.S;

import javax.cache.processor.EntryProcessorException;
import javax.cache.processor.MutableEntry;
import java.util.Map;
import java.util.UUID;

/**
 * Processor to unlock and optionally update the session.
 */
public class PlatformDotnetSessionSetAndUnlockProcessor implements
    CacheEntryProcessor<String, PlatformDotnetSessionData, Void>, Binarylizable {
    /** Lock node ID. */
    private UUID lockNodeId;

    /** Lock ID. */
    private long lockId;

    /** Update flag. */
    private boolean update;

    /** Data. */
    private Map<String, byte[]> items;

    /** Static data. */
    private byte[] staticData;

    /** Timeout. */
    private int timeout;

    /**
     * Constructor.
     *
     * @param lockNodeId Lock node ID.
     * @param lockId Lock ID.
     */
    public PlatformDotnetSessionSetAndUnlockProcessor(UUID lockNodeId, long lockId) {
        this(lockNodeId, lockId, false, null, null, -1);
    }

    /**
     * Constructor.
     *
     * @param lockNodeId Lock node ID.
     * @param lockId Lock ID.
     * @param items Items.
     * @param staticData Static data.
     * @param timeout Timeout.
     */
    public PlatformDotnetSessionSetAndUnlockProcessor(UUID lockNodeId, long lockId,
        Map<String, byte[]> items, byte[] staticData, int timeout) {
        this(lockNodeId, lockId, true, items, staticData, timeout);
    }

    /**
     * Constructor.
     *
     * @param lockNodeId Lock node ID.
     * @param lockId Lock ID.
     * @param update Whether to perform update.
     * @param items Items.
     * @param staticData Static data.
     * @param timeout Timeout.
     */
    public PlatformDotnetSessionSetAndUnlockProcessor(UUID lockNodeId, long lockId, boolean update,
        Map<String, byte[]> items, byte[] staticData, int timeout) {
        this.lockNodeId = lockNodeId;
        this.lockId = lockId;
        this.update = update;
        this.items = items;
        this.staticData = staticData;
        this.timeout = timeout;
    }

    /** {@inheritDoc} */
    @Override public Void process(MutableEntry<String, PlatformDotnetSessionData> entry, Object... args)
        throws EntryProcessorException {
        assert entry.exists();

        PlatformDotnetSessionData data = entry.getValue();

        assert data != null;

        // Unlock and update.
        if (update)
            data = data.updateAndUnlock(lockNodeId, lockId, items, staticData, timeout);
        else
            data = data.unlock(lockNodeId, lockId);

        // Apply.
        entry.setValue(data);

        return null;
    }

    /** {@inheritDoc} */
    @Override public void writeBinary(BinaryWriter writer) throws BinaryObjectException {
        // TODO
    }

    /** {@inheritDoc} */
    @Override public void readBinary(BinaryReader reader) throws BinaryObjectException {
        // TODO
    }

    /** {@inheritDoc} */
    @Override public String toString() {
        return S.toString(PlatformDotnetSessionSetAndUnlockProcessor.class, this);
    }
}
