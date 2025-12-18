using System.Data.SQLite;
using System.Data;
using System.Text;
using static diplom.Form1;

namespace diplom
{
    public partial class Form2 : Form
    {
        private DatabaseManager dbManager;
        private bool isFirstSave = true; // Флаг для отслеживания первого сохранения в сессии
        private int currentConfigurationNumberForSession; // Поле для хранения номера конфигурации для текущей сессии формы
        private static readonly Dictionary<string, string> DeviceTypeRu = new Dictionary<string, string>
        {
            { "Switches", "коммутатор" },
            { "Routers", "маршрутизатор" },
            { "Cameras", "IP-камера" },
            { "Servers", "сервер" },
            { "Cables", "кабель" }
        };
        private static readonly Dictionary<string, string> DeviceFieldRu = new Dictionary<string, string>
        {
            { "PortCount", "Количество портов" },
            { "Speed", "Скорость" },
            { "MACAddresses", "Количество записей MAC адресов" },
            { "PowerConsumption", "Потребляемая мощность" },
            { "Price", "Цена" },
            { "RAM", "Объем оперативной памяти" },
            { "CPUfrequency", "Частота процессора" },
            { "Resolution", "Разрешение" },
            { "FPS", "Частота кадров" },
            { "MatrixPixels", "Число пикселей матрицы" },
            { "ViewingAngle", "Угол обзора" },
            { "CPUCores", "Количество ядер" },
            { "NumberHardSlots", "Количество слотов для жестких дисков" },
            { "MaxStorage", "Максимальный объем памяти" },
            { "WireGauge", "Площадь поперечного сечения" },
            { "SpeedCable", "Скорость передачи данных" },
            { "PriceCable", "Цена за метр" },
            { "Shielding", "Экранирование" }
        };

        public Form2()
        {
            InitializeComponent();
            string connectionString = "Data Source=A:\\DevicesDatabase;Version=3;";

            // При создании формы, получаем номер конфигурации для этой сессии из DatabaseManager
            dbManager = new DatabaseManager(connectionString);
            currentConfigurationNumberForSession = dbManager.CurrentConfigurationNumber;

            // Добавляем обработчики событий для чекбоксов
            checkBox1.CheckedChanged += (s, e) => ToggleDeviceControls("Switches", checkBox1.Checked);
            checkBox2.CheckedChanged += (s, e) => ToggleDeviceControls("Routers", checkBox2.Checked);
            checkBox3.CheckedChanged += (s, e) => ToggleDeviceControls("Cameras", checkBox3.Checked);
            checkBox4.CheckedChanged += (s, e) => ToggleDeviceControls("Servers", checkBox4.Checked);
            checkBox5.CheckedChanged += (s, e) => ToggleDeviceControls("Cables", checkBox5.Checked);

            // Изначально отключаем все элементы управления
            ToggleDeviceControls("Switches", false);
            ToggleDeviceControls("Routers", false);
            ToggleDeviceControls("Cameras", false);
            ToggleDeviceControls("Servers", false);
            ToggleDeviceControls("Cables", false);

            // Заполняем checkedListBoxShielding типами экранирования
            checkedListBoxShielding.Items.Clear();
            checkedListBoxShielding.Items.AddRange(new object[] { "U/UTP", "F/UTP", "SF/UTP", "S/FTP", "U/FTP", "F/FTP" });

            // После checkedListBoxShielding.Items.AddRange(...)
            WiFi_SFP.Items.Clear();
            WiFi_SFP.Items.Add("Поддержка Wi-Fi");
            WiFi_SFP.Items.Add("Поддержка оптоволокна (SFP)");

            // В конце конструктора после инициализации WiFi_SFP
            WiFi_SFP.Enabled = false;
            checkBox2.CheckedChanged += (s, e) => WiFi_SFP.Enabled = checkBox2.Checked;
        }

