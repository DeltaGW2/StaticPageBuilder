using System;
using System.IO;
using System.Collections.Generic;

namespace Raidcore_StaticPageBuilder
{
	class Program
	{
		static void Main(string[] args)
		{
			string source, target;

			do
			{
				Console.Clear();
				Console.WriteLine("Enter source folder:");
				source = Console.ReadLine();
			} while (!Directory.Exists(source));

			do
			{
				Console.Clear();
				Console.WriteLine($"Input: {source}");
				Console.WriteLine("Enter target folder:");
				target = Console.ReadLine();
			} while (source == target);

			Console.Clear();

			Console.WriteLine($"Input: {source}");
			Console.WriteLine($"Output: {target}");
			Console.WriteLine("The target folder will be wiped completely.");
			Console.WriteLine("Confirm? Y/N");

			if (Console.ReadKey().Key == ConsoleKey.Y)
			{
				if(Directory.Exists(target))
				{
					string[] subDirs = Directory.GetDirectories(target);
					foreach (string dir in subDirs)
					{
						if (!dir.Contains(".git")) { Directory.Delete(dir, true); }
					}
					string[] files = Directory.GetFiles(target);
					foreach (string file in files)
					{
						File.Delete(file);
					}
				}

				Directory.CreateDirectory(target);

				File.Copy(Path.Combine(source, "discord.html"), Path.Combine(target, "discord.html"));

				BuildPages(File.ReadAllText(Path.Combine(source, "index.html")), new List<string>(Directory.GetFiles(Path.Combine(source, "Pages"))), target);
				ReplaceGetPageFunc(new List<string>(Directory.GetFiles(target)));
				CopyStaticFiles(source, target);
			}
			else
			{
				Console.WriteLine("Exiting.");
				Console.ReadKey();
			}
		}

		static void BuildPages(string skeleton, List<string> pages, string targetFolder)
		{
			string mainStart = "<main id=\"content\">";

			skeleton = skeleton.Replace("href=\"./", "href=\"https://raidcore.gg");
			skeleton = skeleton.Replace("href=\"favicon", "href=\"https://raidcore.gg/favicon");
			skeleton = skeleton.Replace("href=\"Styles/", "href=\"https://raidcore.gg/Styles/");
			skeleton = skeleton.Replace("src=\"libs/", "src=\"https://raidcore.gg/libs/");

			foreach (string page in pages)
			{
				string pageContent = File.ReadAllText(page);
				string newPage = skeleton.Insert(skeleton.IndexOf(mainStart) + mainStart.Length, pageContent);

				if(page.Contains("main.html"))
				{
					File.WriteAllText(Path.Combine(targetFolder, "index.html"), newPage);
				}
				else
				{
					FileInfo fi = new FileInfo(page);
					File.WriteAllText(Path.Combine(targetFolder, fi.Name), newPage);
				}
			}
		}

		static void ReplaceGetPageFunc(List<string> files)
		{
			foreach (string html in files)
			{
				// onclick="getPage('meme')"
				// href="meme"

				string data = File.ReadAllText(html);
				int index = 0;
				do
				{
					const string pageStart = "onclick=\"getPage('";
					const string linkStart = "href=\"";
					index = data.IndexOf(pageStart);
					if (index + pageStart.Length < data.Length && index > -1)
					{
						int endIndex = data.IndexOf("\"", index + pageStart.Length) - 2;

						Console.WriteLine(data.Substring(index, endIndex - index));

						data = data.Remove(endIndex, 3);
						data = data.Insert(endIndex, "\"");

						data = data.Remove(index, pageStart.Length);
						data = data.Insert(index, linkStart);
					}
				} while (index > -1);

				File.WriteAllText(html, data);
				Console.WriteLine(html);
			}
		}

		static void CopyStaticFiles(string source, string target)
		{
			List<string> allSource = new List<string>(Directory.GetFiles(source, "*", SearchOption.AllDirectories));

			foreach (string file in allSource)
			{
				if (file.Contains("index.html") || file.Contains("\\Pages\\") || file.Contains(".git")) { continue; }

				FileInfo fi = new FileInfo(file);
				string tFile = file.Replace(source, target);
				if(File.Exists(tFile))
				{
					File.Delete(tFile);
				}
				if(!Directory.Exists(tFile)) { Directory.CreateDirectory(file.Replace(source, target).Replace(fi.Name, "")); }
				File.Copy(file, tFile);
			}
		}
	}
}
