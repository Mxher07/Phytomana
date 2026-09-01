namespace Game {
    public class FileAlreadyExistsException : Exception {
        public readonly string Path;

        public FileAlreadyExistsException(string path = null, string message = null, Exception innerException = null) : base(
            message ?? (path == null ? "File Already Exists. 文件已存在。" : $"File Already Exists. 文件已存在。Path 路径: {path}"),
            innerException
        ) {
            Path = path;
        }
    }
}