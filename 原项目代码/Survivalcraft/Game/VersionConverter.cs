using System.Xml.Linq;
using Engine;
using XmlUtilities;

namespace Game {
    public abstract class VersionConverter {
        public abstract string SourceVersion { get; }

        public abstract string TargetVersion { get; }

        //如果没有重写就直接转换
        public virtual void ConvertProjectXml(XElement projectNode) {
            XmlUtils.SetAttributeValue(projectNode, "Version", TargetVersion);
        }

        public virtual void ConvertWorld(string directoryName) {
            string path = Storage.CombinePaths(directoryName, "Project.xml");
            XElement xElement;
            using (Stream stream = Storage.OpenFile(path, OpenFileMode.Read)) {
                xElement = XmlUtils.LoadXmlFromStream(stream, null, true);
            }
            ConvertProjectXml(xElement);
            using (Stream stream2 = Storage.OpenFile(path, OpenFileMode.Create)) {
                XmlUtils.SaveXmlToStream(xElement, stream2, null, true);
            }
        }
    }

    /*
    public class GenericVersionConverter(string sourceVersion,string targetVersion) : VersionConverter
    {
        private string _sourceVersion = sourceVersion;
        private string _targetVersion = targetVersion;

        public override string SourceVersion => _sourceVersion;

        public override string TargetVersion => _targetVersion;

        public override void ConvertProjectXml(XElement projectNode)
        {
            XmlUtils.SetAttributeValue(projectNode,"Version",TargetVersion);
        }

        public override void ConvertWorld(string directoryName)
        {
            string path = Storage.CombinePaths(directoryName,"Project.xml");
            XElement xElement;
            using(Stream stream = Storage.OpenFile(path,OpenFileMode.Read))
            {
                xElement = XmlUtils.LoadXmlFromStream(stream,null,throwOnError: true);
            }
            ConvertProjectXml(xElement);
            using(Stream stream2 = Storage.OpenFile(path,OpenFileMode.Create))
            {
                XmlUtils.SaveXmlToStream(xElement,stream2,null,throwOnError: true);
            }
        }
    // Example usage:
    // var converter = new GenericVersionConverter("1.11", "1.12");
    // converter.ConvertWorld("path_to_world_directory");
}*/
    public class VersionConverter14To15 : VersionConverter {
        public override string SourceVersion => "1.4";

        public override string TargetVersion => "1.5";
    }

    public class VersionConverter15To16 : VersionConverter {
        public override string SourceVersion => "1.5";

        public override string TargetVersion => "1.6";
    }

    public class VersionConverter16To17 : VersionConverter {
        public override string SourceVersion => "1.6";

        public override string TargetVersion => "1.7";
    }

    public class VersionConverter17To18 : VersionConverter {
        public override string SourceVersion => "1.7";

        public override string TargetVersion => "1.8";
    }

    public class VersionConverter18To19 : VersionConverter {
        public override string SourceVersion => "1.8";

        public override string TargetVersion => "1.9";
    }

    public class VersionConverter19To110 : VersionConverter {
        public override string SourceVersion => "1.9";

        public override string TargetVersion => "1.10";
    }

    public class VersionConverter110To111 : VersionConverter {
        public override string SourceVersion => "1.10";

        public override string TargetVersion => "1.11";
    }

    public class VersionConverter111To112 : VersionConverter {
        public override string SourceVersion => "1.11";

        public override string TargetVersion => "1.12";
    }

    public class VersionConverter114To115 : VersionConverter {
        public override string SourceVersion => "1.14";

        public override string TargetVersion => "1.15";
    }

    public class VersionConverter115To116 : VersionConverter {
        public override string SourceVersion => "1.15";

        public override string TargetVersion => "1.16";
    }

    public class VersionConverter116To117 : VersionConverter {
        public override string SourceVersion => "1.16";

        public override string TargetVersion => "1.17";
    }

    public class VersionConverter117To118 : VersionConverter {
        public override string SourceVersion => "1.17";

        public override string TargetVersion => "1.18";
    }

    public class VersionConverter118To119 : VersionConverter {
        public override string SourceVersion => "1.18";

        public override string TargetVersion => "1.19";
    }

    public class VersionConverter119To120 : VersionConverter {
        public override string SourceVersion => "1.19";

        public override string TargetVersion => "1.20";
    }

    public class VersionConverter122To123 : VersionConverter {
        public override string SourceVersion => "1.22";

        public override string TargetVersion => "1.23";
    }

    public class VersionConverter123To124 : VersionConverter {
        public override string SourceVersion => "1.23";

        public override string TargetVersion => "1.24";
    }

    public class VersionConverter124To125 : VersionConverter {
        public override string SourceVersion => "1.24";

        public override string TargetVersion => "1.25";
    }

    public class VersionConverter125To126 : VersionConverter {
        public override string SourceVersion => "1.25";

        public override string TargetVersion => "1.26";
    }

    public class VersionConverter127To128 : VersionConverter {
        public override string SourceVersion => "1.27";

        public override string TargetVersion => "1.28";
    }

    public class VersionConverter129To130 : VersionConverter {
        public override string SourceVersion => "1.29";

        public override string TargetVersion => "1.30";
    }

    public class VersionConverter130To20 : VersionConverter {
        public override string SourceVersion => "1.30";

        public override string TargetVersion => "2.0";
    }
}