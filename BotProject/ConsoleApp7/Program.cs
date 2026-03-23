using System.Diagnostics;
using System.IO.Packaging;
using System.Text;
using System.Text.Json;
using OfficeOpenXml;
using static FParsec.ErrorMessage;

public class LatexFormulaRenderer
{
    public static void GenerateFormulaImage(string latexFormula, string outputPath, string nomer)
    {
        string tempLatinDir = Path.Combine(Path.GetTempPath(), "LatexTemp");
        Directory.CreateDirectory(tempLatinDir);

        string texFilePath = Path.Combine(tempLatinDir, $"predel{nomer}.tex");
        string dviFilePath = Path.Combine(tempLatinDir, $"predel{nomer}.dvi");
        string tempPngPath = Path.Combine(tempLatinDir, $"temp{nomer}.png");

        // Упрощенный шаблон без shell-escape
        string latexContent = $@"
\documentclass[12pt, border=1mm]{{standalone}}
\usepackage[utf8]{{inputenc}}
\usepackage[T2A]{{fontenc}} % Добавьте эту строку
\usepackage{{amsmath}}
\usepackage{{array}}
\usepackage{{xcolor}}
\usepackage[english,russian]{{babel}} % Добавьте эту строку
\begin{{document}}
\thispagestyle{{empty}}
${latexFormula}$
\end{{document}}";

        File.WriteAllText(texFilePath, latexContent, Encoding.UTF8);

        string latexPath = @"C:\Program Files\MiKTeX\miktex\bin\x64\latex.exe";
        string dvipngPath = @"C:\Program Files\MiKTeX\miktex\bin\x64\dvipng.exe";

        // 5. Компиляция LaTeX -> DVI
        // Стало (правильно):
        RunCommand(latexPath, $"--interaction=nonstopmode --output-directory=\"{tempLatinDir}\" \"{texFilePath}\"", tempLatinDir);

        // 6. Конвертация DVI -> PNG (во временный файл)
        RunCommand(dvipngPath, $"-D 300 -T tight -o \"{tempPngPath}\" \"{dviFilePath}\"", tempLatinDir);

        File.Copy(tempPngPath, outputPath, true);
        Console.WriteLine($"Формула сохранена в {outputPath}");
    }

