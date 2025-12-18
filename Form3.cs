using System;
using System.Data.SQLite;
using System.Data;
using System.Text;
using System.Windows.Forms;
using static diplom.Form1;

namespace diplom
{
    public partial class Form3 : Form // Форма для симплекс-метода
    {
        private DatabaseManager dbManager; // Менеджер базы данных
        private bool isFirstSave = true; // Флаг для отслеживания первого сохранения в сессии
        private int currentConfigurationNumberForSession; // Номер конфигурации для текущей сессии формы

        // Словари для сопоставления русских и английских названий столбцов для каждой таблицы
        private Dictionary<string, string> SwitchesColumnNameMapping = new Dictionary<string, string> // Словарь для коммутаторов
        {
            { "Количество портов", "PortCount" },
            { "Скорость", "Speed" },
            { "Количество записей MAC адресов", "MACAddresses" },
            { "Потребляемая мощность", "PowerConsumption" },
            { "Цена", "Price" }
        };

        private Dictionary<string, string> RoutersColumnNameMapping = new Dictionary<string, string> // Словарь для маршрутизаторов
        {
            { "Количество портов", "PortCount" },
            { "Скорость", "Speed" },
            { "Объем оперативной памяти", "RAM" },
            { "Частота процессора", "CPUFrequency" },
            { "Цена", "Price" }
        };

        private Dictionary<string, string> CamerasColumnNameMapping = new Dictionary<string, string> // Словарь для ip-камер
        {
            { "Разрешение", "Resolution" },
            { "Частота кадров", "FPS" },
            { "Число пикселей матрицы", "MatrixPixels" },
            { "Угол обзора", "ViewingAngle" },
            { "Цена", "Price" }
        };

        private Dictionary<string, string> ServersColumnNameMapping = new Dictionary<string, string> // Словарь для серверов
        {
            { "Частота процессора", "CPUFrequency" },
            { "Количество ядер", "CPUCores" },
            { "Количество слотов для жестких дисков", "NumberHardSlots" },
            { "Максимальный объем памяти", "MaxStorage" },
            { "Цена", "Price" }
        };

        private Dictionary<string, string> CablesColumnNameMapping = new Dictionary<string, string> // Словарь для кабелей
        {
            { "Площадь поперечного сечения", "WireGauge" },
            { "Скорость передачи данных", "SpeedCable" },
            { "Цена за метр", "PriceCable" }
        };

        private static readonly Dictionary<string, string> DeviceTypeRu = new Dictionary<string, string> // Словарь для перевода типов устройств на русский
        {
            { "Switches", "коммутатор" },
            { "Routers", "маршрутизатор" },
            { "Cameras", "IP-камера" },
            { "Servers", "сервер" },
            { "Cables", "кабель" }
        };

        public Form3() 
        {
            InitializeComponent();
            string connectionString = "Data Source=A:\\DevicesDatabase;Version=3;";
            dbManager = new DatabaseManager(connectionString); // Инициализация менеджера базы данных
            currentConfigurationNumberForSession = dbManager.CurrentConfigurationNumber; // Получение номера конфигурации

            InitializeFunctionListBoxes(); // Инициализация списков функций
            InitializeCriteriaListBoxes(); // Инициализация списков критериев
            InitializeComboBoxes(); // Инициализация выпадающих списков

            // Добавление обработчиков событий для чекбоксов
            checkBox1.CheckedChanged += (s, e) => ToggleDeviceControls("Switches", checkBox1.Checked);
            checkBox2.CheckedChanged += (s, e) => ToggleDeviceControls("Routers", checkBox2.Checked);
            checkBox3.CheckedChanged += (s, e) => ToggleDeviceControls("Cameras", checkBox3.Checked);
            checkBox4.CheckedChanged += (s, e) => ToggleDeviceControls("Servers", checkBox4.Checked);
            checkBox5.CheckedChanged += checkBox5_CheckedChanged;

            // Настройка SFP
            SFP.Enabled = false;
            checkBox1.CheckedChanged += (s, e) => SFP.Enabled = checkBox1.Checked;

            // Настройка WiFi/SFP
            WiFi_SFP.Items.Clear();
            WiFi_SFP.Items.Add("Поддержка Wi-Fi");
            WiFi_SFP.Items.Add("Поддержка оптоволокна (SFP)");
            WiFi_SFP.Enabled = false;
            checkBox2.CheckedChanged += (s, e) => WiFi_SFP.Enabled = checkBox2.Checked;

            // Отключение всех элементов управления при запуске
            ToggleDeviceControls("Switches", false);
            ToggleDeviceControls("Routers", false);
            ToggleDeviceControls("Cameras", false);
            ToggleDeviceControls("Servers", false);
            ToggleDeviceControls("Cables", false);

            // Заполнение списка типов экранирования
            checkedListBoxShielding.Items.Clear();
            checkedListBoxShielding.Items.AddRange(new object[] { "U/UTP", "F/UTP", "SF/UTP", "S/FTP", "U/FTP", "F/FTP" });
        }

        private void InitializeFunctionListBoxes() // Инициализация списков функций
        {
            foreach (var listBox in new[] { listBoxFunction1, listBoxFunction2, listBoxFunction3, listBoxFunction4, listBoxFunction5 })
            {
                listBox.Items.AddRange(new object[] { "min", "max" });
                listBox.SelectedIndex = 0;
            }
        }

        private void InitializeCriteriaListBoxes() // Инициализация списков критериев
        {
            listBoxCriteria1.Items.AddRange(SwitchesColumnNameMapping.Keys.ToArray());
            listBoxCriteria1.SelectedIndex = 0;

            listBoxCriteria2.Items.AddRange(RoutersColumnNameMapping.Keys.ToArray());
            listBoxCriteria2.SelectedIndex = 0;

            listBoxCriteria3.Items.AddRange(CamerasColumnNameMapping.Keys.ToArray());
            listBoxCriteria3.SelectedIndex = 0;

            listBoxCriteria4.Items.AddRange(ServersColumnNameMapping.Keys.ToArray());
            listBoxCriteria4.SelectedIndex = 0;

            listBoxCriteria5.Items.AddRange(CablesColumnNameMapping.Keys.ToArray());
            listBoxCriteria5.SelectedIndex = 0;
        }

