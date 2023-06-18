using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.ConstrainedExecution;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ObjectExtractor
{
	// Yes this code is god awful but I really dont want to spend time
	// engineering a solution to make this work, so.. sorry :)
	public class VMFExporter
	{


		const float UROT2DEGREE = 0.00549316540360483f;
		const float MODEL_SCALE = 0.5f;

		public VMFExporter() { }

		public void ExportVMF(List<T3DObject> objects, string path)
		{
			StringBuilder sb = new();
			sb.AppendLine("versioninfo");
			sb.AppendLine("{");
			sb.AppendLine("\t\"editorversion\" \"400\"");
			sb.AppendLine("\t\"editorbuild\" \"9349\"");
			sb.AppendLine("\t\"mapversion\" \"2\"");
			sb.AppendLine("\t\"formatversion\" \"100\"");
			sb.AppendLine("\t\"prefab\" \"0\"");
			sb.AppendLine("}");

			sb.AppendLine("viewsettings");
			sb.AppendLine("{");
			sb.AppendLine("\t\"bSnapToGrid\" \"1\"");
			sb.AppendLine("\t\"bShowGrid\" \"1\"");
			sb.AppendLine("\t\"bShowLogicalGrid\" \"0\"");
			sb.AppendLine("\t\"nGridSpacing\" \"64\"");
			sb.AppendLine("\t\"bShow3DGrid\" \"0\"");
			sb.AppendLine("}");
			sb.AppendLine("world");
			sb.AppendLine("{");
			sb.AppendLine("\t\"id\" \"1\"");
			sb.AppendLine("\t\"mapversion\" \"2\"");
			sb.AppendLine("\t\"classname\" \"worldspawn\"");
			sb.AppendLine("\t\"detailmaterial\" \"detail/detailsprites\"");
			sb.AppendLine("\t\"detailvbsp\" \"detail.vbsp\"");
			sb.AppendLine("\t\"maxpropscreenwidth\" \"-1\"");
			sb.AppendLine("\t\"skyname\" \"sky_day01_01\"");
			sb.AppendLine("}");

			int id = 2;

			foreach (T3DObject obj in objects)
			{
				if (!obj.Properties.ContainsKey("StaticMesh"))
				{
					continue;
				}

				WriteEntity(sb, obj, id);
				id++;
			}

			File.WriteAllText(path, sb.ToString());
		}

		private void WriteEntity(StringBuilder sb, T3DObject obj, int id)
		{
			sb.AppendLine("entity");
			sb.AppendLine("{");
			sb.AppendLine($"\t\"id\" \"{id}\"");
			sb.AppendLine("\t\"classname\" \"prop_static\"");
			sb.AppendLine($"\t\"targetname\" \"{obj.Name}\"");

			string modelPath = obj.Properties["StaticMesh"];
			sb.AppendLine($"\t\"model\" \"{ParseModel(obj)}\"");
			sb.AppendLine($"\t\"origin\" \"{ParseLocation(obj)}\"");
			sb.AppendLine($"\t\"angles\" \"{ParseRotation(obj)}\"");
			sb.AppendLine("}");
		}

		private string ParseModel(T3DObject obj)
		{
			string origPath = obj.Properties["StaticMesh"];

			string game = "gow1";
			string modelDir = $"models/elan/{game}";

			// We're aN LD asset
			if (origPath.StartsWith("LD_"))
			{
				modelDir += "/LDAssets/";
			}
			else
			{
				modelDir += "/Enviroments/";
			}

			modelDir += origPath.Replace(".", "/");


			string scale = ParseScale(obj);

			return $"{modelDir.ToLower()}{scale}.mdl";
		}

		private string ParseLocation(T3DObject obj)
		{
			Vector3 origin = new Vector3(0, 0, 0);
			if (obj.Properties.ContainsKey("Location"))
			{
				List<float> vecValues = GetVectorValues(obj.Properties["Location"]);
				origin.X = vecValues[0];
				origin.Y = vecValues[1];
				origin.Z = vecValues[2];
			}

			// Source is FLU, UE is FRU
			float x = -(origin.X * MODEL_SCALE);
			float y = (origin.Y * MODEL_SCALE);
			float z = origin.Z * MODEL_SCALE;

			return $"{x} {y} {z}";
		}

		// Fixes angles so they are always between -360 and 360
		// Because unreal is weird and can allow over these values
		private float FixupAngle(float input)
		{
			float angle = input;

			if (angle < -360)
			{
				angle -= -360;
			}
			else if (angle > 360)
			{
				angle -= 360;
			}

			return angle;
		}

		private string ParseRotation(T3DObject obj)
		{
			Vector3 angles = new Vector3(0, 0, 0);
			if (obj.Properties.ContainsKey("Rotation"))
			{
				List<float> vecValues = GetVectorValues(obj.Properties["Rotation"]);
				angles.X = vecValues[0];
				angles.Y = vecValues[1];
				angles.Z = vecValues[2];
			}

			float x = FixupAngle(angles.X * UROT2DEGREE);
			float y = -(FixupAngle(angles.Y * UROT2DEGREE) - 90);
			float z = FixupAngle(angles.Z * UROT2DEGREE);

			return $"{x} {y} {z}";
		}

		private string ParseScale(T3DObject obj)
		{
			float drawScale = 1.0f;
			if (obj.Properties.ContainsKey("DrawScale"))
			{
				drawScale = float.Parse(obj.Properties["DrawScale"]);
			}

			Vector3 drawScale3d = new Vector3(1, 1, 1);
			if (obj.Properties.ContainsKey("DrawScale3D"))
			{
				List<float> vecValues = GetVectorValues(obj.Properties["DrawScale3D"]);
				drawScale3d.X = vecValues[0];
				drawScale3d.Y = vecValues[1];
				drawScale3d.Z = vecValues[2];
			}

			drawScale3d = Vector3.Multiply(drawScale3d, drawScale);

			float x = drawScale3d.X;
			float y = drawScale3d.Y;
			float z = drawScale3d.Z;

			// We're a mirrored object
			bool isMirrored = false; 
			if (x < 0)
			{
				x *= -1;
				isMirrored = true;
			}

			if (y < 0)
			{
				y *= -1;
			}

			if (z < 0)
			{
				z *= -1;
			}

			string vecString = $"_{x}_{y}_{z}";
			if (isMirrored)
			{
				vecString += "_mirrored";
			}

			// If our scale is equal, dont bother returning it.
			if (x == 1.0f && y == 1.0f && z == 1.0f)
			{
				if (isMirrored)
				{
					return "_mirrored";
				}

				return "";
			}

			return vecString.Replace("_0.", "_p").Replace(".", "p");
		}

		private List<float> GetVectorValues(string vec)
		{
			List<float> values = new();

			Regex rx = new("^\\s*([^=\\s]+)=(.+)$");

			// Split the vector string and parse the results using
			// regex to extract the float values
			string[] splitString = vec.Split(",");
			foreach (string str in splitString)
			{
				Match match = rx.Match(str);
				values.Add(float.Parse(match.Groups[2].Value));
			}

			return values;
		}
	}
}
