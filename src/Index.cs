using System;
using System.IO;

namespace StaticPageBuilder
{
	public static class Index
	{
		public static bool IsBuilt = false;
		public static void Build(string path)
		{
			_ROOT = path;
			_SRC = Path.Combine(_ROOT, "src");

			_COMPONENTS = Path.Combine(_SRC, ".components");
			_TEMPLATES = Path.Combine(_SRC, ".templates");
			_LAYOUTS = Path.Combine(_SRC, ".layouts");

			IsBuilt = true;
		}

		public static string _ROOT;
		public static string _SRC;

		public static string _COMPONENTS;
		public static string _TEMPLATES;
		public static string _LAYOUTS;
	}
}