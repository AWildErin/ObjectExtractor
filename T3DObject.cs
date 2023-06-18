using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ObjectExtractor
{
	public class T3DObject
	{
		public string Class { get; set; } = "";
		public string Name { get; set; } = "";
		public Dictionary<string, string> Properties { get; set; } = new();
		public List<T3DObject> Children = new();

		List<string> props = new()
		{
			"Location",
			"Rotation",
			"DrawScale",
			"DrawScale3D",
			"StaticMesh"
		};

		Regex kvRegex = new("^\\s*([^=\\s]+)=(.+)$", RegexOptions.Multiline);
		Regex packageRegex = new("(_\\d|\\d)+$", RegexOptions.Multiline);

		// Remove numbers from package
		public bool ParseObject(List<string> lines, Func<string, bool> filter)
		{
			// Split using =, each actor has a class, each actor has a name
			var opts = ExtractKVs(lines[0]);
			Class = opts["Class"];
			Name = opts["Name"];

			if (!filter(Class))
			{
				return false;
			}

			var matches = kvRegex.Matches(string.Join("\n", lines));

			foreach (Match match in matches)
			{
				GroupCollection col = match.Groups;
				string key = col[1].Value;
				string value = col[2].Value;

				if (!props.Contains(key))
				{
					continue;
				}

				Properties.Add(key, value);
			}

			return true;
		}

		public void CleanUp()
		{
			// Do some clearning

			Dictionary<string, string> rawProps = new();

			foreach (var prop in Properties)
			{
				string key = prop.Key;
				string value = prop.Value;

				// If it starts with (, then it's going to be a vector
				if (value.StartsWith("("))
				{
					value = value.Replace(",", ", ");
					value = value.Replace("(", "");
					value = value.Replace(")", "");
				}

				if (key == "StaticMesh")
				{
					value = value.Remove(0, 11);
					value = value.Remove(value.Length - 1);

					string[] splitStr = value.Split(".");

					int lastIndex = splitStr.Count() - 1;
					for (int i = 0; i < lastIndex; i++)
					{
						string str = splitStr[i];

						var match = packageRegex.Match(str);
						if (match.Captures.Count <= 0)
						{
							continue;
						}

						splitStr[i] = str.Remove(match.Index);
					}

					value = String.Join(".", splitStr);
				}

				rawProps.Add(key, value);
			}

			Properties.Clear();
			foreach (var prop in rawProps)
			{
				Properties.Add(prop.Key, prop.Value);
			}
		}

		private Dictionary<string, string> ExtractKVs(string input)
		{
			string[] pairs = input.Split(' ');
			Dictionary<string, string> result = new();

			foreach (var pair in pairs)
			{
				string[] kv = pair.Split('=');

				if (kv.Length != 2)
				{
					continue;
				}

				result.Add(kv[0], kv[1]);
			}

			return result;
		}
	}
}
