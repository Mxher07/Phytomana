namespace Game {
    public class MtllibStruct {
        public Dictionary<string, string> TexturePaths = [];

        public static MtllibStruct Load(Stream stream) {
            MtllibStruct mtllibStruct = new();
            using (stream) {
                StreamReader streamReader = new(stream);
                string Tkey = null;
                while (!streamReader.EndOfStream) {
                    string line = streamReader.ReadLine();
                    string[] spl = line.Split([(char)0x09, (char)0x20], StringSplitOptions.None);
                    switch (spl[0]) {
                        case "newmtl": {
                            Tkey = spl[1];
                            break;
                        }
                        case "map_Kd": {
                            if (string.IsNullOrEmpty(Tkey)) {
                                throw new Exception("请先newmtl");
                            }
                            mtllibStruct.TexturePaths.Add(Tkey, spl[1]);
                            break;
                        }
                    }
                }
            }
            return mtllibStruct;
        }
    }
}