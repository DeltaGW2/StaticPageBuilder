using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
using AngleSharp.Html.Parser;
using AngleSharp.Html;

namespace StaticPageBuilder
{
	class Program
	{
		static void Main(string[] args)
		{
			string target;

			if (args.Length == 1)
			{
				target = args[0];
			}
			else
			{
				do
				{
					Console.WriteLine("Enter target folder:");
					target = Console.ReadLine();
				} while (!Directory.Exists(target));
			}

			Console.WriteLine("Parsing: " + target);

			Parse(target);

			Console.WriteLine("Done.");
		}

		/// <summary> Returns all Identifiers in a Dictionary for the given location. </summary>
		static Dictionary<string, string> GetIdentifiers(string path, bool relativeFromSource = true)
		{
			if (relativeFromSource)
			{
				path = Path.Combine(Index._SRC, path);
			}

			Dictionary<string, string> identifiers = new Dictionary<string, string>();

			if (File.Exists(path))
			{
				List<string> lines = File.ReadAllLines(path).ToList();
				foreach (string line in lines)
				{
					// if an empty line is hit, stop the HTML starts afterwards
					if (string.IsNullOrWhiteSpace(line))
					{
						break;
					}

					// Split into array
					string[] pair = line.Split(":");

					// If all empty, move on
					// If only value, set as key, value empty
					// If both or more, use first as key combine the rest as value
					if (pair.Length == 0)
					{
						continue;
					}
					else if (pair.Length == 1)
					{
						string val = pair[0];

						// remove leading space e.g. "Title: Hello World" has a space in front usually
						val = val.TrimStart(' ');

						identifiers.Add(val, "");
					}
					else
					{
						string val = "";
						for (int i = 1; i < pair.Length; i++)
						{
							val += pair[i];
						}

						// remove leading space e.g. "Title: Hello World" has a space in front usually
						val = val.TrimStart(' ');

						identifiers.Add(pair[0], val);
					}
				}
			}
			
			return identifiers;
		}

		/// <summary> Returns only the content of the file, without the identifiers. </summary>
		static string GetContent(string path, bool relativeFromSource = true)
		{
			if (relativeFromSource)
			{
				path = Path.Combine(Index._SRC, path);
			}

			if (File.Exists(path))
			{
				List<string> lines = File.ReadAllLines(path).ToList();

				string html = "";
				bool htmlStartFound = false;

				foreach (string line in lines)
				{
					if (string.IsNullOrWhiteSpace(line))
					{
						htmlStartFound = true;
						continue; // skip once more
					}

					if (!htmlStartFound)
					{
						continue;
					}

					html += line;
				}

				return html;
			}

			return "";
		}

		/// <summary> Resolves a single parameter and returns its value. </summary>
		static string ResolveParameter(string path, string identifier)
		{
			Dictionary<string, string> identifiers = GetIdentifiers(path);

			identifiers.TryGetValue(identifier, out string value);
			if (value == null) { value = ""; }

			return value;
		}

		/// <summary> Finds the referenced Identifier at the given location and returns its value. </summary>
		static string ResolveReference(string path, string identifier)
		{
			string fullPath = Path.Combine(Index._SRC, path + ".html");

			if (File.Exists(fullPath))
			{
				Dictionary<string, string> identifiers = GetIdentifiers(fullPath, false);

				if (!string.IsNullOrWhiteSpace(identifiers[identifier]))
				{
					return identifiers[identifier];
				}
			}

			return "";
		}

		/// <summary> Finds the provided template, inserts the parameters and returns the complete HTML. </summary>
		static string ResolveTemplate(string path, List<string> parameters)
		{
			string fullPath = Path.Combine(Index._TEMPLATES, path + ".html");

			if (File.Exists(fullPath))
			{
				string html = File.ReadAllText(fullPath);

				int i = 0;
				do
				{
					// if all parameters were used, use empty strings, else use the parameters
					string insert = "";
					if (i < parameters.Count)
					{
						insert = parameters[i];
					}

					int start = html.IndexOf("@param::");
					int end = html.IndexOf(";", start);

					html = html.Remove(start, end + 1 - start);
					html = html.Insert(start, insert);

					i++;
				} while (html.IndexOf("@param::") != -1);

				return html;
			}

			return "";
		}

