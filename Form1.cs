using System.Data.SQLite;
using System.Windows.Forms;

namespace diplom
{
    // Главная форма приложения для подбора оптимального оборудования ЛВС
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        // Класс для управления базой данных SQLite
        public class DatabaseManager
        {
            private SQLiteConnection dbConnection;
            // Статическое поле для хранения последнего уникального ID записи
            public static int LastRecordId { get; set; } = 0;
            // Статический счетчик для номера конфигурации
            private static int _nextConfigurationNumber = 0;

            // Номер конфигурации для текущего экземпляра формы
            public int CurrentConfigurationNumber { get; private set; }

            // Конструктор класса DatabaseManager
            // connectionString - строка подключения к базе данных
            public DatabaseManager(string connectionString)
            {
                dbConnection = new SQLiteConnection(connectionString);
                dbConnection.Open();
                LastRecordId = GetMaxRecordId();

                _nextConfigurationNumber = GetMaxConfigurationNumber();
                _nextConfigurationNumber++;
                CurrentConfigurationNumber = _nextConfigurationNumber;
            }

            // Получает максимальный ID из таблицы DevicesDatabase
            // Возвращает максимальный ID записи или 0, если таблица пуста
            private int GetMaxRecordId()
            {
                string query = "SELECT MAX(ID) FROM DevicesDatabase";
                using (SQLiteCommand command = new SQLiteCommand(query, dbConnection))
                {
                    object result = command.ExecuteScalar();
                    if (result != DBNull.Value && result != null)
                    {
                        return Convert.ToInt32(result);
                    }
                    return 0; // Если таблица пуста или нет ID, начинаем с 1
                }
            }

            // Получает максимальный номер конфигурации из таблицы DevicesDatabase
            // Возвращает максимальный номер конфигурации или 0, если таблица пуста
            private int GetMaxConfigurationNumber()
            {
                string query = "SELECT MAX(ConfigurationNumber) FROM DevicesDatabase";
                using (SQLiteCommand command = new SQLiteCommand(query, dbConnection))
                {
                    object result = command.ExecuteScalar();
                    if (result != DBNull.Value && result != null)
                    {
                        return Convert.ToInt32(result);
                    }
                    return 0; // Если таблица пуста или нет номеров конфигурации, начинаем с 0
                }
            }

            // Выполняет SQL-запрос и возвращает результат в виде SQLiteDataReader
            // query - SQL-запрос для выполнения
            public SQLiteDataReader ExecuteQuery(string query)
            {
                SQLiteCommand command = new SQLiteCommand(query, dbConnection); // Создаем команду для выполнения запроса
                return command.ExecuteReader(); // Выполняем запрос и возвращаем результат
            }

            // Выполняет команды, не возвращающие результат (INSERT, UPDATE, DELETE)
            // query - SQL-запрос для выполнения
            // parameters - словарь параметров для запроса
            public int ExecuteNonQuery(string query, Dictionary<string, object> parameters = null)
            {
                using (SQLiteCommand command = new SQLiteCommand(query, dbConnection))
                {
                    if (parameters != null)
                    {
                        foreach (var param in parameters)
                        {
                            command.Parameters.AddWithValue(param.Key, param.Value);
                        }
                    }
                    return command.ExecuteNonQuery();
                }
            }

            // Закрывает соединение с базой данных
            public void CloseConnection()
            {
                dbConnection.Close(); // Закрываем соединение с базой данных
            }
        }

        // Обработчик нажатия кнопки "Метод взвешенных сумм"
        private void button1_Click(object sender, EventArgs e)
        {
            Form2 newForm = new Form2();
            newForm.Show();
        }

        // Обработчик нажатия кнопки "Симплекс-метод"
        private void button2_Click(object sender, EventArgs e)
        {
            Form3 newForm = new Form3();
            newForm.Show();
        }

        // Обработчик нажатия кнопки "Генетический алгоритм"
        private void button3_Click(object sender, EventArgs e)
        {
            Form4 newForm = new Form4();
            newForm.Show();
        }

        // Обработчик нажатия кнопки "Просмотр конфигураций"
        private void button4_Click(object sender, EventArgs e)
        {
            Form5 newForm = new Form5();
            newForm.Show();
        }

        private void button5_Click(object sender, EventArgs e)
        {
            Form6 compareForm = new Form6();
            compareForm.Show();
        }
    }
}
