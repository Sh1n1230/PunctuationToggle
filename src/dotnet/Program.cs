string? line;
while ((line = Console.ReadLine()) != null)
{
    Console.WriteLine(line.Replace("、", "，").Replace("。", "．"));
}