		/// <summary> Finds the provided template, inserts the parameters based on Key and returns the complete HTML. </summary>
		static string ResolveTemplateMapped(string path, Dictionary<string, string> parameters)
		{
			string fullPath = Path.Combine(Index._TEMPLATES, path + ".html");

			if (File.Exists(fullPath))
			{
				string html = File.ReadAllText(fullPath);

				do
				{
					int start = html.IndexOf("@param::");
					int end = html.IndexOf(";", start);
					string key = html.Substring(start + 8, end - (start + 8));

					html = html.Remove(start, end - start);

					parameters.TryGetValue(key, out string value);
					if (value == null) { value = ""; }

					html = html.Insert(start, value);
				} while (html.IndexOf("@param::") != -1);

				return html;
			}

			return "";
		}

		/// <summary> Finds the provided component and returns it as is. </summary>
		static string ResolveComponent(string path)
		{
			string fullPath = Path.Combine(Index._COMPONENTS, path + ".html");

			if (File.Exists(fullPath))
			{
				return File.ReadAllText(fullPath);
			}

			return "";
		}

		/// <summary> Finds the provided template and creates a list with the provided html files. </summary>
		static string ResolveList(string path, string template)
		{
			string fullPathToElements = Path.Combine(Index._SRC, path);

			if (Directory.Exists(fullPathToElements))
			{
				string html = "";

				List<string> files = Directory.GetFiles(fullPathToElements).ToList();

				foreach (string file in files)
				{
					string fileRel = file.Replace(Index._SRC, "");
					// only get the ending after src, so the template can resolve the file itself

					Dictionary<string, string> fileIdentifiers = GetIdentifiers(file, false);

					html += ResolveTemplateMapped(template, fileIdentifiers);
				}

				return html;
			}

			return "";
		}

		/// <summary> Returns the layout with the given name, or the default layout if none exists. </summary>
		static string ResolveLayout(string name, out string title)
		{
			Dictionary<string, string> identifiers = GetIdentifiers(Path.Combine(Index._LAYOUTS, name + ".html"));
			identifiers.TryGetValue("Title", out title);
			if (title == null) { title = ""; }

			return GetContent(Path.Combine(Index._LAYOUTS, name + ".html"));
		}

