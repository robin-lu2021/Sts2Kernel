namespace MegaCrit.Sts2.Core.Helpers;

public static class SceneHelper
{
	public static string GetScenePath(string innerPath)
	{
		if (innerPath.StartsWith('/'))
		{
			string text = innerPath;
			innerPath = text.Substring(1, text.Length - 1);
		}
		return "res://scenes/" + innerPath + ".tscn";
	}
}
