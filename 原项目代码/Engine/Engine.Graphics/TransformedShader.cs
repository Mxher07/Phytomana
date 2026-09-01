namespace Engine.Graphics {
    public class TransformedShader : Shader {
        public readonly ShaderTransforms Transforms;

        public TransformedShader(string vsc, string psc, int maxInstancesCount, params ShaderMacro[] shaderMacros) : base(vsc, psc, shaderMacros) =>
            Transforms = new ShaderTransforms(maxInstancesCount);
    }
}