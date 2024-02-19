using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;


public class ClassLineTextToCsv
{
    // Regex.Matchesを利用したバージョン
    public void ExecuteRegexVer()
    {
        // フォルダパスを入力して貰う
        Console.Write(@"検索するフォルダのパスを入力してください（例：C:\MyProject）: ");
        string folderPathRead = Console.ReadLine();
        string folderPath = @$"{folderPathRead}";

        Console.Write(@"出力するフォルダのパスをを入力してください（例：C:\MyProject）: ");
        string outputFolderPathRead = Console.ReadLine();
        string outputFolderPath = $@"{outputFolderPathRead}";

        // 出力ファイル名の設定
        Console.Write(@"出力するファイル名を入力してください（例：ClassList.csv）: ");
        string fileName = Console.ReadLine();
        string outputPath = Path.Combine(outputFolderPath, fileName);

        // フォルダ内の全ての.csファイルを取得
        string[] csFiles = Directory.GetFiles(folderPath, "*.cs", SearchOption.AllDirectories);

        // 出力ファイル作成のためStreamWriterを生成
        using (StreamWriter writer = new StreamWriter(outputPath))
        {
            writer.WriteLine("File,Access Modifier,Type,Name,Inherited Class"); // CSV ヘッダを作成

            // 各 .cs ファイルのクラス情報を取得
            foreach (string file in csFiles)
            {
                string content = File.ReadAllText(file);
                MatchCollection matches;

                // クラスを検索
                matches = Regex.Matches(content, @"((public|private|internal|protected|partial|abstract|sealed|static)?\s+(class|struct|enum|interface))\s+(\w+)(\s*:\s*(\w+))?", RegexOptions.Singleline);
                WriteMatchesToCsv(matches, file, writer);
            }
        }
    }
    // Regex.Matchesを利用したバージョンのCSVフォイル化
    void WriteMatchesToCsv(MatchCollection matches, string file, StreamWriter writer)
    {
        foreach (Match match in matches)
        {
            writer.WriteLine($"\"{file}\",\"{match.Groups[1].Value.Replace(match.Groups[3].Value, "").Trim()}\",\"{match.Groups[3].Value}\",\"{match.Groups[4].Value}\",\"{match.Groups[6].Value}\""); // CSV 형식으로 파일명, 접근 한정자, 타입, 이름, 상속받은 클래스명을 작성합니다.
        }
    }


    // SyntaxNodeを利用したバージョン
    // NugetパッケージマネージャーからMicrosoft.CodeAnalysis.CSharpのインストールが必要
    // 他のプリプロセッサディレクティブを使用している場合は、それらもpreprocessorSymbolsに追加する必要があります
    public void ExcuteSyntaxNodeVer()
    {
        Console.Write(@"検索するフォルダのパスを入力してください（例：C:\MyProject）: ");
        string directoryPathRead = Console.ReadLine();
        string directoryPath = $@"{directoryPathRead}";

        Console.Write(@"出力するCSVファイルのパスを入力してください（例：C:\MyProject\output.csv）: ");
        string csvFilePathRead = Console.ReadLine();
        string csvFilePath = $@"{csvFilePathRead}";

        var csvContent = new StringBuilder();
        csvContent.AppendLine("File Path,Namespace,CodeLine(Length),CodeCharacter(Length),Modifiers,Type,Name,Base Types");

        var files = Directory.EnumerateFiles(directoryPath, "*.cs", SearchOption.AllDirectories).ToList();
        int totalFiles = files.Count;
        int processedFiles = 0;

        foreach (var file in files)
        {
            var code = File.ReadAllText(file);
            var tree = CSharpSyntaxTree.ParseText(code, new CSharpParseOptions(preprocessorSymbols: new[] { "DEBUG", "UNITY_EDITOR" }));
            var root = tree.GetRoot();

            ProcessNode(root, "", "");

            processedFiles++;
            Console.WriteLine($"총 {totalFiles}개 중 {processedFiles}개 파일 처리 중...");

            void ProcessNode(SyntaxNode node, string parentName, string namespaceName)
            {
                if (node is NamespaceDeclarationSyntax namespaceDeclaration)
                {
                    namespaceName = namespaceDeclaration.Name.ToString();
                }

                if (node is BaseTypeDeclarationSyntax type)
                {
                    var fullName = string.IsNullOrEmpty(parentName) ? type.Identifier.ValueText : $"{parentName}.{type.Identifier.ValueText}";
                    var modifiers = string.Join(" ", type.Modifiers.Select(m => m.ValueText));
                    var baseTypes = type.BaseList != null ? string.Join(", ", type.BaseList.Types.Select(t => t.ToString())) : "";
                    var typeKeyword = type is EnumDeclarationSyntax ? "enum" : ((TypeDeclarationSyntax)type).Keyword.ValueText;

                    var codeLineLength = node.SyntaxTree.GetLineSpan(node.Span).EndLinePosition.Line - node.SyntaxTree.GetLineSpan(node.Span).StartLinePosition.Line + 1;
                    var codeCharacterLength = node.Span.Length;

                    csvContent.AppendLine($"{file},{namespaceName},{codeLineLength},{codeCharacterLength},{modifiers},{typeKeyword},{fullName},{baseTypes}");
                }

                foreach (var child in node.ChildNodes())
                {
                    ProcessNode(child, node is BaseTypeDeclarationSyntax ? ((BaseTypeDeclarationSyntax)node).Identifier.ValueText : parentName, namespaceName);
                }
            }
        }

        File.WriteAllText(csvFilePath, csvContent.ToString());
        Console.WriteLine("Processing completed. The CSV file has been written.");
    }
}

