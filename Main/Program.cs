using Entities;
using Handlers;
using Loders;
using Microsoft.Extensions.Configuration;
using System.Xml;
// See https://aka.ms/new-console-template for more information


IConfiguration configuration = new ConfigurationBuilder()
        .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
        .Build();
DbHandler dbHandler = new DbHandler(configuration);
dbHandler.CreateDB();
while (true)
{
    Console.WriteLine("Введите полный путь к xml файлу");
    string filePath  = Console.ReadLine();
    if (File.Exists(filePath))
    {
        XmlHandler xmlLoader = new XmlHandler(filePath);
        xmlLoader.LoadXml();
        xmlLoader.ConvertToDBFormat();
        if (xmlLoader.DBOrdersList != null)
        {
            dbHandler.AddItems(xmlLoader.Users);
            dbHandler.AddItems(xmlLoader.Orders);
            dbHandler.AddItems(xmlLoader.Products);
            dbHandler.AddItems(xmlLoader.DBOrdersList);
        }
    }
    else
    {
        Console.WriteLine("Файл не существует.");
        continue;
    }

}