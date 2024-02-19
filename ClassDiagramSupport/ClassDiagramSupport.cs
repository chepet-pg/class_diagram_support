using System;

public class ClassDiagramSupport
{
	public static void Main(string[] args)
	{
		//ClassLineTextToCsv classLineTextToCsv = new ClassLineTextToCsv();
		//classLineTextToCsv.ExcuteSyntaxNodeVer();

		MethodLineTextToCsv methodLineTextToCsv = new MethodLineTextToCsv();
		methodLineTextToCsv.ExecuteSummaryToCsv();

	}
}
