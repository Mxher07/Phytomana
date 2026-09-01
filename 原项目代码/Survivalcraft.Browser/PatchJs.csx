IEnumerable<string> dotnetNativeContent = File.ReadLines(DotnetNativePath, Encoding.ASCII);
bool commentNextLine = false;
StringBuilder sb = new(400000);
foreach (string line in dotnetNativeContent) {
    if (commentNextLine) {
        sb.AppendLine($"//{line}");
        commentNextLine = false;
        continue;
    }
    sb.AppendLine(line);
    if (line.StartsWith("function _egl")) {
        commentNextLine = true;
        continue;
    }
    if (line == " } else transferredCanvasNames &&= UTF8ToString(transferredCanvasNames).trim();") {
        sb.AppendLine(
            @" if (transferredCanvasNames === 0
  && !ENVIRONMENT_IS_PTHREAD
  && Module[""canvas""]
  && !Module[""canvas""].transferred
 ) {
  transferredCanvasNames = ""#canvas"";
  Module[""canvas""].transferred = true;
 }"
        );
        continue;
    }
    if (line == " createPath(parent, path, canRead, canWrite) {") {
        sb.AppendLine("  return parent + path;");
    }
}
File.WriteAllText(DotnetNativePath, sb.ToString(), Encoding.ASCII);

string dotnetNativeWorkerContent = File.ReadAllText(DotnetNativeWorkerPath, Encoding.ASCII);
int index = dotnetNativeWorkerContent.IndexOf("    } else if (e.data.cmd) {");
string result = dotnetNativeWorkerContent.Insert(
    index,
    @"    } else if (e.data.cmd === 'resize_canvas') {
        if (Module.canvas) {
            Module.canvas.width = e.data.width;
            Module.canvas.height = e.data.height;
        }
"
);
File.WriteAllText(DotnetNativeWorkerPath, result, Encoding.ASCII);