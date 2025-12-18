using System.Data.SQLite;
using System.Text.RegularExpressions;
using static diplom.Form1;
using System.Linq;
using System.Windows.Forms;

namespace diplom
{
    public partial class Form6 : Form
    {
        // Список панелей конфигураций, отображаемых на форме
        private List<ConfigPanel> configPanels = new List<ConfigPanel>();

        public Form6()
        {
            InitializeComponent();
            LoadConfigNumbers(); // Загружаем номера конфигураций из базы данных
        }

        // Загружает номера конфигураций из базы данных в список выбора
        private void LoadConfigNumbers()
        {
            string connectionString = "Data Source=A:\\DevicesDatabase;Version=3;";
            DatabaseManager dbManager = new DatabaseManager(connectionString);

            string query = "SELECT DISTINCT ConfigurationNumber FROM DevicesDatabase ORDER BY ConfigurationNumber";
            using (SQLiteDataReader reader = dbManager.ExecuteQuery(query))
            {
                while (reader.Read())
                {
                    string configNumber = reader["ConfigurationNumber"].ToString();
                    clbConfigs.Items.Add($"Конфигурация №{configNumber}");
                }
            }
        }

        // Обработчик кнопки "Показать выбранные" — отображает выбранные конфигурации
        private void BtnShow_Click(object sender, EventArgs e)
        {
            // Удаляем старые панели
            foreach (var panel in configPanels)
                this.Controls.Remove(panel);
            configPanels.Clear();

            int x = 20;
            foreach (var item in clbConfigs.CheckedItems)
            {
                // Извлекаем номер конфигурации из строки вида "Конфигурация №X"
                var match = Regex.Match(item.ToString(), @"№(\d+)");
                if (match.Success)
                {
                    int configNumber = int.Parse(match.Groups[1].Value);
                    ConfigPanel panel = new ConfigPanel(configNumber, x, 130); // Создаём панель для каждой конфигурации
                    this.Controls.Add(panel);
                    configPanels.Add(panel);
                    x += panel.Width + 30;
                }
            }
        }

