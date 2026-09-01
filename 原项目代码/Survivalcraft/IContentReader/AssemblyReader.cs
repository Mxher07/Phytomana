using System.Reflection;

namespace Game.IContentReader {
    public class AssemblyReader : IContentReader {
        public override string Type => "System.Reflection.Assembly";
        public override string[] DefaultSuffix => ["dll"];
#pragma warning disable IL2026
        public override object Get(ContentInfo[] contents) => Assembly.Load(ModsManager.StreamToBytes(contents[0].Duplicate()));
#pragma warning restore IL2026
    }
}