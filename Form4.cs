using System.Data.SQLite;
using System.Data;
using System.Text;
using static diplom.Form1;
using System.Diagnostics;

namespace diplom
{
    public partial class Form4 : Form // Форма для генетического алгоритма
    {
        private DatabaseManager dbManager; // Менеджер базы данных
        private StringBuilder calculationLog = new StringBuilder(); // Лог-файл вычислений генетического алгоритм
        private bool isFirstSave = true; // Флаг для отслеживания первого сохранения в сессии

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

        private Dictionary<string, string> CablesColumnNameMapping = new Dictionary<string, string> // Словарь для кабелей(витая пара)
        {
            { "Площадь поперечного сечения", "WireGauge" },
            { "Скорость передачи данных", "SpeedCable" },
            { "Цена за метр", "PriceCable" }
        };

        private static readonly Dictionary<string, string> DeviceTypeRu = new Dictionary<string, string> // Словарь для перевода типов устройств на русский язык
        {
            { "Switches", "коммутатор" },
            { "Routers", "маршрутизатор" },
            { "Cameras", "IP-камера" },
            { "Servers", "сервер" },
            { "Cables", "кабель" }
        };

        private static readonly Dictionary<string, string> DeviceFieldRu = new Dictionary<string, string> // Словарь для перевода технических характеристик устройств на русский язык
        {
            { "Количество портов", "Количество портов" },
            { "Скорость", "Скорость" },
            { "Количество записей MAC адресов", "Количество записей MAC адресов" },
            { "Потребляемая мощность", "Потребляемая мощность" },
            { "Цена", "Цена" },
            { "Объем оперативной памяти", "Объем оперативной памяти" },
            { "Частота процессора", "Частота процессора" },
            { "Разрешение", "Разрешение" },
            { "Частота кадров", "Частота кадров" },
            { "Число пикселей матрицы", "Число пикселей матрицы" },
            { "Угол обзора", "Угол обзора" },
            { "Количество ядер", "Количество ядер" },
            { "Количество слотов для жестких дисков", "Количество слотов для жестких дисков" },
            { "Максимальный объем памяти", "Максимальный объем памяти" },
            { "Площадь поперечного сечения", "Площадь поперечного сечения" },
            { "Скорость передачи данных", "Скорость передачи данных" },
            { "Цена за метр", "Цена за метр" }
        };

        public Form4() 
        {
            InitializeComponent();
            string connectionString = @"Data Source=A:\DevicesDatabase;Version=3;";
            dbManager = new DatabaseManager(connectionString); // Инициализация менеджера базы данных

            InitializeFunctionListBoxes(); // Инициализация списков функций
            InitializeCriteriaListBoxes(); // Инициализация списков критериев
            InitializeComboBoxes(); // Инициализация выпадающих списков

            // Добавление обработчиков событий для чекбоксов
            checkBox1.CheckedChanged += (s, e) => ToggleDeviceControls("Switches", checkBox1.Checked);
            checkBox2.CheckedChanged += (s, e) => ToggleDeviceControls("Routers", checkBox2.Checked);
            checkBox3.CheckedChanged += (s, e) => ToggleDeviceControls("Cameras", checkBox3.Checked);
            checkBox4.CheckedChanged += (s, e) => ToggleDeviceControls("Servers", checkBox4.Checked);
            checkBox5.CheckedChanged += (s, e) => ToggleDeviceControls("Cables", checkBox5.Checked);

            // Отключение всех элементов управления при запуске
            ToggleDeviceControls("Switches", false);
            ToggleDeviceControls("Routers", false);
            ToggleDeviceControls("Cameras", false);
            ToggleDeviceControls("Servers", false);

            // Заполнение списка типов экранирования
            checkedListBoxShielding.Items.Clear();
            checkedListBoxShielding.Items.AddRange(new object[] { "U/UTP", "F/UTP", "SF/UTP", "S/FTP", "U/FTP", "F/FTP" });

            // Инициализация поддержки WiFi/SFP
            WiFi_SFP.Items.Clear();
            WiFi_SFP.Items.Add("Поддержка Wi-Fi");
            WiFi_SFP.Items.Add("Поддержка оптоволокна (SFP)");

            // Настройка поддержки WiFi/SFP
            WiFi_SFP.Enabled = false;
            checkBox2.CheckedChanged += (s, e) => WiFi_SFP.Enabled = checkBox2.Checked;

            // Установка начального состояния элементов управления кабеля
            ToggleDeviceControls("Cables", checkBox5.Checked);
        }

        private void InitializeFunctionListBoxes() // Инициализация списков функций минимизации и максимизации
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

        private void ToggleDeviceControls(string deviceType, bool enabled) // Метод для включения/отключения элементов управления устройств
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
                    textBoxPopulationSizeSwitch.Enabled = enabled;
                    textBoxGenerationsSwitch.Enabled = enabled;
                    textBoxMutationRateSwitch.Enabled = enabled;
                    textBoxCrossoverRateSwitch.Enabled = enabled;
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
                    textBoxPopulationSizeRouter.Enabled = enabled;
                    textBoxGenerationsRouter.Enabled = enabled;
                    textBoxMutationRateRouter.Enabled = enabled;
                    textBoxCrossoverRateRouter.Enabled = enabled;
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
                    textBoxPopulationSizeCamera.Enabled = enabled;
                    textBoxGenerationsCamera.Enabled = enabled;
                    textBoxMutationRateCamera.Enabled = enabled;
                    textBoxCrossoverRateCamera.Enabled = enabled;
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
                    textBoxPopulationSizeServer.Enabled = enabled;
                    textBoxGenerationsServer.Enabled = enabled;
                    textBoxMutationRateServer.Enabled = enabled;
                    textBoxCrossoverRateServer.Enabled = enabled;
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
                    textBoxPopulationSizeCable.Enabled = enabled;
                    textBoxGenerationsCable.Enabled = enabled;
                    textBoxMutationRateCable.Enabled = enabled;
                    textBoxCrossoverRateCable.Enabled = enabled;
                    CalculateCable.Enabled = enabled;
                    textBox1.Enabled = enabled;
                    break;
            }
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

        private List<Func<double[], bool>> ConvertConstraints(List<(string criteria, string constraintType, double value)> constraintsList, Dictionary<string, object> allData, string selectedCriterion) // Метод для преобразования ограничений
        {
            var constraints = new List<Func<double[], bool>>();
            var modelNames = (string[])allData["ModelNames"];

            LogCalculation("\nПреобразование ограничений в функции проверки:");
            foreach (var constraint in constraintsList)
            {
                LogCalculation($"\nОбработка ограничения: {constraint.criteria} {constraint.constraintType} {constraint.value}");

                // Получаем массив значений для данного критерия
                var criterionName = constraint.criteria;
                var constraintValue = constraint.value;
                var constraintType = constraint.constraintType;

                // Проверяем наличие критерия в данных
                if (!allData.ContainsKey(criterionName))
                {
                    LogCalculation($"- Ошибка: критерий '{criterionName}' отсутствует в данных");
                    LogCalculation("- Доступные критерии:");
                    foreach (var key in allData.Keys)
                    {
                        LogCalculation($"  * {key}");
                    }
                    continue;
                }

                var criterionValues = (double[])allData[criterionName];

                LogCalculation($"- Создание функции проверки для {criterionName}");
                LogCalculation("- Значения критерия для всех моделей:");
                for (int i = 0; i < criterionValues.Length; i++)
                {
                    bool satisfiesConstraint = constraintType switch
                    {
                        ">=" => criterionValues[i] >= constraintValue,
                        "<=" => criterionValues[i] <= constraintValue,
                        "=" => Math.Abs(criterionValues[i] - constraintValue) < 0.0001,
                        _ => false
                    };
                    LogCalculation($"  * {modelNames[i]}: значение={criterionValues[i]}, {constraintType}{constraintValue} -> {(satisfiesConstraint ? "удовлетворяет" : "не удовлетворяет")}");
                }

                constraints.Add(individual =>
                {
                    try
                    {
                        if (individual == null || individual.Length == 0)
                        {
                            LogCalculation("- Ошибка: пустой набор значений");
                            return false;
                        }

                        // Находим индекс модели в массиве значений критерия оптимизации
                        int modelIndex = -1;
                        double targetValue = individual[0];

                        // Ищем индекс модели в массиве значений критерия оптимизации
                        var optimizationCriterionValues = (double[])allData[selectedCriterion];
                        for (int i = 0; i < optimizationCriterionValues.Length; i++)
                        {
                            if (Math.Abs(optimizationCriterionValues[i] - targetValue) < 0.0001)
                            {
                                modelIndex = i;
                                break;
                            }
                        }

                        if (modelIndex == -1)
                        {
                            LogCalculation($"- Ошибка: значение {targetValue} не найдено в критерии {selectedCriterion}");
                            // Выводим все значения для отладки
                            LogCalculation("- Значения критерия:");
                            for (int i = 0; i < optimizationCriterionValues.Length; i++)
                            {
                                LogCalculation($"  * Модель {modelNames[i]}: {optimizationCriterionValues[i]}");
                            }
                            return false;
                        }

                        // Получаем значение для текущего критерия
                        double actualValue = criterionValues[modelIndex];

                        // Проверяем ограничение
                        bool result = constraintType switch
                        {
                            ">=" => actualValue >= constraintValue,
                            "<=" => actualValue <= constraintValue,
                            "=" => Math.Abs(actualValue - constraintValue) < 0.0001,
                            _ => false
                        };

                        LogCalculation($"- Проверка {criterionName} для модели {modelNames[modelIndex]}: значение={actualValue}, условие={constraintType}{constraintValue}, результат={result}");
                        return result;
                    }
                    catch (Exception ex)
                    {
                        LogCalculation($"- Ошибка при проверке ограничения: {ex.Message}");
                        if (ex.StackTrace != null)
                            LogCalculation($"Stack Trace: {ex.StackTrace}");
                        return false;
                    }
                });
            }

            return constraints;
        }

