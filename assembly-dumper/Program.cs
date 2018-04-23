namespace assemblydumper
{
	class MainClass
	{
		public static void Main(string[] args)
		{
			Dumper dumper = new Dumper();
			dumper.Introspect(args[0], args[1]);
		}
	}
}
