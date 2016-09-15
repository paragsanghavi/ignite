package com.mapr;

import org.apache.hadoop.fs.FSDataInputStream;
import org.apache.hadoop.fs.FSDataOutputStream;
import org.apache.hadoop.fs.FileStatus;
import org.apache.hadoop.fs.FileSystem;
import org.apache.hadoop.fs.Path;
import org.apache.hadoop.fs.permission.FsPermission;
import org.apache.hadoop.security.Groups;
import org.apache.hadoop.security.UserGroupInformation;
import org.apache.hadoop.util.Progressable;

import java.io.FileNotFoundException;
import java.io.IOException;
import java.lang.reflect.Field;
import java.lang.reflect.Method;
import java.net.URI;

/**
 * Created by vozerov on 9/15/2016.
 */
public class DummyFileSystem extends FileSystem {

    public DummyFileSystem() throws IOException {
        try {
            Method m =  UserGroupInformation.class.getDeclaredMethod("reset");

            m.setAccessible(true);

            m.invoke(null);

            Field f = Groups.class.getDeclaredField("GROUPS");

            f.setAccessible(true);

            f.set(null, null);
        }
        catch (Exception ignore) {
            throw new RuntimeException(ignore);
        }

        UserGroupInformation.getLoginUser();
    }

    @Override
    public URI getUri() {
        return null;
    }

    @Override
    public FSDataInputStream open(Path f, int bufferSize) throws IOException {
        return null;
    }

    @Override
    public FSDataOutputStream create(Path f, FsPermission permission, boolean overwrite, int bufferSize, short replication, long blockSize, Progressable progress) throws IOException {
        return null;
    }

    @Override
    public FSDataOutputStream append(Path f, int bufferSize, Progressable progress) throws IOException {
        return null;
    }

    @Override
    public boolean rename(Path src, Path dst) throws IOException {
        return false;
    }

    @Override
    public boolean delete(Path f, boolean recursive) throws IOException {
        return false;
    }

    @Override
    public FileStatus[] listStatus(Path f) throws FileNotFoundException, IOException {
        return new FileStatus[0];
    }

    @Override
    public void setWorkingDirectory(Path new_dir) {

    }

    @Override
    public Path getWorkingDirectory() {
        return null;
    }

    @Override
    public boolean mkdirs(Path f, FsPermission permission) throws IOException {
        return false;
    }

    @Override
    public FileStatus getFileStatus(Path f) throws IOException {
        return null;
    }
}
