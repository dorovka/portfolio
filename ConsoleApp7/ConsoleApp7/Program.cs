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

        string texFilePath = Path.Combine(tempLatinDir, $"zov{nomer}.tex");
        string dviFilePath = Path.Combine(tempLatinDir, $"zov{nomer}.dvi");
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
                StandardErrorEncoding = Encoding.UTF8
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

    private static string AddNumbers(string blank, int A, int B, int C, int D, int M, int N)
    {
        blank = blank.Replace("dx", "U");
        blank = blank.Replace("ra", "!");
        blank = blank.Replace("ar", "ЛЛ");
        blank = blank.Replace("na", "бб");
        blank = blank.Replace("tan", "ТАН");
        blank = blank.Replace("a", $"{A}");
        blank = blank.Replace("ТАН", "tan");
        blank = blank.Replace("бб", "na");
        blank = blank.Replace("!", "ra");
        blank = blank.Replace("ЛЛ", "ar");
        blank = blank.Replace("U", "dx");

        blank = blank.Replace("b", $"{B}");

        blank = blank.Replace("rac", "U");
        blank = blank.Replace("arc", "@");
        blank = blank.Replace("cos", "!");
        blank = blank.Replace("cdot", "ЖЖ");
        blank = blank.Replace("c", $"{C}");
        blank = blank.Replace("ЖЖ", "cdot");
        blank = blank.Replace("!", "cos");
        blank = blank.Replace("@", "arc");
        blank = blank.Replace("U", "rac");

        blank = blank.Replace("dfrac", "РР");
        blank = blank.Replace("cdot", "ЖЖ");
        blank = blank.Replace("d", $"{D}");
        blank = blank.Replace("ЖЖ", "cdot");
        blank = blank.Replace("РР", "dfrac");

        blank = blank.Replace("lim", "U");
        blank = blank.Replace("prime", "ДД");
        blank = blank.Replace("m", $"{M}");
        blank = blank.Replace("ДД", "prime");
        blank = blank.Replace("U", "lim");

        blank = blank.Replace("ln", "U");
        blank = blank.Replace("inf", "@");
        blank = blank.Replace("neq", "!");
        blank = blank.Replace("sin", "СИН");
        blank = blank.Replace("tan", "ТАН");
        blank = blank.Replace("n", $"{N}");
        blank = blank.Replace("ТАН", "tan");
        blank = blank.Replace("СИН", "sin");
        blank = blank.Replace("!", "neq");
        blank = blank.Replace("@", "inf");
        blank = blank.Replace("U", "ln");

        blank = blank.Replace("--", "+");
        blank = blank.Replace("- -", "+");
        blank = blank.Replace("-+", "-");
        blank = blank.Replace("- +", "-");
        blank = blank.Replace("+-", "-");
        blank = blank.Replace("+ -", "-");



        return blank;
    }

    public static string AddFirstNumbers(string number, int A, int B, int C, int D, int M, int N)
    {


        switch (number)
        {
            case "1":
                return $"{A*A*A+B*A+C}";
            case "2":
                return @"$\dfrac{" + $"{A * A * A + B * A + C}" + "}{" + $"{D * A * A + M * A + N}" + "}$";
            case "4":
                return @"$\lim_{x \to a} \dfrac{" + $"{A - D}" + "x^2 + " + $"{B - M}" + "x + " + $"{C - N}" + @"}{(bx^2 + mx + c)(\sqrt{ax^2 + bx + c} + \sqrt{dx^2 + mx + n})}$";
            case "12":
                return @"$e^{" + $"{A*M}" + "}$";
            case "13":
                return @"$e^{" + $"{A * M}" + "}$";
            case "21":
                return @"$\lim \dfrac{" +$"{A-D}" + "x^2 + " + $"{B-M}" + "x + " + $"{C-N}" + "}{dx^2 + mx + n}(bx^2 + mx + c)$";

            //по умолчанию
            default:
                //открываем документ excel
                ExcelPackage.License.SetNonCommercialOrganization("<My Noncommercial organization>");
                var file = new FileInfo(@"C:\Users\isera\Downloads\Telegram Desktop\ConsoleApp72\ConsoleApp7\ConsoleApp7\ConsoleApp7\sisipisi.xlsx");
                var package = new ExcelPackage(file);
                var worksheet = package.Workbook.Worksheets["Лист1"];
                //увеличиваем номер на один т к первая строка - названия столбцов
                int numberInt = Convert.ToInt32(number) + 1;
                string numberStr = Convert.ToString(numberInt);
                //изначальная ячейка
                string rezult = Convert.ToString(worksheet.Cells[$"D{numberStr}"].Value);

                return rezult;
        }
    }

    private static string AddFakeNumbers(string blank, int A, int B, int C, int D, int M, int N)
    {
        var Egor = new Random();
        int[] allowedAnswers = { A, B, C, D, M, N};
        int A1 = allowedAnswers[Egor.Next(allowedAnswers.Length)];
        int B1 = allowedAnswers[Egor.Next(allowedAnswers.Length)];
        int C1 = allowedAnswers[Egor.Next(allowedAnswers.Length)];
        int D1 = allowedAnswers[Egor.Next(allowedAnswers.Length)];
        int M1 = allowedAnswers[Egor.Next(allowedAnswers.Length)];
        int N1 = A + B - C - D - M + N;


        blank = blank.Replace("dx", "U");
        blank = blank.Replace("ra", "!");
        blank = blank.Replace("ar", "ЛЛ");
        blank = blank.Replace("na", "бб");
        blank = blank.Replace("tan", "ТАН");
        blank = blank.Replace("a", $"{A1}");
        blank = blank.Replace("ТАН", "tan");
        blank = blank.Replace("бб", "na");
        blank = blank.Replace("!", "ra");
        blank = blank.Replace("ЛЛ", "ar");
        blank = blank.Replace("U", "dx");

        blank = blank.Replace("b", $"{B1}");

        blank = blank.Replace("rac", "U");
        blank = blank.Replace("arc", "@");
        blank = blank.Replace("cos", "!");
        blank = blank.Replace("cdot", "ЖЖ");
        blank = blank.Replace("c", $"{C1}");
        blank = blank.Replace("ЖЖ", "cdot");
        blank = blank.Replace("!", "cos");
        blank = blank.Replace("@", "arc");
        blank = blank.Replace("U", "rac");

        blank = blank.Replace("dfrac", "РР");
        blank = blank.Replace("cdot", "ЖЖ");
        blank = blank.Replace("d", $"{D1}");
        blank = blank.Replace("ЖЖ", "cdot");
        blank = blank.Replace("РР", "dfrac");

        blank = blank.Replace("lim", "U");
        blank = blank.Replace("prime", "ДД");
        blank = blank.Replace("m", $"{M1}");
        blank = blank.Replace("ДД", "prime");
        blank = blank.Replace("U", "lim");

        blank = blank.Replace("ln", "U");
        blank = blank.Replace("inf", "@");
        blank = blank.Replace("neq", "!");
        blank = blank.Replace("sin", "СИН");
        blank = blank.Replace("tan", "ТАН");
        blank = blank.Replace("n", $"{N1}");
        blank = blank.Replace("ТАН", "tan");
        blank = blank.Replace("СИН", "sin");
        blank = blank.Replace("!", "neq");
        blank = blank.Replace("@", "inf");
        blank = blank.Replace("U", "ln");

        blank = blank.Replace("--", "+");
        blank = blank.Replace("- -", "+");
        blank = blank.Replace("-+", "-");
        blank = blank.Replace("- +", "-");
        blank = blank.Replace("+-", "-");
        blank = blank.Replace("+ -", "-");



        return blank;
    }

    public static string AddFakeFirstNumbers(string number, int A, int B, int C, int D, int M, int N)
    {
        var Egor = new Random();
        int[] allowedAnswers = { A, B, C, D, M, N };
        int A1 = allowedAnswers[Egor.Next(allowedAnswers.Length)];
        int B1 = allowedAnswers[Egor.Next(allowedAnswers.Length)];
        int C1 = allowedAnswers[Egor.Next(allowedAnswers.Length)];
        int D1 = allowedAnswers[Egor.Next(allowedAnswers.Length)];
        int M1 = allowedAnswers[Egor.Next(allowedAnswers.Length)];
        int N1 = allowedAnswers[Egor.Next(allowedAnswers.Length)];


        switch (number)
        {
            case "1":
                return $"{A1 * A1 * A1 + B1 * A1 + C1}";
            case "2":
                return @"$\dfrac{" + $"{A1 * A1 * A1 + B1 * A1 + C1}" + "}{" + $"{D1 * A1 * A1 + M1 * A1 + N1}" + "}$";
            case "4":
                do
                {
                    A1 = allowedAnswers[Egor.Next(allowedAnswers.Length)];
                    D1 = allowedAnswers[Egor.Next(allowedAnswers.Length)];
                } while (A1 == D1); // Гарантируем A1 ≠ D1

                do
                {
                    B1 = allowedAnswers[Egor.Next(allowedAnswers.Length)];
                    M1 = allowedAnswers[Egor.Next(allowedAnswers.Length)];
                } while (B1 == M1); // Гарантируем B1 ≠ M1

                do
                {
                    C1 = allowedAnswers[Egor.Next(allowedAnswers.Length)];
                    N1 = allowedAnswers[Egor.Next(allowedAnswers.Length)];
                } while (C1 == N1); // Гарантируем C1 ≠ N1
                return @"$\lim_{x \to a} \dfrac{" + $"{A1 - D1}" + "x^2 + " + $"{B1 - M1}" + "x + " + $"{C1 - N1}" + @"}{(bx^2 + mx + c)(\sqrt{ax^2 + bx + c} + \sqrt{dx^2 + mx + n})}$";

            case "12":
                return @"$e^{" + $"{A1 * M1}" + "}$";
            case "13":
                return @"$e^{" + $"{A1 * M1}" + "}$";
            case "21":
                do
                {
                    A1 = allowedAnswers[Egor.Next(allowedAnswers.Length)];
                    D1 = allowedAnswers[Egor.Next(allowedAnswers.Length)];
                } while (A1 == D1); // Гарантируем A1 ≠ D1

                do
                {
                    B1 = allowedAnswers[Egor.Next(allowedAnswers.Length)];
                    M1 = allowedAnswers[Egor.Next(allowedAnswers.Length)];
                } while (B1 == M1); // Гарантируем B1 ≠ M1

                do
                {
                    C1 = allowedAnswers[Egor.Next(allowedAnswers.Length)];
                    N1 = allowedAnswers[Egor.Next(allowedAnswers.Length)];
                } while (C1 == N1); // Гарантируем C1 ≠ N1

                return @"$\lim \dfrac{" + $"{A1 - D1}" + "x^2 + " + $"{B1 - M1}" + "x + " + $"{C1 - N1}" + "}{dx^2 + mx + n}(bx^2 + mx + c)$";
            //по умолчанию
            default:
                //открываем документ excel
                ExcelPackage.License.SetNonCommercialOrganization("<My Noncommercial organization>");
                var file = new FileInfo(@"C:\Users\isera\Downloads\Telegram Desktop\ConsoleApp72\ConsoleApp7\ConsoleApp7\ConsoleApp7\sisipisi.xlsx");
                var package = new ExcelPackage(file);
                var worksheet = package.Workbook.Worksheets["Лист1"];
                //увеличиваем номер на один т к первая строка - названия столбцов
                int numberInt = Convert.ToInt32(number) + 1;
                string numberStr = Convert.ToString(numberInt);
                //изначальная ячейка
                string rezult = Convert.ToString(worksheet.Cells[$"D{numberStr}"].Value);

                return rezult;
        }
    }

    static void Main()
    {
        var Egor = new Random();
        ExcelPackage.License.SetNonCommercialOrganization("<My Noncommercial organization>");
        try
        {
            var file = new FileInfo(@"C:\Users\isera\Downloads\Telegram Desktop\ConsoleApp72\ConsoleApp7\ConsoleApp7\ConsoleApp7\sisipisi.xlsx");
            using (var package = new ExcelPackage(file))
            {
                var worksheet = package.Workbook.Worksheets["Лист1"];
                for (int i = 26 +1, count = worksheet.Dimension.Rows; i < count; i++)
                {

                    string otvetEDIT = Convert.ToString(worksheet.Cells[$"D{i}"].Value);
                    string limit = " - ";
                    string nomer = Convert.ToString(i - 1);

                    // Создаем маппинг: индекс -> номер строки
                    int[] allowedRows = { 2, 3, 5, 6, 7, 12, 13, 21, 26, 28 };

                    int wrongAnswerRow1 = allowedRows[Egor.Next(allowedRows.Length)];
                    int wrongAnswerRow2 = allowedRows[Egor.Next(allowedRows.Length)];
                    int wrongAnswerRow3 = allowedRows[Egor.Next(allowedRows.Length)];

                    //проверка на одинаковость шаблонов неправильных ответов
                    string otvetNEPRAV1 = Convert.ToString(worksheet.Cells[$"D{wrongAnswerRow1}"].Value);
                    while (otvetNEPRAV1 == otvetEDIT)
                    {
                        wrongAnswerRow1 = allowedRows[Egor.Next(allowedRows.Length)];
                        otvetNEPRAV1 = Convert.ToString(worksheet.Cells[$"D{wrongAnswerRow1}"].Value);
                    }

                    string otvetNEPRAV2 = Convert.ToString(worksheet.Cells[$"D{wrongAnswerRow2}"].Value);
                    while (otvetNEPRAV2 == otvetEDIT | otvetNEPRAV2 == otvetNEPRAV1)
                    {
                        wrongAnswerRow2 = allowedRows[Egor.Next(allowedRows.Length)];
                        otvetNEPRAV2 = Convert.ToString(worksheet.Cells[$"D{wrongAnswerRow2}"].Value);
                    }

                    string otvetNEPRAV3 = Convert.ToString(worksheet.Cells[$"D{wrongAnswerRow3}"].Value);
                    while (otvetNEPRAV3 == otvetEDIT | otvetNEPRAV3 == otvetNEPRAV2 | otvetNEPRAV3 == otvetNEPRAV1)
                    {
                        wrongAnswerRow3 = allowedRows[Egor.Next(allowedRows.Length)];
                        otvetNEPRAV3 = Convert.ToString(worksheet.Cells[$"D{wrongAnswerRow3}"].Value);
                    }


                    for (int j = 0; j < 10; j++)
                    {
                        // Генерация переменных с учетом ограничений
                        int A, B, C, D, M, N, X;
                        bool isValid = false;
                        int attempts = 0;

                        do
                        {
                            A = Egor.Next(1, 10);
                            B = Egor.Next(-10, 10);
                            while (B == 0) B = Egor.Next(-10, 10);

                            C = Egor.Next(-10, 10);
                            while (C == 0) C = Egor.Next(-10, 10);

                            D = Egor.Next(-10, 10);
                            while (D == 0) D = Egor.Next(-10, 10);

                            M = Egor.Next(-10, 10);
                            while (M == 0) M = Egor.Next(-10, 10);

                            N = Egor.Next(-10, 10);
                            while (N == 0) N = Egor.Next(-10, 10);


                            // Проверка ограничений в зависимости от номера примера
                            switch (nomer)
                            {
                                case "2":
                                    isValid = D*A*A+M*A+N!=0;
                                    break;
                                case "3":
                                    isValid = ((A*A+B*A+C) == (D*A*A+M*A+N)) & ((A * A + B * A + C) == 0);
                                    break;
                                case "6":
                                    isValid = A == D;
                                    break;
                                case "14":
                                    isValid = (A > 0) & (A != 1);
                                    break;
                                case "18":
                                    limit = Convert.ToString(worksheet.Cells[$"E{i}"].Value);
                                    limit = AddNumbers(limit, A, B, C, D, M, N);
                                    isValid = true;
                                    break;
                                case "19":
                                    limit = Convert.ToString(worksheet.Cells[$"E{i}"].Value);
                                    limit = AddNumbers(limit, A, B, C, D, M, N);
                                    isValid = true;
                                    break;
                                case "20":
                                    limit = Convert.ToString(worksheet.Cells[$"E{i}"].Value);
                                    limit = AddNumbers(limit, A, B, C, D, M, N);
                                    isValid = true;
                                    break;
                                case "21":
                                    limit = Convert.ToString(worksheet.Cells[$"E{i}"].Value);
                                    limit = AddNumbers(limit, A, B, C, D, M, N);
                                    isValid = true;
                                    break;
                                case "22":
                                    limit = Convert.ToString(worksheet.Cells[$"E{i}"].Value);
                                    limit = AddNumbers(limit, A, B, C, D, M, N);
                                    isValid = true;
                                    break;
                                case "23":
                                    limit = Convert.ToString(worksheet.Cells[$"E{i}"].Value);
                                    limit = AddNumbers(limit, A, B, C, D, M, N);
                                    isValid = true;
                                    break;
                                case "24":
                                    limit = Convert.ToString(worksheet.Cells[$"E{i}"].Value);
                                    limit = AddNumbers(limit, A, B, C, D, M, N);
                                    isValid = true;
                                    break;
                                case "25":
                                    limit = Convert.ToString(worksheet.Cells[$"E{i}"].Value);
                                    limit = AddNumbers(limit, A, B, C, D, M, N);
                                    isValid = true;
                                    break;
                                case "27":
                                    limit = Convert.ToString(worksheet.Cells[$"E{i}"].Value);
                                    limit = AddNumbers(limit, A, B, C, D, M, N);
                                    isValid = true;
                                    break;
                                case "28":
                                    limit = Convert.ToString(worksheet.Cells[$"E{i}"].Value);
                                    limit = AddNumbers(limit, A, B, C, D, M, N);
                                    isValid = true;
                                    break;
                                case "29":
                                    limit = Convert.ToString(worksheet.Cells[$"E{i}"].Value);
                                    limit = AddNumbers(limit, A, B, C, D, M, N);
                                    isValid = true;
                                    break;
                                default:
                                    isValid = true;
                                    break;
                            }

                            attempts++;
                            if (attempts >= 50000) // Защита от бесконечного цикла
                            {
                                Console.WriteLine("");
                                Console.WriteLine("");
                                Console.WriteLine("--------------------------------------------------------------------------------------------------------------");
                                Console.WriteLine($"Не удалось сгенерировать значения для примера {nomer}");
                                Console.WriteLine("--------------------------------------------------------------------------------------------------------------");
                                Console.WriteLine("");
                                Console.WriteLine("");


                                break;
                            }

                        } while (!isValid);
                        
                        if (!isValid) continue;


                        int numberInt = Convert.ToInt32(nomer) + 1;
                        string numberStr = Convert.ToString(numberInt);
                        string formulaEDIT = Convert.ToString(worksheet.Cells[$"B{numberStr}"].Value);

                        //упрощение ответов
                        otvetEDIT = AddFirstNumbers(nomer, A, B, C, D, M, N);

                        //заполнение ответов необходимым
                        otvetEDIT = AddNumbers(otvetEDIT, A, B, C, D, M, N);
                        formulaEDIT = AddNumbers(formulaEDIT, A, B, C, D, M, N);


                        int[] specialTaskNumbers = { 2,3,4,5,7,18,19,20,21,27};

                        // Получаем номер текущего задания как число
                        int currentTaskNumber = Convert.ToInt32(nomer);

                        // Проверяем, нужно ли обрабатывать это задание особым образом
                        if (specialTaskNumbers.Contains(currentTaskNumber))
                        {
                            //вставляем один шаблон (правильного ответа) и совершаем неправильное упрощение ответов
                            otvetNEPRAV1 = AddFakeFirstNumbers(nomer, A, B, C, D, M, N);
                            otvetNEPRAV2 = AddFakeFirstNumbers(nomer, A, B, C, D, M, N);
                            otvetNEPRAV3 = AddFakeFirstNumbers(nomer, A, B, C, D, M, N);
                            //неправильное заполнение ответов необходимым
                            otvetNEPRAV1 = AddFakeNumbers(otvetNEPRAV1, A, B, C, D, M, N);
                            otvetNEPRAV2 = AddFakeNumbers(otvetNEPRAV2, A, B, C, D, M, N);
                            otvetNEPRAV3 = AddFakeNumbers(otvetNEPRAV3, A, B, C, D, M, N);
                            while (otvetNEPRAV1 == otvetNEPRAV2 | otvetNEPRAV1 == otvetNEPRAV3 | otvetNEPRAV1 == otvetNEPRAV2 | otvetNEPRAV2 == otvetNEPRAV3 | otvetNEPRAV1 == otvetEDIT | otvetNEPRAV2 == otvetEDIT | otvetNEPRAV3 == otvetEDIT)
                            {
                                //вставляем один шаблон (правильного ответа) и совершаем неправильное упрощение ответов
                                otvetNEPRAV1 = AddFakeFirstNumbers(nomer, A, B, C, D, M, N);
                                otvetNEPRAV2 = AddFakeFirstNumbers(nomer, A, B, C, D, M, N);
                                otvetNEPRAV3 = AddFakeFirstNumbers(nomer, A, B, C, D, M, N);
                                //неправильное заполнение ответов необходимым
                                otvetNEPRAV1 = AddFakeNumbers(otvetNEPRAV1, A, B, C, D, M, N);
                                otvetNEPRAV2 = AddFakeNumbers(otvetNEPRAV2, A, B, C, D, M, N);
                                otvetNEPRAV3 = AddFakeNumbers(otvetNEPRAV3, A, B, C, D, M, N);
                            }
                        }
                        else
                        {
                            //неправильное упрощение ответов
                            otvetNEPRAV1 = AddFakeFirstNumbers(Convert.ToString(wrongAnswerRow1), A, B, C, D, M, N);
                            otvetNEPRAV2 = AddFakeFirstNumbers(Convert.ToString(wrongAnswerRow2), A, B, C, D, M, N);
                            otvetNEPRAV3 = AddFakeFirstNumbers(Convert.ToString(wrongAnswerRow3), A, B, C, D, M, N);
                            //неправильное заполнение ответов необходимым
                            otvetNEPRAV1 = AddFakeNumbers(otvetNEPRAV1, A, B, C, D, M, N);
                            otvetNEPRAV2 = AddFakeNumbers(otvetNEPRAV2, A, B, C, D, M, N);
                            otvetNEPRAV3 = AddFakeNumbers(otvetNEPRAV3, A, B, C, D, M, N);
                            //проверка на одинаковость заполненных шаблонов неправильных ответов
                            while (otvetNEPRAV1 == otvetNEPRAV2 | otvetNEPRAV1 == otvetNEPRAV3 | otvetNEPRAV1 == otvetNEPRAV2 | otvetNEPRAV2 == otvetNEPRAV3 | otvetNEPRAV1 == otvetEDIT | otvetNEPRAV2 == otvetEDIT | otvetNEPRAV3 == otvetEDIT)
                            {
                                //неправильное упрощение ответов
                                otvetNEPRAV1 = AddFakeFirstNumbers(Convert.ToString(wrongAnswerRow1), A, B, C, D, M, N);
                                otvetNEPRAV2 = AddFakeFirstNumbers(Convert.ToString(wrongAnswerRow2), A, B, C, D, M, N);
                                otvetNEPRAV3 = AddFakeFirstNumbers(Convert.ToString(wrongAnswerRow3), A, B, C, D, M, N);
                                //неправильное заполнение ответов необходимым
                                otvetNEPRAV1 = AddFakeNumbers(otvetNEPRAV1, A, B, C, D, M, N);
                                otvetNEPRAV2 = AddFakeNumbers(otvetNEPRAV2, A, B, C, D, M, N);
                                otvetNEPRAV3 = AddFakeNumbers(otvetNEPRAV3, A, B, C, D, M, N);
                            }
                        }
                        if (nomer == "12" | nomer == "13")
                            {
                            //неправильное упрощение ответов
                            otvetNEPRAV1 = @"$\infty$";
                            otvetNEPRAV2 = "$0$";
                            otvetNEPRAV3 = AddFakeFirstNumbers(Convert.ToString(wrongAnswerRow3), A, B, C, D, M, N);
                            //неправильное заполнение ответов необходимым\
                            otvetNEPRAV3 = AddFakeNumbers(otvetNEPRAV3, A, B, C, D, M, N);

                            while (otvetNEPRAV1 == otvetNEPRAV2 | otvetNEPRAV1 == otvetNEPRAV3 | otvetNEPRAV1 == otvetNEPRAV2 | otvetNEPRAV2 == otvetNEPRAV3 | otvetNEPRAV1 == otvetEDIT | otvetNEPRAV2 == otvetEDIT | otvetNEPRAV3 == otvetEDIT)
                            {
                                //неправильное упрощение ответов
                                otvetNEPRAV1 = @"$\infty$";
                                otvetNEPRAV2 = "$0$";
                                otvetNEPRAV3 = AddFakeFirstNumbers(Convert.ToString(wrongAnswerRow3), A, B, C, D, M, N);
                                //неправильное заполнение ответов необходимым\
                                otvetNEPRAV3 = AddFakeNumbers(otvetNEPRAV3, A, B, C, D, M, N);
                            }
                        }


                        //перемешка ответов
                        string[] final_massive = [otvetEDIT, otvetNEPRAV1, otvetNEPRAV2, otvetNEPRAV3];
                        Random.Shared.Shuffle(final_massive);


                        //финальный файл
                        string stringe = $@"
\begin{{array}}{{l}}
\\
\textcolor{{red}}{{\textbf{{Формула:}}}} \, ${formulaEDIT}$ \\
\\
\textcolor{{red}}{{\textbf{{Предел:}}}} \, ${limit}$ \\
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



                        string outputPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), $"zov{nomer}_var{j + 1}_{Array.IndexOf(final_massive, otvetEDIT)+1}.png");
                        GenerateFormulaImage(stringe, outputPath, nomer);


                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка: {ex.Message}");
            Console.ReadLine();
        }
    }
}