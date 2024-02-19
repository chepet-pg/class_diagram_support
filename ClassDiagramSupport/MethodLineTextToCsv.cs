using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

public class MethodLineTextToCsv
{
    public void ExecuteSummaryToCsv()
    {
        Console.Write(@"検索するフォルダのパスを入力してください（例：C:\MyProject）: ");
        string folderPathRead = Console.ReadLine();
        string folderPath = $@"{folderPathRead}";

        Console.Write(@"出力するCSVファイルのパスを入力してください（例：C:\MyProject\output.csv）: ");
        string csvFilePathRead = Console.ReadLine();
        string csvFilePath = $@"{csvFilePathRead}";


        var csvData = new List<string>();

        foreach (var file in Directory.EnumerateFiles(folderPath, "*.cs", SearchOption.AllDirectories))
        {
            var tree = CSharpSyntaxTree.ParseText(File.ReadAllText(file));
            var root = tree.GetRoot();
            var methods = root.DescendantNodes().OfType<MethodDeclarationSyntax>();

            foreach (var method in methods)
            {
                var summary = method.GetLeadingTrivia()
                    .Select(i => i.GetStructure())
                    .OfType<DocumentationCommentTriviaSyntax>()
                    .FirstOrDefault()?
                    .DescendantNodes().OfType<XmlElementSyntax>()
                    .FirstOrDefault(i => i.StartTag.Name.ToString() == "summary")?
                    .Content.ToString();

                if (summary != null)
                {
                    summary = Regex.Replace(summary, @"\s+", " ");
                }

                var className = method.Parent is ClassDeclarationSyntax classDeclaration
                    ? classDeclaration.Identifier.ToString()
                    : "Unknown";

                var parameters = method.ParameterList.Parameters.Select(p =>
                {
                    var paramName = p.Identifier.ToString();
                    var paramComment = method.GetLeadingTrivia()
                        .Select(i => i.GetStructure())
                        .OfType<DocumentationCommentTriviaSyntax>()
                        .FirstOrDefault()?
                        .DescendantNodes().OfType<XmlElementSyntax>()
                        .FirstOrDefault(i => i.StartTag.Name.ToString() == "param" && i.StartTag.ToString().Contains($"name=\"{paramName}\""))?
                        .Content.ToString();

                    if (paramComment != null)
                    {
                        paramComment = Regex.Replace(paramComment, @"\s+", " ");
                    }

                    return $"{paramName}:{paramComment}";
                }).ToList();

                var csvRow = new List<string> { file, className, method.Identifier.ToString(), summary };
                csvRow.AddRange(parameters);

                csvData.Add(string.Join(",", csvRow));
            }
        }

        File.WriteAllLines(csvFilePath, csvData);
    }
}
