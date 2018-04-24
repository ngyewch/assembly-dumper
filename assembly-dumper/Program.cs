using System.IO;
using System.Reflection;

namespace assemblydumper
{
	class MainClass
	{
		public static void Main(string[] args)
		{
			string assemblyPath = args[0];
			string outputDirectory = args[1];

			if (Directory.Exists(outputDirectory)) {
				Directory.Delete(outputDirectory, true);
			}

			Dumper dumper = new Dumper();
			dumper.Introspect(assemblyPath, outputDirectory);

			{
				string projectName = Path.GetFileNameWithoutExtension(assemblyPath);
				string input = Path.Combine(outputDirectory, "src");
				string outputDir = Path.Combine(outputDirectory, "doxygen");

				string doxyfilePath = System.IO.Path.GetTempFileName();
				using (Stream inputStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("assemblydumper.res.Doxyfile"))
				{
					using (StreamReader streamReader = new StreamReader(inputStream, new System.Text.UTF8Encoding()))
					{
						using (Stream outputStream = new FileStream(doxyfilePath, FileMode.Create))
						{
							using (StreamWriter streamWriter = new StreamWriter(outputStream, new System.Text.UTF8Encoding()))
							{
								while (!streamReader.EndOfStream)
								{
									string line = streamReader.ReadLine();
									if (line == null)
									{
										break;
									}
									line = line.Replace("${PROJECT_NAME}", projectName);
									line = line.Replace("${INPUT}", input);
									line = line.Replace("${OUTPUT_DIRECTORY}", outputDir);
									streamWriter.WriteLine(line);
								}
							}
						}
					}
				}

				System.Diagnostics.Process process = new System.Diagnostics.Process();
                System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
                startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
                startInfo.FileName = "doxygen";
                startInfo.Arguments = doxyfilePath;
                process.StartInfo = startInfo;
                process.Start();
			}
		}
	}
}
