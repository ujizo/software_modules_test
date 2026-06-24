using System;
using System.IO;
using MySql.Data.MySqlClient;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using System.Data.Common;

Console.WriteLine("Выберите СУБД для подключения:\n1. MySql\n2. Sqlite");
int smbd = int.Parse(Console.ReadLine()!);

IConfiguration config = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .Build();

string connectionString = "";
DbConnection con = null!;

switch (smbd)
{
    case 1:
        connectionString = config["ConnectionStrings:csMySql"]!;
        con = new MySqlConnection(connectionString);
        break;

    case 2:
        connectionString = config["ConnectionStrings:csSqlite"]!;
        con = new SqliteConnection(connectionString);
        break;

    default:
        Console.WriteLine("Неверный выбор СУБД.");
        return;
}

using (con)
{
    try
    {
        con.Open();

        if (con is MySqlConnection)
            Console.WriteLine($"ВЕРСИЯ MYSQL: {con.ServerVersion}");
        else if (con is SqliteConnection)
            Console.WriteLine($"ВЕРСИЯ SQLITE: {con.ServerVersion}");

        CreateTable(con);

        bool running = true;
        while (running)
        {
            Console.WriteLine("\n=== УПРАВЛЕНИЕ ПОЛЬЗОВАТЕЛЯМИ ===");
            Console.WriteLine("1. Показать всех пользователей (READ)");
            Console.WriteLine("2. Добавить пользователя (CREATE)");
            Console.WriteLine("3. Изменить возраст пользователя (UPDATE)");
            Console.WriteLine("4. Удалить пользователя (DELETE)");
            Console.WriteLine("5. Выход");
            Console.Write("Выберите действие: ");

            string choice = Console.ReadLine()!;

            switch (choice)
            {
                case "1":
                    Console.WriteLine("\n--- Список всех пользователей ---");
                    ReadAllUsers(con);
                    break;

                case "2":
                    Console.WriteLine("\n--- Добавление пользователя ---");
                    Console.Write("Введите имя: ");
                    string name = Console.ReadLine()!;
                    Console.Write("Введите возраст: ");
                    if (int.TryParse(Console.ReadLine(), out int age))
                    {
                        InsertUser(con, name, age);
                    }
                    else
                    {
                        Console.WriteLine("Ошибка: возраст должен быть числом!");
                    }
                    break;

                case "3":
                    Console.WriteLine("\n--- Обновление данных ---");
                    Console.Write("Введите ID пользователя: ");
                    if (int.TryParse(Console.ReadLine(), out int updateId))
                    {
                        Console.Write("Введите новый возраст: ");
                        if (int.TryParse(Console.ReadLine(), out int newAge))
                        {
                            UpdateUserAge(con, updateId, newAge);
                        }
                        else
                        {
                            Console.WriteLine("Ошибка: возраст должен быть числом!");
                        }
                    }
                    else
                    {
                        Console.WriteLine("Ошибка: ID должен быть числом!");
                    }
                    break;

                case "4":
                    Console.WriteLine("\n--- Удаление пользователя ---");
                    Console.Write("Введите ID пользователя для удаления: ");
                    if (int.TryParse(Console.ReadLine(), out int deleteId))
                    {
                        DeleteUser(con, deleteId);
                    }
                    else
                    {
                        Console.WriteLine("Ошибка: ID должен быть числом!");
                    }
                    break;

                case "5":
                    running = false;
                    Console.WriteLine("Выход из программы.");
                    break;

                default:
                    Console.WriteLine("Неверный ввод. Попробуйте снова.");
                    break;
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Ошибка в главном потоке: {ex.Message}");
    }
}

static void CreateTable(DbConnection connection)
{
    string sqlRequest;

    if (connection is MySqlConnection)
    {
        sqlRequest = @"
            CREATE TABLE IF NOT EXISTS Users (
                Id INT AUTO_INCREMENT PRIMARY KEY,
                Name VARCHAR(100) NOT NULL,
                Age INT
            );";
    }
    else
    {
        sqlRequest = @"
            CREATE TABLE IF NOT EXISTS Users (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL,
                Age INTEGER
            );";
    }

    using DbCommand command = connection.CreateCommand();
    command.CommandText = sqlRequest;
    command.ExecuteNonQuery();
    Console.WriteLine("Таблица 'Users' успешно проверена/создана!");
}

static void InsertUser(DbConnection connection, string name, int age)
{
    using DbCommand command = connection.CreateCommand();
    command.CommandText = "INSERT INTO Users (Name, Age) VALUES (@name, @age);";

    DbParameter paramName = command.CreateParameter();
    paramName.ParameterName = "@name";
    paramName.Value = name;
    command.Parameters.Add(paramName);

    DbParameter paramAge = command.CreateParameter();
    paramAge.ParameterName = "@age";
    paramAge.Value = age;
    command.Parameters.Add(paramAge);

    int rowsAffected = command.ExecuteNonQuery();
    Console.WriteLine($"Добавлено строк: {rowsAffected} (Имя: {name})");
}

static void ReadAllUsers(DbConnection connection)
{
    using DbCommand command = connection.CreateCommand();
    command.CommandText = "SELECT Id, Name, Age FROM Users;";

    using DbDataReader reader = command.ExecuteReader();
    
    if (reader.HasRows)
    {
        Console.WriteLine($"{"ID",-5} | {"ИМЯ",-15} | {"ВОЗРАСТ",-10}");
        Console.WriteLine(new string('-', 35));
        
        while (reader.Read())
        {
            // Изменено для универсального чтения ID (работает и с int из MySQL, и с long из SQLite)
            int id = Convert.ToInt32(reader.GetValue(0));
            string name = reader.GetString(1);
            object ageValue = reader.IsDBNull(2) ? "N/A" : reader.GetInt32(2);

            Console.WriteLine($"{id,-5} | {name,-15} | {ageValue,-10}");
        }
    }
    else
    {
        Console.WriteLine("Таблица пуста.");
    }
}

static void UpdateUserAge(DbConnection connection, int id, int newAge)
{
    using DbCommand command = connection.CreateCommand();
    command.CommandText = "UPDATE Users SET Age = @age WHERE Id = @id;";

    DbParameter paramAge = command.CreateParameter();
    paramAge.ParameterName = "@age";
    paramAge.Value = newAge;
    command.Parameters.Add(paramAge);

    DbParameter paramId = command.CreateParameter();
    paramId.ParameterName = "@id";
    paramId.Value = id;
    command.Parameters.Add(paramId);

    int rowsAffected = command.ExecuteNonQuery();
    Console.WriteLine($"Обновлено строк: {rowsAffected}. Пользователю с ID {id} установлен возраст {newAge}.");
}

static void DeleteUser(DbConnection connection, int id)
{
    using DbCommand command = connection.CreateCommand();
    command.CommandText = "DELETE FROM Users WHERE Id = @id;";

    DbParameter paramId = command.CreateParameter();
    paramId.ParameterName = "@id";
    paramId.Value = id;
    command.Parameters.Add(paramId);

    int rowsAffected = command.ExecuteNonQuery();
    Console.WriteLine($"Удалено строк: {rowsAffected}. Пользователь с ID {id} удален.");
}
