using System.Collections.Generic;
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

            if (Directory.Exists(outputDirectory))
            {
                Directory.Delete(outputDirectory, true);
            }

            Dumper dumper = new Dumper();
            dumper.Introspect(assemblyPath, outputDirectory);

            {
                string projectName = Path.GetFileNameWithoutExtension(assemblyPath);
                string input = Path.Combine(outputDirectory, "src");
                string outputDir = Path.Combine(outputDirectory, "doxygen");

                Dictionary<string, string> vars = new Dictionary<string, string>();
                vars.Add("PROJECT_NAME", projectName);
                vars.Add("INPUT", input);
                vars.Add("OUTPUT_DIRECTORY", outputDir);

                string doxyfilePath = generateDoxyfile(vars);
                exec("doxygen", doxyfilePath);
            }
        }

        private static string generateDoxyfile(Dictionary<string, string> vars)
        {
            return substitute("assemblydumper.res.Doxyfile", vars);
        }

        private static string substitute(string resourceName, Dictionary<string, string> vars)
        {
            string outputPath = System.IO.Path.GetTempFileName();
            using (Stream inputStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName))
            {
                using (StreamReader streamReader = new StreamReader(inputStream, new System.Text.UTF8Encoding()))
                {
                    using (Stream outputStream = new FileStream(outputPath, FileMode.Create))
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
                                foreach (KeyValuePair<string, string> entry in vars)
                                {
                                    line = line.Replace("${" + entry.Key + "}", entry.Value);
                                }
                                streamWriter.WriteLine(line);
                            }
                        }
                    }
                }
            }
            return outputPath;
        }

        private static void exec(string cmd, string args)
        {
            System.Diagnostics.Process process = new System.Diagnostics.Process();
            System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
            startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
            startInfo.FileName = cmd;
            startInfo.Arguments = args;
            process.StartInfo = startInfo;
            process.Start();
        }
    }
}
