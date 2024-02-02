using Entities;
using Handlers;
using Loders;
using Microsoft.Extensions.Configuration;
using System.Xml;

namespace DBTest
{
    public class DbClassTest
    {
        DbHandler dbHandler;
        List<User> users;
        List<Order> orders;
        List<OrderProduct> orderProducts;
        List<Product> products; 
        [SetUp]
        public void Setup()
        {
            IConfiguration configuration = new ConfigurationBuilder()
            .AddJsonFile("test_res/appsettings.json", optional: true, reloadOnChange: true)
            .Build();
            dbHandler = new DbHandler(configuration);
            users = new List<User>
            {
                new User { Email = "user1@example.com", FIO = "Иванов Иван" },
                new User { Email = "user2@example.com", FIO = "Петров Петр" },
                new User { Email = "user3@example.com", FIO = "Сидоров Сидор" },
            };
            products = new List<Product>
            {
                new Product { Name = "Product1", Price = 10.0f },
                new Product { Name = "Product2", Price = 20.0f },
                new Product { Name = "Product3", Price = 30.0f },
            };
            orders = new List<Order>();
            foreach (User user in users)
            {
                Order order = new Order
                {
                    No = users.IndexOf(user) + 1,
                    Sum = 100.0f,
                    RegDate = DateTime.Now,
                    User = user
                };

                orders.Add(order);
            }

            orderProducts = new List<OrderProduct>();

            foreach (Order order in orders)
            {
                foreach (Product product in products)
                {
                    OrderProduct orderProduct = new OrderProduct
                    {
                        Order = order,
                        Product = product,
                        Quantity = 2 // Просто пример значения для Quantity, нужно установить фактическое значение
                    };

                    orderProducts.Add(orderProduct);
                }
            }
        }

        [Test]
        public void CreateDataBase()
        {
            dbHandler.CreateDB();
        }
        [Test]
        public void ConnectToDbWithNonExistDb()
        {
            IConfiguration configuration = new ConfigurationBuilder()
            .AddJsonFile("test_res/appsettings2.json", optional: true, reloadOnChange: true)
            .Build();
            new DbHandler(configuration).OpenConnection();
        }
        [Test]
        public void AddElementsToDB()
        {
            Assert.AreEqual(dbHandler.CreateDB(), true);
            Assert.AreEqual(dbHandler.AddItems(users), DbResult.SUCCESS);
            Assert.AreEqual(dbHandler.AddItems(orders), DbResult.SUCCESS);
            Assert.AreEqual(dbHandler.AddItems(products), DbResult.SUCCESS);
            Assert.AreEqual(dbHandler.AddItems(orderProducts), DbResult.SUCCESS);
        }
        [Test]
        public void GetAtributes()
        {
            DateTime dtValid = DateTime.Now;
            var user = new User { Email = "user1@example.com", FIO = "Иванов Иван" };
            Order order = new Order
            {
                No = 1,
                Sum = 100.01f,
                RegDate = dtValid,
                User = user
            };
            Dictionary<string, object> validateDict = new Dictionary<string, object>
            {
                { "No", 1},
                { "Sum", 100.01f},
                { "RegDate",  dtValid},
                { "UserEmail", "user1@example.com"},

            };
            var testValue = DbHandler.GetObjectParameters(order);

            Assert.That(testValue.Count, Is.EqualTo(validateDict.Count));

            foreach (var kvp in validateDict)
            {
                Assert.IsTrue(testValue.ContainsKey(kvp.Key));
                Assert.That(testValue[kvp.Key], Is.EqualTo(kvp.Value));
            }

        }
    }
}