        private void ToggleDeviceControls(string deviceType, bool enabled)
        {
            switch (deviceType)
            {
                case "Switches":
                    textBoxPortWeight.Enabled = enabled;
                    textBoxSpeedWeight.Enabled = enabled;
                    textBoxMACWeight.Enabled = enabled;
                    textBoxPowerWeight.Enabled = enabled;
                    textBoxPriceWeight.Enabled = enabled;
                    CalculateSwitch.Enabled = enabled;
                    SFP.Enabled = enabled;
                    break;
                case "Routers":
                    textBoxRouterPortWeight.Enabled = enabled;
                    textBoxRouterSpeedWeight.Enabled = enabled;
                    textBoxRAMWeight.Enabled = enabled;
                    textBoxRouterCPUWeight.Enabled = enabled;
                    textBoxRouterPriceWeight.Enabled = enabled;
                    button2.Enabled = enabled;
                    break;
                case "Cameras":
                    textBoxCameraResolutionWeight.Enabled = enabled;
                    textBoxCameraFPSWeight.Enabled = enabled;
                    textBoxCameraMatrixWeight.Enabled = enabled;
                    textBoxCameraAngleWeight.Enabled = enabled;
                    textBoxCameraPriceWeight.Enabled = enabled;
                    button3.Enabled = enabled;
                    break;
                case "Servers":
                    textBoxServerCPUfreqWeight.Enabled = enabled;
                    textBoxServerCoresWeight.Enabled = enabled;
                    textBoxServerSlotsWeight.Enabled = enabled;
                    textBoxServerMaxStorageWeight.Enabled = enabled;
                    textBoxServerPriceWeight.Enabled = enabled;
                    button4.Enabled = enabled;
                    break;
                case "Cables":
                    textBoxWireGaugeCable.Enabled = enabled;
                    textBoxCableSpeed.Enabled = enabled;
                    textBoxCablePrice.Enabled = enabled;
                    checkedListBoxShielding.Enabled = enabled;
                    CalculateCable.Enabled = enabled;
                    break;
            }
        }

        // Метод для нормализации значений с использованием минимаксного масштабирования
        private List<double> Normalize(List<double> values)
        {
            double minVal = values.Min();
            double maxVal = values.Max();
            if (maxVal == minVal)
                return values.Select(x => 0.0).ToList(); // Все значения одинаковые, нормализуем как 0.0
            return values.Select(x => (x - minVal) / (maxVal - minVal)).ToList();
        }

        private (string Model, double Score) CalculateWeightedSum(string tableName, (string criteria, double weight)[] criteriaWeights, string customQuery = null)
        {
            string query = customQuery ?? $"SELECT * FROM {tableName}";
            List<(string Model, double Score)> results = new List<(string Model, double Score)>();

            // Объявление переменной data вне блока using
            var data = new Dictionary<string, List<double>>();
            foreach (var (criteria, _) in criteriaWeights)
            {
                data[criteria] = new List<double>();
            }
            // Первый блок using для сбора данных
            using (SQLiteDataReader reader = dbManager.ExecuteQuery(query))
            {
                // Чтение данных из базы данных
                while (reader.Read())
                {
                    foreach (var (criteria, _) in criteriaWeights)
                    {
                        data[criteria].Add(Convert.ToDouble(reader[criteria]));
                    }
                }
            }

            // Нормализация данных
            var normalizedData = data.ToDictionary(
                kvp => kvp.Key,
                kvp => Normalize(kvp.Value)
            );

            // Второй блок using для расчета взвешенных сумм
            using (SQLiteDataReader reader = dbManager.ExecuteQuery(query))
            {
                int currentIndex = 0;
                while (reader.Read())
                {
                    string modelName = reader["ModelName"].ToString();
                    double score = 0;

                    foreach (var (criteria, weight) in criteriaWeights)
                    {
                        // Получение нормализованного значения для текущего критерия
                        double normalizedValue = normalizedData[criteria][currentIndex];
                        score += normalizedValue * weight;
                    }

                    results.Add((modelName, score));
                    currentIndex++;
                }
            }

            // Возвращение оптимальной модели с наивысшей взвешенной суммой
            return results.OrderByDescending(r => r.Score).FirstOrDefault();
        }

