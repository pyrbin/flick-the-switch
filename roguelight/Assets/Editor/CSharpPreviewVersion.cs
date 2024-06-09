using System.IO;
using System.Linq;
using System.Xml.Linq;
using UnityEditor;

namespace CSharpPreviewVersion
{
    public sealed class CSharpPreviewVersion : AssetPostprocessor
    {
        private static string OnGeneratedCSProject(string path, string content)
        {
            var canApply = path.EndsWith("Assembly-CSharp.csproj") || path.EndsWith("Assembly-CSharp-Editor.csproj");
            if (!canApply) return content;

            // csc.rsp
            var baseDir = Path.GetDirectoryName(path);
            string cscRspPath = Path.Combine(baseDir, "Assets", "csc.rsp");
            string cscRspContent = "-nullable:enable -langversion:preview";
            File.WriteAllText(cscRspPath, cscRspContent);

            var xDoc = XDocument.Parse(content);
            XElement langVersion = xDoc.Descendants("LangVersion").FirstOrDefault();
            if (langVersion != null)
            {
                langVersion.Value = "preview";
                langVersion.AddAfterSelf(new XElement("Nullable", "enable"));
                langVersion.AddAfterSelf(new XElement("ImplicitUsings", "enable"));
                langVersion.AddAfterSelf(new XElement("EnforceCodeStyleInBuild", "true"));
            }

            content = xDoc.ToString();
            return content;
        }
    }
}
