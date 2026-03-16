using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using MySql.Data.MySqlClient;
using WindowsFormsApp1;
using Word = Microsoft.Office.Interop.Word;

namespace FlowerShopApp
{
    public partial class FormOrderCreation : Form
    {
        private MySqlConnection connection;
        private string connectionString = DatabaseConfig.ConnectionString;


        // Данные корзины
        private List<CartItem> cartItems = new List<CartItem>();
        private List<Product> allProducts = new List<Product>();
        private int? selectedClientId = null;
        private decimal discountPercent = 0;

        public FormOrderCreation()
        {
            InitializeComponent();

            // Настройка валидации
            SetupValidation();
            
            // Привязка событий к кнопкам
            button1.Click += (s, e) => AddToCart(); // "добавить товар в корзину"
            button2.Click += (s, e) => RemoveFromCart(); // "удалить товар из корзины"
            button3.Click += (s, e) => CreateOrder(); // "оформить заказ"
            button4.Click += (s, e) => GoToOrder(); // "перейти к заказу"
            button5.Click += (s, e) => this.Close(); // "назад"

            // Двойной клик по товару для добавления в корзину
            dataGridView1.CellDoubleClick += (s, e) =>
            {
                if (e.RowIndex >= 0)
                {
                    dataGridView1.Rows[e.RowIndex].Selected = true;
                    AddToCart();
                }
            };

            // Настройка DataGridView для корзины
            SetupCartDataGridView();

            LoadData();
        }

        private void SetupValidation()
        {
            // 1. Для телефона - только цифры, плюс и скобки
            textBox3.KeyPress += TextBox3_KeyPress;

            // 2. Для ФИО - русские буквы, пробел, тире
            textBox2.KeyPress += TextBox2_KeyPress;

            // 3. Для даты - нельзя выбрать прошедшую дату
            dateTimePicker1.ValueChanged += DateTimePicker1_ValueChanged;
            dateTimePicker1.MinDate = DateTime.Now; // Минимальная дата - сегодня
            dateTimePicker1.Value = DateTime.Now.AddDays(1); // По умолчанию - завтра

            // 4. Добавляем placeholder для подсказок
            AddPlaceholder(textBox2, "Иванов Иван Иванович");
            AddPlaceholder(textBox3, "+7 (999) 123-45-67");

            // 5. Добавляем tooltip для подсказок
            var toolTip = new ToolTip();
            toolTip.SetToolTip(textBox2, "Только русские буквы, пробел и тире");
            toolTip.SetToolTip(textBox3, "Только цифры и символы +, (, ), -");
            toolTip.SetToolTip(dateTimePicker1, "Нельзя выбрать прошедшую дату");
        }

