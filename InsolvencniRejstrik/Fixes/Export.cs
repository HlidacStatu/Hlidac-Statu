using System;
using System.IO;
using System.Linq;

namespace InsolvencniRejstrik.Fixes
{
	class Export
	{
		private const int Threshold = 10000;
		private const string ExportDir = "export";

		public void Execute(string proceedings, string cacheFile)
		{
			var keys = proceedings.Split(',').Select(a => a.Trim()).ToHashSet();
			if (keys.Count == 0)
			{
				return;
			}

			var start = DateTime.Now;

			if (!Directory.Exists(ExportDir))
			{
				Directory.CreateDirectory(ExportDir);
			}

			if (File.Exists(cacheFile))
			{
				Console.WriteLine("Cteni dat z cache ...");
				var lines = 0;
				var exportedLines = 0;

				foreach (var line in File.ReadLines(cacheFile))
				{
					lines++;
					var parts = line.Split('#');
					if (parts.Length > 2)
					{
						var proc = parts[1];
						if (keys.Contains(proc))
						{
							File.AppendAllLines($"{ExportDir}\\{proc.Replace(" ", "_").Replace("/", "-")}.txt", new[] { line });
							exportedLines++;
						}
					}
					else
					{
						Console.WriteLine($"Invalid line: {line}");
					}


					if (lines % Threshold == 0)
					{
						var ts = DateTime.Now - start;
						Console.WriteLine($"Prectenych radku: {lines}, exportovanych: {exportedLines}, zpracovani {Threshold} radku: {TimeSpan.FromMilliseconds(ts.TotalMilliseconds * Threshold / lines)}");
					}
				}
			}
		}
	}
}
