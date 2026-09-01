using System.Text.Json;

namespace Game.IContentReader {
    public class JsonArrayReader : IContentReader {
        public override string Type => "System.Text.Json.Nodes.JsonArray";
        public override string[] DefaultSuffix => ["json"];

        public override object Get(ContentInfo[] contents) {
            JsonElement element = JsonDocument.Parse(new StreamReader(contents[0].Duplicate()).ReadToEnd(), JsonDocumentReader.DefaultJsonOptions).RootElement;
            return element.ValueKind == JsonValueKind.Array ? element : throw new InvalidDataException($"{contents[0].Filename}is not Json array");
        }
    }

    public class JsonModelReader : IContentReader {
        public override string Type => "Game.JsonModel";
        public override string[] DefaultSuffix => ["json"];
        public override object Get(ContentInfo[] contents) => Game.JsonModelReader.Load(contents[0].Duplicate());
    }

    public class JsonObjectReader : IContentReader {
        public override string Type => "System.Text.Json.Nodes.JsonObject";
        public override string[] DefaultSuffix => ["json"];

        public override object Get(ContentInfo[] contents) {
            JsonElement element = JsonDocument.Parse(new StreamReader(contents[0].Duplicate()).ReadToEnd(), JsonDocumentReader.DefaultJsonOptions).RootElement;
            return element.ValueKind == JsonValueKind.Object ? element : throw new InvalidDataException($"{contents[0].Filename}is not Json object");
        }
    }

    public class JsonDocumentReader : IContentReader {
        public static readonly JsonDocumentOptions DefaultJsonOptions = new JsonDocumentOptions {
            AllowTrailingCommas = true,
            CommentHandling = JsonCommentHandling.Skip
        };
        public override string Type => "System.Text.Json.JsonDocument";
        public override string[] DefaultSuffix => ["json"];
        public override object Get(ContentInfo[] contents) => JsonDocument.Parse(new StreamReader(contents[0].Duplicate()).ReadToEnd(), DefaultJsonOptions);
    }
}