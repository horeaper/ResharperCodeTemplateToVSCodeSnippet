using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace ResharperCodeTemplateToVSCodeSnippet
{
	class Program
	{
		static void Main(string[] args)
		{
			if (args.Length != 1 || !File.Exists(args[0])) {
				Console.WriteLine(@"Usage: .\T2S.exe ExportedTemplates.DotSettings");
				return;
			}

			try {
				ProcessFile(args[0]);
			}
			catch {
				Console.WriteLine("Unreconized DotSettings file.");
			}
		}

		class DataModel
		{
			public string Shortcut;
			public string Description;
			public string[] Body;

			public override string ToString()
			{
				return $"Shortcut: {Shortcut ?? "(null)"}";
			}
		}

		static void ProcessFile(string sourceFile)
		{
			const string LeadingKeyString = "/Default/PatternsAndTemplates/LiveTemplates/Template/=";

			var document = XDocument.Load(sourceFile);
			if (document.Root == null) {
				throw new NotSupportedException();
			}

			//Get and organize content from xaml
			var content = new Dictionary<string, DataModel>();
			foreach (var rootItem in document.Root.Elements().Where(item => item.Name.LocalName == "String" && item.FirstAttribute?.Name.LocalName == "Key")) {
				var key = rootItem.FirstAttribute.Value;
				var value = rootItem.Value;

				if (key.StartsWith(LeadingKeyString) && key.Length > LeadingKeyString.Length + 32) {
					var id = key.Substring(LeadingKeyString.Length, 32);
					var item = content.GetOrAdd(id);
					
					var type = key.Substring(LeadingKeyString.Length + 32 + 1).Split('/');
					switch (type[0]) {
						case "Shortcut":
							item.Shortcut = value;
							break;
						case "Description":
							item.Description = value;
							break;
						case "Text":
							item.Body = value.Replace("\r", "").Split('\n');
							break;
					}
				}
			}

			//Filter and sort
			var codeTemplates = content.Values.Where(item => !string.IsNullOrEmpty(item.Shortcut) && item.Body?.Length > 0 && !string.IsNullOrEmpty(item.Body[0])).OrderBy(item => item.Shortcut).ToList();

			//Convert all $XXX$ indicators
			foreach (var template in codeTemplates) {
				
			}

			//Output
			var targetFile = Path.Combine(Path.GetDirectoryName(sourceFile), "csharp.json");
		}
	}
}
