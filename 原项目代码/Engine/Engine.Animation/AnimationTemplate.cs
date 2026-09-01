namespace Engine.Animation {
    /// <summary>
    /// 动画模板，定义生物类型的动画特性
    /// </summary>
    public class AnimationTemplate {
        public string Name { get; }
        public Dictionary<string, LayerDefinition> Layers { get; }
        public string[] RequiredBones { get; }

        public AnimationTemplate(string name,
            Dictionary<string, LayerDefinition> layers,
            string[] requiredBones = null) {
            Name = name;
            Layers = layers ?? new Dictionary<string, LayerDefinition>();
            RequiredBones = requiredBones ?? new string[0];
        }
    }
}