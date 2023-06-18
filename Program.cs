using System.Text.Json;
using System.Text.RegularExpressions;

namespace ObjectExtractor
{
	/// <summary>
	/// Takes a t3d file and outputs a json file containing the data of the actors
	/// </summary>
	public class Program
	{
		static List<T3DObject> T3DObjects = new();

		static void Main(string[] args)
		{
			string fileName = args[0];

			// Default output next to the original file with the same name
			string output = fileName.Replace(".t3d", ".json");
			string outputVMF = fileName.Replace(".t3d", ".vmf");

			if (args.Length > 1)
			{
				output = args[1];
			}
			if (args.Length > 1)
			{
				output = args[2];
			}

			if (fileName == null || !File.Exists(fileName))
			{
				PrintHelp();
				return;
			}

			if (!fileName.EndsWith(".t3d"))
			{
				Console.WriteLine("ERROR: File must be .t3d!");
				return;
			}

			if (!ParseFile(fileName))
			{
				Console.WriteLine("ERROR: Failed to parse .t3d file!");
				return;
			}

			// Output our data into a json file
			string json = JsonSerializer.Serialize(T3DObjects, new JsonSerializerOptions
			{
				WriteIndented = true
			});
			File.WriteAllText(output, json);

			// Write VMF
			VMFExporter vmf = new();
			vmf.ExportVMF(T3DObjects, outputVMF);
		}

		static void PrintHelp()
		{
			Console.WriteLine("ObjectExtractor");
			Console.WriteLine("Takes a t3d file and outputs a json file containing the data of the specified objects.");
			Console.WriteLine("Usage: ObjectExtractor.exe [path to t3d]");
		}

		static bool ParseFilter(string className)
		{
			return className == "StaticMeshActor";
		}

		// TODO: Just pass the level object in as main object like it'll literally
		// just work if we do it correctly
		static bool ParseFile(string input)
		{
			// Read all the lines and trim the whitespace from the start.
			List<string> lines = File.ReadAllLines(input)
								.Select(x => x.TrimStart())
								.ToList();

			Regex rx = new("^\\s*([^=\\s]+)=(.+)$");

			// There will always be equal Begin and Ends for actors
			List<string> Begins = lines.Where(x => x.StartsWith("Begin Actor")).ToList();
			List<string> Ends = lines.Where(x => x.StartsWith("End Actor")).ToList();

			for (int i = 0; i < Begins.Count; i++ )
			{
				T3DObject obj = new();

				// Get all the lines between our two begin and ends
				int startIndex = lines.IndexOf(Begins[i]);
				int endIndex = lines.IndexOf(Ends[i], startIndex);

				var objLines = lines.GetRange(startIndex, (endIndex - startIndex) + 1);

				if (obj.ParseObject(objLines, ParseFilter))
				{
					obj.CleanUp();
					T3DObjects.Add(obj);
				}
			}

			return true;
		}
	}
}