        private void TextBox3_KeyPress(object sender, KeyPressEventArgs e)
        {
            // Разрешаем: цифры, Backspace, Delete, плюс, скобки, дефис, пробел
            char[] allowedChars = new char[] { '+', '(', ')', '-', ' ' };

            if (char.IsDigit(e.KeyChar) ||
                e.KeyChar == (char)Keys.Back ||
                e.KeyChar == (char)Keys.Delete ||
                allowedChars.Contains(e.KeyChar))
            {
                // Разрешаем ввод
                e.Handled = false;
            }
            else
            {
                // Запрещаем ввод
                e.Handled = true;

                // Показываем сообщение (только если не служебная клавиша)
                if (!char.IsControl(e.KeyChar))
                {
                    MessageBox.Show("Можно вводить только цифры и символы: +, (, ), -, пробел",
                        "Недопустимый символ",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                }
            }
        }

        private void TextBox2_KeyPress(object sender, KeyPressEventArgs e)
        {
            // Русские буквы в обоих регистрах
            string russianLetters = "абвгдеёжзийклмнопрстуфхцчшщъыьэюя";
            string russianLettersUpper = russianLetters.ToUpper();

            // Разрешаем: русские буквы, пробел, дефис, Backspace, Delete
            if ((russianLetters.Contains(e.KeyChar.ToString().ToLower())) ||
                e.KeyChar == ' ' ||
                e.KeyChar == '-' ||
                e.KeyChar == (char)Keys.Back ||
                e.KeyChar == (char)Keys.Delete)
            {
                // Разрешаем ввод
                e.Handled = false;
            }
            else
            {
                // Запрещаем ввод
                e.Handled = true;

                // Показываем сообщение (только если не служебная клавиша)
                if (!char.IsControl(e.KeyChar))
                {
                    MessageBox.Show("Можно вводить только русские буквы, пробел и тире",
                        "Недопустимый символ",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                }
            }
        }

        private void DateTimePicker1_ValueChanged(object sender, EventArgs e)
        {
            // Проверяем, что дата не в прошлом
            if (dateTimePicker1.Value < DateTime.Now)
            {
                MessageBox.Show("Нельзя выбрать прошедшую дату!",
                    "Ошибка выбора даты",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                dateTimePicker1.Value = DateTime.Now.AddDays(1);
            }
        }

        private void AddPlaceholder(TextBox textBox, string placeholder)
        {
            textBox.Text = placeholder;
            textBox.ForeColor = Color.Gray;

            textBox.Enter += (s, e) =>
            {
                if (textBox.Text == placeholder)
                {
                    textBox.Text = "";
                    textBox.ForeColor = Color.Black;
                }
            };

            textBox.Leave += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(textBox.Text))
                {
                    textBox.Text = placeholder;
                    textBox.ForeColor = Color.Gray;
                }
            };
        }

        private bool IsValidFIO(string fio)
        {
            // Проверка ФИО: только русские буквы, пробел, тире
            // Минимум 2 слова (имя и фамилия), максимум 3
            string pattern = @"^[А-Яа-яёЁ]+([-\s][А-Яа-яёЁ]+){1,2}$";
            return Regex.IsMatch(fio.Trim(), pattern);
        }

        private bool IsValidPhone(string phone)
        {
            // Проверка телефона: минимально 10 цифр
            // Удаляем все нецифровые символы
            string digitsOnly = new string(phone.Where(char.IsDigit).ToArray());
            return digitsOnly.Length >= 10;
        }

        private void SetupCartDataGridView()
        {
            dataGridView2.ReadOnly = true;
            // Настраиваем колонки для корзины
            dataGridView2.Columns.Clear();

            // Добавляем колонки для корзины
            dataGridView2.Columns.Add("product_name", "Товар");
            dataGridView2.Columns.Add("quantity", "Кол-во");
            dataGridView2.Columns.Add("price", "Цена");
            dataGridView2.Columns.Add("total", "Сумма");

            // Настраиваем формат отображения
            dataGridView2.Columns["price"].DefaultCellStyle.Format = "C2";
            dataGridView2.Columns["total"].DefaultCellStyle.Format = "C2";
            dataGridView2.Columns["price"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            dataGridView2.Columns["total"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            dataGridView2.Columns["quantity"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;

            // Автоподбор ширины
            dataGridView2.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        }

        private void LoadData()
        {
            try
            {
                ConnectToDatabase();
                LoadProducts();
                UpdateCartDisplay();
                CalculateTotals();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                connection?.Close();
            }
        }

        private void ConnectToDatabase()
        {
            try
            {
                if (connection == null)
                {
                    connection = new MySqlConnection(connectionString);
                }

                if (connection.State != ConnectionState.Open)
                {
                    connection.Open();
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка подключения к БД: {ex.Message}");
            }
        }

        private void LoadProducts()
        {
            try
            {
                string query = @"
                    SELECT p.id, p.article, p.name, p.price, c.name as category_name
                    FROM products p 
                    LEFT JOIN categories c ON p.category_id = c.id 
                    ORDER BY p.name";

                var adapter = new MySqlDataAdapter(query, connection);
                var dataTable = new DataTable();
                adapter.Fill(dataTable);
                dataGridView1.ReadOnly = true;

                allProducts.Clear();
                dataGridView1.Rows.Clear();

                // Настраиваем колонки для товаров
                dataGridView1.Columns.Clear();
                dataGridView1.Columns.Add("id", "ID");
                dataGridView1.Columns.Add("article", "Артикул");
                dataGridView1.Columns.Add("name", "Название");
                dataGridView1.Columns.Add("price", "Цена");
                dataGridView1.Columns.Add("category", "Категория");
                dataGridView1.Columns["id"].Visible = false;
                dataGridView1.Columns["price"].DefaultCellStyle.Format = "C2";
                dataGridView1.Columns["price"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;

                foreach (DataRow row in dataTable.Rows)
                {
                    var product = new Product
                    {
                        Id = Convert.ToInt32(row["id"]),
                        Article = row["article"].ToString(),
                        Name = row["name"].ToString(),
                        Price = Convert.ToDecimal(row["price"]),
                        CategoryName = row["category_name"].ToString()
                    };

                    allProducts.Add(product);
                    dataGridView1.Rows.Add(product.Id, product.Article, product.Name, product.Price, product.CategoryName);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки товаров: {ex.Message}", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void AddToCart()
        {
            if (dataGridView1.SelectedRows.Count == 0)
            {
                MessageBox.Show("Выберите товар из списка!", "Предупреждение",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!int.TryParse(numericUpDown1.Value.ToString(), out int quantity) || quantity <= 0)
            {
                MessageBox.Show("Введите корректное количество!", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                numericUpDown1.Focus();
                return;
            }

            var selectedRow = dataGridView1.SelectedRows[0];
            int productId = Convert.ToInt32(selectedRow.Cells["id"].Value);
            string productName = selectedRow.Cells["name"].Value.ToString();
            decimal price = Convert.ToDecimal(selectedRow.Cells["price"].Value);

            // Проверяем, есть ли уже такой товар в корзине
            var existingItem = cartItems.FirstOrDefault(item => item.ProductId == productId);

            if (existingItem != null)
            {
                // Увеличиваем количество
                existingItem.Quantity += quantity;
                MessageBox.Show($"Количество товара '{productName}' увеличено на {quantity}.\nТеперь в корзине: {existingItem.Quantity} шт.", "Обновлено",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                // Добавляем новый товар
                cartItems.Add(new CartItem
                {
                    ProductId = productId,
                    ProductName = productName,
                    Price = price,
                    Quantity = quantity
                });
                MessageBox.Show($"Товар '{productName}' добавлен в корзину!\nКоличество: {quantity}", "Успех",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }

            UpdateCartDisplay();
            CalculateTotals();

            // Сбрасываем количество
            numericUpDown1.Value = 1;
        }

        private void RemoveFromCart()
        {
            if (dataGridView2.SelectedRows.Count == 0)
            {
                MessageBox.Show("Выберите товар для удаления из корзины!", "Предупреждение",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int selectedIndex = dataGridView2.SelectedRows[0].Index;

            if (selectedIndex >= 0 && selectedIndex < cartItems.Count)
            {
                string productName = cartItems[selectedIndex].ProductName;
                cartItems.RemoveAt(selectedIndex);

                UpdateCartDisplay();
                CalculateTotals();

                MessageBox.Show($"Товар '{productName}' удален из корзины!", "Успех",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void UpdateCartDisplay()
        {
            dataGridView2.Rows.Clear();

            foreach (var item in cartItems)
            {
                dataGridView2.Rows.Add(
                    item.ProductName,
                    item.Quantity,
                    item.Price,
                    item.TotalPrice
                );
            }

            // Обновляем заголовок корзины
            label7.Text = $"КОРЗИНА ({cartItems.Count} товаров)";
        }

        private void CalculateTotals()
        {
            decimal totalAmount = cartItems.Sum(item => item.TotalPrice);
            decimal discountAmount = totalAmount * (discountPercent / 100);
            decimal finalAmount = totalAmount - discountAmount;

            label9.Text = $"{totalAmount:F2} ₽";
            label10.Text = $"{discountAmount:F2} ₽";
        }

        /// <summary>
        /// РУЧНОЕ СОЗДАНИЕ JSON БЕЗ NEWTONSOFT.JSON
        /// </summary>
        private string CreateItemsJson()
        {
            if (cartItems.Count == 0) return "[]";

            var items = new List<string>();

            foreach (var item in cartItems)
            {
                // Используем InvariantCulture для точки в цене
                string priceStr = item.Price.ToString(System.Globalization.CultureInfo.InvariantCulture);

                items.Add($"{{" +
                         $"\"ProductId\":{item.ProductId}," +
                         $"\"ProductName\":\"{item.ProductName}\"," +
                         $"\"Price\":{priceStr}," +
                         $"\"Quantity\":{item.Quantity}" +
                         $"}}");
            }

            return "[" + string.Join(",", items) + "]";
        }

        private void CreateOrder()
        {

            

            // Проверки
            if (cartItems.Count == 0)
            {
                MessageBox.Show("Корзина пуста! Добавьте товары в заказ.", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Проверка даты
            if (dateTimePicker1.Value < DateTime.UtcNow)
            {
                MessageBox.Show("Нельзя выбрать прошедшую дату!", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                dateTimePicker1.Focus();
                return;
            }


            // Проверка ФИО клиента
            string fio = textBox2.Text.Trim();
            if (string.IsNullOrWhiteSpace(fio) || fio == "Иванов Иван Иванович")
            {
                MessageBox.Show("Введите ФИО клиента!", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                textBox2.Focus();
                return;
            }

            // Валидация ФИО
            if (!IsValidFIO(fio))
            {
                MessageBox.Show("ФИО должно содержать только русские буквы, пробел и тире.\nПример: Иванов Иван или Петрова Анна-Мария",
                    "Некорректное ФИО",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                textBox2.Focus();
                textBox2.SelectAll();
                return;
            }

            // Проверка телефона
            string phone = textBox3.Text.Trim();
            if (string.IsNullOrWhiteSpace(phone) || phone == "+7 (999) 123-45-67")
            {
                MessageBox.Show("Введите телефон клиента!", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                textBox3.Focus();
                return;
            }

            // Валидация телефона
            if (!IsValidPhone(phone))
            {
                MessageBox.Show("Телефон должен содержать минимум 10 цифр!",
                    "Некорректный телефон",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                textBox3.Focus();
                textBox3.SelectAll();
                return;
            }


            try
            {
                ConnectToDatabase();

                // Начинаем транзакцию
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        int clientId;

                        // Проверяем, существует ли клиент с таким телефоном
                        string checkClientQuery = "SELECT id FROM clients WHERE phone = @phone";
                        var checkCmd = new MySqlCommand(checkClientQuery, connection, transaction);
                        checkCmd.Parameters.AddWithValue("@phone", phone);
                        var result = checkCmd.ExecuteScalar();

                        if (result != null)
                        {
                            // Клиент существует
                            clientId = Convert.ToInt32(result);
                        }
                        else
                        {
                            // Создаем нового клиента
                            string insertClientQuery = "INSERT INTO clients (name, phone) VALUES (@name, @phone)";
                            var clientCmd = new MySqlCommand(insertClientQuery, connection, transaction);
                            clientCmd.Parameters.AddWithValue("@name", fio);
                            clientCmd.Parameters.AddWithValue("@phone", phone);
                            clientCmd.ExecuteNonQuery();

                            clientId = (int)clientCmd.LastInsertedId;
                        }

                        // =================================================
                        // СОЗДАЕМ JSON ВРУЧНУЮ (БЕЗ NEWTONSOFT)
                        // =================================================
                        string itemsJson = CreateItemsJson();

                        // Создаем заказ
                        decimal totalAmount = cartItems.Sum(item => item.TotalPrice);

                        string insertOrderQuery = @"
                            INSERT INTO orders 
                            (client_id, status_id, user_id, order_date, completion_date, total_amount, items_json) 
                            VALUES (@client_id, @status_id, @user_id, @order_date, @completion_date, @total_amount, @items_json);
                            SELECT LAST_INSERT_ID();"; // Возвращаем ID созданного заказа

                        var orderCmd = new MySqlCommand(insertOrderQuery, connection, transaction);
                        orderCmd.Parameters.AddWithValue("@client_id", clientId);
                        orderCmd.Parameters.AddWithValue("@status_id", 1); // Статус "Собирается"
                        orderCmd.Parameters.AddWithValue("@user_id", GetCurrentUserId());
                        orderCmd.Parameters.AddWithValue("@order_date", DateTime.Now);
                        orderCmd.Parameters.AddWithValue("@completion_date", dateTimePicker1.Value);
                        orderCmd.Parameters.AddWithValue("@total_amount", totalAmount);
                        orderCmd.Parameters.AddWithValue("@items_json", itemsJson);

                        int orderId = Convert.ToInt32(orderCmd.ExecuteScalar());

                        // Добавляем товары в order_items
                        foreach (var item in cartItems)
                        {
                            string insertItemQuery = @"
                                INSERT INTO order_items (order_id, product_id, quantity, unit_price) 
                                VALUES (@order_id, @product_id, @quantity, @unit_price)";

                            var itemCmd = new MySqlCommand(insertItemQuery, connection, transaction);
                            itemCmd.Parameters.AddWithValue("@order_id", orderId);
                            itemCmd.Parameters.AddWithValue("@product_id", item.ProductId);
                            itemCmd.Parameters.AddWithValue("@quantity", item.Quantity);
                            itemCmd.Parameters.AddWithValue("@unit_price", item.Price);
                            itemCmd.ExecuteNonQuery();
                        }

                        transaction.Commit();

                        // ========== ДИАЛОГ ПЕЧАТИ ЧЕКА ==========
                        DialogResult printResult = MessageBox.Show("Распечатать чек?", "Печать чека",
                            MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                        if (printResult == DialogResult.Yes)
                        {
                            PrintReceiptInWord(orderId, fio, phone, dateTimePicker1.Value, cartItems, totalAmount);
                        }


                        // Показываем созданный JSON для проверки
                        MessageBox.Show($"Заказ №{orderId} успешно создан!\n\n" +
                                      $"JSON состав:\n{itemsJson}\n\n" +
                                      $"Дата выполнения: {dateTimePicker1.Value:dd.MM.yyyy}\n" +
                                      $"Сумма: {totalAmount:F2} ₽\n" +
                                      $"Клиент: {fio}\n" +
                                      $"Телефон: {phone}", "Успех",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);

                        // Очищаем форму после успешного создания
                        ClearForm();
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        MessageBox.Show($"Ошибка создания заказа: {ex.Message}", "Ошибка",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                connection?.Close();
            }
        }


        private void PrintReceiptInWord(int orderId, string clientName, string clientPhone,
    DateTime completionDate, List<CartItem> items, decimal total)
        {
            try
            {
                // Создаём приложение Word
                Word.Application wordApp = new Word.Application();
                wordApp.Visible = true; // Показываем Word
                wordApp.DisplayAlerts = Word.WdAlertLevel.wdAlertsNone;

                // Добавляем новый документ
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

                // Пустая строка
                paragraph = doc.Content.Paragraphs.Add();
                paragraph.Range.InsertParagraphAfter();

                // Создаём таблицу с товарами
                Word.Table table = doc.Tables.Add(paragraph.Range, items.Count + 1, 5);
                table.Borders.Enable = 1;
                table.Range.Font.Size = 11;

                // Заголовки таблицы
                table.Cell(1, 1).Range.Text = "№";
                table.Cell(1, 2).Range.Text = "Товар";
                table.Cell(1, 3).Range.Text = "Кол-во";
                table.Cell(1, 4).Range.Text = "Цена, ₽";
                table.Cell(1, 5).Range.Text = "Сумма, ₽";

                // Выравнивание заголовков
                for (int i = 1; i <= 5; i++)
                {
                    table.Cell(1, i).Range.ParagraphFormat.Alignment = Word.WdParagraphAlignment.wdAlignParagraphCenter;
                    table.Cell(1, i).Range.Font.Bold = 1;
                }

                // Заполнение товаров
                int row = 2;
                int counter = 1;
                foreach (var item in items)
                {
                    table.Cell(row, 1).Range.Text = counter.ToString();
                    table.Cell(row, 2).Range.Text = item.ProductName;
                    table.Cell(row, 3).Range.Text = item.Quantity.ToString();
                    table.Cell(row, 4).Range.Text = item.Price.ToString("F2");
                    table.Cell(row, 5).Range.Text = item.TotalPrice.ToString("F2");

                    // Выравнивание числовых колонок
                    table.Cell(row, 3).Range.ParagraphFormat.Alignment = Word.WdParagraphAlignment.wdAlignParagraphCenter;
                    table.Cell(row, 4).Range.ParagraphFormat.Alignment = Word.WdParagraphAlignment.wdAlignParagraphRight;
                    table.Cell(row, 5).Range.ParagraphFormat.Alignment = Word.WdParagraphAlignment.wdAlignParagraphRight;

                    row++;
                    counter++;
                }

                // Итоговая строка
                row = items.Count + 2; // новая строка после таблицы
                paragraph = doc.Content.Paragraphs.Add();
                paragraph.Range.Text = $"ИТОГО: {total:F2} ₽";
                paragraph.Range.Font.Bold = 1;
                paragraph.Range.Font.Size = 14;
                paragraph.Range.ParagraphFormat.Alignment = Word.WdParagraphAlignment.wdAlignParagraphRight;
                paragraph.Range.InsertParagraphAfter();

                // Освобождаем ресурсы (но не закрываем Word, чтобы пользователь увидел документ)
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


        private void GoToOrder()
        {
            if (cartItems.Count == 0)
            {
                MessageBox.Show("Корзина пуста! Добавьте товары в заказ.", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            MessageBox.Show("Переход к просмотру заказа...", "Информация",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private int GetCurrentUserId()
        {
            return 3; // Временно
        }

        private void ClearForm()
        {
            cartItems.Clear();
            selectedClientId = null;
            discountPercent = 0;

            // Сбрасываем поля с учетом placeholder
            textBox2.Text = "Иванов Иван Иванович";
            textBox2.ForeColor = Color.Gray;
            textBox3.Text = "+7 (999) 123-45-67";
            textBox3.ForeColor = Color.Gray;

            textBox1.Clear(); // очистка поиска
            numericUpDown1.Value = 1;
            dateTimePicker1.Value = DateTime.Now.AddDays(1); // завтра по умолчанию

            UpdateCartDisplay();
            CalculateTotals();
            LoadProducts(); // Обновляем список товаров
        }

        private void textBox1_KeyPress(object sender, KeyPressEventArgs e)
        {
            // Русские буквы в обоих регистрах
            string russianLetters = "абвгдеёжзийклмнопрстуфхцчшщъыьэюя";
            string russianLettersUpper = russianLetters.ToUpper();

            // Разрешаем: русские буквы, пробел, дефис, Backspace, Delete
            if ((russianLetters.Contains(e.KeyChar.ToString().ToLower())) ||
                e.KeyChar == ' ' ||
                e.KeyChar == '-' ||
                e.KeyChar == (char)Keys.Back ||
                e.KeyChar == (char)Keys.Delete)
            {
                // Разрешаем ввод
                e.Handled = false;
            }
            else
            {
                // Запрещаем ввод
                e.Handled = true;

                // Показываем сообщение (только если не служебная клавиша)
                if (!char.IsControl(e.KeyChar))
                {
                    MessageBox.Show("Можно вводить только русские буквы, пробел и тире",
                        "Недопустимый символ",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                }
            }
        }

        // Классы для данных
        public class Product
        {
            public int Id { get; set; }
            public string Article { get; set; }
            public string Name { get; set; }
            public decimal Price { get; set; }
            public string CategoryName { get; set; }
        }

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