using MySql.Data.MySqlClient;
using System;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace WindowsFormsApp1
{
    public partial class FormChangeProduct : Form
    {
        private string connectionString = DatabaseConfig.ConnectionString;
        private int productId;
        // Храним фото как массив байт
        private byte[] currentImageData = null;

        // КНОПКА ДЛЯ ВЫБОРА ФОТО
        private Button btnBrowseImage;

        public class CategoryItem
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }

        // Конструктор с параметрами - ПРИНИМАЕМ ТЕПЕРЬ byte[]
        public FormChangeProduct(int productId, string article, string name,
                               string description, decimal price, int categoryId, byte[] imageData = null)
        {
            InitializeComponent();

            // СОЗДАЕМ КНОПКУ "ОБЗОР"
            CreateBrowseButton();

            this.productId = productId;
            this.currentImageData = imageData; // Сохраняем байты

            // Заполняем поля данными товара
            txtArticle.Text = article;
            txtName.Text = name;
            txtDescription.Text = description;
            txtPrice.Text = price.ToString("N2");

            // Привязка событий
            btnSave.Click += btnSave_Click;
            btnReset.Click += btnReset_Click;
            btnBack.Click += btnBack_Click;

            // Валидация
            txtArticle.KeyPress += TxtArticle_KeyPress;
            txtArticle.TextChanged += TxtArticle_TextChanged;
            txtName.TextChanged += TxtName_TextChanged;
            txtDescription.TextChanged += TxtDescription_TextChanged;
            txtPrice.KeyPress += TxtPrice_KeyPress;

            // Загружаем категории
            LoadCategoriesAndSelect(categoryId);

            // Загружаем изображение
            LoadProductImage();

            // Настройка PictureBox
            pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBox1.BorderStyle = BorderStyle.FixedSingle;
            pictureBox1.BackColor = Color.White;
        }

        // ----------------------------------------------------------
        // СОЗДАЕМ КНОПКУ "ОБЗОР"
        // ----------------------------------------------------------
        private void CreateBrowseButton()
        {
            // Создаем кнопку "Обзор"
            btnBrowseImage = new Button
            {
                Text = "Обзор...",
                Location = new Point(300, 360),
                Size = new Size(60, 29),
                BackColor = Color.LightBlue,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Microsoft Sans Serif", 10)
            };
            btnBrowseImage.Click += BtnBrowseImage_Click;
            this.Controls.Add(btnBrowseImage);

            // Увеличиваем ширину формы
            this.Width = 450;
        }

        // ----------------------------------------------------------
        // ЗАГРУЗКА КАТЕГОРИЙ
        // ----------------------------------------------------------
        private void LoadCategoriesAndSelect(int selectedCategoryId)
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string sql = "SELECT id, name FROM categories WHERE is_deleted = 0 ORDER BY name";
                    MySqlCommand cmd = new MySqlCommand(sql, conn);
                    MySqlDataReader reader = cmd.ExecuteReader();

                    cmbCategory.Items.Clear();

                    while (reader.Read())
                    {
                        cmbCategory.Items.Add(new CategoryItem
                        {
                            Id = reader.GetInt32("id"),
                            Name = reader.GetString("name")
                        });
                    }
                    reader.Close();

                    cmbCategory.DisplayMember = "Name";
                    cmbCategory.ValueMember = "Id";

                    // Устанавливаем выбранную категорию
                    bool categorySelected = false;
                    if (selectedCategoryId > 0)
                    {
                        foreach (CategoryItem item in cmbCategory.Items)
                        {
                            if (item.Id == selectedCategoryId)
                            {
                                cmbCategory.SelectedItem = item;
                                categorySelected = true;
                                break;
                            }
                        }
                    }

                    if (!categorySelected && cmbCategory.Items.Count > 0)
                    {
                        cmbCategory.SelectedIndex = 0;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при загрузке категорий: " + ex.Message,
                                "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ----------------------------------------------------------
        // ЗАГРУЗКА ИЗОБРАЖЕНИЯ ИЗ БАЙТОВ
        // ----------------------------------------------------------
        private void LoadProductImage()
        {
            try
            {
                if (currentImageData != null && currentImageData.Length > 0)
                {
                    using (MemoryStream ms = new MemoryStream(currentImageData))
                    {
                        pictureBox1.Image = Image.FromStream(ms);
                    }
                }
                else
                {
                    pictureBox1.Image = GetDefaultImage();
                }
            }
            catch
            {
                pictureBox1.Image = GetDefaultImage();
            }
        }

        // ----------------------------------------------------------
        // ИЗОБРАЖЕНИЕ ПО УМОЛЧАНИЮ
        // ----------------------------------------------------------
        private Image GetDefaultImage()
        {
            Bitmap defaultImage = new Bitmap(129, 106);
            using (Graphics g = Graphics.FromImage(defaultImage))
            {
                g.Clear(Color.LightGray);
                g.DrawRectangle(Pens.DarkGray, 1, 1, 127, 104);
                using (Font font = new Font("Arial", 10))
                {
                    g.DrawString("Нет фото", font, Brushes.Black, new PointF(25, 40));
                }
            }
            return defaultImage;
        }

        // ----------------------------------------------------------
        // ВЫБОР ИЗОБРАЖЕНИЯ
        // ----------------------------------------------------------
        private void BtnBrowseImage_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog dialog = new OpenFileDialog())
            {
                dialog.Filter = "Картинки|*.jpg;*.jpeg;*.png;*.bmp";
                dialog.Title = "Выберите изображение товара";
                dialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures); // Начинаем с "Моих рисунков"

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        // Загружаем картинку в PictureBox
                        pictureBox1.Image = Image.FromFile(dialog.FileName);

                        // Преобразуем файл в массив байт и сохраняем во временную переменную
                        currentImageData = File.ReadAllBytes(dialog.FileName);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка загрузки изображения: {ex.Message}",
                            "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        // ----------------------------------------------------------
        // СОХРАНЕНИЕ ТОВАРА
        // ----------------------------------------------------------
        private void btnSave_Click(object sender, EventArgs e)
        {
            if (!ValidateInput())
                return;

            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();

                    // Проверка уникальности артикула
                    if (!string.IsNullOrWhiteSpace(txtArticle.Text))
                    {
                        string checkSql = "SELECT COUNT(*) FROM products WHERE article = @article AND id != @productId AND is_deleted = 0";
                        using (MySqlCommand checkCmd = new MySqlCommand(checkSql, conn))
                        {
                            checkCmd.Parameters.AddWithValue("@article", txtArticle.Text.Trim());
                            checkCmd.Parameters.AddWithValue("@productId", productId);
                            int count = Convert.ToInt32(checkCmd.ExecuteScalar());

                            if (count > 0)
                            {
                                MessageBox.Show("Товар с таким артикулом уже существует!",
                                              "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                txtArticle.Focus();
                                return;
                            }
                        }
                    }

                    // Получаем ID категории
                    int categoryId = 0;
                    if (cmbCategory.SelectedItem != null && cmbCategory.SelectedItem is CategoryItem selectedCategory)
                    {
                        categoryId = selectedCategory.Id;
                    }

                    // SQL запрос (image_path заменен на image_data)
                    string updateSql = @"UPDATE products 
                                     SET article = @article, 
                                         name = @name, 
                                         description = @description, 
                                         price = @price, 
                                         category_id = @categoryId,
                                         image_data = @imageData
                                     WHERE id = @id";

                    using (MySqlCommand updateCmd = new MySqlCommand(updateSql, conn))
                    {
                        updateCmd.Parameters.AddWithValue("@article", txtArticle.Text.Trim());
                        updateCmd.Parameters.AddWithValue("@name", txtName.Text.Trim());
                        updateCmd.Parameters.AddWithValue("@description", txtDescription.Text.Trim());
                        updateCmd.Parameters.AddWithValue("@price", decimal.Parse(txtPrice.Text.Trim()));
                        updateCmd.Parameters.AddWithValue("@categoryId", categoryId);
                        updateCmd.Parameters.AddWithValue("@id", productId);

                        // Добавляем параметр для картинки
                        if (currentImageData == null || currentImageData.Length == 0)
                        {
                            updateCmd.Parameters.AddWithValue("@imageData", DBNull.Value);
                        }
                        else
                        {
                            updateCmd.Parameters.AddWithValue("@imageData", currentImageData);
                        }

                        int rowsAffected = updateCmd.ExecuteNonQuery();

                        if (rowsAffected > 0)
                        {
                            MessageBox.Show("Данные товара успешно обновлены!", "Успех",
                                          MessageBoxButtons.OK, MessageBoxIcon.Information);
                            this.DialogResult = DialogResult.OK;
                            this.Close();
                        }
                        else
                        {
                            MessageBox.Show("Не удалось обновить данные товара", "Ошибка",
                                          MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
            }
            catch (MySqlException ex) when (ex.Number == 1062)
            {
                MessageBox.Show("Товар с таким артикулом уже существует!", "Ошибка",
                              MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка",
                              MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ----------------------------------------------------------
        // ВАЛИДАЦИЯ
        // ----------------------------------------------------------
        private bool ValidateInput()
        {
            if (string.IsNullOrWhiteSpace(txtArticle.Text))
            {
                MessageBox.Show("Введите артикул товара", "Ошибка",
                              MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtArticle.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(txtName.Text))
            {
                MessageBox.Show("Введите название товара", "Ошибка",
                              MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtName.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(txtPrice.Text))
            {
                MessageBox.Show("Введите цену товара", "Ошибка",
                              MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtPrice.Focus();
                return false;
            }

            if (!decimal.TryParse(txtPrice.Text.Trim(), out decimal price) || price <= 0)
            {
                MessageBox.Show("Введите корректную цену", "Ошибка",
                              MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtPrice.Focus();
                return false;
            }

            if (cmbCategory.SelectedIndex == -1 || cmbCategory.Items.Count == 0)
            {
                MessageBox.Show("Выберите категорию товара", "Ошибка",
                              MessageBoxButtons.OK, MessageBoxIcon.Warning);
                cmbCategory.Focus();
                return false;
            }

            return true;
        }

        // ----------------------------------------------------------
        // ВАЛИДАЦИЯ АРТИКУЛА - ТОЛЬКО ЦИФРЫ
        // ----------------------------------------------------------
        private void TxtArticle_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (char.IsControl(e.KeyChar))
                return;

            if (!char.IsDigit(e.KeyChar))
            {
                e.Handled = true;
            }
        }

        private void TxtArticle_TextChanged(object sender, EventArgs e)
        {
            int cursor = txtArticle.SelectionStart;
            string cleaned = new string(txtArticle.Text.Where(char.IsDigit).ToArray());

            if (txtArticle.Text != cleaned)
            {
                txtArticle.Text = cleaned;
                txtArticle.SelectionStart = Math.Min(cursor, txtArticle.Text.Length);
            }
        }

        // ----------------------------------------------------------
        // ЗАГЛАВНАЯ БУКВА
        // ----------------------------------------------------------
        private void CapitalizeFirstLetter(TextBox tb)
        {
            if (string.IsNullOrEmpty(tb.Text)) return;

            int cursor = tb.SelectionStart;
            string text = tb.Text;

            tb.Text = char.ToUpper(text[0]) + text.Substring(1);
            tb.SelectionStart = cursor;
        }

        private void TxtName_TextChanged(object sender, EventArgs e)
        {
            CapitalizeFirstLetter(txtName);
        }

        private void TxtDescription_TextChanged(object sender, EventArgs e)
        {
            CapitalizeFirstLetter(txtDescription);
        }

        // ----------------------------------------------------------
        // ВАЛИДАЦИЯ ЦЕНЫ
        // ----------------------------------------------------------
        private void TxtPrice_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (char.IsControl(e.KeyChar))
                return;

            if (!char.IsDigit(e.KeyChar) && e.KeyChar != ',' && e.KeyChar != '.')
            {
                e.Handled = true;
            }

            if (e.KeyChar == '.')
            {
                e.KeyChar = ',';
            }

            if (e.KeyChar == ',' && txtPrice.Text.Contains(","))
            {
                e.Handled = true;
            }
        }

        // ----------------------------------------------------------
        // СБРОС
        // ----------------------------------------------------------
        private void btnReset_Click(object sender, EventArgs e)
        {
            txtArticle.Clear();
            txtDescription.Clear();
            txtName.Clear();
            txtPrice.Clear();
            txtArticle.Focus();
        }

        // ----------------------------------------------------------
        // НАЗАД
        // ----------------------------------------------------------
        private void btnBack_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        private void txtName_KeyPress(object sender, KeyPressEventArgs e)
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

        private void txtDescription_KeyPress(object sender, KeyPressEventArgs e)
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
    }
}