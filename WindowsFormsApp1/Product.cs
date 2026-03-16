using MySql.Data.MySqlClient;
using System;
using System.Data;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace WindowsFormsApp1
{
    public partial class Product : Form
    {
        private string connectionString = DatabaseConfig.ConnectionString;
        private DataTable productsTable;
        private DataTable fullDataTable; // полные данные без пагинации

        // Поля для пагинации
        private int currentPage = 1;
        private int pageSize = 25;
        private int totalRecords = 0;
        private int totalPages = 0;

        public Product()
        {
            InitializeComponent();

            // Настройка пагинации
            cmbPageSize.Items.AddRange(new object[] { 10, 25, 50, 100 });
            cmbPageSize.SelectedIndex = 1; // 25 по умолчанию
            cmbPageSize.SelectedIndexChanged += cmbPageSize_SelectedIndexChanged;

            btnPrevPage.Click += btnPrevPage_Click;
            btnNextPage.Click += btnNextPage_Click;

            dataGridView1.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dataGridView1.MultiSelect = false;

            // Настройка событий
            txtSearch.TextChanged += textBox1_TextChanged;
            cmbCategory.SelectedIndexChanged += cmbCategory_SelectedIndexChanged;
            comboBox2.SelectedIndexChanged += comboBox1_SelectedIndexChanged;

            // Настройка внешнего вида
            dataGridView1.DefaultCellStyle.SelectionBackColor = Color.FromArgb(200, 220, 240);
            dataGridView1.RowTemplate.Height = 80;
            dataGridView1.AllowUserToAddRows = false;
            dataGridView1.AllowUserToDeleteRows = false;
            dataGridView1.ReadOnly = true;
        }

        private void Product_Load(object sender, EventArgs e)
        {
            LoadSortOptions();
            LoadCategories();
            LoadAllProducts(); // Загружаем все данные
        }

        // ----------------------------------------------------------
        // ЗАГРУЗКА СОРТИРОВКИ
        // ----------------------------------------------------------
        private void LoadSortOptions()
        {
            comboBox2.Items.Clear();
            comboBox2.Items.Add("Без сортировки");
            comboBox2.Items.Add("По возрастанию цены");
            comboBox2.Items.Add("По убыванию цены");
            comboBox2.SelectedIndex = 0;
        }

        // ----------------------------------------------------------
        // ЗАГРУЗКА КАТЕГОРИЙ
        // ----------------------------------------------------------
        private void LoadCategories()
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string sql = "SELECT id, name FROM categories ORDER BY name";
                    MySqlDataAdapter da = new MySqlDataAdapter(sql, conn);
                    DataTable dt = new DataTable();
                    da.Fill(dt);

                    DataRow allRow = dt.NewRow();
                    allRow["id"] = 0;
                    allRow["name"] = "Все категории";
                    dt.Rows.InsertAt(allRow, 0);

                    cmbCategory.DataSource = dt;
                    cmbCategory.DisplayMember = "name";
                    cmbCategory.ValueMember = "id";
                    cmbCategory.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки категорий: " + ex.Message);
            }
        }

        // ----------------------------------------------------------
        // ЗАГРУЗКА ВСЕХ ТОВАРОВ (БЕЗ ПАГИНАЦИИ)
        // ----------------------------------------------------------
        private void LoadAllProducts()
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string sql = @"SELECT 
                                p.id,
                                p.article AS Артикул,
                                p.name AS Название,
                                p.description AS Описание,
                                p.price AS Цена,
                                c.name AS Категория,
                                p.image_data AS Фотография
                             FROM products p
                             LEFT JOIN categories c ON p.category_id = c.id
                             WHERE p.is_deleted = 0
                             ORDER BY p.name";

                    MySqlDataAdapter da = new MySqlDataAdapter(sql, conn);
                    fullDataTable = new DataTable();
                    da.Fill(fullDataTable);

                    // Получаем общее количество записей
                    totalRecords = fullDataTable.Rows.Count;

                    // Рассчитываем общее количество страниц
                    CalculateTotalPages();

                    // Применяем пагинацию
                    ApplyPagination();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки товаров: " + ex.Message);
            }
        }

        // ----------------------------------------------------------
        // РАСЧЕТ КОЛИЧЕСТВА СТРАНИЦ
        // ----------------------------------------------------------
        private void CalculateTotalPages()
        {
            if (pageSize <= 0) pageSize = 25;
            totalPages = (int)Math.Ceiling((double)totalRecords / pageSize);
            if (totalPages == 0) totalPages = 1;

            // Проверяем, что текущая страница не выходит за пределы
            if (currentPage > totalPages) currentPage = totalPages;
            if (currentPage < 1) currentPage = 1;

            UpdatePaginationControls();
        }

        // ----------------------------------------------------------
        // ПРИМЕНЕНИЕ ПАГИНАЦИИ
        // ----------------------------------------------------------
        private void ApplyPagination()
        {
            if (fullDataTable == null) return;

            // Создаем новую таблицу для текущей страницы
            productsTable = fullDataTable.Clone();

            int startIndex = (currentPage - 1) * pageSize;
            int endIndex = Math.Min(startIndex + pageSize, totalRecords);

            for (int i = startIndex; i < endIndex; i++)
            {
                productsTable.ImportRow(fullDataTable.Rows[i]);
            }

            // СНАЧАЛА привязываем данные
            dataGridView1.DataSource = productsTable;

            // ПОТОМ настраиваем внешний вид
            SetupDataGridView();

            // ЗАГРУЗКА ФОТОГРАФИЙ
            LoadImagesToGrid();
        }

        // ----------------------------------------------------------
        // ОБНОВЛЕНИЕ ЭЛЕМЕНТОВ УПРАВЛЕНИЯ ПАГИНАЦИЕЙ
        // ----------------------------------------------------------
        private void UpdatePaginationControls()
        {
            lblPageInfo.Text = $"Страница {currentPage} из {totalPages}";
            btnPrevPage.Enabled = currentPage > 1;
            btnNextPage.Enabled = currentPage < totalPages;
        }

        // ----------------------------------------------------------
        // ОБРАБОТЧИКИ ПАГИНАЦИИ
        // ----------------------------------------------------------
        private void cmbPageSize_SelectedIndexChanged(object sender, EventArgs e)
        {
            pageSize = Convert.ToInt32(cmbPageSize.SelectedItem);
            currentPage = 1;
            CalculateTotalPages();
            ApplyPagination();
        }

        private void btnPrevPage_Click(object sender, EventArgs e)
        {
            if (currentPage > 1)
            {
                currentPage--;
                ApplyPagination();
            }
        }

        private void btnNextPage_Click(object sender, EventArgs e)
        {
            if (currentPage < totalPages)
            {
                currentPage++;
                ApplyPagination();
            }
        }

        // ----------------------------------------------------------
        // НАСТРОЙКА ВНЕШНЕГО ВИДА ТАБЛИЦЫ
        // ----------------------------------------------------------
        private void SetupDataGridView()
        {
            // Скрываем заголовки строк (первый пустой столбец)
            dataGridView1.RowHeadersVisible = false;

            // Скрываем ID
            if (dataGridView1.Columns.Contains("id"))
                dataGridView1.Columns["id"].Visible = false;

            // Скрываем колонку с бинарными данными
            if (dataGridView1.Columns.Contains("Фотография"))
                dataGridView1.Columns["Фотография"].Visible = false;

            // Настройка заголовков
            if (dataGridView1.Columns.Contains("Артикул"))
            {
                dataGridView1.Columns["Артикул"].HeaderText = "Артикул";
                dataGridView1.Columns["Артикул"].Width = 80;
            }

            if (dataGridView1.Columns.Contains("Название"))
            {
                dataGridView1.Columns["Название"].HeaderText = "Название";
                dataGridView1.Columns["Название"].Width = 150;
            }

            if (dataGridView1.Columns.Contains("Описание"))
            {
                dataGridView1.Columns["Описание"].HeaderText = "Описание";
                dataGridView1.Columns["Описание"].Width = 200;
            }

            if (dataGridView1.Columns.Contains("Цена"))
            {
                dataGridView1.Columns["Цена"].HeaderText = "Цена, ₽";
                dataGridView1.Columns["Цена"].DefaultCellStyle.Format = "N2";
                dataGridView1.Columns["Цена"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                dataGridView1.Columns["Цена"].Width = 80;
            }

            if (dataGridView1.Columns.Contains("Категория"))
            {
                dataGridView1.Columns["Категория"].HeaderText = "Категория";
                dataGridView1.Columns["Категория"].Width = 120;
            }

            // Добавляем колонку для фото (ПЕРВАЯ КОЛОНКА), если её ещё нет
            if (!dataGridView1.Columns.Contains("Фото"))
            {
                DataGridViewImageColumn imgCol = new DataGridViewImageColumn();
                imgCol.Name = "Фото";
                imgCol.HeaderText = "Фото";
                imgCol.Width = 80;
                imgCol.ImageLayout = DataGridViewImageCellLayout.Zoom;
                dataGridView1.Columns.Insert(0, imgCol);
            }

            // Настройка автоматического заполнения
            dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            if (dataGridView1.Columns.Contains("Фото"))
                dataGridView1.Columns["Фото"].FillWeight = 5;
            if (dataGridView1.Columns.Contains("Артикул"))
                dataGridView1.Columns["Артикул"].FillWeight = 5;
            if (dataGridView1.Columns.Contains("Название"))
                dataGridView1.Columns["Название"].FillWeight = 15;
            if (dataGridView1.Columns.Contains("Описание"))
                dataGridView1.Columns["Описание"].FillWeight = 30;
            if (dataGridView1.Columns.Contains("Цена"))
                dataGridView1.Columns["Цена"].FillWeight = 5;
            if (dataGridView1.Columns.Contains("Категория"))
                dataGridView1.Columns["Категория"].FillWeight = 10;
        }

        // ----------------------------------------------------------
        // ЗАГРУЗКА ФОТОГРАФИЙ В ТАБЛИЦУ ИЗ БАЙТОВ
        // ----------------------------------------------------------
        private void LoadImagesToGrid()
        {
            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                if (row.IsNewRow) continue;

                // Получаем байты изображения из скрытой колонки
                if (productsTable.Columns.Contains("Фотография"))
                {
                    DataRowView rowView = row.DataBoundItem as DataRowView;
                    if (rowView != null)
                    {
                        byte[] imageData = rowView["Фотография"] as byte[];
                        row.Cells["Фото"].Value = GetProductImage(imageData);
                    }
                }
            }
        }

        // ----------------------------------------------------------
        // ПОЛУЧЕНИЕ ИЗОБРАЖЕНИЯ ТОВАРА ИЗ МАССИВА БАЙТ
        // ----------------------------------------------------------
        private Image GetProductImage(byte[] imageData)
        {
            try
            {
                if (imageData != null && imageData.Length > 0)
                {
                    using (MemoryStream ms = new MemoryStream(imageData))
                    {
                        Image img = Image.FromStream(ms);
                        return ResizeImage(img, 70, 70);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ошибка загрузки фото: " + ex.Message);
            }

            return CreateNoImageBitmap();
        }

        // ----------------------------------------------------------
        // ИЗМЕНЕНИЕ РАЗМЕРА ИЗОБРАЖЕНИЯ
        // ----------------------------------------------------------
        private Image ResizeImage(Image original, int width, int height)
        {
            Bitmap resized = new Bitmap(width, height);
            using (Graphics g = Graphics.FromImage(resized))
            {
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                g.DrawImage(original, 0, 0, width, height);
            }
            return resized;
        }

        // ----------------------------------------------------------
        // СОЗДАНИЕ ЗАГЛУШКИ "НЕТ ФОТО"
        // ----------------------------------------------------------
        private Bitmap CreateNoImageBitmap()
        {
            Bitmap noImage = new Bitmap(70, 70);
            using (Graphics g = Graphics.FromImage(noImage))
            {
                g.Clear(Color.LightGray);
                g.DrawRectangle(Pens.DarkGray, 0, 0, 69, 69);
                using (Font font = new Font("Arial", 8))
                {
                    g.DrawString("Нет фото", font, Brushes.Black, new PointF(12, 28));
                }
            }
            return noImage;
        }

        // ----------------------------------------------------------
        // ПРИМЕНЕНИЕ ФИЛЬТРОВ И СОРТИРОВКИ
        // ----------------------------------------------------------
        private void ApplyFilters()
        {
            if (fullDataTable == null) return;

            DataView dv = fullDataTable.DefaultView;
            string filter = "";

            // Фильтр по названию
            if (!string.IsNullOrWhiteSpace(txtSearch.Text))
            {
                filter = $"[Название] LIKE '%{txtSearch.Text}%'";
            }

            // Фильтр по категории
            if (cmbCategory.SelectedIndex > 0)
            {
                if (!string.IsNullOrEmpty(filter)) filter += " AND ";
                filter += $"[Категория] = '{cmbCategory.Text}'";
            }

            // Применяем фильтр
            dv.RowFilter = filter;

            // Применяем сортировку
            switch (comboBox2.SelectedIndex)
            {
                case 1: // По возрастанию цены
                    dv.Sort = "[Цена] ASC";
                    break;
                case 2: // По убыванию цены
                    dv.Sort = "[Цена] DESC";
                    break;
                default:
                    dv.Sort = "[Название] ASC";
                    break;
            }

            // Обновляем количество записей
            totalRecords = dv.Count;
            CalculateTotalPages();

            // Сохраняем отфильтрованные данные
            fullDataTable = dv.ToTable();

            // Применяем пагинацию
            ApplyPagination();
        }

        // ----------------------------------------------------------
        // ОБРАБОТЧИКИ СОБЫТИЙ
        // ----------------------------------------------------------
        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            currentPage = 1; // Сбрасываем на первую страницу при поиске
            ApplyFilters();
        }

        private void cmbCategory_SelectedIndexChanged(object sender, EventArgs e)
        {
            currentPage = 1; // Сбрасываем на первую страницу при смене категории
            ApplyFilters();
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            currentPage = 1; // Сбрасываем на первую страницу при смене сортировки
            ApplyFilters();
        }

        // ----------------------------------------------------------
        // СБРОС ФИЛЬТРОВ
        // ----------------------------------------------------------
        private void button5_Click(object sender, EventArgs e)
        {
            txtSearch.Text = "";
            cmbCategory.SelectedIndex = 0;
            comboBox2.SelectedIndex = 0;
            currentPage = 1;
            ApplyFilters();
        }

        // ----------------------------------------------------------
        // ДОБАВЛЕНИЕ ТОВАРА
        // ----------------------------------------------------------
        private void button1_Click(object sender, EventArgs e)
        {
            FormAddProduct addForm = new FormAddProduct();
            addForm.FormClosed += (s, args) => LoadAllProducts();
            addForm.ShowDialog();
        }

        // ----------------------------------------------------------
        // РЕДАКТИРОВАНИЕ ТОВАРА
        // ----------------------------------------------------------
        private void btnEdit_Click(object sender, EventArgs e)
        {
            if (dataGridView1.SelectedRows.Count == 0)
            {
                MessageBox.Show("Выберите товар для редактирования", "Информация",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            DataGridViewRow row = dataGridView1.SelectedRows[0];

            // Получаем ID товара
            int productId = 0;
            if (dataGridView1.Columns.Contains("id") && row.Cells["id"].Value != null)
                productId = Convert.ToInt32(row.Cells["id"].Value);
            else
            {
                MessageBox.Show("Не удалось получить ID товара", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Получаем остальные данные
            string article = row.Cells["Артикул"].Value?.ToString() ?? "";
            string name = row.Cells["Название"].Value?.ToString() ?? "";
            string description = row.Cells["Описание"].Value?.ToString() ?? "";
            decimal price = 0;
            decimal.TryParse(row.Cells["Цена"].Value?.ToString(), out price);

            int categoryId = GetCategoryIdByName(row.Cells["Категория"].Value?.ToString() ?? "");

            // Получаем байты изображения
            byte[] imageData = null;
            DataRowView rowView = row.DataBoundItem as DataRowView;
            if (rowView != null && productsTable.Columns.Contains("Фотография"))
            {
                imageData = rowView["Фотография"] as byte[];
            }

            // Открываем форму редактирования (передаем byte[])
            FormChangeProduct editForm = new FormChangeProduct(
                productId, article, name, description, price, categoryId, imageData);

            editForm.FormClosed += (s, args) =>
            {
                if (editForm.DialogResult == DialogResult.OK)
                    LoadAllProducts();
            };

            editForm.ShowDialog();
        }

        // ----------------------------------------------------------
        // ПОЛУЧЕНИЕ ID КАТЕГОРИИ ПО НАЗВАНИЮ
        // ----------------------------------------------------------
        private int GetCategoryIdByName(string categoryName)
        {
            if (string.IsNullOrEmpty(categoryName)) return 0;

            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string sql = "SELECT id FROM categories WHERE name = @name";
                    using (MySqlCommand cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@name", categoryName);
                        object result = cmd.ExecuteScalar();
                        return result != null ? Convert.ToInt32(result) : 0;
                    }
                }
            }
            catch
            {
                return 0;
            }
        }

        // ----------------------------------------------------------
        // УДАЛЕНИЕ ТОВАРА (МЯГКОЕ УДАЛЕНИЕ ЧЕРЕЗ is_deleted)
        // ----------------------------------------------------------
        private void btnDelet_Click(object sender, EventArgs e)
        {
            if (dataGridView1.SelectedRows.Count == 0)
            {
                MessageBox.Show("Выберите товар для удаления", "Информация",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            DataGridViewRow row = dataGridView1.SelectedRows[0];
            int productId = 0;

            if (dataGridView1.Columns.Contains("id") && row.Cells["id"].Value != null)
                productId = Convert.ToInt32(row.Cells["id"].Value);
            else
                return;

            string productName = row.Cells["Название"].Value?.ToString() ?? "";

            DialogResult result = MessageBox.Show(
                $"Вы действительно хотите удалить товар \"{productName}\"?",
                "Подтверждение удаления",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (result == DialogResult.Yes)
            {
                try
                {
                    using (MySqlConnection conn = new MySqlConnection(connectionString))
                    {
                        conn.Open();

                        // МЯГКОЕ УДАЛЕНИЕ (через is_deleted)
                        string sql = "UPDATE products SET is_deleted = 1 WHERE id = @id";

                        using (MySqlCommand cmd = new MySqlCommand(sql, conn))
                        {
                            cmd.Parameters.AddWithValue("@id", productId);
                            int rows = cmd.ExecuteNonQuery();

                            if (rows > 0)
                            {
                                MessageBox.Show("Товар успешно удален", "Успех",
                                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                                LoadAllProducts();
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка удаления: {ex.Message}", "Ошибка",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        // ----------------------------------------------------------
        // НАЗАД
        // ----------------------------------------------------------
        private void button4_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void cmbPageSize_SelectedIndexChanged_1(object sender, EventArgs e)
        {

        }
    }
}