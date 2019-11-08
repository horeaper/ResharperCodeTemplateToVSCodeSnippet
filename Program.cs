using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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
			var placeHolder = new Dictionary<string, int>();
			foreach (var template in codeTemplates) {
				placeHolder.Clear();
				foreach (var line in template.Body) {
					int currentIndex = 0;
					while (true) {
						int startIndex = line.IndexOf('$', currentIndex);
						if (startIndex == -1) {
							break;;
						}
						int endIndex = line.IndexOf('$', startIndex + 1);
						if (endIndex == -1) {
							break;;
						}
						currentIndex = endIndex + 1;

						string name = line.Substring(startIndex + 1, endIndex - startIndex - 1);
						if (name != "END") {
							if (!placeHolder.TryGetValue(name, out var number)) {
								number = placeHolder.Count + 1;
								placeHolder.Add(name, number);
							}
						}
					}
				}

				for (int index = 0; index < template.Body.Length; ++index) {
					template.Body[index] = template.Body[index].Replace("$END$", "$0");

					foreach (var item in placeHolder) {
						template.Body[index] = template.Body[index].Replace($"${item.Key}$", $"${{{item.Value}:{item.Key}}}");
					}
				}
			}

			//Generate
			var builder = new StringBuilder(4096);
			builder.Append("{\n");
			foreach (var template in codeTemplates) {
				builder.Append($"\t\"{template.Shortcut}\":{{\n");
				{
					builder.Append($"\t\t\"prefix\": \"{template.Shortcut}\",\n");
					if (!string.IsNullOrEmpty(template.Description)) {
						builder.Append($"\t\t\"description\": \"{template.Description}\",\n");
					}

					builder.Append("\t\t\"body\": ");
					if (template.Body.Length == 1) {
						builder.Append($"\"{template.Body[0]}\"\n");
					}
					else {
						builder.Append("[\n");
						foreach (var line in template.Body) {
							builder.Append($"\t\t\t\"{line.Replace("\t", "\\t")}\",\n");
						}
						builder.Append("\t\t]\n");
					}
				}
				builder.Append("\t},\n");
			}
			builder.Append("}");

			//Output
			var targetFile = Path.Combine(Path.GetDirectoryName(sourceFile), "csharp.json");
			File.WriteAllText(targetFile, builder.ToString());
		}
	}
}
