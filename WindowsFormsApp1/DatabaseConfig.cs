using System;
using System.IO;
using System.Windows.Forms;
using System.Xml.Serialization;

namespace WindowsFormsApp1
{
    public static class DatabaseConfig
    {
        private static string configFilePath = Path.Combine(Application.StartupPath, "dbconfig.xml");

        public static string Server { get; set; } = "localhost";
        public static string Database { get; set; } = "flower_shop";
        public static string UserId { get; set; } = "root";
        public static string Password { get; set; } = "";

        public static string ConnectionString =>
            $"Server={Server};Database={Database};Uid={UserId};Pwd={Password};charset=utf8mb4;";

        // Загрузка из файла
        public static void Load()
        {
            if (!File.Exists(configFilePath)) return;

            try
            {
                var serializer = new XmlSerializer(typeof(ConfigData));
                using (var fs = new FileStream(configFilePath, FileMode.Open))
                {
                    var data = (ConfigData)serializer.Deserialize(fs);
                    Server = data.Server;
                    Database = data.Database;
                    UserId = data.UserId;
                    Password = data.Password;
                }
            }
            catch { /* ошибка – остаются значения по умолчанию */ }
        }

        // Сохранение в файл (именно этот метод вызывается из формы настроек)
        public static void Save(string server, string database, string userId, string password)
        {
            Server = server;
            Database = database;
            UserId = userId;
            Password = password;

            try
            {
                var serializer = new XmlSerializer(typeof(ConfigData));
                using (var fs = new FileStream(configFilePath, FileMode.Create))
                {
                    var data = new ConfigData
                    {
                        Server = server,
                        Database = database,
                        UserId = userId,
                        Password = password
                    };
                    serializer.Serialize(fs, data);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка сохранения: " + ex.Message);
            }
        }
    }

    [Serializable]
    public class ConfigData
    {
        public string Server { get; set; }
        public string Database { get; set; }
        public string UserId { get; set; }
        public string Password { get; set; }
    }
}