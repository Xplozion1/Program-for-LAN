using System;
using System.Data.SQLite;
using System.Data;
using System.Windows.Forms;
using static diplom.Form1;

namespace diplom
{
    public partial class Form5 : Form // Форма для отображения найденных конфигураций
    {
        public Form5()
        {
            InitializeComponent();
        }

        private void Form5_Load(object sender, EventArgs e)
        {
            string connectionString = "Data Source=A:\\DevicesDatabase;Version=3;"; // Строка подключения к базе данных
            DatabaseManager dbManager = null; // Менеджер базы данных

            try
            {
                dbManager = new DatabaseManager(connectionString); // Инициализация менеджера базы данных

                string query = "SELECT ID, ConfigurationNumber, DeviceType, TechnicalChar, OptimizationMethod FROM DevicesDatabase ORDER BY ConfigurationNumber, ID"; // SQL-запрос для получения данных

                using (SQLiteDataReader reader = dbManager.ExecuteQuery(query)) // Выполнение запроса
                {
                    DataTable dt = new DataTable(); // Создание таблицы данных

                    // Добавление столбцов с русскими названиями
                    dt.Columns.Add("ID", typeof(int)); // Столбец ID
                    dt.Columns.Add("Номер конфигурации", typeof(int)); // Столбец номера конфигурации
                    dt.Columns.Add("Тип устройства", typeof(string)); // Столбец типа устройства
                    dt.Columns.Add("Технические характеристики", typeof(string)); // Столбец технических характеристик
                    dt.Columns.Add("Метод оптимизации", typeof(string)); // Столбец метода оптимизации

                    while (reader.Read()) // Чтение данных из результата запроса
                    {
                        DataRow row = dt.NewRow(); // Создание новой строки
                        row["ID"] = reader["ID"]; // Заполнение ID
                        row["Номер конфигурации"] = reader["ConfigurationNumber"]; // Заполнение номера конфигурации
                        row["Тип устройства"] = reader["DeviceType"]; // Заполнение типа устройства
                        row["Технические характеристики"] = reader["TechnicalChar"]; // Заполнение технических характеристик
                        row["Метод оптимизации"] = reader["OptimizationMethod"]; // Заполнение метода оптимизации
                        dt.Rows.Add(row); // Добавление строки в таблицу
                    }

                    dataGridView1.DataSource = dt; // Установка источника данных для таблицы
                }
            }
            catch (Exception ex) // Обработка ошибок
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error); // Отображение сообщения об ошибке
            }
            finally
            {
                if (dbManager != null) // Закрытие соединения с базой данных
                {
                    dbManager.CloseConnection();
                }
            }
        }

        // Обработчик нажатия кнопки сравнения конфигураций

    }
}
