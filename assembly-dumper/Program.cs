namespace assemblydumper
{
	class MainClass
	{
		public static void Main(string[] args)
		{
			Dumper.Introspect(args[0], args[1]);
		}
	}
}