        private void InitializeComboBoxes() // Инициализация выпадающих списков
        {
            foreach (var comboBox in new[]
            {
                comboBoxConstraintTypePortSwitch, comboBoxConstraintTypeSpeedSwitch, comboBoxConstraintTypeMAC,
                comboBoxConstraintTypePower, comboBoxConstraintTypePriceSwitch, comboBoxConstraintTypePortRouter,
                comboBoxConstraintTypeSpeedRouter, comboBoxConstraintTypeRAMRouter, comboBoxConstraintTypeCPUFreqRouter,
                comboBoxConstraintTypePriceRouter, comboBoxConstraintTypeResolution, comboBoxConstraintTypeFPS,
                comboBoxConstraintTypeMatrixPixels, comboBoxConstraintTypeViewingAngle, comboBoxConstraintTypePriceCamera,
                comboBoxConstraintTypeCPUFreqServer, comboBoxConstraintTypeCPUCores, comboBoxConstraintTypeNumberHardSlots,
                comboBoxConstraintTypeMaxStorage, comboBoxConstraintTypePriceServer,
                comboBoxWireGaugeCable, comboBoxCableSpeed, comboBoxCablePrice
            })
            {
                comboBox.Items.AddRange(new object[] { ">=", "<=", "=" });
            }
        }

        private void ToggleDeviceControls(string deviceType, bool enabled) // Метод для включения/отключения элементов заполнения параметров для устройств
        {
            switch (deviceType)
            {
                case "Switches":
                    listBoxFunction1.Enabled = enabled;
                    listBoxCriteria1.Enabled = enabled;
                    textBoxPortSwitch.Enabled = enabled;
                    textBoxSpeedSwitch.Enabled = enabled;
                    textBoxMAC.Enabled = enabled;
                    textBoxPower.Enabled = enabled;
                    textBoxPriceSwitch.Enabled = enabled;
                    comboBoxConstraintTypePortSwitch.Enabled = enabled;
                    comboBoxConstraintTypeSpeedSwitch.Enabled = enabled;
                    comboBoxConstraintTypeMAC.Enabled = enabled;
                    comboBoxConstraintTypePower.Enabled = enabled;
                    comboBoxConstraintTypePriceSwitch.Enabled = enabled;
                    button1.Enabled = enabled;
                    SFP.Enabled = enabled;
                    break;

                case "Routers":
                    listBoxFunction2.Enabled = enabled;
                    listBoxCriteria2.Enabled = enabled;
                    textBoxPortRouter.Enabled = enabled;
                    textBoxSpeedRouter.Enabled = enabled;
                    textBoxRAMRouter.Enabled = enabled;
                    textBoxCPUFreqRouter.Enabled = enabled;
                    textBoxPriceRouter.Enabled = enabled;
                    comboBoxConstraintTypePortRouter.Enabled = enabled;
                    comboBoxConstraintTypeSpeedRouter.Enabled = enabled;
                    comboBoxConstraintTypeRAMRouter.Enabled = enabled;
                    comboBoxConstraintTypeCPUFreqRouter.Enabled = enabled;
                    comboBoxConstraintTypePriceRouter.Enabled = enabled;
                    button2.Enabled = enabled;
                    break;

                case "Cameras":
                    listBoxFunction3.Enabled = enabled;
                    listBoxCriteria3.Enabled = enabled;
                    textBoxResolutionCamera.Enabled = enabled;
                    textBoxFPS.Enabled = enabled;
                    textBoxMatrixPixels.Enabled = enabled;
                    textBoxViewingAngle.Enabled = enabled;
                    textBoxPriceCamera.Enabled = enabled;
                    comboBoxConstraintTypeResolution.Enabled = enabled;
                    comboBoxConstraintTypeFPS.Enabled = enabled;
                    comboBoxConstraintTypeMatrixPixels.Enabled = enabled;
                    comboBoxConstraintTypeViewingAngle.Enabled = enabled;
                    comboBoxConstraintTypePriceCamera.Enabled = enabled;
                    button3.Enabled = enabled;
                    break;

                case "Servers":
                    listBoxFunction4.Enabled = enabled;
                    listBoxCriteria4.Enabled = enabled;
                    textBoxCPUFreqServer.Enabled = enabled;
                    textBoxCPUCores.Enabled = enabled;
                    textBoxNumberHardSlots.Enabled = enabled;
                    textBoxMaxStorage.Enabled = enabled;
                    textBoxPriceServer.Enabled = enabled;
                    comboBoxConstraintTypeCPUFreqServer.Enabled = enabled;
                    comboBoxConstraintTypeCPUCores.Enabled = enabled;
                    comboBoxConstraintTypeNumberHardSlots.Enabled = enabled;
                    comboBoxConstraintTypeMaxStorage.Enabled = enabled;
                    comboBoxConstraintTypePriceServer.Enabled = enabled;
                    button4.Enabled = enabled;
                    break;

                case "Cables":
                    listBoxFunction5.Enabled = enabled;
                    listBoxCriteria5.Enabled = enabled;
                    textBoxWireGaugeCable.Enabled = enabled;
                    textBoxCableSpeed.Enabled = enabled;
                    textBoxCablePrice.Enabled = enabled;
                    comboBoxWireGaugeCable.Enabled = enabled;
                    comboBoxCableSpeed.Enabled = enabled;
                    comboBoxCablePrice.Enabled = enabled;
                    checkedListBoxShielding.Enabled = enabled;
                    CalculateCable.Enabled = enabled;
                    break;
            }
        }

        private void checkBox5_CheckedChanged(object sender, EventArgs e) // Обработчик изменения состояния чекбокса кабеля
        {
            bool enabled = checkBox5.Checked;
            listBoxFunction5.Enabled = enabled;
            listBoxCriteria5.Enabled = enabled;
            textBoxWireGaugeCable.Enabled = enabled;
            textBoxCableSpeed.Enabled = enabled;
            textBoxCablePrice.Enabled = enabled;
            comboBoxWireGaugeCable.Enabled = enabled;
            comboBoxCableSpeed.Enabled = enabled;
            comboBoxCablePrice.Enabled = enabled;
            checkedListBoxShielding.Enabled = enabled;
            CalculateCable.Enabled = enabled;
        }

