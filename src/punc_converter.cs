using System;

class Program
{
    static void Main()
    {
        string line;
        // 標準入力から1行ずつ読み込み
        while ((line = Console.ReadLine()) != null)
        {
            Console.WriteLine(line.Replace("、", "，").Replace("。", "．"));
        }
    }
}