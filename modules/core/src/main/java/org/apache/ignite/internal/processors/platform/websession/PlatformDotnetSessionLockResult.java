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
import org.apache.ignite.internal.util.typedef.internal.S;

import java.sql.Timestamp;

/**
 * Result of the {@link PlatformDotnetSessionLockProcessor} execution.
 */
@SuppressWarnings({"AssignmentToDateFieldFromParameter", "ReturnOfDateField"})
public class PlatformDotnetSessionLockResult implements Binarylizable {
    /** Success flag. */
    private boolean success;

    /** Data. */
    private PlatformDotnetSessionData data;

    /** Lock time. */
    private Timestamp lockTime;

    /**
     * Ctor.
     *
     * @param success Success flag.
     * @param data Session data.
     * @param lockTime Lock time.
     */
    public PlatformDotnetSessionLockResult(boolean success, PlatformDotnetSessionData data, Timestamp lockTime) {
        this.success = success;
        this.data = data;
        this.lockTime = lockTime;
    }

    /**
     * @return Success flag.
     */
    public boolean success() {
        return success;
    }

    /**
     * @return Session state data.
     */
    public PlatformDotnetSessionData data() {
        return data;
    }

    /**
     * @return Lock time.
     */
    public Timestamp lockTime() {
        return lockTime;
    }

    /** {@inheritDoc} */
    @Override public void writeBinary(BinaryWriter writer) throws BinaryObjectException {
        BinaryRawWriter raw = writer.rawWriter();

        raw.writeBoolean(success);
        raw.writeObject(data);
        raw.writeTimestamp(lockTime);
    }

    /** {@inheritDoc} */
    @Override public void readBinary(BinaryReader reader) throws BinaryObjectException {
        BinaryRawReader raw = reader.rawReader();

        success = raw.readBoolean();
        data = raw.readObject();
        lockTime = raw.readTimestamp();
    }

    /** {@inheritDoc} */
    @Override public String toString() {
        return S.toString(PlatformDotnetSessionLockResult.class, this);
    }
}