        private void button1_Click(object sender, EventArgs e) // Обработчик нажатия кнопки расчета коммутатора
        {
            try
            {
                SolveForDevice("Switches", checkBox1, listBoxFunction1, listBoxCriteria1);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}");
            }
        }

        private void button2_Click(object sender, EventArgs e) // Обработчик нажатия кнопки расчета маршрутизатора
        {
            try
            {
                SolveForDevice("Routers", checkBox2, listBoxFunction2, listBoxCriteria2);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}");
            }
        }

        private void button3_Click(object sender, EventArgs e) // Обработчик нажатия кнопки расчета ip-камеры
        {
            try
            {
                SolveForDevice("Cameras", checkBox3, listBoxFunction3, listBoxCriteria3);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}");
            }
        }

        private void button4_Click(object sender, EventArgs e) // Обработчик нажатия кнопки расчета сервера
        {
            try
            {
                SolveForDevice("Servers", checkBox4, listBoxFunction4, listBoxCriteria4);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}");
            }
        }

        private void button5_Click(object sender, EventArgs e) // Обработчик нажатия кнопки расчета кабеля
        {
            try
            {
                SolveForDevice("Cables", checkBox5, listBoxFunction5, listBoxCriteria5);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}");
            }
        }

        private void SolveForDevice(string tableName, CheckBox checkBox, ListBox listBoxFunction, ListBox listBoxCriteria) // Метод для решения задачи оптимизации устройства
        {
            try
            {
                string objectiveFunctionType = listBoxFunction.SelectedItem?.ToString() ?? throw new InvalidOperationException("Целевая функция не выбрана.");
                string selectedCriterion = listBoxCriteria.SelectedItem?.ToString() ?? throw new InvalidOperationException("Критерий не выбран.");

                Dictionary<string, object> allData;
                List<string> modelNames;
                List<string> shieldings = null; // Хранение типов экранирования для кабеля

                //Получение данных из базы (для каждого типа устройства свой способ)
                if (tableName == "Cables") //для кабелей
                {
                    var selectedShieldings = checkedListBoxShielding.CheckedItems.Cast<string>().ToList();
                    if (selectedShieldings.Count == 0)
                    {
                        MessageBox.Show("Пожалуйста, выберите тип экранирования.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                    // Получаем все данные из базы только по выбранному типу экранирования (Shielding)
                    var columnNameMapping = GetColumnNameMapping(tableName);
                    allData = new Dictionary<string, object>();
                    var dataLists = new Dictionary<string, List<double>>();
                    modelNames = new List<string>();
                    shieldings = new List<string>();
                    string inClause = string.Join(",", selectedShieldings.Select(s => "'" + s + "'"));
                    string query = $"SELECT ModelName, WireGauge, SpeedCable, PriceCable, Shielding FROM Cables WHERE Shielding IN ({inClause})";
                    using (SQLiteDataReader reader = dbManager.ExecuteQuery(query))
                    {
                        while (reader.Read())
                        {
                            modelNames.Add(reader["ModelName"].ToString());
                            shieldings.Add(reader["Shielding"].ToString());
                            dataLists.TryAdd("Площадь поперечного сечения", new List<double>());
                            dataLists.TryAdd("Скорость передачи данных", new List<double>());
                            dataLists.TryAdd("Цена за метр", new List<double>());
                            dataLists["Площадь поперечного сечения"].Add(Convert.ToDouble(reader["WireGauge"]));
                            dataLists["Скорость передачи данных"].Add(Convert.ToDouble(reader["SpeedCable"]));
                            dataLists["Цена за метр"].Add(Convert.ToDouble(reader["PriceCable"]));
                        }
                    }
                    if (modelNames.Count == 0)
                    {
                        MessageBox.Show("Нет кабелей с выбранным типом экранирования, удовлетворяющих критериям.", "Результат", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }
                    // Преобразуем List<double> в double[] для соответствия общему формату
                    foreach(var kvp in dataLists)
                    {
                        allData[kvp.Key] = kvp.Value.ToArray();
                    }
                     allData["ModelNames"] = modelNames.ToArray(); // Добавляем названия моделей
                }
                else if (tableName == "Switches" && SFP.Checked) //для коммутаторов
                {
                    // Получаем все данные из базы только для коммутаторов с поддержкой оптоволокна (SFP)
                    var columnNameMapping = GetColumnNameMapping(tableName);
                    allData = new Dictionary<string, object>();
                    var dataLists = new Dictionary<string, List<double>>();
                    modelNames = new List<string>();
                    string query = $"SELECT ModelName, {string.Join(", ", columnNameMapping.Values)} FROM {tableName} WHERE SFP = 'да'";
                    using (SQLiteDataReader reader = dbManager.ExecuteQuery(query))
                    {
                        while (reader.Read())
                        {
                            modelNames.Add(reader["ModelName"].ToString());
                            int columnOffset = 1;
                            foreach (var mapping in columnNameMapping)
                            {
                                if (!dataLists.ContainsKey(mapping.Key))
                                    dataLists[mapping.Key] = new List<double>();
                                dataLists[mapping.Key].Add(Convert.ToDouble(reader[columnOffset++]));
                            }
                        }
                    }
                    if (modelNames.Count == 0)
                    {
                        MessageBox.Show("Нет коммутаторов с поддержкой SFP, удовлетворяющих критериям.", "Результат", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }
                    foreach (var kvp in dataLists)
                    {
                        allData[kvp.Key] = kvp.Value.ToArray();
                    }
                    allData["ModelNames"] = modelNames.ToArray();
                }
                else if (tableName == "Routers") // Для маршрутизаторов
                {
                    var columnNameMapping = GetColumnNameMapping(tableName); // Используем исходное отображение без поддержки WiFi/SFP
                    allData = new Dictionary<string, object>();
                    var dataLists = new Dictionary<string, List<double>>();
                    modelNames = new List<string>();
                    var wifiStatus = new List<string>(); // Для хранения статуса поддержки Wi-Fi
                    var sfpStatus = new List<string>();  // Для хранения статуса поддержки оптоволокна (SFP)

                    List<string> filterConditions = new List<string>();
                    if (WiFi_SFP.CheckedItems.Contains("Поддержка Wi-Fi"))
                    {
                        filterConditions.Add("WiFi = 'да'");
                    }
                    if (WiFi_SFP.CheckedItems.Contains("Поддержка оптоволокна (SFP)"))
                    {
                        filterConditions.Add("SFP = 'да'");
                    }

                    string whereClause = filterConditions.Count > 0 ? $" WHERE {string.Join(" AND ", filterConditions)}" : "";

                    // Выбираем числовые столбцы для оптимизации + ModelName, WiFi, SFP
                    string columnsToSelect = string.Join(", ", columnNameMapping.Values.Where(v => v != "WiFi" && v != "SFP"));
                    string query = $"SELECT ModelName, {columnsToSelect}, WiFi, SFP FROM {tableName}{whereClause}";

                    using (SQLiteDataReader reader = dbManager.ExecuteQuery(query))
                    {
                        while (reader.Read())
                        {
                            modelNames.Add(reader["ModelName"].ToString());
                            wifiStatus.Add(reader["WiFi"].ToString()); // Считываем статус поддержки Wi-Fi как строку
                            sfpStatus.Add(reader["SFP"].ToString());   // Считываем статус поддержки оптоволокна (SFP) как строку

                            // Считываем числовые данные для оптимизации
                            foreach (var mapping in columnNameMapping.Where(m => m.Value != "WiFi" && m.Value != "SFP"))
                            {
                                if (!dataLists.ContainsKey(mapping.Key))
                                    dataLists[mapping.Key] = new List<double>();
                                dataLists[mapping.Key].Add(Convert.ToDouble(reader[mapping.Value]));
                            }
                        }
                    }

                    if (modelNames.Count == 0)
                    {
                        MessageBox.Show("Нет маршрутизаторов с выбранными характеристиками, удовлетворяющих критериям.", "Результат", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }

                    foreach (var kvp in dataLists)
                    {
                        allData[kvp.Key] = kvp.Value.ToArray();
                    }
                    allData["ModelNames"] = modelNames.ToArray();
                    // Сохраняем статусы поддержки WiFi и SFP отдельно для вывода
                    allData["WiFiStatus"] = wifiStatus.ToArray();
                    allData["SFPStatus"] = sfpStatus.ToArray();
                }
                else // Логика для ip-камеры и сервера
                {
                     allData = GetDataFromDatabase(tableName);
                     if (allData == null || allData.Count == 0 || !allData.ContainsKey("ModelNames"))
                     {
                         MessageBox.Show($"Нет данных для устройства {tableName}.", "Результат", MessageBoxButtons.OK, MessageBoxIcon.Information);
                         return;
                     }
                     modelNames = ((string[])allData["ModelNames"]).ToList();
                }


                if (!allData.ContainsKey(selectedCriterion))
                    throw new ArgumentException($"Нет данных для критерия '{selectedCriterion}'.");

                var criterionData = (double[])allData[selectedCriterion];
                int numVariables = criterionData.Length;

                // Проверяем, какие модели удовлетворяют всем ограничениям
                var constraints = GetConstraints(checkBox, tableName);
                List<int> validIndices = new List<int>();

                for (int i = 0; i < numVariables; i++)
                {
                    bool isValid = true;
                    foreach (var constraint in constraints)
                    {
                        if (!allData.ContainsKey(constraint.criteria))
                            continue; // Пропускаем, если данных для критерия ограничения нет

                        var constraintData = (double[])allData[constraint.criteria];
                        double value = constraintData[i];
                        switch (constraint.constraintType)
                        {
                            case ">=":
                                if (value < constraint.value) isValid = false;
                                break;
                            case "<=":
                                if (value > constraint.value) isValid = false;
                                break;
                            case "=":
                                if (Math.Abs(value - constraint.value) > 1e-10) isValid = false;
                                break;
                        }
                        if (!isValid) break;
                    }
                    if (isValid) validIndices.Add(i);
                }

                if (validIndices.Count == 0)
                {
                    MessageBox.Show("Не удалось определить оптимальную модель по заданным критериям и ограничениям.", "Результат", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                // Среди допустимых моделей находим оптимальную
                int optimalIndex = validIndices[0];
                double optimalValue = criterionData[optimalIndex];

                foreach (int index in validIndices)
                {
                    double currentValue = criterionData[index];
                    if (objectiveFunctionType == "max")
                    {
                        if (currentValue > optimalValue)
                        {   optimalValue = currentValue; optimalIndex = index; }
                    }
                    else // min
                    {
                        if (currentValue < optimalValue)
                        {   optimalValue = currentValue; optimalIndex = index; }
                    }
                }

                // Формируем и выводим результат
                string optWord = tableName == "Cameras" ? "Оптимальная" : "Оптимальный";
                string ruType = DeviceTypeRu.ContainsKey(tableName) ? DeviceTypeRu[tableName] : tableName;
                StringBuilder result = new StringBuilder();
                result.AppendLine($"Оптимальное значение критерия: {optimalValue:F2}");
                result.AppendLine($"{optWord} {ruType}: {modelNames[optimalIndex]}");
                result.AppendLine("\nХарактеристики выбранной модели:");

                // Получаем единицы измерения (общая логика)
                var unitsSwitches = new Dictionary<string, string> { { "Количество портов", "шт." }, { "Скорость", "Гбит/с" }, { "Количество записей MAC адресов", "записей" }, { "Потребляемая мощность", "Вт" }, { "Цена", "руб." } };
                var unitsRouters = new Dictionary<string, string> { { "Количество портов", "шт." }, { "Скорость", "Гбит/с" }, { "Объем оперативной памяти", "ГБ" }, { "Частота процессора", "ГГц" }, { "Цена", "руб." } };
                var unitsCameras = new Dictionary<string, string> { { "Разрешение", "пикселей" }, { "Частота кадров", "кадр/с" }, { "Число пикселей матрицы", "Мп" }, { "Угол обзора", "град." }, { "Цена", "руб." } };
                var unitsServers = new Dictionary<string, string> { { "Частота процессора", "ГГц" }, { "Количество ядер", "шт." }, { "Количество слотов для жестких дисков", "шт." }, { "Максимальный объем памяти", "ГБ" }, { "Цена", "руб." } };
                var unitsCables = new Dictionary<string, string> { { "Площадь поперечного сечения", "мм²" }, { "Скорость передачи данных", "Гбит/с" }, { "Цена за метр", "руб./м" } };
                Dictionary<string, string> units = tableName switch
                {
                    "Switches" => unitsSwitches,
                    "Routers" => unitsRouters,
                    "Cameras" => unitsCameras,
                    "Servers" => unitsServers,
                    "Cables" => unitsCables,
                    _ => null
                };

                // Получаем mapping для полей. Сопоставление единиц измерения
                Dictionary<string, string> fieldRu = GetColumnNameMapping(tableName);

                // Вывод технических характеристик
                var integerFields = new HashSet<string> { "Количество портов", "Количество записей MAC адресов", "Количество ядер", "Количество слотов для жестких дисков", "Частота кадров", "Потребляемая мощность", "Цена", "Число пикселей матрицы", "Максимальный объем памяти", "Разрешение" };

                foreach (var criterionEn in allData.Keys.Where(k => k != "ModelNames" && k != "WiFiStatus" && k != "SFPStatus"))
                {   // Используем allData, которое теперь заполнено и для кабелей
                    var values = (double[])allData[criterionEn];
                    // Поиск русского названия по английскому
                    string ruName = fieldRu.FirstOrDefault(x => x.Value == criterionEn).Key ?? criterionEn; 
                    string formattedValue = integerFields.Contains(ruName) ? values[optimalIndex].ToString("F0") : values[optimalIndex].ToString("F2");
                    string unit = units != null && units.ContainsKey(ruName) ? $" {units[ruName]}" : "";
                    result.AppendLine($"{ruName}: {formattedValue}{unit}");
                }

                // Добавляем тип экранирования для кабеля в конце результата
                if (tableName == "Cables" && shieldings != null && optimalIndex < shieldings.Count)
                {
                    result.AppendLine($"Тип экранирования: {shieldings[optimalIndex]}");
                }

                // Добавляем информацию о поддержке (SFP) в конце (только для коммутаторов)
                if (tableName == "Switches")
                {
                    string sfpValue = "нет";
                    try
                    {
                        string safeModel = modelNames[optimalIndex].Replace("'", "''");
                        string sfpQuery = $"SELECT SFP FROM Switches WHERE ModelName = '{safeModel}' LIMIT 1";
                        using (var reader = dbManager.ExecuteQuery(sfpQuery))
                        {
                            if (reader.Read())
                            {
                                sfpValue = reader["SFP"].ToString();
                            }
                        }
                    }
                    catch { }
                    result.AppendLine($"Поддержка оптоволокна (SFP): {sfpValue}");
                }

                // Добавляем информацию о поддержке WiFi и  оптоволокна (SFP) в конце (только для маршрутизаторов)
                if (tableName == "Routers")
                {
                    string[] wifiStatus = (string[])allData["WiFiStatus"];
                    string[] sfpStatus = (string[])allData["SFPStatus"];
                    result.AppendLine($"Поддержка Wi-Fi: {wifiStatus[optimalIndex]}");
                    result.AppendLine($"Поддержка оптоволокна (SFP): {sfpStatus[optimalIndex]}");
                }

                MessageBox.Show(result.ToString(), "Результат оптимизации");

                // Сохраняем результат в базу данных. Генерируем новый уникальный ID для этой записи
                DatabaseManager.LastRecordId++;
                int uniqueId = DatabaseManager.LastRecordId;

                string queryInsert = "INSERT INTO DevicesDatabase (ID, ConfigurationNumber, DeviceType, TechnicalChar, OptimizationMethod) VALUES (@id, @configNumber, @deviceType, @technicalChar, @optimizationMethod)";
                var parameters = new Dictionary<string, object>
                {
                    { "@id", uniqueId },
                    { "@configNumber", currentConfigurationNumberForSession },
                    { "@deviceType", ruType },
                    { "@technicalChar", result.ToString() },
                    { "@optimizationMethod", "Симплекс-метод" }
                };

                if (dbManager != null)
                {   int rowsAffected = dbManager.ExecuteNonQuery(queryInsert, parameters);
                    MessageBox.Show($"Запрос выполнен. Добавлено строк: {rowsAffected}", "Сохранение данных", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {   MessageBox.Show("Ошибка: Соединение с базой данных не установлено.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}");
            }
        }

        private (double optimalValue, List<double> solution) SolveSimplex(List<double> objectiveFunction, List<List<double>> constraints, List<double> bValues) // Метод для решения симплекс-методом
        {
            try
            {
                int numVariables = objectiveFunction.Count;
                int numConstraints = constraints.Count;
                int numSlackVariables = 0;
                int numArtificialVariables = 0;

                // Подсчитываем количество дополнительных и искусственных переменных
                foreach (var constraint in constraints)
                {
                    if (constraint.Count > 0) // Пропускаем пустые ограничения
                    {
                        numSlackVariables++;
                        if (bValues[constraints.IndexOf(constraint)] < 0)
                        {
                            numArtificialVariables++;
                        }
                    }
                }

                int totalVariables = numVariables + numSlackVariables + numArtificialVariables;

                // Создаем расширенную симплекс-таблицу
                double[,] tableau = new double[numConstraints + 1, totalVariables + 1];

                // Заполняем основные переменные
                for (int i = 0; i < numConstraints; i++)
                {
                    for (int j = 0; j < numVariables; j++)
                    {
                        tableau[i, j] = constraints[i][j];
                    }
                }

                // Добавляем фиктивные переменные в симплекс-таблицу
                int slackColumn = numVariables; // позиция для фиктивных переменных
                int artificialColumn = numVariables + numSlackVariables; //позиция для искуственных переменных

                for (int i = 0; i < numConstraints; i++)
                {
                    // Добавляем фиктивную переменную для преобразования неравенства в равенство
                    tableau[i, slackColumn + i] = 1.0;

                    // Если правая часть отрицательная, добавляем искусственную переменную
                    if (bValues[i] < 0)
                    {
                        tableau[i, artificialColumn] = 1.0;
                        artificialColumn++;
                    }

                    // Добавляем правую часть ограничения
                    tableau[i, totalVariables] = Math.Abs(bValues[i]);
                }

                // Заполняем целевую функцию (последняя строка)
                for (int j = 0; j < numVariables; j++)
                {
                    tableau[numConstraints, j] = -objectiveFunction[j];
                }

                // Первая фаза - избавляемся от искусственных переменных
                if (numArtificialVariables > 0)
                {
                    // Создаем вспомогательную целевую функцию
                    for (int j = numVariables + numSlackVariables; j < totalVariables; j++)
                    {
                        tableau[numConstraints, j] = 1.0;
                    }

                    // Решаем вспомогательную задачу
                    while (true)
                    {
                        // Находим разрешающий столбец
                        int pivotCol = -1;
                        double minValue = 0;
                        for (int j = 0; j < totalVariables; j++)
                        {
                            if (tableau[numConstraints, j] < minValue)
                            {
                                minValue = tableau[numConstraints, j];
                                pivotCol = j;
                            }
                        }

                        if (pivotCol == -1) break;

                        // Находим разрешающую строку
                        int pivotRow = -1;
                        double minRatio = double.MaxValue;
                        for (int i = 0; i < numConstraints; i++)
                        {
                            if (tableau[i, pivotCol] > 1e-10)
                            {
                                double ratio = tableau[i, totalVariables] / tableau[i, pivotCol];
                                if (ratio >= 0 && ratio < minRatio)
                                {
                                    minRatio = ratio;
                                    pivotRow = i;
                                }
                            }
                        }

                        if (pivotRow == -1) return (0, null);

                        // Выполняем преобразование Жордана-Гаусса
                        double pivotElement = tableau[pivotRow, pivotCol];
                        for (int j = 0; j <= totalVariables; j++)
                        {
                            tableau[pivotRow, j] /= pivotElement;
                        }

                        for (int i = 0; i <= numConstraints; i++)
                        {
                            if (i != pivotRow)
                            {
                                double factor = tableau[i, pivotCol];
                                for (int j = 0; j <= totalVariables; j++)
                                {
                                    tableau[i, j] -= factor * tableau[pivotRow, j];
                                }
                            }
                        }
                    }

                    // Проверяем, что все искусственные переменные равны нулю
                    for (int j = numVariables + numSlackVariables; j < totalVariables; j++)
                    {
                        if (Math.Abs(tableau[numConstraints, j]) > 1e-10)
                        {
                            return (0, null);
                        }
                    }

                    // Восстанавливаем исходную целевую функцию
                    for (int j = 0; j < numVariables; j++)
                    {
                        tableau[numConstraints, j] = -objectiveFunction[j];
                    }
                    for (int j = numVariables; j < totalVariables; j++)
                    {
                        tableau[numConstraints, j] = 0;
                    }
                }

                // Вторая фаза - решаем основную задачу
                int maxIterations = 1000;
                int iteration = 0;

                while (iteration < maxIterations)
                {
                    iteration++;

                    // Находим разрешающий столбец
                    int pivotCol = -1;
                    double minValue = 0;
                    for (int j = 0; j < totalVariables; j++)
                    {
                        if (tableau[numConstraints, j] < minValue)
                        {
                            minValue = tableau[numConstraints, j];
                            pivotCol = j;
                        }
                    }

                    if (pivotCol == -1) break;

                    // Находим разрешающую строку
                    int pivotRow = -1;
                    double minRatio = double.MaxValue;
                    for (int i = 0; i < numConstraints; i++)
                    {
                        if (tableau[i, pivotCol] > 1e-10)
                        {
                            double ratio = tableau[i, totalVariables] / tableau[i, pivotCol];
                            if (ratio >= 0 && ratio < minRatio)
                            {
                                minRatio = ratio;
                                pivotRow = i;
                            }
                        }
                    }

                    if (pivotRow == -1) return (0, null);

                    // Выполняем преобразование Жордана-Гаусса
                    double pivotElement = tableau[pivotRow, pivotCol];
                    for (int j = 0; j <= totalVariables; j++)
                    {
                        tableau[pivotRow, j] /= pivotElement;
                    }

                    for (int i = 0; i <= numConstraints; i++)
                    {
                        if (i != pivotRow)
                        {
                            double factor = tableau[i, pivotCol];
                            for (int j = 0; j <= totalVariables; j++)
                            {
                                tableau[i, j] -= factor * tableau[pivotRow, j];
                            }
                        }
                    }
                }

                if (iteration >= maxIterations)
                    return (0, null);

                // Формируем решение
                List<double> solution = new List<double>();
                for (int j = 0; j < numVariables; j++)
                {
                    double value = 0;
                    int basicRow = -1;
                    bool isBasic = true;

                    for (int i = 0; i < numConstraints; i++)
                    {
                        if (Math.Abs(tableau[i, j]) > 1e-10)
                        {
                            if (Math.Abs(tableau[i, j] - 1) < 1e-10 && basicRow == -1)
                                basicRow = i;
                            else
                            {
                                isBasic = false;
                                break;
                            }
                        }
                    }

                    if (isBasic && basicRow != -1)
                        value = tableau[basicRow, totalVariables];

                    // Округляем значения близкие к 0 или 1 (для бинарных переменных)
                    if (Math.Abs(value) < 1e-10)
                        value = 0;
                    else if (Math.Abs(value - 1) < 1e-10)
                        value = 1;

                    solution.Add(value);
                }

                // Проверяем допустимость решения
                for (int i = 0; i < numConstraints; i++)
                {
                    double sum = 0;
                    for (int j = 0; j < numVariables; j++)
                    {
                        sum += constraints[i][j] * solution[j];
                    }
                    if (Math.Abs(sum - bValues[i]) > 1e-8)
                        return (0, null);
                }

                double optimalValue = -tableau[numConstraints, totalVariables];
                return (optimalValue, solution);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка в симплекс-методе: {ex.Message}");
                return (0, null);
            }
        }

        private List<double> GetColumnDataFromDatabase(string tableName, string columnName) // Метод для получения данных столбца из базы данных
        {
            List<double> values = new List<double>();
            string query = $"SELECT {columnName} FROM {tableName}";

            using (SQLiteDataReader reader = dbManager.ExecuteQuery(query))
            {
                while (reader.Read())
                {
                    if (!reader.IsDBNull(0))
                    {
                        values.Add(reader.GetDouble(0));
                    }
                }
            }

            return values;
        }

        private List<(string criteria, string constraintType, double value)> GetConstraints(CheckBox checkBox, string tableName) // Метод для получения ограничений
        {
            var constraints = new List<(string criteria, string constraintType, double value)>();

            if (checkBox == checkBox1 && tableName == "Switches")
            {
                if (!string.IsNullOrEmpty(textBoxPortSwitch.Text) && comboBoxConstraintTypePortSwitch.SelectedItem != null)
                    constraints.Add(("Количество портов", comboBoxConstraintTypePortSwitch.SelectedItem.ToString(), GetDoubleValue(textBoxPortSwitch.Text)));

                if (!string.IsNullOrEmpty(textBoxSpeedSwitch.Text) && comboBoxConstraintTypeSpeedSwitch.SelectedItem != null)
                    constraints.Add(("Скорость", comboBoxConstraintTypeSpeedSwitch.SelectedItem.ToString(), GetDoubleValue(textBoxSpeedSwitch.Text)));

                if (!string.IsNullOrEmpty(textBoxMAC.Text) && comboBoxConstraintTypeMAC.SelectedItem != null)
                    constraints.Add(("Количество записей MAC адресов", comboBoxConstraintTypeMAC.SelectedItem.ToString(), GetDoubleValue(textBoxMAC.Text)));

                if (!string.IsNullOrEmpty(textBoxPower.Text) && comboBoxConstraintTypePower.SelectedItem != null)
                    constraints.Add(("Потребляемая мощность", comboBoxConstraintTypePower.SelectedItem.ToString(), GetDoubleValue(textBoxPower.Text)));

                if (!string.IsNullOrEmpty(textBoxPriceSwitch.Text) && comboBoxConstraintTypePriceSwitch.SelectedItem != null)
                    constraints.Add(("Цена", comboBoxConstraintTypePriceSwitch.SelectedItem.ToString(), GetDoubleValue(textBoxPriceSwitch.Text)));
            }
            else if (checkBox == checkBox2 && tableName == "Routers")
            {
                if (!string.IsNullOrEmpty(textBoxPortRouter.Text) && comboBoxConstraintTypePortRouter.SelectedItem != null)
                    constraints.Add(("Количество портов", comboBoxConstraintTypePortRouter.SelectedItem.ToString(), GetDoubleValue(textBoxPortRouter.Text)));

                if (!string.IsNullOrEmpty(textBoxSpeedRouter.Text) && comboBoxConstraintTypeSpeedRouter.SelectedItem != null)
                    constraints.Add(("Скорость", comboBoxConstraintTypeSpeedRouter.SelectedItem.ToString(), GetDoubleValue(textBoxSpeedRouter.Text)));

                if (!string.IsNullOrEmpty(textBoxRAMRouter.Text) && comboBoxConstraintTypeRAMRouter.SelectedItem != null)
                    constraints.Add(("Объем оперативной памяти", comboBoxConstraintTypeRAMRouter.SelectedItem.ToString(), GetDoubleValue(textBoxRAMRouter.Text)));

                if (!string.IsNullOrEmpty(textBoxCPUFreqRouter.Text) && comboBoxConstraintTypeCPUFreqRouter.SelectedItem != null)
                    constraints.Add(("Частота процессора", comboBoxConstraintTypeCPUFreqRouter.SelectedItem.ToString(), GetDoubleValue(textBoxCPUFreqRouter.Text)));

                if (!string.IsNullOrEmpty(textBoxPriceRouter.Text) && comboBoxConstraintTypePriceRouter.SelectedItem != null)
                    constraints.Add(("Цена", comboBoxConstraintTypePriceRouter.SelectedItem.ToString(), GetDoubleValue(textBoxPriceRouter.Text)));
            }
            else if (checkBox == checkBox3 && tableName == "Cameras")
            {
                if (!string.IsNullOrEmpty(textBoxResolutionCamera.Text) && comboBoxConstraintTypeResolution.SelectedItem != null)
                    constraints.Add(("Разрешение", comboBoxConstraintTypeResolution.SelectedItem.ToString(), GetDoubleValue(textBoxResolutionCamera.Text)));

                if (!string.IsNullOrEmpty(textBoxFPS.Text) && comboBoxConstraintTypeFPS.SelectedItem != null)
                    constraints.Add(("Частота кадров", comboBoxConstraintTypeFPS.SelectedItem.ToString(), GetDoubleValue(textBoxFPS.Text)));

                if (!string.IsNullOrEmpty(textBoxMatrixPixels.Text) && comboBoxConstraintTypeMatrixPixels.SelectedItem != null)
                    constraints.Add(("Число пикселей матрицы", comboBoxConstraintTypeMatrixPixels.SelectedItem.ToString(), GetDoubleValue(textBoxMatrixPixels.Text)));

                if (!string.IsNullOrEmpty(textBoxViewingAngle.Text) && comboBoxConstraintTypeViewingAngle.SelectedItem != null)
                    constraints.Add(("Угол обзора", comboBoxConstraintTypeViewingAngle.SelectedItem.ToString(), GetDoubleValue(textBoxViewingAngle.Text)));

                if (!string.IsNullOrEmpty(textBoxPriceCamera.Text) && comboBoxConstraintTypePriceCamera.SelectedItem != null)
                    constraints.Add(("Цена", comboBoxConstraintTypePriceCamera.SelectedItem.ToString(), GetDoubleValue(textBoxPriceCamera.Text)));
            }
            else if (checkBox == checkBox4 && tableName == "Servers")
            {
                if (!string.IsNullOrEmpty(textBoxCPUFreqServer.Text) && comboBoxConstraintTypeCPUFreqServer.SelectedItem != null)
                    constraints.Add(("Частота процессора", comboBoxConstraintTypeCPUFreqServer.SelectedItem.ToString(), GetDoubleValue(textBoxCPUFreqServer.Text)));

                if (!string.IsNullOrEmpty(textBoxCPUCores.Text) && comboBoxConstraintTypeCPUCores.SelectedItem != null)
                    constraints.Add(("Количество ядер", comboBoxConstraintTypeCPUCores.SelectedItem.ToString(), GetDoubleValue(textBoxCPUCores.Text)));

                if (!string.IsNullOrEmpty(textBoxNumberHardSlots.Text) && comboBoxConstraintTypeNumberHardSlots.SelectedItem != null)
                    constraints.Add(("Количество слотов для жестких дисков", comboBoxConstraintTypeNumberHardSlots.SelectedItem.ToString(), GetDoubleValue(textBoxNumberHardSlots.Text)));

                if (!string.IsNullOrEmpty(textBoxMaxStorage.Text) && comboBoxConstraintTypeMaxStorage.SelectedItem != null)
                    constraints.Add(("Максимальный объем памяти", comboBoxConstraintTypeMaxStorage.SelectedItem.ToString(), GetDoubleValue(textBoxMaxStorage.Text)));

                if (!string.IsNullOrEmpty(textBoxPriceServer.Text) && comboBoxConstraintTypePriceServer.SelectedItem != null)
                    constraints.Add(("Цена", comboBoxConstraintTypePriceServer.SelectedItem.ToString(), GetDoubleValue(textBoxPriceServer.Text)));
            }
            else if (checkBox == checkBox5 && tableName == "Cables")
            {
                if (!string.IsNullOrEmpty(textBoxWireGaugeCable.Text) && comboBoxWireGaugeCable.SelectedItem != null)
                    constraints.Add(("Площадь поперечного сечения", comboBoxWireGaugeCable.SelectedItem.ToString(), GetDoubleValue(textBoxWireGaugeCable.Text)));
                if (!string.IsNullOrEmpty(textBoxCableSpeed.Text) && comboBoxCableSpeed.SelectedItem != null)
                    constraints.Add(("Скорость передачи данных", comboBoxCableSpeed.SelectedItem.ToString(), GetDoubleValue(textBoxCableSpeed.Text)));
                if (!string.IsNullOrEmpty(textBoxCablePrice.Text) && comboBoxCablePrice.SelectedItem != null)
                    constraints.Add(("Цена за метр", comboBoxCablePrice.SelectedItem.ToString(), GetDoubleValue(textBoxCablePrice.Text)));
            }

            return constraints;
        }

        private double GetDoubleValue(string input) // Метод для преобразования строки в число
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                throw new ArgumentException("Поле не может быть пустым.");
            }

            if (!double.TryParse(input, out double result))
            {
                throw new ArgumentException("Некорректное числовое значение.");
            }

            return result;
        }

        private Dictionary<string, string> GetColumnNameMapping(string tableName) // Метод для получения сопоставления названий столбцов
        {
            return tableName switch
            {
                "Switches" => SwitchesColumnNameMapping,
                "Routers" => RoutersColumnNameMapping,
                "Cameras" => CamerasColumnNameMapping,
                "Servers" => ServersColumnNameMapping,
                "Cables" => CablesColumnNameMapping,
                _ => throw new ArgumentException("Неизвестная таблица")
            };
        }

        private Dictionary<string, object> GetDataFromDatabase(string tableName) // Метод для получения данных из базы данных
        {
            if (tableName == null) throw new ArgumentNullException(nameof(tableName));

            var columnNameMapping = GetColumnNameMapping(tableName);
            var data = new Dictionary<string, List<double>>();
            var modelNames = new List<string>();

            // Инициализируем списки для каждого критерия
            foreach (var mapping in columnNameMapping)
            {
                data[mapping.Key] = new List<double>();
            }

            string query = $"SELECT ModelName, {string.Join(", ", columnNameMapping.Values)} FROM {tableName}";
            using (SQLiteDataReader reader = dbManager.ExecuteQuery(query))
            {
                while (reader.Read())
                {
                    try
                    {
                        modelNames.Add(reader.GetString(0)); // Получаем название модели
                        int columnOffset = 1; // Смещение на 1 из-за ModelName
                        foreach (var mapping in columnNameMapping)
                        {
                            double value = reader.GetDouble(columnOffset++);
                            data[mapping.Key].Add(value);
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

        private string SolveLinearProgram(string objectiveFunction, string selectedCriterion, List<(string criteria, string constraintType, double value)> constraints, List<double> data) // Метод для решения линейной программы
        {
            if (data == null || data.Count == 0)
                throw new ArgumentException("Нет данных для оптимизации.");

            // Найти максимум/минимум в данных
            double optimalValue = objectiveFunction == "max" ? data.Max() : data.Min();

            // Учитываем ограничения
            var filteredData = data.Where(value =>
                constraints.All(c =>
                    (c.constraintType == ">=" && value >= c.value) ||
                    (c.constraintType == "<=" && value <= c.value) ||
                    (c.constraintType == "=" && value == c.value)
                )
            ).ToList();

            if (filteredData.Count == 0)
                return "Нет решений, удовлетворяющих ограничениям.";

            // После применения ограничений выбираем оптимальное значение
            optimalValue = objectiveFunction == "max" ? filteredData.Max() : filteredData.Min();

            return $"Оптимальная модель: {optimalValue}";
        }

        private void CalculateCable_Click(object sender, EventArgs e) // Обработчик нажатия кнопки расчета кабеля
        {
            try
            {
                SolveForDevice("Cables", checkBox5, listBoxFunction5, listBoxCriteria5);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}");
            }
        }
    }
}