    private static void RunCommand(string command, string arguments, string workingDir)
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = command,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = workingDir,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8,
                // Добавляем эти строки для снижения привилегий
                LoadUserProfile = false,
                ErrorDialog = false,
                WindowStyle = ProcessWindowStyle.Hidden
            }
        };

        // Запускаем и логируем
        Console.WriteLine($"Running: {command} {arguments}");
        process.Start();

        // Читаем вывод в фоне, чтобы избежать дедлоков
        var output = new StringBuilder();
        var error = new StringBuilder();

        process.OutputDataReceived += (_, e) => { if (e.Data != null) output.AppendLine(e.Data); };
        process.ErrorDataReceived += (_, e) => { if (e.Data != null) error.AppendLine(e.Data); };

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        // Таймаут 15 секунд
        if (!process.WaitForExit(15000))
        {
            process.Kill();
            throw new TimeoutException($"Process hanged. Output: {output}, Error: {error}");
        }

        if (process.ExitCode != 0)
            throw new Exception($"LaTeX failed (code {process.ExitCode}): {error}");
    }

    private static string AddNumbers(string blank, string A, string B, string C, string D, string F, string S, string G, string H, string K, string M)
    {
        if (string.IsNullOrEmpty(blank)) return blank;

        // Проверяем все параметры на null и заменяем на пустую строку
        A = A ?? "";
        B = B ?? "";
        C = C ?? "";
        D = D ?? "";
        F = F ?? "";
        S = S ?? "";
        G = G ?? "";
        H = H ?? "";
        K = K ?? "";
        M = M ?? "";

        blank = blank.Replace("operatorname", "ОПЕРАТОРНАМЕ");
        blank = blank.Replace("lim", "ЛИМ");
        blank = blank.Replace("dfrac", "ДФРАК");
        blank = blank.Replace("frac", "ФРАК");
        blank = blank.Replace("sqrt", "СКВРТ");
        blank = blank.Replace("infty", "ИНФТ");
        blank = blank.Replace("mathrm", "МАФРМ");
        blank = blank.Replace("cdot", "СДОТ");
        blank = blank.Replace("arc", "АРК");
        blank = blank.Replace("tan", "ТАН");
        blank = blank.Replace("ctg", "КТГ");
        blank = blank.Replace("tg", "ТГ");
        blank = blank.Replace("sin", "СИН");
        blank = blank.Replace("cos", "КОС");
        blank = blank.Replace("prime", "ПРАЙМ");
        blank = blank.Replace("pm", "ПМ");
        blank = blank.Replace("right", "РАЙТ");
        blank = blank.Replace("left", "ЛЕФТ");

        blank = blank.Replace("a", $"{A}");
        blank = blank.Replace("b", $"{B}");
        blank = blank.Replace("c", $"{C}");
        blank = blank.Replace("d", $"{D}");
        blank = blank.Replace("f", $"{F}");
        blank = blank.Replace("s", $"{S}");
        blank = blank.Replace("g", $"{G}");
        blank = blank.Replace("h", $"{H}");
        blank = blank.Replace("k", $"{K}");
        blank = blank.Replace("m", $"{M}");

        blank = blank.Replace("ОПЕРАТОРНАМЕ", "operatorname");
        blank = blank.Replace("ЛИМ", "lim");
        blank = blank.Replace("ДФРАК", "dfrac");
        blank = blank.Replace("ФРАК", "frac");
        blank = blank.Replace("СКВРТ", "sqrt");
        blank = blank.Replace("ИНФТ", "infty");
        blank = blank.Replace("МАФРМ", "mathrm");
        blank = blank.Replace("СДОТ", "cdot");
        blank = blank.Replace("АРК", "arc");
        blank = blank.Replace("ТАН", "tan");
        blank = blank.Replace("КТГ", "ctg");
        blank = blank.Replace("ТГ", "tg");
        blank = blank.Replace("СИН", "sin");
        blank = blank.Replace("КОС", "cos");
        blank = blank.Replace("ПРАЙМ", "prime");
        blank = blank.Replace("ПМ", "pm");
        blank = blank.Replace("РАЙТ", "right");
        blank = blank.Replace("ЛЕФТ", "left");

        blank = blank.Replace("00x", "НОЛЬx");
        blank = blank.Replace("10x", "ОДИНx");
        blank = blank.Replace("20x", "ДВАx");
        blank = blank.Replace("30x", "ТРИx");
        blank = blank.Replace("40x", "ЧЕТЫРЕx");
        blank = blank.Replace("50x", "ПЯТЬx");
        blank = blank.Replace("60x", "ШЕСТЬx");
        blank = blank.Replace("70x", "СЕМЬx");
        blank = blank.Replace("80x", "ВОСЕМЬx");
        blank = blank.Replace("90x", "ДЕВЯТЬx");

        blank = blank.Replace("0x^2", "Ж");
        blank = blank.Replace("0x^3", "Ж");
        blank = blank.Replace("0x", "Ж");
        blank = blank.Replace("0 x", "Ж");


        blank = blank.Replace("+ 0", "");
        blank = blank.Replace("- 0", "");

        blank = blank.Replace("+ Ж", "");
        blank = blank.Replace("- Ж", "");
        blank = blank.Replace("Ж", "");

        blank = blank.Replace("НОЛЬx", "00x");
        blank = blank.Replace("ОДИНx", "10x");
        blank = blank.Replace("ДВАx", "20x");
        blank = blank.Replace("ТРИx", "30x");
        blank = blank.Replace("ЧЕТЫРЕx", "40x");
        blank = blank.Replace("ПЯТЬx", "50x");
        blank = blank.Replace("ШЕСТЬx", "60x");
        blank = blank.Replace("СЕМЬx", "70x");
        blank = blank.Replace("ВОСЕМЬx", "80x");
        blank = blank.Replace("ДЕВЯТЬx", "90x");

        blank = blank.Replace("1x", "x");
        blank = blank.Replace("1 x", "x");

        blank = blank.Replace("--", "+");
        blank = blank.Replace("- -", "+");
        blank = blank.Replace("-+", "-");
        blank = blank.Replace("- +", "-");
        blank = blank.Replace("+-", "-");
        blank = blank.Replace("+ -", "-");

        blank = blank.Replace("{+", "{");
        blank = blank.Replace("{ +", "{");

        return blank;
    }

    static void Main()
    {
        ExcelPackage.License.SetNonCommercialOrganization("<My Noncommercial organization>");
        try
        {
            var file = new FileInfo(@"C:\Users\isera\OneDrive\Desktop\Другое\Кострова2\ConsoleApp7\base.xlsx");

            // Проверяем существование файла
            if (!file.Exists)
            {
                Console.WriteLine($"Файл не найден: {file.FullName}");
                Console.ReadLine();
                return;
            }

            using (var package = new ExcelPackage(file))
            {
                // Проверяем, что книга создана
                if (package.Workbook == null)
                {
                    Console.WriteLine("Не удалось создать рабочую книгу Excel");
                    Console.ReadLine();
                    return;
                }

                // Получаем лист с проверкой
                var worksheet = package.Workbook.Worksheets["Лист1"];
                if (worksheet == null)
                {
                    // Попробуем получить первый лист
                    worksheet = package.Workbook.Worksheets.FirstOrDefault();
                    if (worksheet == null)
                    {
                        Console.WriteLine("В файле нет рабочих листов");
                        Console.ReadLine();
                        return;
                    }
                    Console.WriteLine($"Используется лист: {worksheet.Name}");
                }

                // Определяем количество строк
                int totalRows = 189;
                Console.WriteLine($"Обработка строк с 2 по {totalRows}");

                for (int i = 2; i <= totalRows; i=i+1)
                {
                    

                    string otvetEDIT = worksheet.Cells[$"M{i}"].Value?.ToString()?.Trim() ?? "";
                    string nomer = worksheet.Cells[$"A{i}"].Value?.ToString()?.Trim() ?? "";
                    string formulaEDIT = worksheet.Cells[$"B{i}"].Value?.ToString()?.Trim() ?? "";


                    string A = worksheet.Cells[$"C{i}"].Value?.ToString()?.Trim() ?? "";
                    string B = worksheet.Cells[$"D{i}"].Value?.ToString()?.Trim() ?? "";
                    string C = worksheet.Cells[$"E{i}"].Value?.ToString()?.Trim() ?? "";
                    string D = worksheet.Cells[$"F{i}"].Value?.ToString()?.Trim() ?? "";
                    string S = worksheet.Cells[$"G{i}"].Value?.ToString()?.Trim() ?? "";
                    string F = worksheet.Cells[$"H{i}"].Value?.ToString()?.Trim() ?? "";
                    string G = worksheet.Cells[$"I{i}"].Value?.ToString()?.Trim() ?? "";
                    string H = worksheet.Cells[$"J{i}"].Value?.ToString()?.Trim() ?? "";
                    string K = worksheet.Cells[$"K{i}"].Value?.ToString()?.Trim() ?? "";
                    string M = worksheet.Cells[$"L{i}"].Value?.ToString()?.Trim() ?? "";

                    var ChooseVariant = new Random();
                    formulaEDIT = AddNumbers(formulaEDIT, A, B, C, D, F, S, G, H, K, M);
                    int Number_neprav1 = ChooseVariant.Next(2, totalRows);
                    int Number_neprav2 = ChooseVariant.Next(2, totalRows);
                    int Number_neprav3 = ChooseVariant.Next(2, totalRows);

                    string otvetNEPRAV1 = worksheet.Cells[$"M{Number_neprav1}"].Value?.ToString()?.Trim() ?? "";
                    string otvetNEPRAV2 = worksheet.Cells[$"M{Number_neprav2}"].Value?.ToString()?.Trim() ?? "";
                    string otvetNEPRAV3 = worksheet.Cells[$"M{Number_neprav3}"].Value?.ToString()?.Trim() ?? "";

                    while (otvetNEPRAV1 == otvetNEPRAV2 || otvetNEPRAV1 == otvetNEPRAV3 || otvetNEPRAV2 == otvetNEPRAV3 || otvetEDIT == otvetNEPRAV1 || otvetEDIT == otvetNEPRAV2 || otvetEDIT == otvetNEPRAV3 ||
                        otvetNEPRAV1 == "'0" || otvetNEPRAV2 == "'0" || otvetNEPRAV3 == "'0")
                    {
                        Number_neprav1 = ChooseVariant.Next(2, totalRows);
                        Number_neprav2 = ChooseVariant.Next(2, totalRows);
                        Number_neprav3 = ChooseVariant.Next(2, totalRows);

                        otvetNEPRAV1 = worksheet.Cells[$"M{Number_neprav1}"].Value?.ToString()?.Trim() ?? "";
                        otvetNEPRAV2 = worksheet.Cells[$"M{Number_neprav2}"].Value?.ToString()?.Trim() ?? "";
                        otvetNEPRAV3 = worksheet.Cells[$"M{Number_neprav3}"].Value?.ToString()?.Trim() ?? "";
                    }

                    string[] final_massive = [otvetEDIT, otvetNEPRAV1, otvetNEPRAV2, otvetNEPRAV3];
                    Random.Shared.Shuffle(final_massive);
                    int Number_prav = Array.IndexOf(final_massive, otvetEDIT) + 1;

                    string stringe = $@"
\begin{{array}}{{l}}
\\
\textcolor{{red}}{{\textbf{{Формула:}}}} \, ${formulaEDIT}$ \\
\\
\text{{1 вариант: }} ${final_massive[0]}$ \\
\\
\text{{2 вариант: }} ${final_massive[1]}$ \\
\\
\text{{3 вариант: }} ${final_massive[2]}$ \\
\\
\text{{4 вариант: }} ${final_massive[3]}$ \\
\end{{array}}
";

                    try
                    {
                        string outputPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), $"predel{nomer}_{Number_prav}.png");
                        GenerateFormulaImage(stringe, outputPath, nomer);
                        Console.WriteLine($"Обработана строка {i}: номер {nomer}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("~~~~~~~~~~~~~~~~~~~~~~~~~~~~");
                        Console.WriteLine();
                        Console.WriteLine("-------------------------------------------------------------------------------------");
                        Console.WriteLine();
                        Console.WriteLine(ex);
                        Console.WriteLine();
                        Console.WriteLine("-------------------------------------------------------------------------------------");
                        Console.WriteLine();
                        Console.WriteLine("~~~~~~~~~~~~~~~~~~~~~~~~~~~~");

                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка: {ex.Message}");
            Console.WriteLine($"StackTrace: {ex.StackTrace}");
            Console.ReadLine();
        }
    }
}