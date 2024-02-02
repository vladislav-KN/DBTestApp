using Entities;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;
using System.Reflection;

namespace Handlers
{
    public class DbHandler
    {
        private readonly IConfiguration _configuration;

        public DbHandler(IConfiguration configuration) 
        {
            this._configuration = configuration;
        }

        public DbResult AddItems<T>(List<T> items)
        {
            if (items == null || items.Count == 0) return DbResult.INCORRECT_DATA;
            SqlConnection connection = OpenConnection();
            
            foreach (T item in items)
            {
                Dictionary<string, object> columnValues = GetObjectParameters(item);
                try
                {
                    // Формируем запрос INSERT
                    string insertQuery = $"INSERT INTO [dbo].[{typeof(T).Name}] ({string.Join(", ", columnValues.Keys)}) VALUES ({string.Join(", ", columnValues.Keys.Select(key => "@" + key))})";

                    using (SqlCommand command = new SqlCommand(insertQuery, connection))
                    {
                        // Добавляем параметры в запрос
                        foreach (var keyValuePair in columnValues)
                        {
                            command.Parameters.AddWithValue("@" + keyValuePair.Key, keyValuePair.Value);
                        }

                        int rowsAffected = command.ExecuteNonQuery();

                        if (rowsAffected > 0)
                        {
                            Console.WriteLine($"{DateTime.Now}: Запись добавлена в базу данных: {string.Join(", ", columnValues.Values.Select(value => value.ToString()))}");
                        }
                        else
                        {
                            Console.WriteLine($"{DateTime.Now}: Ошибка при добавлении записи в базу данных: {string.Join(", ", columnValues.Values.Select(value => value.ToString()))}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"{DateTime.Now}: Ошибка при добавлении записи в базу данных: {ex.Message}");
                    return DbResult.NOT_PREDICTED_ERROR;
                }
            }

            CloseConnection(connection);
            return DbResult.SUCCESS;  
        }



        public SqlConnection OpenConnection()
        {
            SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("DataBase"));
            try
            {
                
                connection.Open();
                Console.WriteLine($"{DateTime.Now}: Соединение с базой данных установлено успешно.");
                return connection;
            }
            catch (SqlException ex)
            {
                foreach (SqlError error in ex.Errors)
                {
                    if (error.Number == 4060)
                    {
                        SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder(_configuration.GetConnectionString("DataBase"));
                        if (builder.InitialCatalog.Any())
                        {
                            Console.WriteLine($"{DateTime.Now}: Попытка создать базу данных");
                            if (CreateDB())
                            {
                                Console.WriteLine($"{DateTime.Now}:База данных создана попытка повторного подключения");
                                
                                return OpenConnection();
                            }
                            else
                            {
                                Console.WriteLine($"{DateTime.Now}: Не удалось создать базу данных");
                                return null;
                            }
                        }
                        else
                        {
                            Console.WriteLine($"{DateTime.Now}: В строке подключения не указана база данных");
                            return null;
                        }
                    }
                    else
                    {
                        Console.WriteLine($"{DateTime.Now}: Ошибка при подключении к базе данных: {error.Message}");
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{DateTime.Now}: Общая ошибка при подключении к базе данных: {ex.Message}");
                return null;
            }
        }

        //возвращает true если бд создана false в любом другом случае
        public bool CreateDB()
        {
            try
            {
                SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder(_configuration.GetConnectionString("DataBase"));
                using (SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("DataBaseMaster")))
                {
                    connection.Open();
                    string createDatabaseQuery = $"CREATE DATABASE {builder.InitialCatalog}";
                    using (SqlCommand command = new SqlCommand(createDatabaseQuery, connection))
                    {
                        try
                        {
                            command.ExecuteNonQuery();
                            Console.WriteLine($"{DateTime.Now}: База данных {builder.InitialCatalog} успешно создана.") ;
                            
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"{DateTime.Now}: Ошибка при создании таблиц: {ex.Message}");
                        }
                    }
                    connection.Close();
                }
                string createTablesQuery = @"
CREATE TABLE [User] (
    Email NVARCHAR(300) PRIMARY KEY,
    FIO NVARCHAR(MAX)
);

CREATE TABLE [Order] (
    No INT PRIMARY KEY,
    Sum FLOAT,
    RegDate DATE,
    UserEmail NVARCHAR(300),
    FOREIGN KEY (UserEmail) REFERENCES [User](Email)
);

CREATE TABLE [Product] (
    Name NVARCHAR(300) PRIMARY KEY,
    Price FLOAT,
);

CREATE TABLE [OrderProduct] (
    Id INT PRIMARY KEY IDENTITY(1,1),
    OrderNo INT,
    ProductName NVARCHAR(300),
    Quantity INT,
    FOREIGN KEY (OrderNo) REFERENCES [Order](No),
    FOREIGN KEY (ProductName) REFERENCES Product(Name)
);";
                bool result = false;
                using (SqlConnection connection = new SqlConnection(builder.ConnectionString))
                {
                    connection.Open();

                    using (SqlCommand command = new SqlCommand(createTablesQuery, connection))
                    {
                        try
                        {
                            var x = command.ExecuteNonQuery();
                            Console.WriteLine($"{DateTime.Now}: Таблицы успешно созданы.");
                            result = true;
                        }

                        catch (Exception ex)
                        {
                            
                            Console.WriteLine($"{DateTime.Now}:Ошибка при создании таблиц: {ex.Message}");
                            
                        }
                         
                    }
                    connection.Close();

                }
                return result;
            }
 
            catch (Exception ex)
            {
                Console.WriteLine($"{DateTime.Now}: Общая ошибка при подключении к базе данных: {ex.Message}");
                return false;
            }
        }

        public bool CloseConnection(SqlConnection connection)
        {
            try
            {
                if (connection.State == System.Data.ConnectionState.Open)
                {
                    connection.Close();
                    Console.WriteLine($"Соединение с базой данных успешно закрыто.");
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при закрытии соединения с базой данных: {ex.Message}");
            }
            return false;
        }
        public static Dictionary<string, object> GetObjectParameters(object? obj)
        {
            Dictionary<string, object> parameters = new Dictionary<string, object>();

            Type type = obj.GetType();
            PropertyInfo[] properties = type.GetProperties();

            foreach (PropertyInfo property in properties)
            {
                if (!Attribute.IsDefined(property, typeof(ExcludeFromParametersAttribute)))
                {
                    parameters.Add(property.Name, property.GetValue(obj));
                }
            }

            return parameters;
        }
    }
}

public enum DbResult
{
    INCORRECT_DATA,
    SUCCESS,
    NOT_PREDICTED_ERROR
}
