using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Word = Microsoft.Office.Interop.Word;

namespace WindowsFormsApp1
{
    public partial class FormOrdersSeller : Form
    {
        private string connectionString = DatabaseConfig.ConnectionString;
        private DataTable ordersTable;

        public FormOrdersSeller()
        {
            InitializeComponent();
        }

        private void FormOrdersSeller_Load(object sender, EventArgs e)
        {
            LoadStatuses();
            LoadOrders();
            InitializeDataGridView();
        }

        private void LoadStatuses()
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string sql = "SELECT id, name FROM order_statuses ORDER BY name";
                    MySqlDataAdapter da = new MySqlDataAdapter(sql, conn);
                    DataTable dt = new DataTable();
                    da.Fill(dt);

                    DataRow allStatusesRow = dt.NewRow();
                    allStatusesRow["id"] = 0;
                    allStatusesRow["name"] = "Все статусы";
                    dt.Rows.InsertAt(allStatusesRow, 0);

                    cmbStatus.DataSource = dt;
                    cmbStatus.DisplayMember = "name";
                    cmbStatus.ValueMember = "id";
                    cmbStatus.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при загрузке статусов: " + ex.Message);
            }
        }

        private void LoadOrders()
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    // ДОБАВЛЯЕМ items_json В ЗАПРОС
                    string sql = @"SELECT 
                                o.id AS 'ID',
                                o.client_id AS 'ID клиента',
                                c.name AS 'Клиент',
                                c.phone AS 'Телефон',
                                o.status_id AS 'ID статуса',
                                s.name AS 'Статус',
                                o.user_id AS 'ID пользователя',
                                u.full_name AS 'Пользователь',
                                DATE_FORMAT(o.order_date, '%d.%m.%Y %H:%i') AS 'Дата заказа',
                                DATE_FORMAT(o.completion_date, '%d.%m.%Y') AS 'Дата выполнения',
                                o.total_amount AS 'Сумма',
                                o.total_amount AS total_amount_numeric,
                                o.items_json AS 'СоставJSON',  -- ДОБАВЛЯЕМ JSON
                                o.client_id,
                                o.status_id,
                                o.user_id
                         FROM orders o
                         LEFT JOIN clients c ON o.client_id = c.id
                         LEFT JOIN order_statuses s ON o.status_id = s.id
                         LEFT JOIN users u ON o.user_id = u.id
                         ORDER BY o.order_date DESC";

                    MySqlDataAdapter da = new MySqlDataAdapter(sql, conn);
                    ordersTable = new DataTable();
                    da.Fill(ordersTable);

                    dataGridView1.DataSource = ordersTable;

                    // Скрываем технические поля
                    string[] hiddenColumns = { "ID", "client_id", "status_id", "user_id",
                                               "total_amount_numeric", "ID клиента",
                                               "ID статуса", "ID пользователя", "СоставJSON" };

                    foreach (string columnName in hiddenColumns)
                    {
                        if (dataGridView1.Columns.Contains(columnName))
                            dataGridView1.Columns[columnName].Visible = false;
                    }

                    // ДОБАВЛЯЕМ КОЛОНКУ ДЛЯ СОСТАВА
                    AddCompositionColumn();

                    ConfigureColumnWidths();
                    ConfigureGridViewAppearance();

                    // ЗАПОЛНЯЕМ СОСТАВ
                    FillCompositionColumn();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при загрузке заказов: " + ex.Message,
                               "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ========== НОВЫЙ МЕТОД: ДОБАВЛЕНИЕ КОЛОНКИ СОСТАВА ==========
        private void AddCompositionColumn()
        {
            if (!dataGridView1.Columns.Contains("Состав"))
            {
                DataGridViewTextBoxColumn compColumn = new DataGridViewTextBoxColumn();
                compColumn.Name = "Состав";
                compColumn.HeaderText = "Состав заказа";
                compColumn.Width = 250;
                compColumn.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
                compColumn.DefaultCellStyle.Alignment = DataGridViewContentAlignment.TopLeft;
                dataGridView1.Columns.Add(compColumn);
            }
        }

        // ========== НОВЫЙ МЕТОД: ЗАПОЛНЕНИЕ СОСТАВА ==========
        private void FillCompositionColumn()
        {
            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                if (row.IsNewRow) continue;

                if (row.Cells["СоставJSON"].Value != null &&
                    row.Cells["СоставJSON"].Value != DBNull.Value)
                {
                    string json = row.Cells["СоставJSON"].Value.ToString();
                    string composition = FormatCompositionFromJson(json);
                    row.Cells["Состав"].Value = composition;
                }
                else
                {
                    // Если JSON нет, пробуем получить из order_items
                    int orderId = Convert.ToInt32(row.Cells["ID"].Value);
                    string composition = GetCompositionFromOrderItems(orderId);
                    row.Cells["Состав"].Value = composition;
                }
            }

            // Автоматически подгоняем высоту строк под содержимое
            dataGridView1.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
        }

        // ========== НОВЫЙ МЕТОД: ФОРМАТИРОВАНИЕ JSON В ТЕКСТ ==========
        private string FormatCompositionFromJson(string json)
        {
            try
            {
                var items = JsonConvert.DeserializeObject<List<OrderItem>>(json);
                if (items == null || items.Count == 0)
                    return "Нет товаров";

                return string.Join("\n", items.Select(item =>
                    $"{item.ProductName} x{item.Quantity} = {item.Price * item.Quantity:C}"));
            }
            catch
            {
                return json; // Если не удалось распарсить, показываем как есть
            }
        }

        // ========== НОВЫЙ МЕТОД: ПОЛУЧЕНИЕ СОСТАВА ИЗ order_items ==========
        private string GetCompositionFromOrderItems(int orderId)
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string sql = @"SELECT p.name, oi.quantity, oi.unit_price 
                                  FROM order_items oi
                                  JOIN products p ON oi.product_id = p.id
                                  WHERE oi.order_id = @orderId";

                    using (MySqlCommand cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@orderId", orderId);
                        using (MySqlDataReader reader = cmd.ExecuteReader())
                        {
                            var items = new List<string>();
                            while (reader.Read())
                            {
                                string name = reader["name"].ToString();
                                int qty = Convert.ToInt32(reader["quantity"]);
                                decimal price = Convert.ToDecimal(reader["unit_price"]);
                                items.Add($"{name} x{qty} = {price * qty:C}");
                            }
                            return items.Count > 0 ? string.Join("\n", items) : "Нет товаров";
                        }
                    }
                }
            }
            catch
            {
                return "Ошибка загрузки";
            }
        }

        // ========== КЛАСС ДЛЯ ДЕСЕРИАЛИЗАЦИИ JSON ==========
        public class OrderItem
        {
            public int ProductId { get; set; }
            public string ProductName { get; set; }
            public string Article { get; set; }
            public decimal Price { get; set; }
            public int Quantity { get; set; }
        }

        private void ConfigureColumnWidths()
        {
            if (dataGridView1.Columns.Contains("Клиент"))
                dataGridView1.Columns["Клиент"].Width = 150;
            if (dataGridView1.Columns.Contains("Телефон"))
                dataGridView1.Columns["Телефон"].Width = 100;
            if (dataGridView1.Columns.Contains("Статус"))
                dataGridView1.Columns["Статус"].Width = 120;
            if (dataGridView1.Columns.Contains("Пользователь"))
                dataGridView1.Columns["Пользователь"].Width = 150;
            if (dataGridView1.Columns.Contains("Дата заказа"))
                dataGridView1.Columns["Дата заказа"].Width = 120;
            if (dataGridView1.Columns.Contains("Дата выполнения"))
                dataGridView1.Columns["Дата выполнения"].Width = 120;
            if (dataGridView1.Columns.Contains("Сумма"))
                dataGridView1.Columns["Сумма"].Width = 90;
            if (dataGridView1.Columns.Contains("Состав"))
                dataGridView1.Columns["Состав"].Width = 250;
        }

        private void ConfigureGridViewAppearance()
        {
            dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dataGridView1.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
            dataGridView1.ColumnHeadersDefaultCellStyle.Font = new Font("Arial", 10, FontStyle.Bold);
            dataGridView1.EnableHeadersVisualStyles = false;
            dataGridView1.ColumnHeadersDefaultCellStyle.BackColor = Color.LightGray;
            dataGridView1.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dataGridView1.AlternatingRowsDefaultCellStyle.BackColor = Color.Lavender;

            if (dataGridView1.Columns.Contains("Сумма"))
                dataGridView1.Columns["Сумма"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            if (dataGridView1.Columns.Contains("Дата заказа"))
                dataGridView1.Columns["Дата заказа"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            if (dataGridView1.Columns.Contains("Дата выполнения"))
                dataGridView1.Columns["Дата выполнения"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            if (dataGridView1.Columns.Contains("Состав"))
                dataGridView1.Columns["Состав"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.TopLeft;
        }

        private void InitializeDataGridView()
        {
            dataGridView1.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dataGridView1.MultiSelect = false;
            dataGridView1.ReadOnly = true;
            dataGridView1.AllowUserToAddRows = false;
            dataGridView1.AllowUserToDeleteRows = false;
            dataGridView1.RowHeadersVisible = false;
        }

        // ========== ФИЛЬТРАЦИЯ ==========
        private void ApplyFilters()
        {
            if (ordersTable == null) return;

            string filter = "";

            if (!string.IsNullOrWhiteSpace(txtSearch.Text))
                filter += $"[Клиент] LIKE '%{txtSearch.Text}%'";

            if (cmbStatus.SelectedIndex > 0)
            {
                if (!string.IsNullOrEmpty(filter)) filter += " AND ";
                filter += $"[Статус] = '{cmbStatus.Text}'";
            }

            if (!string.IsNullOrWhiteSpace(txtPriceFilter.Text))
            {
                if (!string.IsNullOrEmpty(filter)) filter += " AND ";
                if (decimal.TryParse(txtPriceFilter.Text, out decimal amount))
                    filter += $"[total_amount_numeric] >= {amount}";
            }

            ordersTable.DefaultView.RowFilter = filter;

            // После фильтрации нужно перезаполнить состав
            FillCompositionColumn();
        }

        private void txtSearch_TextChanged(object sender, EventArgs e) => ApplyFilters();
        private void cmbStatus_SelectedIndexChanged(object sender, EventArgs e) => ApplyFilters();
        private void txtPriceFilter_TextChanged(object sender, EventArgs e) => ApplyFilters();

        // ========== МЕТОД ДЛЯ ПОЛУЧЕНИЯ ДАННЫХ ОТЧЕТА ==========
        private DataTable GetReportData()
        {
            DataTable reportTable = new DataTable();

            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();

                    string sql = @"SELECT 
                                o.id,
                                c.name AS 'Клиент',
                                c.phone AS 'Телефон',
                                s.name AS 'Статус',
                                u.full_name AS 'Пользователь',
                                DATE_FORMAT(o.order_date, '%d.%m.%Y %H:%i') AS 'Дата заказа',
                                DATE_FORMAT(o.completion_date, '%d.%m.%Y') AS 'Дата выполнения',
                                o.total_amount AS 'Сумма',
                                o.items_json AS 'Состав'
                              FROM orders o
                              LEFT JOIN clients c ON o.client_id = c.id
                              LEFT JOIN order_statuses s ON o.status_id = s.id
                              LEFT JOIN users u ON o.user_id = u.id
                              ORDER BY o.order_date DESC";

                    MySqlDataAdapter da = new MySqlDataAdapter(sql, conn);
                    da.Fill(reportTable);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка получения данных для отчета: {ex.Message}",
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            return reportTable;
        }



        // ========== ОСТАЛЬНЫЕ МЕТОДЫ ==========
        private int GetSelectedOrderId()
        {
            if (dataGridView1.SelectedRows.Count == 0) return 0;
            DataGridViewRow selectedRow = dataGridView1.SelectedRows[0];
            if (dataGridView1.Columns.Contains("ID") && selectedRow.Cells["ID"].Value != null)
                return Convert.ToInt32(selectedRow.Cells["ID"].Value);
            return 0;
        }












        private void btnReset_Click(object sender, EventArgs e)
        {
            txtSearch.Text = "";
            cmbStatus.SelectedIndex = 0;
            txtPriceFilter.Text = "";

            // Полностью перезагружаем данные из БД
            LoadOrders();
        }

        private void btnBack_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void BtnPrintReceipt_Click(object sender, EventArgs e)
        {
            if (dataGridView1.SelectedRows.Count == 0)
            {
                MessageBox.Show("Выберите заказ для печати чека.", "Информация",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            DataGridViewRow row = dataGridView1.SelectedRows[0];

            int orderId = Convert.ToInt32(row.Cells["ID"].Value);
            string clientName = row.Cells["Клиент"].Value?.ToString() ?? "";
            string clientPhone = row.Cells["Телефон"].Value?.ToString() ?? "";
            DateTime completionDate;
            DateTime.TryParse(row.Cells["Дата выполнения"].Value?.ToString(), out completionDate);
            decimal totalAmount = Convert.ToDecimal(row.Cells["Сумма"].Value);

            // Получаем состав заказа из JSON
            string json = row.Cells["СоставJSON"].Value?.ToString();
            List<OrderItem> items = null;
            if (!string.IsNullOrEmpty(json))
            {
                try
                {
                    items = JsonConvert.DeserializeObject<List<OrderItem>>(json);
                }
                catch { }
            }

            // Если JSON не удалось распарсить, пробуем получить из order_items
            if (items == null || items.Count == 0)
            {
                items = GetOrderItemsFromDb(orderId);
            }

            if (items == null || items.Count == 0)
            {
                MessageBox.Show("Не удалось получить состав заказа.", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Преобразуем OrderItem в CartItem для метода печати (или адаптируем метод)
            var cartItems = items.Select(i => new CartItem
            {
                ProductId = i.ProductId,
                ProductName = i.ProductName,
                Price = i.Price,
                Quantity = i.Quantity
            }).ToList();

            PrintReceiptInWord(orderId, clientName, clientPhone, completionDate, cartItems, totalAmount);
        }

        // ========== Метод печати чека в Word (скопирован из FormOrderCreation) ==========
        private void PrintReceiptInWord(int orderId, string clientName, string clientPhone,
            DateTime completionDate, List<CartItem> items, decimal total)
        {
            try
            {
                Word.Application wordApp = new Word.Application();
                wordApp.Visible = true;
                wordApp.DisplayAlerts = Word.WdAlertLevel.wdAlertsNone;

                Word.Document doc = wordApp.Documents.Add();
                Word.Paragraph paragraph;

                // Заголовок
                paragraph = doc.Content.Paragraphs.Add();
                paragraph.Range.Text = $"ЧЕК №{orderId}";
                paragraph.Range.Font.Bold = 1;
                paragraph.Range.Font.Size = 18;
                paragraph.Range.ParagraphFormat.Alignment = Word.WdParagraphAlignment.wdAlignParagraphCenter;
                paragraph.Range.InsertParagraphAfter();

                // Информация о заказе
                paragraph = doc.Content.Paragraphs.Add();
                paragraph.Range.Text = $"Дата заказа: {DateTime.Now:dd.MM.yyyy HH:mm}";
                paragraph.Range.Font.Size = 12;
                paragraph.Range.InsertParagraphAfter();

                paragraph = doc.Content.Paragraphs.Add();
                paragraph.Range.Text = $"Дата выполнения: {completionDate:dd.MM.yyyy}";
                paragraph.Range.Font.Size = 12;
                paragraph.Range.InsertParagraphAfter();

                paragraph = doc.Content.Paragraphs.Add();
                paragraph.Range.Text = $"Клиент: {clientName}";
                paragraph.Range.Font.Size = 12;
                paragraph.Range.InsertParagraphAfter();

                paragraph = doc.Content.Paragraphs.Add();
                paragraph.Range.Text = $"Телефон: {clientPhone}";
                paragraph.Range.Font.Size = 12;
                paragraph.Range.InsertParagraphAfter();

                paragraph = doc.Content.Paragraphs.Add();
                paragraph.Range.InsertParagraphAfter();

                // Таблица товаров
                Word.Table table = doc.Tables.Add(paragraph.Range, items.Count + 1, 5);
                table.Borders.Enable = 1;
                table.Range.Font.Size = 11;

                table.Cell(1, 1).Range.Text = "№";
                table.Cell(1, 2).Range.Text = "Товар";
                table.Cell(1, 3).Range.Text = "Кол-во";
                table.Cell(1, 4).Range.Text = "Цена, ₽";
                table.Cell(1, 5).Range.Text = "Сумма, ₽";

                for (int i = 1; i <= 5; i++)
                {
                    table.Cell(1, i).Range.ParagraphFormat.Alignment = Word.WdParagraphAlignment.wdAlignParagraphCenter;
                    table.Cell(1, i).Range.Font.Bold = 1;
                }

                int row = 2;
                int counter = 1;
                foreach (var item in items)
                {
                    table.Cell(row, 1).Range.Text = counter.ToString();
                    table.Cell(row, 2).Range.Text = item.ProductName;
                    table.Cell(row, 3).Range.Text = item.Quantity.ToString();
                    table.Cell(row, 4).Range.Text = item.Price.ToString("F2");
                    table.Cell(row, 5).Range.Text = item.TotalPrice.ToString("F2");

                    table.Cell(row, 3).Range.ParagraphFormat.Alignment = Word.WdParagraphAlignment.wdAlignParagraphCenter;
                    table.Cell(row, 4).Range.ParagraphFormat.Alignment = Word.WdParagraphAlignment.wdAlignParagraphRight;
                    table.Cell(row, 5).Range.ParagraphFormat.Alignment = Word.WdParagraphAlignment.wdAlignParagraphRight;

                    row++;
                    counter++;
                }

                // Итог
                paragraph = doc.Content.Paragraphs.Add();
                paragraph.Range.Text = $"ИТОГО: {total:F2} ₽";
                paragraph.Range.Font.Bold = 1;
                paragraph.Range.Font.Size = 14;
                paragraph.Range.ParagraphFormat.Alignment = Word.WdParagraphAlignment.wdAlignParagraphRight;
                paragraph.Range.InsertParagraphAfter();

                // Освобождение ресурсов (Word остаётся открытым)
                System.Runtime.InteropServices.Marshal.ReleaseComObject(table);
                System.Runtime.InteropServices.Marshal.ReleaseComObject(paragraph);
                System.Runtime.InteropServices.Marshal.ReleaseComObject(doc);
                System.Runtime.InteropServices.Marshal.ReleaseComObject(wordApp);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при создании чека в Word: {ex.Message}", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ========== Вспомогательный метод для получения состава заказа из БД (если JSON отсутствует) ==========
        private List<OrderItem> GetOrderItemsFromDb(int orderId)
        {
            var items = new List<OrderItem>();
            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string sql = @"SELECT p.id, p.name, p.article, oi.unit_price, oi.quantity
                                  FROM order_items oi
                                  JOIN products p ON oi.product_id = p.id
                                  WHERE oi.order_id = @orderId";
                    using (MySqlCommand cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@orderId", orderId);
                        using (MySqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                items.Add(new OrderItem
                                {
                                    ProductId = reader.GetInt32("id"),
                                    ProductName = reader.GetString("name"),
                                    Article = reader.GetString("article"),
                                    Price = reader.GetDecimal("unit_price"),
                                    Quantity = reader.GetInt32("quantity")
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки состава заказа: {ex.Message}");
            }
            return items;
        }

        // ========== Классы для данных ==========


        public class CartItem
        {
            public int ProductId { get; set; }
            public string ProductName { get; set; }
            public decimal Price { get; set; }
            public int Quantity { get; set; }
            public decimal TotalPrice => Price * Quantity;
        }
    }
}
