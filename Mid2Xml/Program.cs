using System;

namespace Mid2Xml
{
	static class Program
	{
		static string Input;
		static string Output;

		static void Main(string[] args)
		{
			Arguments(args);

			if (string.IsNullOrWhiteSpace(Input))
				return;

			if (string.Equals(System.IO.Path.GetExtension(Input), ".xml"))
			{
				XmlFile.Load(Input);
				MidFile.Save(Output);
			}
			else
			{
				MidFile.Load(Input);
				XmlFile.Save(Output);
			}
		}

		private static void Arguments(string[] args)
		{
			if (args.Length == 0)
				Console.WriteLine("usage: Mid2Xml input [output]");
			else
			{
				if (!System.IO.File.Exists(args[0]))
				{
					Console.WriteLine("File not found.");
					return;
				}

				Input = args[0];

				if (args.Length > 1)
				{
					Output = args[1];
				}
				else if (System.IO.Path.GetExtension(Input) == ".xml")
				{
					Output = System.IO.Path.GetFileNameWithoutExtension(Input) + ".mid";
				}
				else
				{
					Output = System.IO.Path.GetFileNameWithoutExtension(Input) + ".xml";
				}
			}
		}
	}
}
