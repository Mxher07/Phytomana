#include <emscripten/wasmfs.h>
#include <unistd.h>
#include <stdio.h>
#include <sys/stat.h>
#include <errno.h>

int MountOPFS(const char* mountPath) {
    backend_t opfs_backend = wasmfs_create_opfs_backend();
    return wasmfs_create_directory(mountPath, 0777, opfs_backend);
}

// 创建软链接
// target: 真实存在的物理路径 (例如 /__root__/SaveData)
// linkpath: 你想要访问的虚拟路径 (例如 /SaveData)
int CreateSymlink(const char* target, const char* linkpath) {
    return symlink(target, linkpath);
}