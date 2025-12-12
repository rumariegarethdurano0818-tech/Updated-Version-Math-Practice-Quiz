
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Math_Learning_For_Kids_Fun
{
    //interface IPausable
    //{
    //    void Pause(string message = "⏸️ Press any key to continue...");
    //}

    interface ISaveable
    {
        void SaveData();
        void LoadData();
    }

    interface IStudent
    {
        void TakeQuiz();
        void ViewMyResults();
    }

    internal class User
    {
        public string Name { get; set; }
        public string StudentID { get; set; }

        public User(string name, string id)
        {
            Name = name;
            StudentID = id;
        }
    }

    internal class Student : User, IStudent, ISaveable
    {
        private List<string> results = new List<string>();

        public Student(string name, string id) : base(name, id) { }

        public void Pause(string message = "⏸️ Press any key to continue...")
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(message);
            Console.ResetColor();
            Console.ReadKey();
        }

        private string GetResultsFile()
        {
            string folder = "Student Results";
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            return Path.Combine(folder, $"{Name}_results.txt");
        }

        // Column widths (content widths; borders add the extra spaces)
        private readonly int wDate = 22;
        private readonly int wGrade = 7;
        private readonly int wDiff = 12;
        private readonly int wTime = 8;
        private readonly int wScore = 12;
        private readonly int wCheat = 8;

        private string BuildTopBorder()
        {
            return "┌" + string.Concat(
                new string('─', wDate + 2), "┬",
                new string('─', wGrade + 2), "┬",
                new string('─', wDiff + 2), "┬",
                new string('─', wTime + 2), "┬",
                new string('─', wScore + 2), "┬",
                new string('─', wCheat + 2)
            ) + "┐";
        }

        private string BuildHeaderLine()
        {
            return $"│ {Pad("Date/Time", wDate)} │ {Pad("Grade", wGrade)} │ {Pad("Difficulty", wDiff)} │ {Pad("Time", wTime)} │ {Pad("Score", wScore)} │ {Pad("Cheat", wCheat)} │";
        }

        private string BuildHeaderSeparator()
        {
            return "├" + string.Concat(
                new string('─', wDate + 2), "┼",
                new string('─', wGrade + 2), "┼",
                new string('─', wDiff + 2), "┼",
                new string('─', wTime + 2), "┼",
                new string('─', wScore + 2), "┼",
                new string('─', wCheat + 2)
            ) + "┤";
        }

        private string BuildBottomBorder()
        {
            return "└" + string.Concat(
                new string('─', wDate + 2), "┴",
                new string('─', wGrade + 2), "┴",
                new string('─', wDiff + 2), "┴",
                new string('─', wTime + 2), "┴",
                new string('─', wScore + 2), "┴",
                new string('─', wCheat + 2)
            ) + "┘";
        }

        private string Pad(string s, int width)
        {
            if (s == null) s = "";
            if (s.Length > width) return s.Substring(0, width - 1) + "…";
            return s.PadRight(width);
        }

        private string FormatDataRow(string dateTime, string grade, string diff, string time, string score, string cheat)
        {
            return $"│ {Pad(dateTime, wDate)} │ {Pad(grade, wGrade)} │ {Pad(diff, wDiff)} │ {Pad(time, wTime)} │ {Pad(score, wScore)} │ {Pad(cheat, wCheat)} │";
        }

        // Ensure the file exists and has a proper table header. If the file exists but is in old plain format,
        // this will migrate the existing lines into rows inside the table (best-effort).
        private void EnsureTableFileExists(string filePath)
        {
            if (!File.Exists(filePath))
            {
                var init = new List<string>
            {
                BuildTopBorder(),
                BuildHeaderLine(),
                BuildHeaderSeparator(),
                BuildBottomBorder()
            };
                File.WriteAllLines(filePath, init);
                return;
            }

            var lines = File.ReadAllLines(filePath).ToList();

            // If the file already is a boxed table (starts with box top), do nothing.
            if (lines.Count > 0 && lines[0].StartsWith("┌")) return;

            // Otherwise migrate: create a new table and insert each existing line as a data row (best-effort).
            var newTable = new List<string>
        {
            BuildTopBorder(),
            BuildHeaderLine(),
            BuildHeaderSeparator()
        };

            if (lines.Count > 0)
            {
                foreach (var plain in lines)
                {
                    // Example old format:
                    // [12/3/2025 12:25] Grade 4 Difficulty: Normal TimeLimit:40s Score: 3/5, Cheat Sheet: No

                    string dt = "";
                    string grade = "";
                    string diff = "";
                    string time = "";
                    string score = "";
                    string cheat = "";

                    try
                    {
                        // Extract datetime inside brackets
                        int a = plain.IndexOf('[');
                        int b = plain.IndexOf(']');
                        if (a != -1 && b != -1 && b > a)
                            dt = plain.Substring(a + 1, b - a - 1);

                        // Extract Grade
                        grade = ExtractAfter(plain, "Grade ", " ");

                        // Extract Difficulty
                        diff = ExtractAfter(plain, "Difficulty: ", " ");

                        // Extract TimeLimit
                        time = ExtractAfter(plain, "TimeLimit:", "s") + "s";

                        // Extract Score (x/y)
                        int scIndex = plain.IndexOf("Score:");
                        if (scIndex != -1)
                        {
                            string part = plain.Substring(scIndex + 6).Trim();
                            int comma = part.IndexOf(',');
                            if (comma != -1) part = part.Substring(0, comma);
                            score = part.Trim();
                        }

                        // Extract Cheat Sheet
                        cheat = plain.Contains("Yes") ? "Yes" : "No";
                    }
                    catch
                    {
                        // fallback → put whole line inside Score column
                        score = plain;
                    }

                    newTable.Add(FormatDataRow(
                        Pad(dt, wDate),
                        Pad(grade, wGrade),
                        Pad(diff, wDiff),
                        Pad(time, wTime),
                        Pad(score, wScore),
                        Pad(cheat, wCheat)
                    ));
                }

            }

            newTable.Add(BuildBottomBorder());
            File.WriteAllLines(filePath, newTable);
        }
        private string ExtractAfter(string src, string start, string end)
        {
            int a = src.IndexOf(start);
            if (a == -1) return "";
            a += start.Length;

            int b = src.IndexOf(end, a);
            if (b == -1) return src.Substring(a).Trim();

            return src.Substring(a, b - a).Trim();
        }


        // Append one row to the boxed table (preserving header and bottom border).
        public void AddResult(int grade, string difficulty, int timeLimitSeconds, int score, int totalQuestions, bool usedCheat)
        {
            string filePath = GetResultsFile();
            EnsureTableFileExists(filePath);

            var lines = File.ReadAllLines(filePath).ToList();

            // Build formatted row
            string date = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
            string gradeStr = grade.ToString();
            string timeStr = timeLimitSeconds + "s";
            string scoreStr = $"{score}/{totalQuestions}";
            string cheatStr = usedCheat ? "Yes" : "No";

            string row = FormatDataRow(date, gradeStr, difficulty, timeStr, scoreStr, cheatStr);

            // Find bottom border index (look from end)
            int bottomIndex = -1;
            for (int i = lines.Count - 1; i >= 0; i--)
            {
                if (lines[i].StartsWith("└")) { bottomIndex = i; break; }
            }

            if (bottomIndex == -1)
            {
                // unexpected; append with a fresh table
                lines.Add(row);
                lines.Add(BuildBottomBorder());
            }
            else
            {
                lines.Insert(bottomIndex, row);
            }

            File.WriteAllLines(filePath, lines);

            // Also keep a simple results list (each stored as the row string)
            results.Add(row);
        }

        // SaveData writes the current in-memory results back into the boxed table (rewrites file).
        public void SaveData()
        {
            string filePath = GetResultsFile();

            var lines = new List<string>
        {
            BuildTopBorder(),
            BuildHeaderLine(),
            BuildHeaderSeparator()
        };

            foreach (var r in results)
            {
                // assume r is already a formatted row; otherwise skip or pad
                if (r.StartsWith("│")) lines.Add(r);
            }

            lines.Add(BuildBottomBorder());
            File.WriteAllLines(filePath, lines);
        }

        // LoadData reads the boxed table and populates the in-memory results list (data rows only)
        public void LoadData()
        {
            string filePath = GetResultsFile();
            results.Clear();

            if (!File.Exists(filePath)) return;

            var lines = File.ReadAllLines(filePath);
            bool inData = false;
            foreach (var line in lines)
            {
                if (line.StartsWith("├")) { inData = true; continue; } // header separator -> then data rows follow
                if (!inData) continue;
                if (line.StartsWith("└")) break; // bottom border -> stop
                if (line.StartsWith("│"))
                    results.Add(line);
            }
        }

        // Display the file exactly as the boxed table in console (preserves perfect connected lines)
        public void ViewMyResults()
        {
            Console.Clear();
            Console.WriteLine($"📊 Quiz Results for {Name} ({StudentID})");
            Console.WriteLine();

            string filePath = GetResultsFile();

            if (!File.Exists(filePath))
            {
                Console.WriteLine("No saved results yet.");
                Pause();
                return;
            }

            // Ensure table format exists; if old plain file, it will be migrated
            EnsureTableFileExists(filePath);

            foreach (var line in File.ReadAllLines(filePath))
                Console.WriteLine(line);

            Pause();
        }

        public void TakeQuiz()
        {
            QuizEngine.StartQuiz(this);
        }
    }

    internal class Question
    {
        public int Operand1 { get; }
        public int Operand2 { get; }
        public string Operation { get; }
        public double CorrectAnswer { get; }

        public Question(int a, int b, string op)
        {
            Operand1 = a;
            Operand2 = b;
            Operation = op;

            switch (op)
            {
                case "+": CorrectAnswer = a + b; break;
                case "-": CorrectAnswer = a - b; break;
                case "×": CorrectAnswer = a * b; break;
                case "÷": CorrectAnswer = Math.Round((double)a / b, 2); break;
                default: CorrectAnswer = 0; break;
            }
        }

        public void Display()
        {
            Console.WriteLine();
            int width = Math.Max(Operand1.ToString().Length, Operand2.ToString().Length) + 2;
            Console.WriteLine(Operand1.ToString().PadLeft(width));
            Console.WriteLine($"{Operation}{Operand2.ToString().PadLeft(width - 1)}");
            Console.WriteLine(new string('-', width));
        }
    }

    internal static class QuizEngine
    {
        private static Random rand = new Random();
        private const string CheatFileName = "cheat_sheet.txt";

       
        private static string CreateSingleCheatSheet()
        {
            var lines = new List<string>();

            lines.Add("=======================================");
            lines.Add("             CHEAT SHEET");
            lines.Add("=======================================");
            lines.Add("");
            lines.Add("This file contains Addition, Subtraction, Multiplication, and Division facts (0–12).");
            lines.Add("Division values are rounded to 2 decimal places.");
            lines.Add("");
            lines.Add($"File: {CheatFileName}");
            lines.Add($"Location: {Path.GetFullPath(CheatFileName)}");
            lines.Add("");
            lines.Add("You can open this file with Notepad or any text editor.");
            lines.Add("It will be deleted automatically after the quiz ends.");
            lines.Add("");
            lines.Add("---------------------------------------");
            lines.Add("");

            lines.Add("===== ADDITION =====");
            for (int b = 1; b <= 12; b++)
            {
                string row = "";
                for (int a = 0; a <= 12; a++)
                {
                    string item = $"{a}+{b}={a + b}";
                    row += item.PadRight(12);
                }
                lines.Add(row.TrimEnd());
            }
            lines.Add("");
            lines.Add("---------------------------------------");
            lines.Add("");


            lines.Add("===== SUBTRACTION =====");
            for (int a = 0; a <= 12; a++)
            {
                string row = "";
                for (int b = 0; b <= 12; b++)
                {
                    int big = Math.Max(a, b);
                    int small = Math.Min(a, b);
                    string item = $"{big}-{small}={big - small}";
                    row += item.PadRight(12);
                }
                lines.Add(row.TrimEnd());
            }
            lines.Add("");
            lines.Add("---------------------------------------");
            lines.Add("");


            lines.Add("===== MULTIPLICATION =====");
            for (int b = 1; b <= 12; b++)
            {
                string row = "";
                for (int a = 0; a <= 12; a++)
                {
                    string item = $"{a}×{b}={a * b}";
                    row += item.PadRight(12);
                }
                lines.Add(row.TrimEnd());
            }
            lines.Add("");
            lines.Add("---------------------------------------");
            lines.Add("");

            lines.Add("===== DIVISION (rounded 2 decimals) =====");
            for (int b = 1; b <= 12; b++)
            {
                string row = "";
                for (int a = 0; a <= 12; a++)
                {
                    double val = Math.Round((double)a / b, 2);
                    string item = $"{a}÷{b}={val:0.00}";
                    row += item.PadRight(14);
                }
                lines.Add(row.TrimEnd());
            }
            lines.Add("");
            lines.Add("---------------------------------------");
            lines.Add("");

            File.WriteAllLines(CheatFileName, lines);
            return CheatFileName;
        }

        private static void DeleteSingleCheatSheet()
        {
            try
            {
                if (File.Exists(CheatFileName))
                    File.Delete(CheatFileName);
            }
            catch { }
        }


        
        private static void ShowStudyMode(int grade)
        {
            Console.Clear();
            Console.WriteLine("📖 Study Mode");
            Console.WriteLine("-------------------");

            if (grade == 1)
            {
                Console.WriteLine("Grade 1: Simple Addition and Subtraction (0–10) with fun objects!");
                Console.WriteLine();
                Console.WriteLine("Example:");
                Console.WriteLine();
                Console.WriteLine("  3 + 4 = 7");
                Console.WriteLine("  🍎 🍎 🍎 + 🍎 🍎 🍎 🍎 = 🍎 🍎 🍎 🍎 🍎 🍎 🍎");
                Console.WriteLine();
                Console.WriteLine("  5 - 2 = 3");
                Console.WriteLine("  🐶 🐶 🐶 🐶 🐶 - 🐶 🐶 = 🐶 🐶 🐶");
                Console.WriteLine();
                Console.WriteLine("  2 + 3 = 5");
                Console.WriteLine("  🍌 🍌 + 🍌 🍌 🍌 = 🍌 🍌 🍌 🍌 🍌");
                Console.WriteLine();
                Console.WriteLine("  4 - 1 = 3");
                Console.WriteLine("  🐱 🐱 🐱 🐱 - 🐱 = 🐱 🐱 🐱");
            }
            else if (grade == 2)
            {
                Console.WriteLine("Grade 2: Two-digit Addition and Subtraction with Carrying/Borrowing and Negative Numbers");
                Console.WriteLine();
                Console.WriteLine("Example 1: Addition with carrying");
                Console.WriteLine("  47");
                Console.WriteLine("+ 38");
                Console.WriteLine("-----");
                Console.WriteLine("  85 (Carry the 1 from 7+8=15)");
                Console.WriteLine();
                Console.WriteLine("Example 2: Subtraction with borrowing");
                Console.WriteLine("  42");
                Console.WriteLine("- 57");
                Console.WriteLine("-----");
                Console.WriteLine("  -15 (Since 42 < 57, result is negative)");
                Console.WriteLine();
                Console.WriteLine("Tip: Start subtracting from the rightmost digit!");
            }
            else if (grade == 3)
            {
                Console.WriteLine("Grade 3: Addition, Subtraction, Multiplication, and Division");
                Console.WriteLine();

                Console.WriteLine("Example 1: Addition with carrying");
                Console.WriteLine("  276");
                Console.WriteLine("+ 487");
                Console.WriteLine("-----");
                Console.WriteLine("Step 1: Add rightmost digits: 6 + 7 = 13 → write 3, carry 1");
                Console.WriteLine("Step 2: Add tens: 7 + 8 + 1(carry) = 16 → write 6, carry 1");
                Console.WriteLine("Step 3: Add hundreds: 2 + 4 + 1(carry) = 7");
                Console.WriteLine("Result: 763");
                Console.WriteLine();

                Console.WriteLine("Example 2: Subtraction with borrowing");
                Console.WriteLine("  504");
                Console.WriteLine("- 279");
                Console.WriteLine("-----");
                Console.WriteLine("Step 1: Rightmost digit: 4 - 9 → borrow 1 from tens: 0 becomes -1? No, borrow from hundreds → 5 becomes 4, tens 0 becomes 10, then borrow 1 → 10-1=9 → rightmost becomes 14-9=5");
                Console.WriteLine("Step 2: Tens: 9 - 7 = 2");
                Console.WriteLine("Step 3: Hundreds: 4 - 2 = 2");
                Console.WriteLine("Result: 225");
                Console.WriteLine();

                Console.WriteLine("Example 3: Multiplication");
                Console.WriteLine("  12 × 5");
                Console.WriteLine("Step 1: Multiply units: 2 × 5 = 10 → write 0, carry 1");
                Console.WriteLine("Step 2: Multiply tens: 1 × 5 = 5 → add carry 1 → 6");
                Console.WriteLine("Result: 60");
                Console.WriteLine();

                Console.WriteLine("Example 4: Division");
                Console.WriteLine("  17 ÷ 4");
                Console.WriteLine("Step 1: 4 goes into 17 four times → 4 × 4 = 16");
                Console.WriteLine("Step 2: Subtract 16 from 17 → remainder 1");
                Console.WriteLine("Step 3: Decimal: remainder 1 → 1.0 / 4 = 0.25");
                Console.WriteLine("Result: 4.25");
            }

            Console.WriteLine();
            Console.WriteLine("Press any key when you are ready to start the quiz...");
            Console.ReadKey();
        }

        public static void StartQuiz(Student student)
        {
            Console.Clear();
            Console.WriteLine("🎮 Welcome to the Math Quiz!");
            Console.WriteLine("---------------------------");

            int grade = GetGrade();
            ShowStudyMode(grade);

            string difficulty = GetDifficulty();
            int timeLimitSeconds = GetTimeLimitForDifficulty(difficulty);

            string operation = GetOperation(grade);
            int totalQuestions = GetQuestionCount();

            bool usedCheat = AskCheatSheet();
            string cheatFile = null;

            try
            {
                    if (usedCheat)
                    {
          
                        cheatFile = CreateSingleCheatSheet();

                        
                        try
                        {
                            Process.Start("notepad.exe", cheatFile);
                        }
                        catch
                        {
                            Console.WriteLine("⚠️ Could not open Notepad automatically.");
                            Console.WriteLine("Please open the cheat sheet manually:");
                            Console.WriteLine(cheatFile);
                        }
                        Console.WriteLine("\n📄 Cheat sheet is open. Arrange your screens if needed.");
                        Console.WriteLine("Press any key when you are ready to start the quiz...");
                        Console.ReadKey();
                        Console.Clear();
                    }


                int score = 0;
                List<string> review = new List<string>();

            
                List<Question> questions = BuildQuestionSet(operation, grade, difficulty, totalQuestions);

                for (int i = 0; i < questions.Count; i++)
                {
                    var q = questions[i];
                    int displayIndex = i + 1;

                    Console.Clear();
                    Console.WriteLine($"Question {displayIndex}/{totalQuestions} — Difficulty: {difficulty} — Time: {timeLimitSeconds}s");
                    q.Display();

                    double? answer = GetAnswer(timeLimitSeconds);

                    if (answer.HasValue && Math.Abs(answer.Value - q.CorrectAnswer) < 0.01)
                    {
                        string[] positive = {
                            "🌟 Awesome! You're a math star!",
                            "✅ Correct! Great job!",
                            "🎉 Fantastic! You got it right!",
                            "👏 Super! You’re really good at this!",
                            "💪 Nice work! Keep it up!",
                            "🏅 Brilliant! Math master in the making!",
                            "🚀 Wow! You’re on fire!",
                            "🥳 Perfect! You really know your stuff!"
                        };
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine(positive[rand.Next(positive.Length)]);
                        Console.ResetColor();
                        score++;
                        review.Add($"✅ {q.Operand1} {q.Operation} {q.Operand2} = {answer.Value}");
                    }
                    else
                    {
                        if (!answer.HasValue)
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine("⏱️ Time's up!");
                            Console.ResetColor();
                            review.Add($"⏱️ TIMEOUT {q.Operand1} {q.Operation} {q.Operand2} (Correct: {q.CorrectAnswer})");
                        }
                        else
                        {
                            string[] retry = {
                                "😅 Oops! Not quite right.",
                                "❌ Wrong! But don’t worry, you’ll get the next one!",
                                "🙃 Almost there! Try again next time!",
                                "🧐 Hmm… not the answer. You got this though!",
                                "😕 Oof! Close, but not correct.",
                                "💡 Don’t give up — practice makes perfect!"
                            };
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine(retry[rand.Next(retry.Length)]);
                            Console.WriteLine($"Correct answer: {q.CorrectAnswer}");
                            Console.ResetColor();
                            review.Add($"❌ {q.Operand1} {q.Operation} {q.Operand2} = {answer.Value} (Correct: {q.CorrectAnswer})");
                        }
                    }

                    Console.WriteLine();
                    Console.WriteLine("Press any key for next question...");
                    Console.ReadKey();
                }

                Console.Clear();
                Console.WriteLine("🎉 Quiz Complete!");
                Console.WriteLine("-----------------");
                Console.WriteLine($"Score: {score}/{totalQuestions}");
                Console.WriteLine($"Used Cheat Sheet: {(usedCheat ? "Yes" : "No")} — Difficulty: {difficulty} — Time per question: {timeLimitSeconds}s");
                Console.WriteLine();

                foreach (var item in review)
                    Console.WriteLine(item);

                student.AddResult(grade, difficulty, timeLimitSeconds, score, totalQuestions, usedCheat);

            }
            finally
            {
                DeleteSingleCheatSheet();
                Console.WriteLine("\n🧹 Cheat sheet deleted automatically!");
            }

            student.Pause("🎯 Press any key to return to menu...");
        }

        private static int GetTimeLimitForDifficulty(string difficulty)
        {
            switch (difficulty)
            {
                case "Easy": return 60;
                case "Normal": return 40;
                case "Hard": return 20;
                default: return 40;
            }
        }

        private static int GetGrade()
        {
            Console.Clear();
            Console.WriteLine("Select your Grade:");
            Console.WriteLine("1. Grade 1");
            Console.WriteLine("2. Grade 2");
            Console.WriteLine("3. Grade 3");
            while (true)
            {
                Console.Write("Enter choice (1–3): ");
                string input = Console.ReadLine();
                if (input == "1") return 1;
                if (input == "2") return 2;
                if (input == "3") return 3;
                Console.WriteLine("❌ Invalid choice. Try again.");
            }
        }

        private static string GetDifficulty()
        {
            Console.Clear();
            Console.WriteLine("Select Difficulty:");
            Console.WriteLine("1. Easy");
            Console.WriteLine("2. Normal");
            Console.WriteLine("3. Hard");

            while (true)
            {
                Console.Write("Enter choice (1-3): ");
                string input = Console.ReadLine();

                if (input == "1") return "Easy";
                if (input == "2") return "Normal";
                if (input == "3") return "Hard";

                Console.WriteLine("❌ Invalid choice. Try again.");
            }
        }

        private static string GetOperation(int grade)
        {
            Console.Clear();
            Console.WriteLine("Select Operation:");

            if (grade == 1 || grade == 2)
            {
                Console.WriteLine("1. Addition (+)");
                Console.WriteLine("2. Subtraction (-)");
            }
            else
            {
                Console.WriteLine("1. Addition (+)");
                Console.WriteLine("2. Subtraction (-)");
                Console.WriteLine("3. Multiplication (×)");
                Console.WriteLine("4. Division (÷)");
                Console.WriteLine("5. Mixed");
            }

            while (true)
            {
                Console.Write("Enter your choice: ");
                string input = Console.ReadLine();

                if (grade == 1 || grade == 2)
                {
                    if (input == "1") return "+";
                    if (input == "2") return "-";
                }
                else
                {
                    switch (input)
                    {
                        case "1": return "+";
                        case "2": return "-";
                        case "3": return "×";
                        case "4": return "÷";
                        case "5": return "Mixed";
                    }
                }
                Console.WriteLine("❌ Invalid choice. Please try again.");
            }
        }

        private static int GetQuestionCount()
        {
            Console.Clear();
            Console.Write("Enter number of questions (5, 10, 15): ");
            while (true)
            {
                string input = Console.ReadLine();
                if (int.TryParse(input, out int num) && (num == 5 || num == 10 || num == 15))
                    return num;
                Console.Write("❌ Invalid number. Try again: ");
            }
        }

        private static bool AskCheatSheet()
        {
            Console.Clear();
            Console.Write("Use a Cheat Sheet? (y/n): ");
            string ans = Console.ReadLine().ToLower();
            return ans == "y";
        }

      
        private static double? GetAnswer(int timeLimitSeconds)
        {
            Stopwatch sw = Stopwatch.StartNew();

         
            Console.Write("Answer: ");
            int answerLeft = Console.CursorLeft;
            int answerTop = Console.CursorTop;

            Console.WriteLine();                 
            int timerTop = Console.CursorTop;

            string userInput = "";

            while (true)
            {
                var readTask = Task.Run(() => Console.ReadLine());

                while (!readTask.IsCompleted)
                {
                    int elapsed = (int)sw.Elapsed.TotalSeconds;
                    int remaining = timeLimitSeconds - elapsed;

                    if (remaining <= 0)
                    {
                        Console.SetCursorPosition(0, timerTop);
                        Console.Write("Time left:   0s   ");
                        return null;
                    }

                   
                    Console.SetCursorPosition(0, timerTop);
                    Console.Write($"Time left: {remaining,3}s   ");

                   
                    Console.SetCursorPosition(answerLeft, answerTop);

                    if (readTask.Wait(TimeSpan.FromMilliseconds(200)))
                        break;
                }

                Console.WriteLine();

                if (!readTask.IsCompleted)
                    return null;

                userInput = readTask.Result.Trim();

                if (double.TryParse(userInput, out double ans))
                    return ans;

              
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("❌ Please enter a valid number.");
                Console.ResetColor();

                if (sw.Elapsed.TotalSeconds >= timeLimitSeconds)
                    return null;

                Console.Write("Answer: ");
                answerLeft = Console.CursorLeft;
                answerTop = Console.CursorTop;

                Console.WriteLine();
                timerTop = Console.CursorTop;
            }
        }


      
        private static List<Question> BuildQuestionSet(string operation, int grade, string difficulty, int totalQuestions)
        {
           
            string[] ops = operation == "Mixed" ? new[] { "+", "-", "×", "÷" } : new[] { operation };

            int opsCount = ops.Length;
            int basePerOp = totalQuestions / opsCount;
            int remainder = totalQuestions % opsCount;

            List<Question> pool = new List<Question>();

            for (int i = 0; i < opsCount; i++)
            {
                string op = ops[i];
                int countForOp = basePerOp + (i < remainder ? 1 : 0);

                var patternCounts = GetPatternCounts(grade, op, countForOp);

                foreach (var kv in patternCounts)
                {
                    string pattern = kv.Key;
                    int count = kv.Value;
                    for (int c = 0; c < count; c++)
                    {
                        pool.Add(GenerateQuestionForPattern(op, grade, difficulty, pattern));
                    }
                }
            }

          
            for (int i = pool.Count - 1; i > 0; i--)
            {
                int j = rand.Next(i + 1);
                var tmp = pool[i];
                pool[i] = pool[j];
                pool[j] = tmp;
            }

            if (pool.Count > totalQuestions) pool = pool.Take(totalQuestions).ToList();
            while (pool.Count < totalQuestions)
            {
                var fillOp = ops[rand.Next(ops.Length)];
                pool.Add(GenerateQuestionForPattern(fillOp, grade, difficulty, "1x1"));
            }

            return pool;
        }

        private static Dictionary<string, int> GetPatternCounts(int grade, string operation, int qCount)
        {
            // Patterns: "1x1", "2x1", "2x2", "3x1", "3x2"
            var d = new Dictionary<string, int> { { "1x1", 0 }, { "2x1", 0 }, { "2x2", 0 }, { "3x1", 0 }, { "3x2", 0 } };

            // Simple grade-based templates (tuned to examples you approved)
            if (grade == 1)
            {
                if (qCount <= 5)
                {
                    d["1x1"] = Math.Min(2, qCount);
                    d["2x1"] = qCount - d["1x1"];
                }
                else if (qCount == 10)
                {
                    d["1x1"] = 5; d["2x1"] = 5;
                }
                else // 15
                {
                    d["1x1"] = (int)Math.Round(qCount * 0.45);
                    d["2x1"] = qCount - d["1x1"];
                }
            }
            else if (grade == 2)
            {
                if (qCount <= 5)
                {
                    d["1x1"] = 1; d["2x1"] = 2; d["2x2"] = qCount - 3;
                }
                else if (qCount == 10)
                {
                    d["1x1"] = 3; d["2x1"] = 4; d["2x2"] = 3;
                }
                else // 15
                {
                    d["1x1"] = (int)Math.Round(qCount * 0.25);
                    d["2x1"] = (int)Math.Round(qCount * 0.40);
                    d["2x2"] = qCount - d["1x1"] - d["2x1"];
                }
            }
            else // grade 3
            {
                if (qCount <= 5)
                {
                    d["1x1"] = 1; d["2x1"] = 2; d["2x2"] = 1; d["3x1"] = 1;
                }
                else if (qCount == 10)
                {
                    d["1x1"] = 2; d["2x1"] = 4; d["2x2"] = 3; d["3x1"] = 1;
                }
                else // 15
                {
                    d["1x1"] = (int)Math.Round(qCount * 0.20);
                    d["2x1"] = (int)Math.Round(qCount * 0.35);
                    d["2x2"] = (int)Math.Round(qCount * 0.25);
                    int assigned = d["1x1"] + d["2x1"] + d["2x2"];
                    d["3x1"] = (int)Math.Round((qCount - assigned) * 0.75);
                    d["3x2"] = qCount - d["1x1"] - d["2x1"] - d["2x2"] - d["3x1"];
                    if (d["3x2"] < 0) d["3x2"] = 0;
                }
            }

            int total = d.Values.Sum();
            if (total == 0)
            {
                d["1x1"] = Math.Min(1, qCount);
                total = d.Values.Sum();
            }

            if (total != qCount)
            {
                d["1x1"] += (qCount - total);
            }

            // remove zero entries for cleanliness (optional)
            var keys = d.Keys.ToList();
            foreach (var k in keys)
            {
                if (d[k] <= 0)
                    d[k] = 0;
            }

            return d;
        }

        private static Question GenerateQuestionForPattern(string operation, int grade, string difficulty, string pattern)
        {
            double multiplier = 1.0;
            if (difficulty == "Easy") multiplier = 0.6;
            if (difficulty == "Hard") multiplier = 1.5;

            int aMin = 1, aMax = 9, bMin = 1, bMax = 9;

            switch (pattern)
            {
                case "1x1":
                    aMin = 1; aMax = (int)Math.Max(9 * multiplier, 9);
                    bMin = 1; bMax = (int)Math.Max(9 * multiplier, 9);
                    break;
                case "2x1":
                    aMin = (int)Math.Max(10, Math.Round(10 * multiplier));
                    aMax = (int)Math.Max(99, Math.Round(30 * multiplier));
                    bMin = 1; bMax = (int)Math.Max(9 * multiplier, 9);
                    break;
                case "2x2":
                    aMin = (int)Math.Max(10, Math.Round(10 * multiplier));
                    aMax = (int)Math.Max(99, Math.Round(70 * multiplier));
                    bMin = (int)Math.Max(10, Math.Round(10 * multiplier));
                    bMax = (int)Math.Max(99, Math.Round(70 * multiplier));
                    break;
                case "3x1":
                    aMin = (int)Math.Max(100, Math.Round(100 * multiplier));
                    aMax = (int)Math.Max(999, Math.Round(300 * multiplier));
                    bMin = 1; bMax = (int)Math.Max(9 * multiplier, 9);
                    break;
                case "3x2":
                    aMin = (int)Math.Max(100, Math.Round(100 * multiplier));
                    aMax = (int)Math.Max(999, Math.Round(500 * multiplier));
                    bMin = (int)Math.Max(10, Math.Round(10 * multiplier));
                    bMax = (int)Math.Max(99, Math.Round(99 * multiplier));
                    break;
                default:
                    aMin = 1; aMax = (int)Math.Max(9 * multiplier, 9);
                    bMin = 1; bMax = (int)Math.Max(9 * multiplier, 9);
                    break;
            }

            if (aMax < aMin) aMax = aMin;
            if (bMax < bMin) bMax = bMin;

            int a = rand.Next(aMin, aMax + 1);
            int b = rand.Next(bMin, bMax + 1);

            if (operation == "-")
            {
                if (b > a)
                {
                    int tmp = a; a = b; b = tmp;
                }
                return new Question(a, b, "-");
            }
            else if (operation == "÷")
            {
                // Make dividend a multiple of divisor for cleaner division facts
                int divisor = b;
                int quotient = Math.Max(1, a);
                int dividend = divisor * quotient;

                // If dividend seems too big/small, fallback to safe picks
                if (dividend <= 0 || dividend > int.MaxValue / 2)
                {
                    divisor = Math.Max(1, rand.Next(1, Math.Max(2, bMax)));
                    quotient = Math.Max(1, rand.Next(1, Math.Max(2, aMax)));
                    dividend = divisor * quotient;
                }

                return new Question(dividend, divisor, "÷");
            }
            else // + or ×
            {
                return new Question(a, b, operation);
            }
        }

        // --- Program / student management (unchanged except for minor comments) ---
        internal class Program
        {
            static List<Student> students = new List<Student>();
            static string studentFile = "students.txt";

            static void Main(string[] args)
            {
                Console.OutputEncoding = System.Text.Encoding.UTF8;
                LoadStudents();
                ShowWelcome();

                while (true)
                {
                    Console.Clear();
                    Console.WriteLine("🌟 Math Learning for Kids 🌟");
                    Console.WriteLine("----------------------------");
                    Console.WriteLine("1. Register");
                    Console.WriteLine("2. Login");
                    Console.WriteLine("3. Exit");
                    Console.Write("Select option: ");
                    string choice = Console.ReadLine();

                    if (choice == "1") RegisterStudent();
                    else if (choice == "2") LoginStudent();
                    else if (choice == "3") break;
                    else
                    {
                        Console.WriteLine("❌ Invalid choice!");
                        Console.ReadKey();
                    }
                }
            }

            static void ShowWelcome()
            {
                Console.Clear();
                Console.WriteLine("=========================================");
                Console.WriteLine("      🎓 WELCOME TO MATH PRACTICE QUIZ!");
                Console.WriteLine("=========================================");
                Console.WriteLine();
                Console.WriteLine("📖 HOW TO PLAY:");
                Console.WriteLine("1️⃣  Enter your name to create or load your profile.");
                Console.WriteLine("2️⃣  Choose your grade (1–3) and difficulty(Easy, Normal, Hard.");
                Console.WriteLine("3️⃣  Choose what kind of math operation to practice.");
                Console.WriteLine("4️⃣  You can use a cheat sheet (if you want help 😁).");
                Console.WriteLine("5️⃣  Answer each question — your score will be saved!");
                Console.WriteLine();
                Console.WriteLine("💾 Each student has their own notebook file to store all quiz results.");
                Console.WriteLine("📁 Example file: 'Gareth_results.txt'");
                Console.WriteLine();
                Console.WriteLine("👉 Press any key to continue...");
                Console.ReadKey();
                Console.Clear();
            }

            static void SaveStudent(Student s)
            {
                if (!students.Contains(s))
                    students.Add(s);

                File.AppendAllText(studentFile, $"{s.Name},{s.StudentID}{Environment.NewLine}");
            }

            static void LoadStudents()
            {
                students.Clear();

                if (!File.Exists(studentFile))
                    return;

                foreach (var line in File.ReadAllLines(studentFile))
                {
                    string[] parts = line.Split(',');
                    if (parts.Length == 2)
                    {
                        if (!students.Any(x => x.StudentID == parts[1]))
                            students.Add(new Student(parts[0], parts[1]));
                    }
                }
            }

            static void RegisterStudent()
            {
                Console.Clear();
                Console.Write("Enter your name: ");
                string name = Console.ReadLine().Trim();

                var existing = students.FirstOrDefault(s => s.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
                if (existing != null)
                {
                    Console.WriteLine($"⚠️ A student with the name '{name}' already exists!");
                    Console.Write("Log in to existing account instead? (y/n): ");
                    string choice = Console.ReadLine().Trim().ToLower();
                    if (choice == "y") { ShowStudentMenu(existing); return; }
                    else Console.WriteLine("You can create a new account with a different name.");
                }

                string id = $"ID-{(students.Count + 1):D4}";
                Student s = new Student(name, id);

                SaveStudent(s);
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"✅ Registered successfully! Your ID is: {id}");
                Console.ResetColor();
                s.Pause("🎯 Press any key to continue...");
            }

            static void LoginStudent()
            {
                Console.Clear();
                Console.WriteLine("📥 Login to your account");
                Console.WriteLine("You can login using:");
                Console.WriteLine("1️⃣ Your Name (e.g., Gareth)");
                Console.WriteLine("2️⃣ Your Student ID (e.g., ID-0003)");
                Console.Write("Enter Name or Student ID: ");
                string input = Console.ReadLine().Trim();

                var student = students.FirstOrDefault(s =>
                    s.Name.Equals(input, StringComparison.OrdinalIgnoreCase) ||
                    s.StudentID.Equals(input, StringComparison.OrdinalIgnoreCase)
                );

                if (student != null) ShowStudentMenu(student);
                else
                {
                    Console.WriteLine("❌ Student not found. Please register first.");
                    Console.ReadKey();
                }
            }

            static void ShowStudentMenu(Student s)
            {
                bool loggedIn = true;
                while (loggedIn)
                {
                    Console.Clear();
                    Console.WriteLine($"👋 Hello {s.Name}! ({s.StudentID})");
                    Console.WriteLine("-----------------------------");
                    Console.WriteLine("1. Take Quiz");
                    Console.WriteLine("2. View My Results");
                    Console.WriteLine("3. Logout");
                    Console.Write("Select option: ");
                    string choice = Console.ReadLine();

                    if (choice == "1") s.TakeQuiz();
                    else if (choice == "2") s.ViewMyResults();
                    else if (choice == "3") loggedIn = false;
                    else
                    {
                        Console.WriteLine("❌ Invalid input. Try again!");
                        Console.ReadKey();
                    }
                }
            }
        }
    }
}
