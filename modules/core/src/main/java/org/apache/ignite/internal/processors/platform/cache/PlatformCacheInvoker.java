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

package org.apache.ignite.internal.processors.platform.cache;

import org.apache.ignite.IgniteCache;
import org.apache.ignite.binary.BinaryRawReader;
import org.apache.ignite.internal.binary.BinaryRawReaderEx;
import org.apache.ignite.internal.processors.platform.websession.PlatformDotnetSessionLockProcessor;
import org.apache.ignite.internal.processors.platform.websession.PlatformDotnetSessionSetAndUnlockProcessor;

import java.sql.Timestamp;
import java.util.Map;
import java.util.TreeMap;
import java.util.UUID;

/**
 * Custom entry processor invoker.
 */
public class PlatformCacheInvoker {
    /** */
    public static final int OP_SESSION_LOCK = 1;

    /** */
    public static final int OP_SESSION_SET_AND_UNLOCK = 2;

    /**
     * Invokes the custom processor.
     *
     * @param reader Reader.
     * @param cache Cache.
     *
     * @return Result.
     */
    @SuppressWarnings("unchecked")
    public static Object invoke(BinaryRawReader reader, IgniteCache cache) {
        int opCode = reader.readInt();

        String key = reader.readString();

        Object res = null;

        switch (opCode) {
            case OP_SESSION_LOCK: {
                UUID lockNodeId = reader.readUuid();
                long lockId = reader.readLong();
                Timestamp lockTime = reader.readTimestamp();

                res = cache.invoke(key, new PlatformDotnetSessionLockProcessor(lockNodeId, lockId, lockTime));

                break;
            }

            case OP_SESSION_SET_AND_UNLOCK:
                UUID lockNodeId = reader.readUuid();
                long lockId = reader.readLong();

                PlatformDotnetSessionSetAndUnlockProcessor proc;

                if (reader.readBoolean()) {
                    boolean isDiff = !reader.readBoolean();

                    int count = reader.readInt();

                    Map<String, byte[]> entries = new TreeMap<>();

                    for (int i = 0; i < count; i++)
                        entries.put(reader.readString(), reader.readByteArray());

                    byte[] staticData = reader.readByteArray();
                    int timeout = reader.readInt();

                    proc = new PlatformDotnetSessionSetAndUnlockProcessor(lockNodeId, lockId, entries, isDiff,
                        staticData, timeout);
                }
                else
                    proc = new PlatformDotnetSessionSetAndUnlockProcessor(lockNodeId, lockId);

                cache.invoke(key, proc);

                break;
        }

        return res;
    }
}
