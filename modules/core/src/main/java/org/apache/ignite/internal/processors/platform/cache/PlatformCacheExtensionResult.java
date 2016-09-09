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

/**
 * Result of {@link PlatformCacheExtension} invocation.
 */
public class PlatformCacheExtensionResult {
    /** Whether extension handled the case. */
    private final boolean isMatch;

    /** Result. */
    private final Object result;

    /**
     * Ctor.
     *
     * @param isMatch Whether extension handled the case.
     * @param result Invocation result.
     */
    public PlatformCacheExtensionResult(boolean isMatch, Object result) {
        this.isMatch = isMatch;
        this.result = result;
    }

    /**
     * @return Whether extension handled the case.
     */
    public boolean isMatch() {
        return isMatch;
    }

    /**
     * @return Invocation result.
     */
    public Object result() {
        return result;
    }
}
