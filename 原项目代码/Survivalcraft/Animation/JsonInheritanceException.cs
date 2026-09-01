using System.Runtime.Serialization;

namespace Engine.Animation {
    /// <summary>
    /// Exception thrown when JSON template inheritance encounters an error,
    /// such as circular inheritance or missing base template.
    /// </summary>
    [Serializable]
    public class JsonInheritanceException : Exception {
        /// <summary>
        /// Gets the inheritance chain that caused the exception.
        /// May be null if the chain could not be determined.
        /// </summary>
        public string InheritanceChain { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonInheritanceException" /> class
        /// with a specified error message and optional inheritance chain.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="chain">The inheritance chain that caused the error.</param>
        public JsonInheritanceException(string message, string chain = null) : base(message) => InheritanceChain = chain;

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonInheritanceException" /> class
        /// with a specified error message, inheritance chain, and a reference to the inner exception.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="chain">The inheritance chain that caused the error.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public JsonInheritanceException(string message, string chain, Exception innerException) : base(message, innerException) =>
            InheritanceChain = chain;

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonInheritanceException" /> class
        /// with serialized data.
        /// </summary>
        /// <param name="info">The object that holds the serialized object data.</param>
        /// <param name="context">The contextual information about the source or destination.</param>
        [Obsolete("Obsolete")]
        public JsonInheritanceException(SerializationInfo info, StreamingContext context) : base(info, context) =>
            InheritanceChain = info.GetString(nameof(InheritanceChain));

        /// <summary>
        /// Sets the <see cref="SerializationInfo" /> with information about the exception.
        /// </summary>
        /// <param name="info">The object that holds the serialized object data.</param>
        /// <param name="context">The contextual information about the source or destination.</param>
        [Obsolete(
            "This API supports obsolete formatter-based serialization. It should not be called or extended by application code.",
            DiagnosticId = "SYSLIB0051",
            UrlFormat = "https://aka.ms/dotnet-warnings/{0}"
        )]
        public override void GetObjectData(SerializationInfo info, StreamingContext context) {
            base.GetObjectData(info, context);
            info.AddValue(nameof(InheritanceChain), InheritanceChain);
        }
    }
}