		static void Parse(string path)
		{
			Index.Build(path);

			ClearRoot();

			if (!Directory.Exists(Index._SRC))
			{
				return;
			}

			List<string> files = Directory.GetFiles(Index._SRC, "*.*", SearchOption.AllDirectories).ToList();

			// filter & process files
			foreach (string file in files)
			{
				// ignore templates, layouts and components
				if (file.StartsWith(Index._LAYOUTS) || file.StartsWith(Index._COMPONENTS) || file.StartsWith(Index._TEMPLATES))
				{
					continue;
				}

				// process actual html
				Dictionary<string, string> identifiers = GetIdentifiers(file, false);
				string content = GetContent(file, false);

				identifiers.TryGetValue("Layout", out string layout);
				layout = ResolveLayout(layout, out string title);

				if (string.IsNullOrWhiteSpace(layout))
				{
					continue;
				}

				// Add title to page
				if (!string.IsNullOrWhiteSpace(title))
				{
					identifiers.TryGetValue("Title", out string pageTitle);

					if (!string.IsNullOrWhiteSpace(pageTitle))
					{
						title = pageTitle + " | " + title;
					}
				}
				int idxHead = layout.IndexOf("<head>");
				if (idxHead != -1) // sanity
				{
					layout = layout.Insert(idxHead + 6, $"<title>{title}</title>");
				}

				// insert page content into layout
				layout = layout.Replace("@content;", content);

				// parse components
				while (layout.IndexOf("@component::") != -1)
				{
					int idxComponentStart = layout.IndexOf("@component::");
					int idxComponentEnd = layout.IndexOf(";", idxComponentStart);
					string key = layout.Substring(idxComponentStart + 12, idxComponentEnd - (idxComponentStart + 12));

					layout = layout.Remove(idxComponentStart, idxComponentEnd + 1 - idxComponentStart);

					layout = layout.Insert(idxComponentStart, ResolveComponent(key));
				}

				// parse references
				while (layout.IndexOf("@ref::") != -1)
				{
					int idxRefStart = layout.IndexOf("@ref::");
					int idxRefEnd = layout.IndexOf(";", idxRefStart);

					int idxRefIdEnd = layout.IndexOf("(", idxRefStart);

					string key = layout.Substring(idxRefStart + 6, idxRefIdEnd - (idxRefStart + 6));
					string refPath = layout.Substring(idxRefIdEnd + 1, idxRefEnd - 1 - (idxRefIdEnd + 1)); // this is absolute spaghetti, explained later for templates

					refPath = refPath.TrimStart('"');
					refPath = refPath.TrimEnd('"');

					layout = layout.Remove(idxRefStart, idxRefEnd + 1 - idxRefStart);

					layout = layout.Insert(idxRefStart, ResolveReference(refPath, key));
				}

				// parse lists
				while (layout.IndexOf("@list::") != -1)
				{
					int idxListStart = layout.IndexOf("@list::");
					int idxListEnd = layout.IndexOf(";", idxListStart);

					int idxListIdEnd = layout.IndexOf("(", idxListStart);

					string key = layout.Substring(idxListStart + 7, idxListIdEnd - (idxListStart + 7));
					string listPath = layout.Substring(idxListIdEnd + 1, idxListEnd - 1 - (idxListIdEnd + 1)); // this is absolute spaghetti, explained later for templates

					listPath = listPath.TrimStart('"');
					listPath = listPath.TrimEnd('"');

					layout = layout.Remove(idxListStart, idxListEnd + 1 - idxListStart);

					layout = layout.Insert(idxListStart, ResolveList(listPath, key));
				}

				// parse templates
				while (layout.IndexOf("@template::") != -1)
				{
					int idxTemplateStart = layout.IndexOf("@template::");
					int idxTemplateEnd = layout.IndexOf(";", idxTemplateStart);

					int idxTemplateIdEnd = layout.IndexOf("(", idxTemplateStart);

					string key = layout.Substring(idxTemplateStart + 11, idxTemplateIdEnd - (idxTemplateStart + 11));
					string templateParams = layout.Substring(idxTemplateIdEnd + 1, idxTemplateEnd - 1 - (idxTemplateIdEnd + 1)); 
					// explanation for the clownfiesta above:
					// idxTemplateIdEnd + -> the start is where the identifier ends + 1, to skip the opening paranthesis
					// the entire length of the string is the index of the template end - 1 to skip the closing paranthesis
					// and then once more subtracted from the start of the parameters, or rather the end of the identifier + 1
					List<string> parameters = templateParams.Split(", ").ToList();

					for (int i = 0; i < parameters.Count; i++)
					{
						parameters[i] = parameters[i].TrimStart('"');
						parameters[i] = parameters[i].TrimEnd('"');
					}

					layout = layout.Remove(idxTemplateStart, idxTemplateEnd + 1 - idxTemplateStart);

					layout = layout.Insert(idxTemplateStart, ResolveTemplate(key, parameters));
				}

				// copy generated output to root folder, mirroring the folder structure
				string outPath = file.Replace(Index._SRC, Index._ROOT);

				HtmlParser parser = new HtmlParser();

				var document = parser.ParseDocument(layout);

				var sw = new StringWriter();
				document.ToHtml(sw, new PrettyMarkupFormatter());

				layout = sw.ToString();

				File.WriteAllText(outPath, layout);
			}
		}
		#region Helpers
		static string[] _IGNORE = { "src", "fon", "fonts", "img", "images", "res", "resources", "vid", "videos", "css", "styles", "js", "scripts", ".git", ".vscode", "CNAME", ".gitattributes", ".gitignore" };

		/// <summary> Clears the root directory of all files and folders, except the whitelist. </summary>
		static void ClearRoot()
		{
			if (Index.IsBuilt)
			{
				List<string> dirs = Directory.GetDirectories(Index._ROOT).ToList();
				List<string> files = Directory.GetFiles(Index._ROOT).ToList();

				foreach (string dir in dirs)
				{
					bool matchesAny = false;
					foreach (string ignore in _IGNORE)
					{
						if (dir.EndsWith(ignore, StringComparison.InvariantCultureIgnoreCase))
						{
							matchesAny = true;
							break;
						}
					}

					if (matchesAny)
					{
						continue;
					}
					else
					{
						Directory.Delete(dir, true);
					}
				}

				foreach (string file in files)
				{
					bool matchesAny = false;
					foreach (string ignore in _IGNORE)
					{
						if (file.EndsWith(ignore, StringComparison.InvariantCultureIgnoreCase) || file.Contains("favicon"))
						{
							matchesAny = true;
							break;
						}
					}

					if (matchesAny)
					{
						continue;
					}
					else
					{
						File.Delete(file);
					}
				}
			}
			else
			{
				throw new Exception("Index not built.");
			}
		}
		#endregion
	}
}
