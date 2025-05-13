using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

public class Kbar
{
    public double a { get; set; }
    public double c { get; set; }
    public string t { get; set; }
    public double v { get; set; }
    public double h { get; set; }
    public string slot { get; set; }
    public double l { get; set; }
    public int n { get; set; }
    public double o { get; set; }
}

public class Root
{
    public Kbar kbar { get; set; }
    public string type { get; set; }
    public string pair { get; set; }
    public string SERVER { get; set; }
    public string TS { get; set; }
}

class Program
{
    static void Main()
    {
        string jsonFilePath = @"path";
        string jsonString = File.ReadAllText(jsonFilePath);

        List<Root> data = JsonSerializer.Deserialize<List<Root>>(jsonString);

        foreach (var item in data)
        {
            Console.WriteLine($"Open: {item.kbar.o}");
            Console.WriteLine($"Close: {item.kbar.c}");
            Console.WriteLine($"High: {item.kbar.h}");
            Console.WriteLine($"Low: {item.kbar.l}");
            Console.WriteLine("----");
        }
    }
}