        // Обработчик кнопки "Рассчитать стоимости"
        private void BtnCalculate_Click(object sender, EventArgs e)
        {
            foreach (var panel in configPanels)
                panel.CalculateTotal(); // Считаем итоговую стоимость для каждой панели

            // Поиск панели с минимальной стоимостью
            if (configPanels.Count > 0)
            {
                var minPanel = configPanels.OrderBy(p => p.TotalCost).First();
                MessageBox.Show($"Оптимальная конфигурация: №{minPanel.ConfigNumber}\nИтоговая стоимость: {minPanel.TotalCost:N0} руб.", "Оптимальная конфигурация", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        // Вложенный класс для панели одной конфигурации
        private class ConfigPanel : Panel
        {
            private int configNumber; // Номер конфигурации
            private List<TextBox> qtyBoxes = new List<TextBox>(); // Список текстбоксов для ввода количества
            private List<decimal> prices = new List<decimal>();   // Список цен
            private Label lblTotal; // Метка для итоговой стоимости
            public decimal TotalCost { get; private set; } // Итоговая стоимость
            public int ConfigNumber => configNumber; // Номер конфигурации 

            // Конструктор панели конфигурации
            public ConfigPanel(int configNumber, int left, int top)
            {
                this.configNumber = configNumber;
                this.Left = left;
                this.Top = top;
                this.Width = 470;
                this.Height = 350;
                this.BorderStyle = BorderStyle.FixedSingle;

                string connectionString = "Data Source=A:\\DevicesDatabase;Version=3;";
                DatabaseManager dbManager = new DatabaseManager(connectionString);

                // Получаем метод оптимизации для этой конфигурации
                string methodOptimization = "";
                string methodQuery = $"SELECT OptimizationMethod FROM DevicesDatabase WHERE ConfigurationNumber = {configNumber} LIMIT 1";
                using (SQLiteDataReader reader = dbManager.ExecuteQuery(methodQuery))
                {
                    if (reader.Read())
                        methodOptimization = reader["OptimizationMethod"].ToString();
                }

                // Заголовок панели
                Label lblHeader = new Label();
                lblHeader.Text = $"Конфигурация №{configNumber} ({methodOptimization})";
                lblHeader.Left = 10;
                lblHeader.Top = 10;
                lblHeader.Width = 550;
                lblHeader.Font = new System.Drawing.Font("Arial", 12, System.Drawing.FontStyle.Bold);
                this.Controls.Add(lblHeader);

                // Надпись "Количество:" над текстбоксами
                Label lblQtyHeader = new Label();
                lblQtyHeader.Text = "Количество:";
                lblQtyHeader.Left = 360;
                lblQtyHeader.Top = 38;
                lblQtyHeader.Width = 80;
                lblQtyHeader.Height = 18;
                lblQtyHeader.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
                lblQtyHeader.Font = new System.Drawing.Font("Arial", 9, System.Drawing.FontStyle.Bold);
                this.Controls.Add(lblQtyHeader);

                // Получаем список устройств для конфигурации
                string query = $"SELECT DeviceType, TechnicalChar FROM DevicesDatabase WHERE ConfigurationNumber = {configNumber}";
                using (SQLiteDataReader reader = dbManager.ExecuteQuery(query))
                {
                    int y = 58; // Начальная координата по Y
                    while (reader.Read())
                    {
                        string deviceType = reader["DeviceType"].ToString();
                        string techChar = reader["TechnicalChar"].ToString();
                        decimal price = ExtractPrice(techChar, out bool isPerMeter);

                        // Название устройства
                        Label lbl = new Label();
                        lbl.Text = $"{deviceType}";
                        lbl.Left = 10;
                        lbl.Top = y;
                        lbl.Width = 200;
                        lbl.Height = 18;
                        this.Controls.Add(lbl);

                        // Модель устройства (если есть)
                        string model = ExtractModel(techChar);
                        Label lblModel = null;
                        if (!string.IsNullOrEmpty(model))
                        {
                            lblModel = new Label();
                            lblModel.Text = model;
                            lblModel.Left = 10;
                            lblModel.Top = y + 18;
                            lblModel.Width = 200;
                            lblModel.Height = 18;
                            lblModel.Font = new System.Drawing.Font("Arial", 8, System.Drawing.FontStyle.Italic);
                            this.Controls.Add(lblModel);
                        }

                        int rowHeight = (lblModel != null ? 40 : 30);
                        int rowY = y;

                        // Цена устройства
                        Label lblPrice = new Label();
                        string priceLabel = isPerMeter ? "Цена за метр: " : "Цена: ";
                        if (price > 0)
                            lblPrice.Text = $"{priceLabel}{FormatMoney(price)} руб.";
                        else
                            lblPrice.Text = $"Цена не указана";
                        lblPrice.Left = 220;
                        lblPrice.Top = rowY;
                        lblPrice.Width = 130;
                        lblPrice.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
                        this.Controls.Add(lblPrice);

                        // Текстбокс для ввода количества
                        TextBox tb = new TextBox();
                        tb.Left = 360;
                        tb.Top = rowY;
                        tb.Width = 40;
                        tb.Text = "0";
                        this.Controls.Add(tb);
                        qtyBoxes.Add(tb);

                        // Единица измерения (шт. или м)
                        Label lblUnit = new Label();
                        lblUnit.Left = 410;
                        lblUnit.Top = rowY;
                        lblUnit.Width = 30;
                        lblUnit.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
                        if (isPerMeter || deviceType.ToLower().Contains("кабель"))
                            lblUnit.Text = "м";
                        else
                            lblUnit.Text = "шт.";
                        this.Controls.Add(lblUnit);

                        prices.Add(price);

                        y += rowHeight;
                    }
                }

                // Метка для итоговой стоимости
                lblTotal = new Label();
                lblTotal.Text = "Итоговая стоимость: 0 руб.";
                lblTotal.Left = 10;
                lblTotal.Top = 270;
                lblTotal.Width = 400;
                lblTotal.Font = new System.Drawing.Font("Arial", 11, System.Drawing.FontStyle.Bold);
                this.Controls.Add(lblTotal);
            }

            // Считает итоговую стоимость для данной конфигурации
            public void CalculateTotal()
            {
                decimal total = 0;
                for (int i = 0; i < qtyBoxes.Count; i++)
                {
                    int qty = 0;
                    int.TryParse(qtyBoxes[i].Text, out qty);
                    total += prices[i] * qty;
                }
                lblTotal.Text = $"Итоговая стоимость: {FormatMoney(total)} руб.";
                TotalCost = total;
            }

            // Извлекает цену из технических характеристик
            private decimal ExtractPrice(string techChar, out bool isPerMeter)
            {
                // Ищет "цена" или "стоимость", за метр/1 метр/метр, дробные числа, "руб" с любым окончанием
                var match = Regex.Match(techChar, @"(цена|стоимость)( за( 1)? метр)?:\s*([\d\s.,]+)\s*руб", RegexOptions.IgnoreCase);
                isPerMeter = false;
                if (match.Success)
                {
                    isPerMeter = !string.IsNullOrEmpty(match.Groups[2].Value);
                    string priceStr = match.Groups[4].Value.Replace(" ", "").Replace(",", ".");
                    if (decimal.TryParse(priceStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal price))
                        return price;
                }
                return 0;
            }

            // Извлекает модель устройства из технических характеристик
            private string ExtractModel(string techChar)
            {
                // Ищем строки типа "Оптимальный ...: ..." или "Оптимальная ...: ..." или "Модель: ..."
                var match = Regex.Match(techChar, @"(Оптимальн(ый|ая) [^:]+|Модель):\s*([^\n\r]+)", RegexOptions.IgnoreCase);
                if (match.Success)
                    return match.Groups[3].Value.Trim();
                return null;
            }

            // Форматирует число как денежную сумму
            private string FormatMoney(decimal value)
            {
                return value.ToString("N0", System.Globalization.CultureInfo.GetCultureInfo("ru-RU"));
            }
        }
    }
}
