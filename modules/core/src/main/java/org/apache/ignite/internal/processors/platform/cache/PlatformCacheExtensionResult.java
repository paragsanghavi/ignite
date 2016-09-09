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
