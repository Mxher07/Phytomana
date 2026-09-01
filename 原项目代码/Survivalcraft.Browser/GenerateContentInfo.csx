var info = new FileInfo(ContentFilePath);
long size = info.Exists ? info.Length : 0;
string content = $@"namespace Game {{
    public static class ContentFileInfo {{
        public const long FileSize = {size};
    }}
}}";
File.WriteAllText("ContentFileInfo.g.cs", content);