using Engine.Graphics;

namespace Game.IContentReader {
    public class ShaderReader : IContentReader {
        public override string Type => "Engine.Graphics.Shader";
        public override string[] DefaultSuffix => ["vsh", "psh"];

        public override object Get(ContentInfo[] contents) {
            ShaderMacro[] shaderMacros = contents[0].Filename.StartsWith("AlphaTested") ? [new ShaderMacro("ALPHATESTED")] : [];
            return new Shader(
                new StreamReader(contents[0].Duplicate()).ReadToEnd(),
                new StreamReader(contents[1].Duplicate()).ReadToEnd(),
                shaderMacros
            );
        }
    }
}