        private void LogCalculation(string message) // Метод для логирования вычислений
        {
            Debug.WriteLine(message);
            calculationLog.AppendLine(message);
        }

        private void SolveForDevice(string tableName, CheckBox checkBox, ListBox listBoxFunction, ListBox listBoxCriteria) // Метод для решения задачи подбора оптимального устройства
        {
            try
            {
                calculationLog.Clear();
                string objectiveFunctionType = listBoxFunction.SelectedItem?.ToString() ??
                    throw new InvalidOperationException("Не выбрана целевая функция");
                string selectedCriterion = listBoxCriteria.SelectedItem?.ToString() ??
                    throw new InvalidOperationException("Не выбран критерий");
                LogCalculation("\n=== Начало поиска решения ===");
                LogCalculation($"Целевая функция: {objectiveFunctionType}");
                LogCalculation($"Выбранный критерий: {selectedCriterion}");
                Dictionary<string, object> allData;
                if (tableName == "Cables")
                {
                    var selectedShieldings = checkedListBoxShielding.CheckedItems.Cast<string>().ToList();
                    if (selectedShieldings.Count == 0)
                    {
                        MessageBox.Show("Пожалуйста, выберите тип экранирования.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                    allData = GetCableDataFilteredByShielding(selectedShieldings);
                }
                else
                {
                    allData = GetDataFromDatabase(tableName);
                }
                if (allData == null || !allData.Any())
                    throw new InvalidOperationException($"Не удалось получить данные из таблицы '{tableName}'");

                // Получаем числовые характеристики для генетического алгоритма
                Dictionary<string, double[]> numericAllData;
                if (tableName == "Routers")
                {   // Для маршрутизаторов все числовые данные, кроме ModelNames, CharacteristicsCount, WiFiStatus, SFPStatus
                    numericAllData = allData.Where(kvp => kvp.Key != "ModelNames" && kvp.Key != "CharacteristicsCount" && kvp.Key != "WiFiStatus" && kvp.Key != "SFPStatus")
                                        .ToDictionary(kvp => kvp.Key, kvp => (double[])kvp.Value);
                }
                else
                {   // Для других устройств используем все данные, кроме ModelNames, CharacteristicsCount, Shielding (для кабелей), SFP (для коммутаторов)
                    numericAllData = allData.Where(kvp => kvp.Key != "ModelNames" && kvp.Key != "CharacteristicsCount" && kvp.Key != "Shielding" && kvp.Key != "SFP")
                                        .ToDictionary(kvp => kvp.Key, kvp => (double[])kvp.Value);
                }

                var modelNames = (string[])allData["ModelNames"];
                LogCalculation($"\nЗагружено моделей: {modelNames.Length}");

                LogCalculation("Список всех моделей и их числовых характеристик:");
                for (int i = 0; i < modelNames.Length; i++)
                {
                    LogCalculation($"\nМодель {i + 1}: {modelNames[i]}");
                    foreach (var key in numericAllData.Keys)
                    {
                        var values = (double[])allData[key];
                        LogCalculation($"- {key}: {values[i]}");
                    }
                }

                // Логирование строковых статусов для маршрутизаторов
                if (tableName == "Routers" && allData.ContainsKey("WiFiStatus") && allData.ContainsKey("SFPStatus"))
                {
                    var wifiStatus = (string[])allData["WiFiStatus"];
                    var sfpStatus = (string[])allData["SFPStatus"];
                    LogCalculation("\nСтроковые статусы для маршрутизаторов:");
                    LogCalculation($"- WiFiStatus: {string.Join(", ", wifiStatus)}");
                    LogCalculation($"- SFPStatus: {string.Join(", ", sfpStatus)}");
                }

                if (!numericAllData.ContainsKey(selectedCriterion))
                {
                    throw new InvalidOperationException($"Критерий '{selectedCriterion}' не найден в данных");
                }
                var criterionData = (double[])numericAllData[selectedCriterion];
                if (criterionData == null || criterionData.Length == 0)
                    throw new InvalidOperationException("Отсутствуют данные для выбранного критерия");
                var constraintsList = GetConstraints(checkBox, tableName);
                var constraints = ConvertConstraints(constraintsList, allData, selectedCriterion);
                int populationSize = GetIntValue(tableName switch
                {
                    "Switches" => textBoxPopulationSizeSwitch.Text,
                    "Routers" => textBoxPopulationSizeRouter.Text,
                    "Cameras" => textBoxPopulationSizeCamera.Text,
                    "Servers" => textBoxPopulationSizeServer.Text,
                    "Cables" => textBoxPopulationSizeCable.Text,
                    _ => throw new InvalidOperationException("Неизвестный тип устройства")
                });
                int maxGenerations = GetIntValue(tableName switch
                {
                    "Switches" => textBoxGenerationsSwitch.Text,
                    "Routers" => textBoxGenerationsRouter.Text,
                    "Cameras" => textBoxGenerationsCamera.Text,
                    "Servers" => textBoxGenerationsServer.Text,
                    "Cables" => textBoxGenerationsCable.Text,
                    _ => throw new InvalidOperationException("Неизвестный тип устройства")
                });
                double mutationRate = GetDoubleValue(tableName switch
                {
                    "Switches" => textBoxMutationRateSwitch.Text,
                    "Routers" => textBoxMutationRateRouter.Text,
                    "Cameras" => textBoxMutationRateCamera.Text,
                    "Servers" => textBoxMutationRateServer.Text,
                    "Cables" => textBoxMutationRateCable.Text,
                    _ => throw new InvalidOperationException("Неизвестный тип устройства")
                });
                double crossoverRate = GetDoubleValue(tableName switch
                {
                    "Switches" => textBoxCrossoverRateSwitch.Text,
                    "Routers" => textBoxCrossoverRateRouter.Text,
                    "Cameras" => textBoxCrossoverRateCamera.Text,
                    "Servers" => textBoxCrossoverRateServer.Text,
                    "Cables" => textBoxCrossoverRateCable.Text,
                    _ => throw new InvalidOperationException("Неизвестный тип устройства")
                });
                LogCalculation("\nПараметры генетического алгоритма:");
                LogCalculation($"- Размер популяции: {populationSize}");
                LogCalculation($"- Число поколений: {maxGenerations}");
                LogCalculation($"- Вероятность мутации: {mutationRate}");
                LogCalculation($"- Вероятность скрещивания: {crossoverRate}");
                double maxCriterionValue = criterionData.Max();
                double minCriterionValue = criterionData.Min();
                LogCalculation($"\nДиапазон значений критерия: от {minCriterionValue} до {maxCriterionValue}");
                Func<double[], double> fitnessFunction = individual =>
                {
                    try
                    {
                        if (individual == null || individual.Length == 0)
                        {
                            LogCalculation("fitnessFunction: получен пустой набор значений");
                            return double.MinValue;
                        }
                        int modelIndex = -1;
                        for (int i = 0; i < criterionData.Length; i++)
                        {
                            if (Math.Abs(criterionData[i] - individual[0]) < 0.0001)
                            {
                                modelIndex = i;
                                break;
                            }
                        }
                        if (modelIndex == -1)
                        {
                            LogCalculation($"fitnessFunction: значение {individual[0]} не соответствует ни одной модели");
                            return double.MinValue;
                        }
                        foreach (var constraint in constraints)
                        {
                            if (!constraint(individual))
                            {
                                LogCalculation($"fitnessFunction: модель {modelNames[modelIndex]} не удовлетворяет ограничениям");
                                return double.MinValue;
                            }
                        }
                        double value = criterionData[modelIndex];
                        LogCalculation($"fitnessFunction: модель={modelNames[modelIndex]}, {selectedCriterion}={value}, тип оптимизации={objectiveFunctionType}");
                        if (objectiveFunctionType == "max")
                        {
                            LogCalculation($"fitnessFunction: максимизация - фитнес = {value:F2}");
                            return value;
                        }
                        else // "min"
                        {
                            double invertedValue = maxCriterionValue - value;
                            LogCalculation($"fitnessFunction: минимизация - для значения {value:F2}");
                            LogCalculation($"               максимальное значение критерия: {maxCriterionValue:F2}");
                            LogCalculation($"               вычисленный фитнес: {invertedValue:F2}");
                            return invertedValue;
                        }
                    }
                    catch (Exception ex)
                    {
                        LogCalculation($"Ошибка в fitnessFunction: {ex.Message}");
                        return double.MinValue;
                    }
                };
                var geneticAlgorithm = new GeneticAlgorithm(populationSize, mutationRate, maxGenerations,
                    crossoverRate, fitnessFunction, constraints, modelNames);
                var result = geneticAlgorithm.Solve(criterionData);
                ShowResults(result, allData, modelNames, selectedCriterion, objectiveFunctionType, constraintsList, tableName);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}");
                LogCalculation($"\nОшибка: {ex.Message}");
                if (ex.StackTrace != null)
                    LogCalculation($"Stack Trace: {ex.StackTrace}");
            }
        }

        private void ShowResults((double optimalValue, int optimalIndex, string calculations) result, Dictionary<string, object> allData, string[] modelNames, string selectedCriterion, string objectiveFunctionType, List<(string criteria, string constraintType, double value)> constraintsList, string tableName) // Метод для отображения результатов
        {
            // Показываем лог работы генетического алгоритма
            string reportContent = string.IsNullOrEmpty(result.calculations) ? calculationLog.ToString() : result.calculations;
            var reportForm = new Form()
            {
                Text = "Лог работы генетического алгоритма",
                Width = 800,
                Height = 600,
                StartPosition = FormStartPosition.CenterScreen
            };
            var textBox = new TextBox()
            {
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                Dock = DockStyle.Fill,
                Font = new System.Drawing.Font("Consolas", 10),
                Text = reportContent
            };
            reportForm.Controls.Add(textBox);
            reportForm.Show();

            StringBuilder resultMessage = new StringBuilder();
            resultMessage.AppendLine($"Оптимальное значение критерия: {result.optimalValue:F2}");
            string optWord = (tableName == "Cameras") ? "Оптимальная" : "Оптимальный";
            resultMessage.AppendLine($"{optWord} {DeviceTypeRu[tableName]}: {modelNames[result.optimalIndex]}");
            resultMessage.AppendLine("\nХарактеристики выбранной модели:");
            var integerFields = new HashSet<string> {
                "Количество портов",
                "Количество записей MAC адресов",
                "Количество ядер",
                "Количество слотов для жестких дисков",
                "Частота кадров",
                "Потребляемая мощность",
                "Цена",
                "Число пикселей матрицы",
                "Максимальный объем памяти",
                "Разрешение"
            };
            var unitsSwitches = new Dictionary<string, string> {
                {"Количество портов", "шт."},
                {"Скорость", "Гбит/с"},
                {"Количество записей MAC адресов", "записей"},
                {"Потребляемая мощность", "Вт"},
                {"Цена", "руб."}
            };
            var unitsRouters = new Dictionary<string, string> {
                {"Количество портов", "шт."},
                {"Скорость", "Гбит/с"},
                {"Объем оперативной памяти", "ГБ"},
                {"Частота процессора", "ГГц"},
                {"Цена", "руб."}
            };
            var unitsCameras = new Dictionary<string, string> {
                {"Разрешение", "пикселей"},
                {"Частота кадров", "кадр/с"},
                {"Число пикселей матрицы", "Мп"},
                {"Угол обзора", "град."},
                {"Цена", "руб."}
            };
            var unitsServers = new Dictionary<string, string> {
                {"Частота процессора", "ГГц"},
                {"Количество ядер", "шт."},
                {"Количество слотов для жестких дисков", "шт."},
                {"Максимальный объем памяти", "ГБ"},
                {"Цена", "руб."}
            };
            var unitsCables = new Dictionary<string, string> {
                {"Площадь поперечного сечения", "мм²"},
                {"Скорость передачи данных", "Гбит/с"},
                {"Цена за метр", "руб./м"}
            };
            Dictionary<string, string> units = null;
            if (allData.Keys.Contains("Количество портов") && allData.Keys.Contains("Количество записей MAC адресов")) units = unitsSwitches;
            else if (allData.Keys.Contains("Объем оперативной памяти")) units = unitsRouters;
            else if (allData.Keys.Contains("Разрешение")) units = unitsCameras;
            else if (allData.Keys.Contains("Количество ядер")) units = unitsServers;
            else if (allData.Keys.Contains("Площадь поперечного сечения")) units = unitsCables;
            // Выводим характеристики, исключая SFP
            foreach (var key in allData.Keys.Where(k => k != "ModelNames" && k != "CharacteristicsCount" && k != "Shielding" && k != "SFP" && k != "WiFiStatus" && k != "SFPStatus"))
            {
                var values = (double[])allData[key];
                if (result.optimalIndex < values.Length)
                {
                    string ruName = DeviceFieldRu.ContainsKey(key) ? DeviceFieldRu[key] : key;
                    string formattedValue = integerFields.Contains(key)
                        ? values[result.optimalIndex].ToString("F0")
                        : values[result.optimalIndex].ToString("F2");
                    string unit = units != null && units.ContainsKey(key) ? $" {units[key]}" : "";
                    resultMessage.AppendLine($"{ruName}: {formattedValue}{unit}");
                }
            }
            // Добавляем вывод типа экранирования для кабеля
            if (tableName == "Cables")
            {
                // тип экранирования shieldings формируется при выборке данных для кабеля. Если его нет, то получаем из базы
                List<string> shieldings = null;
                if (allData.ContainsKey("Shielding"))
                {
                    var shieldArr = allData["Shielding"] as string[];
                    if (shieldArr != null)
                        shieldings = shieldArr.ToList();
                }
                // Если тип экранирования shieldings есть и индекс в диапазоне
                if (shieldings != null && result.optimalIndex < shieldings.Count)
                {
                    resultMessage.AppendLine($"Тип экранирования: {shieldings[result.optimalIndex]}");
                }
            }
            // Добавляем вывод информации о поддержке SFP для коммутаторов
            if (tableName == "Switches")
            {
                if (allData.ContainsKey("SFP"))
                {
                    var sfpValues = (double[])allData["SFP"];
                    if (result.optimalIndex < sfpValues.Length)
                    {
                        string sfpStatus = sfpValues[result.optimalIndex] == 1.0 ? "Да" : "Нет";
                        resultMessage.AppendLine($"Поддержка оптоволокна (SFP): {sfpStatus}");
                    }
                }
            }
            // Добавляем вывод информации о поддержке WiFi и SFP для маршрутизаторов
            if (tableName == "Routers")
            {
                // Получаем строковые статусы из allData
                if (allData.ContainsKey("WiFiStatus") && allData.ContainsKey("SFPStatus"))
                {
                    var wifiStatus = (string[])allData["WiFiStatus"];
                    var sfpStatus = (string[])allData["SFPStatus"];
                    if (result.optimalIndex < wifiStatus.Length && result.optimalIndex < sfpStatus.Length)
                    {                                                
                        resultMessage.AppendLine($"Поддержка Wi-Fi: {wifiStatus[result.optimalIndex]}");
                        resultMessage.AppendLine($"Поддержка оптоволокна (SFP): {sfpStatus[result.optimalIndex]}");
                    }
                }
            }
            MessageBox.Show(resultMessage.ToString(), "Результат оптимизации");

            // Логика сохранения в базу данных
            DatabaseManager.LastRecordId++; // Увеличиваем статический счетчик для нового уникального ID
            int uniqueId = DatabaseManager.LastRecordId; // Присваиваем новый уникальный ID

            string query = "INSERT INTO DevicesDatabase (ID, ConfigurationNumber, DeviceType, TechnicalChar, OptimizationMethod) VALUES (@id, @configNumber, @deviceType, @technicalChar, @optimizationMethod)";
            var parameters = new Dictionary<string, object>
            {
                { "@id", uniqueId }, // Используем сгенерированный уникальный ID
                { "@configNumber", dbManager.CurrentConfigurationNumber }, // Используем номер конфигурации из текущей сессии DatabaseManager
                { "@deviceType", DeviceTypeRu[tableName] },
                { "@technicalChar", resultMessage.ToString().Replace($"Оптимальное значение критерия: {result.optimalValue:F2}\n", "").Replace($"{DeviceTypeRu[tableName]} {DeviceTypeRu[tableName]}: {modelNames[result.optimalIndex]}\n\nХарактеристики выбранной модели:\n", "") },
                { "@optimizationMethod", "Генетический алгоритм" }
            };

            if (dbManager != null)
            {
                int rowsAffected = dbManager.ExecuteNonQuery(query, parameters);
                MessageBox.Show($"Запрос выполнен. Добавлено строк: {rowsAffected}", "Сохранение данных", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show("Ошибка: Соединение с базой данных не установлено.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private List<(string criteria, string constraintType, double value)> GetConstraints(CheckBox checkBox, string tableName) // Метод для получения ограничений
        {
            var constraints = new List<(string criteria, string constraintType, double value)>();

            try
            {
                switch (tableName)
                {
                    case "Switches":
                        if (!string.IsNullOrEmpty(textBoxPortSwitch.Text) && !string.IsNullOrEmpty(comboBoxConstraintTypePortSwitch.Text))
                        {
                            if (double.TryParse(textBoxPortSwitch.Text.Trim(), out double portsValue))
                            {
                                constraints.Add(("Количество портов", comboBoxConstraintTypePortSwitch.Text.Trim(), portsValue));
                            }
                        }
                        if (!string.IsNullOrEmpty(textBoxSpeedSwitch.Text) && !string.IsNullOrEmpty(comboBoxConstraintTypeSpeedSwitch.Text))
                        {
                            if (double.TryParse(textBoxSpeedSwitch.Text.Trim(), out double speedValue))
                            {
                                constraints.Add(("Скорость", comboBoxConstraintTypeSpeedSwitch.Text.Trim(), speedValue));
                            }
                        }
                        if (!string.IsNullOrEmpty(textBoxMAC.Text) && !string.IsNullOrEmpty(comboBoxConstraintTypeMAC.Text))
                        {
                            if (double.TryParse(textBoxMAC.Text.Trim(), out double macValue))
                            {
                                constraints.Add(("Количество записей MAC адресов", comboBoxConstraintTypeMAC.Text.Trim(), macValue));
                            }
                        }
                        if (!string.IsNullOrEmpty(textBoxPower.Text) && !string.IsNullOrEmpty(comboBoxConstraintTypePower.Text))
                        {
                            if (double.TryParse(textBoxPower.Text.Trim(), out double powerValue))
                            {
                                constraints.Add(("Потребляемая мощность", comboBoxConstraintTypePower.Text.Trim(), powerValue));
                            }
                        }
                        if (!string.IsNullOrEmpty(textBoxPriceSwitch.Text) && !string.IsNullOrEmpty(comboBoxConstraintTypePriceSwitch.Text))
                        {
                            if (double.TryParse(textBoxPriceSwitch.Text.Trim(), out double priceValue))
                            {
                                constraints.Add(("Цена", comboBoxConstraintTypePriceSwitch.Text.Trim(), priceValue));
                            }
                        }
                        break;

                    case "Routers":
                        LogCalculation("\nФормирование ограничений для маршрутизаторов:");

                        // Ограничение по портам
                        if (!string.IsNullOrEmpty(textBoxPortRouter.Text) && !string.IsNullOrEmpty(comboBoxConstraintTypePortRouter.Text))
                        {
                            if (double.TryParse(textBoxPortRouter.Text.Trim(), out double portsValue))
                            {
                                LogCalculation($"- Добавляю ограничение по портам: {comboBoxConstraintTypePortRouter.Text.Trim()} {portsValue}");
                                constraints.Add(("Количество портов", comboBoxConstraintTypePortRouter.Text.Trim(), portsValue));
                            }
                        }

                        // Ограничение по скорости
                        if (!string.IsNullOrEmpty(textBoxSpeedRouter.Text) && !string.IsNullOrEmpty(comboBoxConstraintTypeSpeedRouter.Text))
                        {
                            if (double.TryParse(textBoxSpeedRouter.Text.Trim(), out double speedValue))
                            {
                                LogCalculation($"- Добавляю ограничение по скорости: {comboBoxConstraintTypeSpeedRouter.Text.Trim()} {speedValue}");
                                constraints.Add(("Скорость", comboBoxConstraintTypeSpeedRouter.Text.Trim(), speedValue));
                            }
                        }

                        // Ограничение по RAM
                        if (!string.IsNullOrEmpty(textBoxRAMRouter.Text) && !string.IsNullOrEmpty(comboBoxConstraintTypeRAMRouter.Text))
                        {
                            if (double.TryParse(textBoxRAMRouter.Text.Trim(), out double ramValue))
                            {
                                LogCalculation($"- Добавляю ограничение по RAM: {comboBoxConstraintTypeRAMRouter.Text.Trim()} {ramValue}");
                                constraints.Add(("Объем оперативной памяти", comboBoxConstraintTypeRAMRouter.Text.Trim(), ramValue));
                            }
                        }

                        // Ограничение по частоте процессора
                        if (!string.IsNullOrEmpty(textBoxCPUFreqRouter.Text) && !string.IsNullOrEmpty(comboBoxConstraintTypeCPUFreqRouter.Text))
                        {
                            if (double.TryParse(textBoxCPUFreqRouter.Text.Trim(), out double cpuValue))
                            {
                                LogCalculation($"- Добавляю ограничение по CPU: {comboBoxConstraintTypeCPUFreqRouter.Text.Trim()} {cpuValue}");
                                constraints.Add(("Частота процессора", comboBoxConstraintTypeCPUFreqRouter.Text.Trim(), cpuValue));
                            }
                        }

                        // Ограничение по цене
                        if (!string.IsNullOrEmpty(textBoxPriceRouter.Text) && !string.IsNullOrEmpty(comboBoxConstraintTypePriceRouter.Text))
                        {
                            if (double.TryParse(textBoxPriceRouter.Text.Trim(), out double priceValue))
                            {
                                LogCalculation($"- Добавляю ограничение по цене: {comboBoxConstraintTypePriceRouter.Text.Trim()} {priceValue}");
                                constraints.Add(("Цена", comboBoxConstraintTypePriceRouter.Text.Trim(), priceValue));
                            }
                        }

                        LogCalculation($"\nВсего добавлено ограничений для маршрутизатора: {constraints.Count}");
                        foreach (var constraint in constraints)
                        {
                            LogCalculation($"- {constraint.criteria}: {constraint.constraintType} {constraint.value}");
                        }
                        break;

                    case "Cameras":
                        if (!string.IsNullOrEmpty(textBoxResolutionCamera.Text) && !string.IsNullOrEmpty(comboBoxConstraintTypeResolution.Text))
                        {
                            if (double.TryParse(textBoxResolutionCamera.Text.Trim(), out double resolutionValue))
                            {
                                constraints.Add(("Разрешение", comboBoxConstraintTypeResolution.Text.Trim(), resolutionValue));
                            }
                        }
                        if (!string.IsNullOrEmpty(textBoxFPS.Text) && !string.IsNullOrEmpty(comboBoxConstraintTypeFPS.Text))
                        {
                            if (double.TryParse(textBoxFPS.Text.Trim(), out double fpsValue))
                            {
                                constraints.Add(("Частота кадров", comboBoxConstraintTypeFPS.Text.Trim(), fpsValue));
                            }
                        }
                        if (!string.IsNullOrEmpty(textBoxMatrixPixels.Text) && !string.IsNullOrEmpty(comboBoxConstraintTypeMatrixPixels.Text))
                        {
                            if (double.TryParse(textBoxMatrixPixels.Text.Trim(), out double pixelsValue))
                            {
                                constraints.Add(("Число пикселей матрицы", comboBoxConstraintTypeMatrixPixels.Text.Trim(), pixelsValue));
                            }
                        }
                        if (!string.IsNullOrEmpty(textBoxViewingAngle.Text) && !string.IsNullOrEmpty(comboBoxConstraintTypeViewingAngle.Text))
                        {
                            if (double.TryParse(textBoxViewingAngle.Text.Trim(), out double angleValue))
                            {
                                constraints.Add(("Угол обзора", comboBoxConstraintTypeViewingAngle.Text.Trim(), angleValue));
                            }
                        }
                        if (!string.IsNullOrEmpty(textBoxPriceCamera.Text) && !string.IsNullOrEmpty(comboBoxConstraintTypePriceCamera.Text))
                        {
                            if (double.TryParse(textBoxPriceCamera.Text.Trim(), out double priceValue))
                            {
                                constraints.Add(("Цена", comboBoxConstraintTypePriceCamera.Text.Trim(), priceValue));
                            }
                        }
                        break;

                    case "Servers":
                        if (!string.IsNullOrEmpty(textBoxCPUFreqServer.Text) && !string.IsNullOrEmpty(comboBoxConstraintTypeCPUFreqServer.Text))
                        {
                            if (double.TryParse(textBoxCPUFreqServer.Text.Trim(), out double cpuFreqValue))
                            {
                                constraints.Add(("Частота процессора", comboBoxConstraintTypeCPUFreqServer.Text.Trim(), cpuFreqValue));
                            }
                        }
                        if (!string.IsNullOrEmpty(textBoxCPUCores.Text) && !string.IsNullOrEmpty(comboBoxConstraintTypeCPUCores.Text))
                        {
                            if (double.TryParse(textBoxCPUCores.Text.Trim(), out double cpuCoresValue))
                            {
                                constraints.Add(("Количество ядер", comboBoxConstraintTypeCPUCores.Text.Trim(), cpuCoresValue));
                            }
                        }
                        if (!string.IsNullOrEmpty(textBoxNumberHardSlots.Text) && !string.IsNullOrEmpty(comboBoxConstraintTypeNumberHardSlots.Text))
                        {
                            if (double.TryParse(textBoxNumberHardSlots.Text.Trim(), out double slotsValue))
                            {
                                constraints.Add(("Количество слотов для жестких дисков", comboBoxConstraintTypeNumberHardSlots.Text.Trim(), slotsValue));
                            }
                        }
                        if (!string.IsNullOrEmpty(textBoxMaxStorage.Text) && !string.IsNullOrEmpty(comboBoxConstraintTypeMaxStorage.Text))
                        {
                            if (double.TryParse(textBoxMaxStorage.Text.Trim(), out double storageValue))
                            {
                                constraints.Add(("Максимальный объем памяти", comboBoxConstraintTypeMaxStorage.Text.Trim(), storageValue));
                            }
                        }
                        if (!string.IsNullOrEmpty(textBoxPriceServer.Text) && !string.IsNullOrEmpty(comboBoxConstraintTypePriceServer.Text))
                        {
                            if (double.TryParse(textBoxPriceServer.Text.Trim(), out double priceValue))
                            {
                                constraints.Add(("Цена", comboBoxConstraintTypePriceServer.Text.Trim(), priceValue));
                            }
                        }
                        break;

                    case "Cables":
                        if (!string.IsNullOrEmpty(textBoxWireGaugeCable.Text) && comboBoxWireGaugeCable.SelectedItem != null)
                            constraints.Add(("Площадь поперечного сечения", comboBoxWireGaugeCable.SelectedItem.ToString(), GetDoubleValue(textBoxWireGaugeCable.Text)));
                        if (!string.IsNullOrEmpty(textBoxCableSpeed.Text) && comboBoxCableSpeed.SelectedItem != null)
                            constraints.Add(("Скорость передачи данных", comboBoxCableSpeed.SelectedItem.ToString(), GetDoubleValue(textBoxCableSpeed.Text)));
                        if (!string.IsNullOrEmpty(textBoxCablePrice.Text) && comboBoxCablePrice.SelectedItem != null)
                            constraints.Add(("Цена за метр", comboBoxCablePrice.SelectedItem.ToString(), GetDoubleValue(textBoxCablePrice.Text)));
                        break;
                }

                // Выводим информацию о добавленных ограничениях
                if (constraints.Count == 0)
                {
                    LogCalculation("\nПредупреждение: не заданы ограничения");
                    MessageBox.Show("Не заданы ограничения. Будут рассмотрены все доступные модели.");
                }
                else
                {
                    LogCalculation("\nИтоговый список всех ограничений:");
                    foreach (var constraint in constraints)
                    {
                        LogCalculation($"- {constraint.criteria} {constraint.constraintType} {constraint.value}");
                    }
                }

                return constraints;
            }
            catch (Exception ex)
            {
                LogCalculation($"\nОшибка при формировании ограничений: {ex.Message}");
                if (ex.StackTrace != null)
                    LogCalculation($"Stack Trace: {ex.StackTrace}");
                MessageBox.Show($"Ошибка при формировании ограничений: {ex.Message}");
                return new List<(string criteria, string constraintType, double value)>();
            }
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

        private int GetIntValue(string input) // Метод для преобразования строки в целое число
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                throw new ArgumentException("Поле не может быть пустым.");
            }

            if (!int.TryParse(input, out int result))
            {
                throw new ArgumentException("Некорректное целое значение.");
            }

            return result;
        }

        private Dictionary<string, object> GetDataFromDatabase(string tableName) // Метод для получения данных из базы данных
        {
            var columnNameMapping = GetColumnNameMapping(tableName);
            var data = new Dictionary<string, List<double>>();
            var modelNames = new List<string>();

            // Для кабелей — отдельная логика с Shielding
            if (tableName == "Cables")
            {
                var shieldings = new List<string>();
                string cableQuery = $"SELECT ModelName, {string.Join(", ", columnNameMapping.Values)}, Shielding FROM {tableName}";
                using (SQLiteDataReader reader = dbManager.ExecuteQuery(cableQuery))
                {
                    while (reader.Read())
                    {
                        modelNames.Add(reader.GetString(0));
                        int columnOffset = 1;
                        foreach (var mapping in columnNameMapping)
                        {
                            double value = reader.GetDouble(columnOffset++);
                            if (!data.ContainsKey(mapping.Key))
                                data[mapping.Key] = new List<double>();
                            data[mapping.Key].Add(value);
                        }
                        shieldings.Add(reader["Shielding"].ToString());
                    }
                }
                var cableResult = new Dictionary<string, object>();
                foreach (var kvp in data)
                    cableResult[kvp.Key] = kvp.Value.ToArray();
                cableResult["ModelNames"] = modelNames.ToArray();
                cableResult["Shielding"] = shieldings.ToArray();
                cableResult["CharacteristicsCount"] = columnNameMapping.Count;
                return cableResult;
            }

            // Для маршрутизаторов собираем числовые характеристики и строковые статусы отдельно
            if (tableName == "Routers")
            {
                var numericData = new Dictionary<string, List<double>>();
                var modelNamesList = new List<string>();
                var wifiStatusList = new List<string>();
                var sfpStatusList = new List<string>();

                List<string> filterConditions = new List<string>();
                var selectedWiFiSfpFilters = WiFi_SFP.CheckedItems.Cast<string>().ToList();

                if (selectedWiFiSfpFilters.Contains("Поддержка Wi-Fi"))
                {
                    filterConditions.Add("WiFi = 'да'");
                }
                if (selectedWiFiSfpFilters.Contains("Поддержка оптоволокна (SFP)"))
                {
                    filterConditions.Add("SFP = 'да'");
                }

                string whereClause = filterConditions.Count > 0 ? $" WHERE {string.Join(" AND ", filterConditions)}" : "";

                // Выбираем все столбцы для маршрутизаторов, включая WiFi и SFP
                string routerQuery = $"SELECT ModelName, {string.Join(", ", columnNameMapping.Values)}, WiFi, SFP FROM {tableName}{whereClause}";

                using (SQLiteDataReader reader = dbManager.ExecuteQuery(routerQuery))
                {
                    while (reader.Read())
                    {
                        modelNamesList.Add(reader.GetString(reader.GetOrdinal("ModelName")));

                        // Считываем числовые характеристики
                        foreach (var mapping in columnNameMapping)
                        {
                            if (!numericData.ContainsKey(mapping.Key))
                                numericData[mapping.Key] = new List<double>();
                            numericData[mapping.Key].Add(reader.GetDouble(reader.GetOrdinal(mapping.Value)));
                        }

                        // Считываем строковые статусы WiFi и SFP
                         try
                        {
                            wifiStatusList.Add(reader.GetString(reader.GetOrdinal("WiFi")));
                         }
                         catch (IndexOutOfRangeException)
                         {
                             wifiStatusList.Add("нет данных");
                         }

                         try
                        {
                             sfpStatusList.Add(reader.GetString(reader.GetOrdinal("SFP")));
                         }
                         catch (IndexOutOfRangeException)
                         {
                             sfpStatusList.Add("нет данных");
                         }
                    }
                }

                if (modelNamesList.Count == 0)
            {
                    MessageBox.Show("Нет маршрутизаторов с выбранными характеристиками, удовлетворяющих критериям.", "Результат", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return new Dictionary<string, object>(); // Возвращаем пустой словарь
                }

                // Собираем результат: числовые характеристики и строковые статусы отдельно
                var routerResult = new Dictionary<string, object>();
                routerResult["ModelNames"] = modelNamesList.ToArray();
                
                // Добавляем числовые характеристики
                foreach(var kvp in numericData)
                {
                    routerResult[kvp.Key] = kvp.Value.ToArray();
                }

                // Добавляем строковые статусы
                routerResult["WiFiStatus"] = wifiStatusList.ToArray();
                routerResult["SFPStatus"] = sfpStatusList.ToArray();
                routerResult["CharacteristicsCount"] = columnNameMapping.Count; // Считаем только числовые характеристики

                return routerResult;
            }

            string deviceQuery = $"SELECT ModelName, {string.Join(", ", columnNameMapping.Values)} FROM {tableName}";

            // Добавляем выборку столбца SFP для коммутаторов
            if (tableName == "Switches")
            {
                deviceQuery = $"SELECT ModelName, {string.Join(", ", columnNameMapping.Values)}, SFP FROM {tableName}";
            }

            using (SQLiteDataReader reader = dbManager.ExecuteQuery(deviceQuery))
            {
                while (reader.Read())
                {
                    modelNames.Add(reader.GetString(0));
                    int columnOffset = 1;
                    foreach (var mapping in columnNameMapping)
                    {
                        double value = reader.GetDouble(columnOffset++);
                        if (!data.ContainsKey(mapping.Key))
                            data[mapping.Key] = new List<double>();
                        data[mapping.Key].Add(value);
                    }

                    // Считываем значение SFP для коммутаторов
                    if (tableName == "Switches")
                    {
                        if (!data.ContainsKey("SFP"))
                            data["SFP"] = new List<double>(); // Используем double временно, потом переделаем на string
                        // Читаем как строку и конвертируем в 1.0 (Да) или 0.0 (Нет) для совместимости с текущей структурой data
                        string sfpValue = reader["SFP"].ToString();
                        data["SFP"].Add(sfpValue.Equals("Да", StringComparison.OrdinalIgnoreCase) ? 1.0 : 0.0);
                    }
                }
            }

            var deviceResult = new Dictionary<string, object>();
            foreach (var kvp in data)
            {
                deviceResult[kvp.Key] = kvp.Value.ToArray();
            }
            deviceResult["ModelNames"] = modelNames.ToArray();

            // Добавляем информацию о количестве характеристик
            deviceResult["CharacteristicsCount"] = columnNameMapping.Count;

            return deviceResult;
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

        private double[] GetValuesForIndex(int index, Dictionary<string, object> allData) // Метод для получения значений по индексу
        {
            try
            {
                if (index < 0)
                {
                    LogCalculation($"GetValuesForIndex: Получен отрицательный индекс {index}");
                    return null;
                }

                var modelNames = (string[])allData["ModelNames"];
                if (index >= modelNames.Length)
                {
                    LogCalculation($"GetValuesForIndex: Индекс {index} больше количества моделей {modelNames.Length}");
                    return null;
                }

                LogCalculation($"\nПолучение значений для модели {modelNames[index]}:");

                // Получаем все характеристики из данных, исключая служебные поля
                var characteristics = allData.Keys
                    .Where(k => k != "ModelNames" && k != "CharacteristicsCount" && k != "Shielding" && k != "WiFiStatus" && k != "SFPStatus")
                    .ToArray();

                var values = new List<double>();

                foreach (var characteristic in characteristics)
                {
                    var data = (double[])allData[characteristic];
                    if (data == null || index >= data.Length)
                    {
                        LogCalculation($"- Ошибка: нет данных для {characteristic} или индекс вне диапазона");
                        return null;
                    }

                    values.Add(data[index]);
                    LogCalculation($"- {characteristic}: {data[index]}");
                }

                LogCalculation($"Успешно получены все значения для модели {modelNames[index]}");
                return values.ToArray();
            }
            catch (Exception ex)
            {
                LogCalculation($"Ошибка в GetValuesForIndex: {ex.Message}");
                if (ex.StackTrace != null)
                    LogCalculation($"Stack Trace: {ex.StackTrace}");
                return null;
            }
        }

        private void Form4_Load(object sender, EventArgs e) 
        {

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

        private Dictionary<string, object> GetCableDataFilteredByShielding(List<string> selectedShieldings) // Метод для получения данных кабеля с фильтрацией по экранированию
        {
            var columnNameMapping = GetColumnNameMapping("Cables");
            var data = new Dictionary<string, List<double>>();
            var modelNames = new List<string>();
            var shieldings = new List<string>();
            foreach (var mapping in columnNameMapping)
                data[mapping.Key] = new List<double>();
            string inClause = string.Join(",", selectedShieldings.Select(s => $"'{s}'"));
            string query = $"SELECT ModelName, {string.Join(", ", columnNameMapping.Values)}, Shielding FROM Cables WHERE Shielding IN ({inClause})";
            using (SQLiteDataReader reader = dbManager.ExecuteQuery(query))
            {
                while (reader.Read())
                {
                    modelNames.Add(reader.GetString(0));
                    int columnOffset = 1;
                    foreach (var mapping in columnNameMapping)
                    {
                        double value = reader.GetDouble(columnOffset++);
                        data[mapping.Key].Add(value);
                    }
                    shieldings.Add(reader["Shielding"].ToString());
                }
            }
            var result = new Dictionary<string, object>();
            foreach (var kvp in data)
                result[kvp.Key] = kvp.Value.ToArray();
            result["ModelNames"] = modelNames.ToArray();
            result["Shielding"] = shieldings.ToArray();
            result["CharacteristicsCount"] = columnNameMapping.Count;
            return result;
        }
    }

    public class GeneticAlgorithm // Класс для реализации генетического алгоритма
    {
        private Random random; // Генератор случайных чисел
        private int populationSize; // Размер популяции
        private double mutationRate; // Частота мутаций
        private int maxGenerations; // Максимальное количество поколений
        private double crossoverRate; // Частота скрещивания
        private Func<double[], double> fitnessFunction; // Функция приспособленности
        private List<Func<double[], bool>> constraints; // Список ограничений
        private string[] modelNames; // Массив названий моделей
        private Form progressForm; // Форма для отображения прогресса
        private ProgressBar progressBar; // Прогресс
        private Label progressLabel; // Метка прогресса
        private Label detailsLabel; // Метка с деталями
        private StringBuilder calculationLog; // Лог вычислений
        private double[] criterionData; // Данные критерия
        private int generationsWithoutImprovement; // Количество поколений без улучшения

        private string GetModelNameForValue(double value) // Метод для получения названия модели по значению
        {
            for (int i = 0; i < criterionData.Length; i++)
            {
                if (Math.Abs(criterionData[i] - value) < 0.0001)
                {
                    return modelNames[i];
                }
            }
            return "неизвестная модель";
        }

        public GeneticAlgorithm(int populationSize, double mutationRate, int maxGenerations, double crossoverRate, Func<double[], double> fitnessFunction, List<Func<double[], bool>> constraints, string[] modelNames) // Конструктор генетического алгоритма
        {
            this.random = new Random();
            this.populationSize = populationSize;
            this.mutationRate = mutationRate;
            this.maxGenerations = maxGenerations;
            this.crossoverRate = crossoverRate;
            this.fitnessFunction = fitnessFunction;
            this.constraints = constraints;
            this.modelNames = modelNames;
            calculationLog = new StringBuilder();
        }

        private void ShowProgress(int current, int total, double bestValue) // Метод для отображения прогресса
        {
            if (progressForm == null || progressForm.IsDisposed)
            {
                progressForm = new Form
                {
                    Width = 400,
                    Height = 200,
                    FormBorderStyle = FormBorderStyle.FixedDialog,
                    Text = "Выполняется поиск решения...",
                    StartPosition = FormStartPosition.CenterScreen,
                    ControlBox = false
                };

                progressBar = new ProgressBar
                {
                    Width = 350,
                    Height = 20,
                    Maximum = total,
                    Value = 0,
                    Location = new System.Drawing.Point(25, 20)
                };

                progressLabel = new Label
                {
                    Width = 350,
                    Height = 20,
                    Location = new System.Drawing.Point(25, 50),
                    Text = "Поколение: 0 / " + total
                };

                detailsLabel = new Label
                {
                    Width = 350,
                    Height = 80,
                    Location = new System.Drawing.Point(25, 80),
                    Text = "Идет поиск решения...\nПожалуйста, подождите."
                };

                progressForm.Controls.Add(progressBar);
                progressForm.Controls.Add(progressLabel);
                progressForm.Controls.Add(detailsLabel);
                progressForm.Show();
            }

            if (current <= total)
            {
                progressBar.Value = current;
                progressLabel.Text = $"Поколение: {current} / {total}";
                
                string status;
                if (bestValue == double.MinValue)
                {
                    status = "Поиск допустимого решения...";
                }
                else
                {
                    status = $"Текущее лучшее значение: {bestValue:F2}";
                }

                detailsLabel.Text = $"Статус:\n{status}\n\nЕсли процесс занимает много времени,\nпопробуйте ослабить ограничения.";
            }

            // Принудительно обновляем форму
            progressForm.Update();
            progressBar.Update();
            progressLabel.Update();
            detailsLabel.Update();
            Application.DoEvents();
        }

        private double[] GenerateRandomSolution(int numVariables, double[] initialSolution) // Метод для генерации случайного решения
        {
            var solution = new double[1];
            solution[0] = initialSolution[random.Next(initialSolution.Length)];
            return solution;
        }

        private void Mutate(double[] individual) // Метод для мутации особи
        {
            if (individual == null || individual.Length == 0)
                return;

            if (random.NextDouble() < mutationRate)
            {
                // Мутация на основе существующих значений
                double currentValue = individual[0];
                // Изменяем значение в пределах ±20%
                double variation = currentValue * (0.8 + random.NextDouble() * 0.4);
                individual[0] = variation;
            }
        }

        private double[] Crossover(double[] parent1, double[] parent2) // Метод для скрещивания особей
        {
            if (parent1 == null || parent2 == null || parent1.Length != 1 || parent2.Length != 1)
                return null;

            var offspring = new double[1];
            
            if (random.NextDouble() < crossoverRate)
            {
                // Берем среднее значение между родителями
                offspring[0] = (parent1[0] + parent2[0]) / 2;
                }
                else
                {
                // Копируем одного из родителей
                offspring[0] = random.NextDouble() < 0.5 ? parent1[0] : parent2[0];
                }

            return offspring;
            }

        private void LogCalculation(string message) // Метод для логирования вычислений
        {
            Debug.WriteLine(message);
            calculationLog.AppendLine(message);
        }

        private double[] SelectParent(List<double[]> population, double[] fitnessValues) // Метод для выбора родителя
        {
            // Выбираем случайно 3 кандидата и возвращаем лучшего по фитнес-функции
            try
            {
                if (population.Count == 0)
                {
                    LogCalculation("SelectParent: популяция пуста");
                    return null;
                }

                var tournamentSize = Math.Min(3, population.Count);
                var candidates = new List<int>();

                LogCalculation("\nТурнирный отбор:");

                // Выбираем случайных кандидатов
                for (int i = 0; i < tournamentSize; i++)
                {
                    int candidateIndex = random.Next(population.Count);
                    candidates.Add(candidateIndex);

                    // Находим название модели для логирования
                    string modelName = GetModelNameForValue(population[candidateIndex][0]);
                    LogCalculation($"- Участник: {modelName}, значение={population[candidateIndex][0]:F2}, приспособленность={fitnessValues[candidateIndex]:F2}");
                }

                // Находим лучшего по функции приспособленности
                int bestCandidateIndex = candidates[0];
                double bestFitness = fitnessValues[bestCandidateIndex];

                for (int i = 1; i < candidates.Count; i++)
                {
                    if (fitnessValues[candidates[i]] > bestFitness)
                    {
                        bestCandidateIndex = candidates[i];
                        bestFitness = fitnessValues[bestCandidateIndex];
                    }
                }

                if (bestFitness == double.MinValue)
                {
                    LogCalculation("SelectParent: нет допустимых решений, выбираем случайное решение");
                    return population[random.Next(population.Count)];
                }

                string winnerModelName = GetModelNameForValue(population[bestCandidateIndex][0]);
                LogCalculation($"Выбран победитель: {winnerModelName}, значение={population[bestCandidateIndex][0]:F2}, приспособленность={bestFitness:F2}");

                return population[bestCandidateIndex];
            }
            catch (Exception ex)
            {
                LogCalculation($"Ошибка в SelectParent: {ex.Message}");
                if (population.Count > 0)
                {
                    return population[0];
                }
                return null;
            }
        }

        public (double optimalValue, int optimalIndex, string calculations) Solve(double[] criterionData) // Метод для решения задачи генетическим алгоритмом
        {
            calculationLog.Clear();
            LogCalculation("Начало работы генетического алгоритма\n");
            LogCalculation($"Параметры алгоритма:");
            LogCalculation($"- Размер популяции: {populationSize}");
            LogCalculation($"- Количество поколений: {maxGenerations}");
            LogCalculation($"- Вероятность мутации: {mutationRate:F2}");
            LogCalculation($"- Вероятность скрещивания: {crossoverRate:F2}\n");

            try
            {
                if (criterionData == null || criterionData.Length == 0)
                {
                    throw new ArgumentException("Отсутствуют данные для оптимизации");
                }

                this.criterionData = criterionData;
                generationsWithoutImprovement = 0;

                // Инициализация начальной популяции
                var population = new List<double[]>();
                LogCalculation("\nСоздание начальной популяции:");

                // Проверяем каждую модель на соответствие ограничениям
                var validIndices = new List<int>();
                LogCalculation("\nПроверка моделей на соответствие ограничениям:");

                for (int i = 0; i < criterionData.Length; i++)
                {
                    var solution = new double[1] { criterionData[i] };
                    bool isValid = true;

                    foreach (var constraint in constraints)
                    {
                        if (!constraint(solution))
                        {
                            isValid = false;
                            break;
                        }
                    }

                    if (isValid)
                    {
                        validIndices.Add(i);
                        LogCalculation($"+ Модель {modelNames[i]} удовлетворяет всем ограничениям");
                        LogCalculation($"  - Значение критерия: {criterionData[i]:F2}");
                }
                else
                {
                        LogCalculation($"- Модель {modelNames[i]} не удовлетворяет ограничениям");
                    }
                }

                LogCalculation($"\nВсего найдено моделей, удовлетворяющих ограничениям: {validIndices.Count}");
                
                if (validIndices.Count == 0)
                {
                    throw new InvalidOperationException("Не найдено ни одной модели, удовлетворяющей всем ограничениям. Проверьте ограничения.");
                }

                // Создаем начальную популяцию только из валидных моделей
                foreach (int index in validIndices)
                {
                    population.Add(new double[1] { criterionData[index] });
                }

                // Дополняем популяцию до нужного размера копиями валидных моделей
                while (population.Count < populationSize)
                {
                    int randomIndex = validIndices[random.Next(validIndices.Count)];
                    population.Add(new double[1] { criterionData[randomIndex] });
                }

                // Вычисляем функцию приспособленности для всех моделей для отладки
                var modelFitnessValues = new Dictionary<int, double>();
                LogCalculation("\nЗначения фитнес-функции для всех валидных моделей:");
                
                foreach (int idx in validIndices)
                {
                    double[] individual = new double[] { criterionData[idx] };
                    double fitnessValue = fitnessFunction(individual);
                    modelFitnessValues[idx] = fitnessValue;
                    LogCalculation($"- Модель {modelNames[idx]}: значение критерия={criterionData[idx]:F2}, фитнес={fitnessValue:F2}");
                }

                // Отсортированные модели по фитнес-значению для отладки
                LogCalculation("\nМодели, отсортированные по убыванию фитнес-значения:");
                foreach (var kvp in modelFitnessValues.OrderByDescending(m => m.Value))
                {
                    LogCalculation($"- Модель {modelNames[kvp.Key]}: значение критерия={criterionData[kvp.Key]:F2}, фитнес={kvp.Value:F2}");
                }

                // Находим модель с максимальным фитнесом (для валидации результата в конце)
                var theoreticallyBestModel = modelFitnessValues.OrderByDescending(m => m.Value).FirstOrDefault();
                LogCalculation($"\nТеоретически оптимальная модель: {modelNames[theoreticallyBestModel.Key]}, значение критерия: {criterionData[theoreticallyBestModel.Key]:F2}, фитнес: {theoreticallyBestModel.Value:F2}");
            
                double bestFitnessEver = double.MinValue;
                int bestIndexEver = -1;
                int lastImprovementGeneration = 0;

                // Основной цикл генетического алгоритма
                for (int generation = 0; generation < maxGenerations; generation++)
                {
                    ShowProgress(generation + 1, maxGenerations, bestFitnessEver);

                    // Вычисляем приспособленность для каждой особи
                    var fitnessValues = new double[population.Count];
                    for (int i = 0; i < population.Count; i++)
                    {
                        fitnessValues[i] = fitnessFunction(population[i]);
                    }

                    // Находим лучшее решение в текущем поколении
                    int currentBestIndex = -1;
                    double currentBestFitness = double.MinValue;

                    for (int i = 0; i < fitnessValues.Length; i++)
                    {
                        if (fitnessValues[i] > currentBestFitness)
                        {
                            currentBestFitness = fitnessValues[i];
                            currentBestIndex = i;
                        }
                    }

                    // Обновляем лучшее решение за все время
                    bool improved = false;
                    if (currentBestIndex != -1 && currentBestFitness > bestFitnessEver)
                    {
                        bestFitnessEver = currentBestFitness;
                        
                        // Получаем значение критерия из текущей популяции
                        double bestValue = population[currentBestIndex][0];
                        
                        // Находим соответствующий индекс в исходном массиве
                        bestIndexEver = -1;
                        for (int i = 0; i < criterionData.Length; i++)
                        {
                            if (Math.Abs(criterionData[i] - bestValue) < 0.0001)
                            {
                                bestIndexEver = i;
                                break;
                            }
                        }
                        
                        if (bestIndexEver == -1)
                        {
                            LogCalculation($"Ошибка: не удалось найти соответствующий индекс для значения {bestValue}");
                            // Используем текущий лучший индекс из validIndices как запасной вариант
                            bestIndexEver = validIndices[0];
                        }
                        
                        generationsWithoutImprovement = 0;
                        lastImprovementGeneration = generation;
                        improved = true;

                        LogCalculation($"\nПоколение {generation + 1}: найдено новое лучшее решение");
                        LogCalculation($"- Модель: {modelNames[bestIndexEver]}");
                        LogCalculation($"- Значение критерия: {criterionData[bestIndexEver]:F2}");
                        LogCalculation($"- Значение фитнеса: {bestFitnessEver:F2}");
            }
            else
            {
                        generationsWithoutImprovement++;
                        
                        if (generation % 5 == 0 && !improved) // логируем каждые 5 поколений без улучшения
                        {
                            LogCalculation($"\nПоколение {generation + 1}: без улучшений");
                            LogCalculation($"- Поколений без улучшений: {generationsWithoutImprovement}");
                            LogCalculation($"- Текущее лучшее значение: {bestFitnessEver:F2} (с поколения {lastImprovementGeneration + 1})");
                        }
                    }

                    // Создаем новую популяцию
                    var newPopulation = new List<double[]>();

                    // Элитизм - сохраняем лучшие особи
                    var eliteCount = Math.Max(1, population.Count / 10);
                    var eliteIndices = Enumerable.Range(0, population.Count)
                        .OrderByDescending(i => fitnessValues[i])
                        .Take(eliteCount);

                    // Добавляем элитные особи без изменений
                    foreach (var index in eliteIndices)
                    {
                        newPopulation.Add(population[index].ToArray());
                        
                        if (generation % 5 == 0)
                        {
                            LogCalculation($"Сохранение элитной особи: {GetModelNameForValue(population[index][0])}, фитнес: {fitnessValues[index]:F2}");
                        }
                    }

                    // Создаем новые особи с помощью скрещивания и мутации
                    while (newPopulation.Count < populationSize)
                    {
                        var parent1 = SelectParent(population, fitnessValues);
                        var parent2 = SelectParent(population, fitnessValues);

                        if (parent1 != null && parent2 != null)
                        {
                            // Вместо скрещивания просто выбираем случайную валидную модель
                            int randomValidIndex = validIndices[random.Next(validIndices.Count)];
                            var offspring = new double[1] { criterionData[randomValidIndex] };
                            
                            // Применяем мутацию случайно (вероятность mutationRate)
                if (random.NextDouble() < mutationRate)
                {
                                // Мутация - заменяем на другую случайную валидную модель
                                int mutatedIndex = validIndices[random.Next(validIndices.Count)];
                                offspring[0] = criterionData[mutatedIndex];
                                
                                if (generation % 5 == 0 || improved) // логируем только иногда для уменьшения объема
                                {
                                    LogCalculation($"Мутация: {GetModelNameForValue(parent1[0])} -> {modelNames[mutatedIndex]}");
                                }
                            }
                            
                            newPopulation.Add(offspring);
                        }
                    }

                    population = newPopulation;

                    // Проверяем условие остановки
                    if (generationsWithoutImprovement >= 20)
                    {
                        LogCalculation($"\nАлгоритм остановлен на поколении {generation + 1}:");
                        LogCalculation("- Достигнута стабилизация решения");
                        LogCalculation($"- Поколений без улучшений: {generationsWithoutImprovement}");
                        break;
                    }
                }

                if (bestIndexEver == -1)
                {
                    // Если не удалось найти решение через генетический алгоритм,
                    bestIndexEver = theoreticallyBestModel.Key;
                    bestFitnessEver = theoreticallyBestModel.Value;
                    LogCalculation("\nВнимание: генетический алгоритм не нашел решение, используем теоретически оптимальную модель.");
                }

                // Проверяем, совпадает ли результат генетического алгоритма с теоретически наилучшим решением
                LogCalculation($"\nРезультаты работы генетического алгоритма:");
                LogCalculation($"- Оптимальное значение критерия: {criterionData[bestIndexEver]:F2}");
                LogCalculation($"- Оптимальная модель: {modelNames[bestIndexEver]}");
                LogCalculation($"- Значение фитнеса: {bestFitnessEver:F2}");
                
                if (bestIndexEver != theoreticallyBestModel.Key)
                {
                    LogCalculation($"\nВнимание: теоретически оптимальное решение отличается от найденного:");
                    LogCalculation($"- Теоретическая модель: {modelNames[theoreticallyBestModel.Key]}");
                    LogCalculation($"- Значение критерия: {criterionData[theoreticallyBestModel.Key]:F2}");
                    LogCalculation($"- Значение фитнеса: {theoreticallyBestModel.Value:F2}");
                }
                else
                {
                    LogCalculation("\nПримечание: генетический алгоритм успешно нашел теоретически оптимальное решение.");
                }

                return (criterionData[bestIndexEver], bestIndexEver, calculationLog.ToString());
            }
            finally
            {
                if (progressForm != null && !progressForm.IsDisposed)
                {
                    progressForm.Invoke((MethodInvoker)delegate {
                        progressForm.Close();
                        progressForm.Dispose();
                    });
                    progressForm = null;
                }
            }
        }
    }
}