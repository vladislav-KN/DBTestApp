using Entities;
using Handlers;
using Loders;
using Microsoft.Extensions.Configuration;
using System.Data.SqlClient;
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
        Send(dbHandler, xmlLoader);
    }
    else
    {
        Console.WriteLine("Файл не существует.");
        continue;
    }

}
 
static void Send(DbHandler dbHandler, XmlHandler xmlLoader)
{
    xmlLoader.LoadXml();
    xmlLoader.ConvertToDBFormat();
    SqlConnection sqlConnection = dbHandler.OpenConnection();
    var userData = xmlLoader.Users;
    var orderData = xmlLoader.Orders;
    var productData = xmlLoader.Products;
    var orderProdData = xmlLoader.DBOrdersList;
    if(orderProdData == null)
    {
        dbHandler.CloseConnection(sqlConnection);
        return;
    }

    foreach (User user in userData)
    {
        if(dbHandler.AddItem<User>(user, sqlConnection) == DbResult.NOT_SAVED)
        {
            var noList = orderData.Where(x=>x.UserEmail == user.Email).Select(x => x.No);
            //удаляем все order где не получилось добавить пользователя
            orderData = orderData.Where(x => noList.Any(y => y != x.No)).ToList();
            //удаляем всю информацию о товарах в заказе где неполучилось добавить пользователя
            orderProdData = orderProdData.Where(x=> noList.Any(y=>y!=x.OrderNo)).ToList();

        }
    }
    foreach (Product product in productData)
    {
        if (dbHandler.AddItem<Product>(product, sqlConnection) == DbResult.NOT_SAVED)
        {
            //либо удаляем все заказы
            var noList = orderProdData.Where(x => x.ProductName == product.Name).Select(x => x.OrderNo);
            orderData = orderData.Where(x => noList.Any(y => y != x.No)).ToList();
            orderProdData = orderProdData.Where(x => x.ProductName != product.Name).ToList();
            //либо только информацию о товаре в этом заказе 
            orderProdData = orderProdData.Where(x => x.ProductName != product.Name).ToList();

        }
    }

    try
    {
        foreach (Order order in orderData)
        {
            SqlTransaction transaction;
            transaction = sqlConnection.BeginTransaction();

            if (dbHandler.AddItem<Order>(order, sqlConnection, transaction) == DbResult.NOT_SAVED)
            {
                orderProdData = orderProdData.Where(x => x.OrderNo != order.No).ToList();
                transaction.Rollback();
            }

            var opDataToWrite = orderProdData.Where(x => x.OrderNo == order.No);
            int numOfSucsessWrites = 0;
            foreach (OrderProduct opData in orderProdData.Where(x => x.OrderNo == order.No))
            {
                if (dbHandler.AddItem<OrderProduct>(opData, sqlConnection, transaction) == DbResult.SUCCESS)
                    numOfSucsessWrites++;
            }
            //удаляем если нет ни 1 товара в заказе
            if (numOfSucsessWrites == 0) transaction.Rollback();
            else transaction.Commit();

            transaction.Dispose();
        }

    }
    catch (Exception ex)
    {
        Console.WriteLine($"{DateTime.Now}: Ошибка: {ex.Message}");
 
    }
 
    dbHandler.CloseConnection(sqlConnection);  

}
 