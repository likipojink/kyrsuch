using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace WindowsFormsApp1
{
    public partial class FormOrders : Form
    {
        private string connectionString = DatabaseConfig.ConnectionString;
        private DataTable ordersTable;
        private bool showFullPhoneNumbers = false; // Поле для отслеживания состояния отображения номеров

        public FormOrders()
        {
            InitializeComponent();
        }

        private void FormOrders_Load(object sender, EventArgs e)
        {
            LoadStatuses();
            LoadOrders();
            InitializeDataGridView();

            // Подписываемся на событие форматирования ячеек
            dataGridView1.CellFormatting += DataGridView1_CellFormatting;
        }

        // ========== МАСКИРОВАНИЕ НОМЕРА ТЕЛЕФОНА ==========
        private string MaskPhoneNumber(string phone)
        {
            if (string.IsNullOrEmpty(phone) || phone.Length < 4)
                return phone;

            // Оставляем последние 4 цифры, остальные заменяем звездочками
            string lastFour = phone.Substring(Math.Max(0, phone.Length - 4));
            string mask = new string('*', Math.Max(0, phone.Length - 4));

            return mask + lastFour;
        }

        // ========== ОБРАБОТЧИК ФОРМАТИРОВАНИЯ ЯЧЕЕК ==========
        private void DataGridView1_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            // Проверяем, что это колонка "Телефон" и значение не null
            if (dataGridView1.Columns[e.ColumnIndex].Name == "Телефон" && e.Value != null && e.Value != DBNull.Value)
            {
                string phone = e.Value.ToString();

                // Если не показываем полные номера - маскируем
                if (!showFullPhoneNumbers)
                {
                    e.Value = MaskPhoneNumber(phone);
                    e.FormattingApplied = true;
                }
                // Если показываем полные номера - оставляем как есть
            }
        }

        // ========== КНОПКА ПОКАЗА/СКРЫТИЯ НОМЕРОВ ==========
        private void btnShowPhone_Click(object sender, EventArgs e)
        {
            // Меняем состояние
            showFullPhoneNumbers = !showFullPhoneNumbers;

            // Меняем текст кнопки
            btnShowPhone.Text = showFullPhoneNumbers ? "Скрыть номера" : "Показать номера";

            // Принудительно перерисовываем DataGridView, чтобы применить маскировку/показ
            dataGridView1.Refresh();
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
                                o.items_json AS 'СоставJSON',
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

                    // Скрываем технические поля, но НЕ скрываем "Телефон"
                    string[] hiddenColumns = { "ID", "client_id", "status_id", "user_id",
                                               "total_amount_numeric", "ID клиента",
                                               "ID статуса", "ID пользователя", "СоставJSON" };

                    foreach (string columnName in hiddenColumns)
                    {
                        if (dataGridView1.Columns.Contains(columnName))
                            dataGridView1.Columns[columnName].Visible = false;
                    }

                    // Убеждаемся, что колонка "Телефон" видима
                    if (dataGridView1.Columns.Contains("Телефон"))
                    {
                        dataGridView1.Columns["Телефон"].Visible = true;
                        dataGridView1.Columns["Телефон"].HeaderText = "Телефон";
                        dataGridView1.Columns["Телефон"].Width = 100;
                    }

                    AddCompositionColumn();
                    ConfigureColumnWidths();
                    ConfigureGridViewAppearance();
                    FillCompositionColumn();

                    // Принудительно обновляем отображение ячеек
                    dataGridView1.Refresh();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при загрузке заказов: " + ex.Message,
                               "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ========== ДОБАВЛЕНИЕ КОЛОНКИ СОСТАВА ==========
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

        // ========== ЗАПОЛНЕНИЕ СОСТАВА ==========
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

        // ========== ФОРМАТИРОВАНИЕ JSON В ТЕКСТ ==========
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

        // ========== ПОЛУЧЕНИЕ СОСТАВА ИЗ order_items ==========
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

        // ========== ОТЧЕТ В EXCEL ==========
        private void btnReport_Click(object sender, EventArgs e)
        {
            try
            {
                DataTable reportData = GetReportData();

                if (reportData.Rows.Count == 0)
                {
                    MessageBox.Show("Нет данных для формирования отчета!",
                        "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                Microsoft.Office.Interop.Excel.Application excelApp = new Microsoft.Office.Interop.Excel.Application();
                excelApp.Visible = true;
                excelApp.DisplayAlerts = false;

                Microsoft.Office.Interop.Excel.Workbook workbook = excelApp.Workbooks.Add(Type.Missing);
                Microsoft.Office.Interop.Excel.Worksheet worksheet = workbook.ActiveSheet;
                worksheet.Name = "Отчет по продажам";

                // ЗАГОЛОВОК
                Microsoft.Office.Interop.Excel.Range titleRange = worksheet.Range["A1", "I1"];
                titleRange.Merge();
                titleRange.Value = $"ОТЧЕТ ПО ПРОДАЖАМ ЦВЕТОЧНОГО МАГАЗИНА";
                titleRange.Font.Bold = true;
                titleRange.Font.Size = 16;
                titleRange.HorizontalAlignment = Microsoft.Office.Interop.Excel.XlHAlign.xlHAlignCenter;
                titleRange.Interior.Color = Color.LightBlue;
                titleRange.RowHeight = 30;

                // Дата формирования
                worksheet.Cells[2, 1] = $"Дата формирования: {DateTime.Now.ToString("dd.MM.yyyy HH:mm")}";
                worksheet.Range["A2", "I2"].Merge();
                worksheet.Range["A2"].Font.Italic = true;

                // ИТОГОВАЯ СТАТИСТИКА
                worksheet.Cells[4, 1] = "ИТОГОВАЯ СТАТИСТИКА:";
                worksheet.Range["A4"].Font.Bold = true;
                worksheet.Range["A4"].Font.Size = 12;

                decimal totalSum = 0;
                foreach (DataRow row in reportData.Rows)
                    totalSum += Convert.ToDecimal(row["Сумма"]);

                worksheet.Cells[5, 1] = "Общая сумма всех заказов:";
                worksheet.Cells[5, 2] = totalSum;
                worksheet.Range["B5"].NumberFormat = "#,##0.00 ₽";
                worksheet.Range["B5"].Font.Bold = true;
                worksheet.Range["B5"].Font.Color = Color.DarkGreen;

                worksheet.Cells[6, 1] = "Количество заказов:";
                worksheet.Cells[6, 2] = reportData.Rows.Count;

                worksheet.Cells[7, 1] = "Средний чек:";
                worksheet.Cells[7, 2] = totalSum / reportData.Rows.Count;
                worksheet.Range["B7"].NumberFormat = "#,##0.00 ₽";

                // ЗАГОЛОВКИ ТАБЛИЦЫ
                string[] headers = { "№", "Клиент", "Телефон", "Статус", "Менеджер", "Дата заказа", "Дата выполнения", "Сумма, ₽", "Состав заказа" };
                for (int i = 0; i < headers.Length; i++)
                {
                    worksheet.Cells[9, i + 1] = headers[i];
                    Microsoft.Office.Interop.Excel.Range headerCell = worksheet.Cells[9, i + 1];
                    headerCell.Font.Bold = true;
                    headerCell.Interior.Color = Color.LightGray;
                    headerCell.HorizontalAlignment = Microsoft.Office.Interop.Excel.XlHAlign.xlHAlignCenter;
                    headerCell.Borders.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;
                }

                // ДАННЫЕ ТАБЛИЦЫ
                int rowIndex = 10;
                foreach (DataRow row in reportData.Rows)
                {
                    worksheet.Cells[rowIndex, 1] = rowIndex - 9;
                    worksheet.Cells[rowIndex, 2] = row["Клиент"].ToString();
                    worksheet.Cells[rowIndex, 3] = row["Телефон"].ToString(); // Полный номер для отчета
                    worksheet.Cells[rowIndex, 4] = row["Статус"].ToString();
                    worksheet.Cells[rowIndex, 5] = row["Пользователь"].ToString();
                    worksheet.Cells[rowIndex, 6] = row["Дата заказа"].ToString();
                    worksheet.Cells[rowIndex, 7] = row["Дата выполнения"].ToString();

                    Microsoft.Office.Interop.Excel.Range sumCell = worksheet.Cells[rowIndex, 8];
                    sumCell.Value = Convert.ToDecimal(row["Сумма"]);
                    sumCell.HorizontalAlignment = Microsoft.Office.Interop.Excel.XlHAlign.xlHAlignRight;

                    // СОСТАВ
                    if (row["Состав"] != DBNull.Value)
                    {
                        string json = row["Состав"].ToString();
                        string composition = FormatCompositionFromJson(json);
                        worksheet.Cells[rowIndex, 9] = composition;
                    }

                    // Чередование цвета строк
                    if ((rowIndex - 9) % 2 == 0)
                    {
                        Microsoft.Office.Interop.Excel.Range rowRange = worksheet.Range[
                            worksheet.Cells[rowIndex, 1],
                            worksheet.Cells[rowIndex, 9]];
                        rowRange.Interior.Color = Color.FromArgb(240, 240, 240);
                    }

                    Microsoft.Office.Interop.Excel.Range borderRange = worksheet.Range[
                        worksheet.Cells[rowIndex, 1],
                        worksheet.Cells[rowIndex, 9]];
                    borderRange.Borders.LineStyle = Microsoft.Office.Interop.Excel.XlLineStyle.xlContinuous;

                    rowIndex++;
                }

                // ИТОГОВАЯ СТРОКА
                worksheet.Cells[rowIndex, 8] = totalSum;
                worksheet.Range[worksheet.Cells[rowIndex, 8], worksheet.Cells[rowIndex, 8]].Font.Bold = true;
                worksheet.Range[worksheet.Cells[rowIndex, 8], worksheet.Cells[rowIndex, 8]].Interior.Color = Color.LightYellow;

                Microsoft.Office.Interop.Excel.Range dataRange = worksheet.Range["A9", $"I{rowIndex}"];
                dataRange.Columns.AutoFit();
                dataRange.VerticalAlignment = Microsoft.Office.Interop.Excel.XlVAlign.xlVAlignCenter;

                worksheet.Cells[rowIndex + 2, 1] = $"Всего записей: {reportData.Rows.Count}";
                worksheet.Cells[rowIndex + 3, 1] = $"Отчет сформирован: {DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss")}";

                MessageBox.Show("Отчет успешно сформирован в Microsoft Excel!",
                    "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при создании отчета: {ex.Message}",
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ========== ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ ==========
        private int GetSelectedOrderId()
        {
            if (dataGridView1.SelectedRows.Count == 0) return 0;
            DataGridViewRow selectedRow = dataGridView1.SelectedRows[0];
            if (dataGridView1.Columns.Contains("ID") && selectedRow.Cells["ID"].Value != null)
                return Convert.ToInt32(selectedRow.Cells["ID"].Value);
            return 0;
        }

        private void DeleteOrder(int orderId)
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string deleteItemsSql = "DELETE FROM order_items WHERE order_id = @orderId";
                    using (MySqlCommand itemsCmd = new MySqlCommand(deleteItemsSql, conn))
                    {
                        itemsCmd.Parameters.AddWithValue("@orderId", orderId);
                        itemsCmd.ExecuteNonQuery();
                    }

                    string deleteOrderSql = "DELETE FROM orders WHERE id = @orderId";
                    using (MySqlCommand orderCmd = new MySqlCommand(deleteOrderSql, conn))
                    {
                        orderCmd.Parameters.AddWithValue("@orderId", orderId);
                        int rowsAffected = orderCmd.ExecuteNonQuery();
                        if (rowsAffected > 0)
                        {
                            MessageBox.Show("Заказ успешно удален!", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            LoadOrders();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка удаления: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void dataGridView1_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0) btnEdit_Click(sender, e);
        }

        private void btnBack_Click_1(object sender, EventArgs e) => this.Close();

        private void btnDelete_Click(object sender, EventArgs e)
        {
            if (dataGridView1.SelectedRows.Count == 0)
            {
                MessageBox.Show("Выберите заказ для удаления", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            int orderId = GetSelectedOrderId();
            if (orderId == 0) return;

            DataGridViewRow row = dataGridView1.SelectedRows[0];
            string clientName = row.Cells["Клиент"].Value?.ToString() ?? "";
            string orderDate = row.Cells["Дата заказа"].Value?.ToString() ?? "";
            string amount = row.Cells["Сумма"].Value?.ToString() ?? "";

            DialogResult result = MessageBox.Show(
                $"Удалить заказ:\nID: {orderId}\nКлиент: {clientName}\nДата: {orderDate}\nСумма: {amount}",
                "Подтверждение удаления", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

            if (result == DialogResult.Yes) DeleteOrder(orderId);
        }

        private void btnEdit_Click(object sender, EventArgs e)
        {
            int orderId = GetSelectedOrderId();
            if (orderId == 0) return;

            // Показываем подробную информацию о заказе
            DataGridViewRow row = dataGridView1.SelectedRows[0];
            string composition = row.Cells["Состав"].Value?.ToString() ?? "";

            MessageBox.Show($"Заказ №{orderId}\n\nСостав:\n{composition}",
                "Детали заказа", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void btnReset_Click_1(object sender, EventArgs e)
        {
            txtSearch.Text = "";
            cmbStatus.SelectedIndex = 0;
            txtPriceFilter.Text = "";
            if (ordersTable != null) ordersTable.DefaultView.RowFilter = "";
        }
    }
}