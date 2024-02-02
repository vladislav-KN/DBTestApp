using Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using System.Globalization;

namespace Loders
{
    public class XmlHandler
    {
        private readonly string filePath;
        private orders? _orders;
        private List<OrderProduct>? _ordersList;

        public orders? XmlOrders
        {
            get
            {
                return _orders;
            }
        }
        public List<User> Users { get; private set; } = new List<User>();
        public List<Order> Orders { get; private set; } = new List<Order>();
        public List<Product> Products { get; private set; } = new List<Product>();

        public List<OrderProduct>? DBOrdersList
        {
            get
            {
                return _ordersList;
            }
        }
        public XmlHandler(string path)
        {
            this.filePath = path;
            _ordersList = new List<OrderProduct>();
        }

        public void LoadXml()
        {
            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(orders));

                using (XmlReader reader = XmlReader.Create(filePath))
                {
                    _orders = (orders?)serializer.Deserialize(reader);
                }
            }
            catch (InvalidOperationException ex) 
            {
                _orders = null;
            }
        }
        public void ConvertToDBFormat()
        {
            if (_orders == null)
                return;
            int i = 0;
            foreach(var xmlFormatOforder in _orders.Items)
            {
                if(xmlFormatOforder is null) continue;

                User? user = GetUser(xmlFormatOforder,i);
               
                //если некорректное поле users заказ не учитываем
                if (user == null) continue;
                if(!Users.Where(x=>x.Email == user.Email).Any()) Users.Add(user);
                  
                Order? order = GetOrder(xmlFormatOforder, user, i);
                //аналогично пропускаем запись при некорректных данных
                if (order == null) continue;
                Orders.Add(order);

                List<(Product,int)> products = GetProducts(xmlFormatOforder, i);
                if (products == null || products.Count == 0) continue;

                Products = Products.Concat(products.Select(pare => pare.Item1).Where(newProduct => !Products.Any(existingProduct => existingProduct.Name == newProduct.Name))).ToList();

                List<OrderProduct> orderProducts = GetOrderProducts(products, order);
                _ordersList = _ordersList?.Concat(orderProducts).ToList();
                i++;

            }
        }
        private List<OrderProduct> GetOrderProducts(List<(Product, int)> products, Order? order)
        {
            List<OrderProduct> orderProducts = new List<OrderProduct>();
            foreach (var product in products)
            {
                orderProducts.Add(new OrderProduct
                {
                    Order = order, Product = product.Item1, Quantity = product.Item2
                });
            }
            return orderProducts;
        }

        private List<(Product, int)> GetProducts(ordersOrder xmlFormatOforder, int i)
        {
            if (xmlFormatOforder.product == null || xmlFormatOforder.product.Length == 0)
            {
                Console.WriteLine($"{DateTime.Now}: Данные не записаны, в заказе {i} не указаны продукты");
                return null;
            }
            List<(Product, int)> products = new List<(Product, int)>();
            int y = 0;
            foreach(var product in xmlFormatOforder.product)
            {
                float price; 
                if (!float.TryParse(product.price, NumberStyles.Float, CultureInfo.InvariantCulture, out price))
                {
                    Console.WriteLine($"{DateTime.Now}: Данные не записаны, некорректная цена товара {y} у заказа {i} в файле XML");
                    continue;
                }
                string name = product.name;
                if (name == null)
                {
                    Console.WriteLine($"{DateTime.Now}: Данные не записаны, некорректная имя товара {y} у заказа {i} в файле XML");
                    continue;
                }
                int quntity;
                if (!int.TryParse(product.quantity, out quntity))
                {
                    Console.WriteLine($"{DateTime.Now}: Данные не записаны, некорректная цена товара {y} у заказа {i} в файле XML");
                    continue;
                }
                    products.Add( (new Product
                    {
                        Name = name,
                        Price = price,
                 
                    }, quntity)
                );
                y++;
            }
            
            return products;
        }

        private Order? GetOrder(ordersOrder xmlFormatOforder, User? user, int i)
        {
            int NO;
            if (!int.TryParse(xmlFormatOforder.no, out NO)) 
            {
                Console.WriteLine($"{DateTime.Now}: Данные не записаны, отсутствует номер у заказа {i} в файле XML");
                return null;
            }

            DateTime regDate;
            if(!DateTime.TryParse(xmlFormatOforder.reg_date, out regDate))
            {
                Console.WriteLine($"{DateTime.Now}: Данные не записаны, некорректная дата у заказа {i} в файле XML");
                return null;
            }
            float sum;
            if (!float.TryParse(xmlFormatOforder.sum, NumberStyles.Float, CultureInfo.InvariantCulture, out sum))
            {
                Console.WriteLine($"{DateTime.Now}: Данные не записаны, некорректная сумма у заказа {i} в файле XML");
                return null;
            }
            return new Order
            {
                No = NO,
                Sum = sum,
                RegDate = regDate,
                User = user,
            };
        }

        private User? GetUser(ordersOrder xmlFormatOforder, int i)
        {

            if (xmlFormatOforder.user == null)
            {
                Console.WriteLine($"{DateTime.Now}: Данные не записаны, в заказе {i} не указан пользователь");
                return null;
            }

            if (xmlFormatOforder.user.Length != 1) 
            {
                Console.WriteLine($"{DateTime.Now}: Данные не записаны, в заказе {i} ожидался 1 пользователь, получено: {xmlFormatOforder.user.Length}");
                return null;
            }  

            string fio = xmlFormatOforder.user[0].fio;
            if (fio == null)
            {
                Console.WriteLine($"{DateTime.Now}: Данные не записаны, некорректные данные пользователя в заказе {i} в файле XML");
                return null;
            } 

            string email = xmlFormatOforder.user[0].email;
            try
            {
                email = new MailAddress(email).Address;
            }
            catch (FormatException)
            {
                Console.WriteLine($"{DateTime.Now}: Данные не записаны, некорректная почта у пользователя в заказе {i} в файле XML");
                return null;
            }

            return new User()
            {
                FIO = fio,
                Email = email, 
            };
        }
    }
}