        // Метод для сохранения результата оптимизации в базу данных
        private void SaveOptimizationResult(int configId, string deviceType, string technicalChar, string optimizationMethod)
        {
            string query = "INSERT INTO DevicesDatabase (ID, ConfigurationNumber, DeviceType, TechnicalChar, OptimizationMethod) VALUES (@id, @configNumber, @deviceType, @technicalChar, @optimizationMethod)";
            // Используем метод DatabaseManager для выполнения не-SELECT запроса
            var parameters = new Dictionary<string, object>
            {
                { "@id", configId },
                { "@configNumber", currentConfigurationNumberForSession },
                { "@deviceType", deviceType },
                { "@technicalChar", technicalChar },
                { "@optimizationMethod", optimizationMethod }
            };
            // Проверяем, что dbManager и соединение активны перед выполнением запроса
            if (dbManager != null)
            {
                int rowsAffected = dbManager.ExecuteNonQuery(query, parameters); // Предполагается наличие такого метода в DatabaseManager
                MessageBox.Show($"Запрос выполнен. Добавлено строк: {rowsAffected}", "Сохранение данных", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show("Ошибка: Соединение с базой данных не установлено.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private Dictionary<string, object> GetDataFromDatabase(string tableName, string customFilter = null)
        {
            if (tableName == null) throw new ArgumentNullException(nameof(tableName));

            var data = new Dictionary<string, List<double>>();
            var modelNames = new List<string>();

            // Определяем столбцы в зависимости от типа устройства
            string[] columns = tableName switch
            {
                "Switches" => new[] { "PortCount", "Speed", "MACAddresses", "PowerConsumption", "Price" },
                "Routers" => new[] { "PortCount", "Speed", "RAM", "CPUfrequency", "Price" },
                "Cameras" => new[] { "Resolution", "FPS", "MatrixPixels", "ViewingAngle", "Price" },
                "Servers" => new[] { "CPUfrequency", "CPUCores", "NumberHardSlots", "MaxStorage", "Price" },
                _ => throw new ArgumentException("Неизвестная таблица")
            };

            // Инициализируем списки для каждого критерия
            foreach (var column in columns)
            {
                data[column] = new List<double>();
            }

            string query = $"SELECT ModelName, {string.Join(", ", columns)} FROM {tableName}";
            if (!string.IsNullOrEmpty(customFilter))
                query += $" WHERE {customFilter}";
            using (SQLiteDataReader reader = dbManager.ExecuteQuery(query))
            {
                while (reader.Read())
                {
                    try
                    {
                        modelNames.Add(reader.GetString(0)); // Получаем название модели
                        int columnOffset = 1; // Смещение на 1 из-за ModelName
                        foreach (var column in columns)
                        {
                            double value = reader.GetDouble(columnOffset++);
                            data[column].Add(value);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка при чтении данных: {ex.Message}");
                        return null;
                    }
                }
            }

            // Преобразуем List<double> в double[]
            var result = new Dictionary<string, object>();
            foreach (var kvp in data)
            {
                result[kvp.Key] = kvp.Value.ToArray();
            }
            // Добавляем массив названий моделей как строковый массив
            result["ModelNames"] = modelNames.ToArray();

            return result;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            string customFilter = null;
            string customQuery = null;
            if (checkBox1.Checked && SFP.Checked)
            {
                customFilter = "SFP = 'да'";
                customQuery = "SELECT * FROM Switches WHERE SFP = 'да'";
            }
            var optimalSwitch = CalculateWeightedSum(
                "Switches",
                new (string, double)[]
                {
                    ("PortCount", double.Parse(textBoxPortWeight.Text)),
                    ("Speed", double.Parse(textBoxSpeedWeight.Text)),
                    ("MACAddresses", double.Parse(textBoxMACWeight.Text)),
                    ("PowerConsumption", double.Parse(textBoxPowerWeight.Text)),
                    ("Price", double.Parse(textBoxPriceWeight.Text))
                },
                customQuery
            );

            StringBuilder result = new StringBuilder();
            result.AppendLine($"Результат взвешенной суммы: {optimalSwitch.Score:F2}");
            result.AppendLine($"Оптимальный {DeviceTypeRu["Switches"]}: {optimalSwitch.Model}");
            result.AppendLine("\nХарактеристики выбранной модели:");

            var allData = GetDataFromDatabase("Switches", customFilter);
            var modelNames = (string[])allData["ModelNames"];
            int modelIndex = Array.IndexOf(modelNames, optimalSwitch.Model);

            // Получаем значение SFP для выбранной модели
            string sfpValue = "нет";
            try
            {
                string safeModel = optimalSwitch.Model.Replace("'", "''");
                string sfpQuery = $"SELECT SFP FROM Switches WHERE ModelName = '{safeModel}' LIMIT 1";
                using (var reader = dbManager.ExecuteQuery(sfpQuery))
                {
                    if (reader.Read())
                    {
                        sfpValue = reader["SFP"].ToString();
                    }
                }
            }
            catch { /* если что-то не так, просто оставим 'нет' */ }

            var integerFields = new HashSet<string> {
                "PortCount",
                "MACAddresses",
                "CPUCores",
                "NumberHardSlots",
                "FPS",
                "PowerConsumption",
                "Price",
                "MatrixPixels",
                "MaxStorage",
                "Resolution"
            };

            // Словари единиц измерения для каждого типа устройства
            var unitsSwitches = new Dictionary<string, string> {
                {"PortCount", "шт."},
                {"Speed", "Гбит/с"},
                {"MACAddresses", "записей"},
                {"PowerConsumption", "Вт"},
                {"Price", "руб."}
            };
            var unitsRouters = new Dictionary<string, string> {
                {"PortCount", "шт."},
                {"Speed", "Гбит/с"},
                {"RAM", "ГБ"},
                {"CPUfrequency", "ГГц"},
                {"Price", "руб."}
            };
            var unitsCameras = new Dictionary<string, string> {
                {"Resolution", "пикселей"},
                {"FPS", "кадр/с"},
                {"MatrixPixels", "Мп"},
                {"ViewingAngle", "град."},
                {"Price", "руб."}
            };
            var unitsServers = new Dictionary<string, string> {
                {"CPUfrequency", "ГГц"},
                {"CPUCores", "шт."},
                {"NumberHardSlots", "шт."},
                {"MaxStorage", "ГБ"},
                {"Price", "руб."}
            };
            Dictionary<string, string> units = null;
            if (allData.Keys.Contains("PortCount") && allData.Keys.Contains("MACAddresses")) units = unitsSwitches;
            else if (allData.Keys.Contains("RAM")) units = unitsRouters;
            else if (allData.Keys.Contains("Resolution")) units = unitsCameras;
            else if (allData.Keys.Contains("CPUCores")) units = unitsServers;

            foreach (var key in allData.Keys.Where(k => k != "ModelNames" && k != "CharacteristicsCount"))
            {
                var values = (double[])allData[key];
                string ruName = DeviceFieldRu.ContainsKey(key) ? DeviceFieldRu[key] : key;
                string formattedValue = integerFields.Contains(key)
                    ? values[modelIndex].ToString("F0")
                    : values[modelIndex].ToString("F2");
                string unit = units != null && units.ContainsKey(key) ? $" {units[key]}" : "";
                result.AppendLine($"{ruName}: {formattedValue}{unit}");
            }

            // Добавляем строку о поддержке оптоволокна
            result.AppendLine($"Поддержка оптоволокна (SFP): {sfpValue}");

            MessageBox.Show(result.ToString(), "Результат оптимизации");

            // Генерируем новый уникальный ID для этой записи
            DatabaseManager.LastRecordId++;
            int uniqueId = DatabaseManager.LastRecordId;

            SaveOptimizationResult(uniqueId, DeviceTypeRu["Switches"], result.ToString().Replace($"Результат взвешенной суммы: {optimalSwitch.Score:F2}\n", "").Replace($"Оптимальный {DeviceTypeRu["Switches"]}: {optimalSwitch.Model}\n\nХарактеристики выбранной модели:\n", ""), "Метод взвешенных сумм");
        }

        private void Form_Closed(object sender, FormClosedEventArgs e)
        {
            dbManager.CloseConnection();
        }

        private void label7_Click(object sender, EventArgs e)
        {

        }

        private void label11_Click(object sender, EventArgs e)
        {

        }

        private void Form2_Load(object sender, EventArgs e)
        {

        }

        private void button2_Click_1(object sender, EventArgs e)
        {
            string customFilter = null;
            string customQuery = null;
            if (checkBox2.Checked && WiFi_SFP.CheckedItems.Count > 0)
            {
                var filters = new List<string>();
                foreach (var item in WiFi_SFP.CheckedItems)
                {
                    if (item.ToString().Contains("Wi-Fi"))
                        filters.Add("WiFi = 'да'");
                    if (item.ToString().Contains("SFP"))
                        filters.Add("SFP = 'да'");
                }
                if (filters.Count > 0)
                {
                    customFilter = string.Join(" AND ", filters);
                    customQuery = $"SELECT * FROM Routers WHERE {customFilter}";
                }
            }
            var optimalRouter = CalculateWeightedSum(
                "Routers",
                new (string, double)[]
                {
                    ("PortCount", double.Parse(textBoxRouterPortWeight.Text)),
                    ("Speed", double.Parse(textBoxRouterSpeedWeight.Text)),
                    ("RAM", double.Parse(textBoxRAMWeight.Text)),
                    ("CPUfrequency", double.Parse(textBoxRouterCPUWeight.Text)),
                    ("Price", double.Parse(textBoxRouterPriceWeight.Text))
                },
                customQuery
            );

            StringBuilder result = new StringBuilder();
            result.AppendLine($"Результат взвешенной суммы: {optimalRouter.Score:F2}");
            result.AppendLine($"Оптимальный {DeviceTypeRu["Routers"]}: {optimalRouter.Model}");
            result.AppendLine("\nХарактеристики выбранной модели:");

            var allData = GetDataFromDatabase("Routers", customFilter);
            var modelNames = (string[])allData["ModelNames"];
            int modelIndex = Array.IndexOf(modelNames, optimalRouter.Model);

            var integerFields = new HashSet<string> {
                "PortCount",
                "MACAddresses",
                "CPUCores",
                "NumberHardSlots",
                "FPS",
                "PowerConsumption",
                "Price",
                "MatrixPixels",
                "MaxStorage",
                "Resolution"
            };

            // Словари единиц измерения для каждого типа устройства
            var unitsSwitches = new Dictionary<string, string> {
                {"PortCount", "шт."},
                {"Speed", "Гбит/с"},
                {"MACAddresses", "записей"},
                {"PowerConsumption", "Вт"},
                {"Price", "руб."}
            };
            var unitsRouters = new Dictionary<string, string> {
                {"PortCount", "шт."},
                {"Speed", "Гбит/с"},
                {"RAM", "ГБ"},
                {"CPUfrequency", "ГГц"},
                {"Price", "руб."}
            };
            var unitsCameras = new Dictionary<string, string> {
                {"Resolution", "пикселей"},
                {"FPS", "кадр/с"},
                {"MatrixPixels", "Мп"},
                {"ViewingAngle", "град."},
                {"Price", "руб."}
            };
            var unitsServers = new Dictionary<string, string> {
                {"CPUfrequency", "ГГц"},
                {"CPUCores", "шт."},
                {"NumberHardSlots", "шт."},
                {"MaxStorage", "ГБ"},
                {"Price", "руб."}
            };
            Dictionary<string, string> units = null;
            if (allData.Keys.Contains("PortCount") && allData.Keys.Contains("MACAddresses")) units = unitsSwitches;
            else if (allData.Keys.Contains("RAM")) units = unitsRouters;
            else if (allData.Keys.Contains("Resolution")) units = unitsCameras;
            else if (allData.Keys.Contains("CPUCores")) units = unitsServers;

            foreach (var key in allData.Keys.Where(k => k != "ModelNames" && k != "CharacteristicsCount"))
            {
                var values = (double[])allData[key];
                string ruName = DeviceFieldRu.ContainsKey(key) ? DeviceFieldRu[key] : key;
                string formattedValue = integerFields.Contains(key)
                    ? values[modelIndex].ToString("F0")
                    : values[modelIndex].ToString("F2");
                string unit = units != null && units.ContainsKey(key) ? $" {units[key]}" : "";
                result.AppendLine($"{ruName}: {formattedValue}{unit}");
            }

            // В конце button2_Click_1 после определения modelIndex
            string wifiValue = "нет";
            string sfpValue = "нет";
            try
            {
                string safeModel = optimalRouter.Model.Replace("'", "''");
                string query = $"SELECT WiFi, SFP FROM Routers WHERE ModelName = '{safeModel}' LIMIT 1";
                using (var reader = dbManager.ExecuteQuery(query))
                {
                    if (reader.Read())
                    {
                        wifiValue = reader["WiFi"].ToString();
                        sfpValue = reader["SFP"].ToString();
                    }
                }
            }
            catch { /* если что-то не так, просто оставим 'нет' */ }
            result.AppendLine($"Поддержка Wi-Fi: {wifiValue}");
            result.AppendLine($"Поддержка оптоволокна (SFP): {sfpValue}");

            MessageBox.Show(result.ToString(), "Результат оптимизации");

            // Генерируем новый уникальный ID для этой записи
            DatabaseManager.LastRecordId++;
            int uniqueId = DatabaseManager.LastRecordId;

            SaveOptimizationResult(uniqueId, DeviceTypeRu["Routers"], result.ToString().Replace($"Результат взвешенной суммы: {optimalRouter.Score:F2}\n", "").Replace($"Оптимальный {DeviceTypeRu["Routers"]}: {optimalRouter.Model}\n\nХарактеристики выбранной модели:\n", ""), "Метод взвешенных сумм");
        }

        private void button3_Click(object sender, EventArgs e)
        {
            var optimalCamera = CalculateWeightedSum(
                "Cameras",
                new (string, double)[]
                {
                    ("Resolution", double.Parse(textBoxCameraResolutionWeight.Text)),
                    ("FPS", double.Parse(textBoxCameraFPSWeight.Text)),
                    ("MatrixPixels", double.Parse(textBoxCameraMatrixWeight.Text)),
                    ("ViewingAngle", double.Parse(textBoxCameraAngleWeight.Text)),
                    ("Price", double.Parse(textBoxCameraPriceWeight.Text))
                }
            );

            StringBuilder result = new StringBuilder();
            result.AppendLine($"Результат взвешенной суммы: {optimalCamera.Score:F2}");
            result.AppendLine($"Оптимальная {DeviceTypeRu["Cameras"]}: {optimalCamera.Model}");
            result.AppendLine("\nХарактеристики выбранной модели:");

            var allData = GetDataFromDatabase("Cameras");
            var modelNames = (string[])allData["ModelNames"];
            int modelIndex = Array.IndexOf(modelNames, optimalCamera.Model);

            var integerFields = new HashSet<string> {
                "PortCount",
                "MACAddresses",
                "CPUCores",
                "NumberHardSlots",
                "FPS",
                "PowerConsumption",
                "Price",
                "MatrixPixels",
                "MaxStorage",
                "Resolution"
            };

            // Словари единиц измерения для каждого типа устройства
            var unitsSwitches = new Dictionary<string, string> {
                {"PortCount", "шт."},
                {"Speed", "Гбит/с"},
                {"MACAddresses", "записей"},
                {"PowerConsumption", "Вт"},
                {"Price", "руб."}
            };
            var unitsRouters = new Dictionary<string, string> {
                {"PortCount", "шт."},
                {"Speed", "Гбит/с"},
                {"RAM", "ГБ"},
                {"CPUfrequency", "ГГц"},
                {"Price", "руб."}
            };
            var unitsCameras = new Dictionary<string, string> {
                {"Resolution", "пикселей"},
                {"FPS", "кадр/с"},
                {"MatrixPixels", "Мп"},
                {"ViewingAngle", "град."},
                {"Price", "руб."}
            };
            var unitsServers = new Dictionary<string, string> {
                {"CPUfrequency", "ГГц"},
                {"CPUCores", "шт."},
                {"NumberHardSlots", "шт."},
                {"MaxStorage", "ГБ"},
                {"Price", "руб."}
            };
            Dictionary<string, string> units = null;
            if (allData.Keys.Contains("PortCount") && allData.Keys.Contains("MACAddresses")) units = unitsSwitches;
            else if (allData.Keys.Contains("RAM")) units = unitsRouters;
            else if (allData.Keys.Contains("Resolution")) units = unitsCameras;
            else if (allData.Keys.Contains("CPUCores")) units = unitsServers;

            foreach (var key in allData.Keys.Where(k => k != "ModelNames" && k != "CharacteristicsCount"))
            {
                var values = (double[])allData[key];
                string ruName = DeviceFieldRu.ContainsKey(key) ? DeviceFieldRu[key] : key;
                string formattedValue = integerFields.Contains(key)
                    ? values[modelIndex].ToString("F0")
                    : values[modelIndex].ToString("F2");
                string unit = units != null && units.ContainsKey(key) ? $" {units[key]}" : "";
                result.AppendLine($"{ruName}: {formattedValue}{unit}");
            }

            MessageBox.Show(result.ToString(), "Результат оптимизации");

            // Генерируем новый уникальный ID для этой записи
            DatabaseManager.LastRecordId++;
            int uniqueId = DatabaseManager.LastRecordId;

            SaveOptimizationResult(uniqueId, DeviceTypeRu["Cameras"], result.ToString().Replace($"Результат взвешенной суммы: {optimalCamera.Score:F2}\n", "").Replace($"Оптимальная {DeviceTypeRu["Cameras"]}: {optimalCamera.Model}\n\nХарактеристики выбранной модели:\n", ""), "Метод взвешенных сумм");
        }

        private void button4_Click(object sender, EventArgs e)
        {
            var optimalServer = CalculateWeightedSum(
                "Servers",
                new (string, double)[]
                {
                    ("CPUfrequency", double.Parse(textBoxServerCPUfreqWeight.Text)),
                    ("CPUCores", double.Parse(textBoxServerCoresWeight.Text)),
                    ("NumberHardSlots", double.Parse(textBoxServerSlotsWeight.Text)),
                    ("MaxStorage", double.Parse(textBoxServerMaxStorageWeight.Text)),
                    ("Price", double.Parse(textBoxServerPriceWeight.Text))
                }
            );

            StringBuilder result = new StringBuilder();
            result.AppendLine($"Результат взвешенной суммы: {optimalServer.Score:F2}");
            result.AppendLine($"Оптимальный {DeviceTypeRu["Servers"]}: {optimalServer.Model}");
            result.AppendLine("\nХарактеристики выбранной модели:");

            var allData = GetDataFromDatabase("Servers");
            var modelNames = (string[])allData["ModelNames"];
            int modelIndex = Array.IndexOf(modelNames, optimalServer.Model);

            var integerFields = new HashSet<string> {
                "PortCount",
                "MACAddresses",
                "CPUCores",
                "NumberHardSlots",
                "FPS",
                "PowerConsumption",
                "Price",
                "MatrixPixels",
                "MaxStorage",
                "Resolution"
            };

            // Словари единиц измерения для каждого типа устройства
            var unitsSwitches = new Dictionary<string, string> {
                {"PortCount", "шт."},
                {"Speed", "Гбит/с"},
                {"MACAddresses", "записей"},
                {"PowerConsumption", "Вт"},
                {"Price", "руб."}
            };
            var unitsRouters = new Dictionary<string, string> {
                {"PortCount", "шт."},
                {"Speed", "Гбит/с"},
                {"RAM", "ГБ"},
                {"CPUfrequency", "ГГц"},
                {"Price", "руб."}
            };
            var unitsCameras = new Dictionary<string, string> {
                {"Resolution", "пикселей"},
                {"FPS", "кадр/с"},
                {"MatrixPixels", "Мп"},
                {"ViewingAngle", "град."},
                {"Price", "руб."}
            };
            var unitsServers = new Dictionary<string, string> {
                {"CPUfrequency", "ГГц"},
                {"CPUCores", "шт."},
                {"NumberHardSlots", "шт."},
                {"MaxStorage", "ГБ"},
                {"Price", "руб."}
            };
            Dictionary<string, string> units = null;
            if (allData.Keys.Contains("PortCount") && allData.Keys.Contains("MACAddresses")) units = unitsSwitches;
            else if (allData.Keys.Contains("RAM")) units = unitsRouters;
            else if (allData.Keys.Contains("Resolution")) units = unitsCameras;
            else if (allData.Keys.Contains("CPUCores")) units = unitsServers;

            foreach (var key in allData.Keys.Where(k => k != "ModelNames" && k != "CharacteristicsCount"))
            {
                var values = (double[])allData[key];
                string ruName = DeviceFieldRu.ContainsKey(key) ? DeviceFieldRu[key] : key;
                string formattedValue = integerFields.Contains(key)
                    ? values[modelIndex].ToString("F0")
                    : values[modelIndex].ToString("F2");
                string unit = units != null && units.ContainsKey(key) ? $" {units[key]}" : "";
                result.AppendLine($"{ruName}: {formattedValue}{unit}");
            }

            MessageBox.Show(result.ToString(), "Результат оптимизации");

            // Генерируем новый уникальный ID для этой записи
            DatabaseManager.LastRecordId++;
            int uniqueId = DatabaseManager.LastRecordId;

            SaveOptimizationResult(uniqueId, DeviceTypeRu["Servers"], result.ToString().Replace($"Результат взвешенной суммы: {optimalServer.Score:F2}\n", "").Replace($"Оптимальный {DeviceTypeRu["Servers"]}: {optimalServer.Model}\n\nХарактеристики выбранной модели:\n", ""), "Метод взвешенных сумм");
        }

        private void CalculateCable_Click(object sender, EventArgs e)
        {
            // Получаем выбранный тип экранирования
            string selectedShielding = null;
            if (checkedListBoxShielding.CheckedItems.Count > 0)
                selectedShielding = checkedListBoxShielding.CheckedItems[0].ToString();
            if (string.IsNullOrEmpty(selectedShielding))
            {
                MessageBox.Show("Пожалуйста, выберите тип экранирования.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Получаем веса критериев
            if (!double.TryParse(textBoxWireGaugeCable.Text, out double weightWireGauge) ||
                !double.TryParse(textBoxCableSpeed.Text, out double weightSpeedCable) ||
                !double.TryParse(textBoxCablePrice.Text, out double weightPriceCable))
            {
                MessageBox.Show("Пожалуйста, введите корректные веса для всех критериев.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Формируем запрос с фильтром по Shielding
            string query = $"SELECT * FROM Cables WHERE Shielding = '{selectedShielding}'";
            var data = new Dictionary<string, List<double>>
            {
                { "WireGauge", new List<double>() },
                { "SpeedCable", new List<double>() },
                { "PriceCable", new List<double>() }
            };
            var modelNames = new List<string>();
            using (SQLiteDataReader reader = dbManager.ExecuteQuery(query))
            {
                while (reader.Read())
                {
                    modelNames.Add(reader["ModelName"].ToString());
                    data["WireGauge"].Add(Convert.ToDouble(reader["WireGauge"]));
                    data["SpeedCable"].Add(Convert.ToDouble(reader["SpeedCable"]));
                    data["PriceCable"].Add(Convert.ToDouble(reader["PriceCable"]));
                }
            }
            if (modelNames.Count == 0)
            {
                MessageBox.Show("Нет кабелей с выбранным типом экранирования.", "Результат", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            // Нормализация
            var normalizedData = data.ToDictionary(
                kvp => kvp.Key,
                kvp => Normalize(kvp.Value)
            );
            // Расчет взвешенной суммы
            double maxScore = double.MinValue;
            int bestIndex = -1;
            for (int i = 0; i < modelNames.Count; i++)
            {
                double score = normalizedData["WireGauge"][i] * weightWireGauge +
                               normalizedData["SpeedCable"][i] * weightSpeedCable +
                               normalizedData["PriceCable"][i] * weightPriceCable;
                if (score > maxScore)
                {
                    maxScore = score;
                    bestIndex = i;
                }
            }
            // Формируем результат
            StringBuilder result = new StringBuilder();
            result.AppendLine($"Результат взвешенной суммы: {maxScore:F2}");
            result.AppendLine($"Оптимальный {DeviceTypeRu["Cables"]}: {modelNames[bestIndex]}");
            result.AppendLine("\nХарактеристики выбранной модели:");
            result.AppendLine($"{DeviceFieldRu["WireGauge"]}: {data["WireGauge"][bestIndex]:F3} мм²");
            result.AppendLine($"{DeviceFieldRu["SpeedCable"]}: {data["SpeedCable"][bestIndex]:F0} Гбит/с");
            result.AppendLine($"{DeviceFieldRu["PriceCable"]}: {data["PriceCable"][bestIndex]:F0} руб./м");
            result.AppendLine($"{DeviceFieldRu["Shielding"]}: {selectedShielding}");
            MessageBox.Show(result.ToString(), "Результат оптимизации");

            // Генерируем новый уникальный ID для этой записи
            DatabaseManager.LastRecordId++;
            int uniqueId = DatabaseManager.LastRecordId;

            SaveOptimizationResult(uniqueId, DeviceTypeRu["Cables"], result.ToString().Replace($"Результат взвешенной суммы: {maxScore:F2}\n", "").Replace($"Оптимальный {DeviceTypeRu["Cables"]}: {modelNames[bestIndex]}\n\nХарактеристики выбранной модели:\n", ""), "Метод взвешенных сумм");
        }

        private void tabPageCamera_Click(object sender, EventArgs e)
        {

        }
    }
}
