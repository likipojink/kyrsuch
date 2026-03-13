using MySql.Data.MySqlClient;
using System;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace WindowsFormsApp1
{
    public partial class FormAddProduct : Form
    {
        private string connectionString = DatabaseConfig.ConnectionString;

        // Храним фото как массив байт
        private byte[] currentImageData = null;

        // КНОПКА ДЛЯ ВЫБОРА ФОТО
        private Button btnBrowseImage;

        public class CategoryItem
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }

        public FormAddProduct()
        {
            InitializeComponent();

            // СОЗДАЕМ КНОПКУ "ОБЗОР" ПРОГРАММНО
            CreateBrowseButton();

            // Настройка валидации
            txtArticle.KeyPress += TxtArticle_KeyPress;
            txtArticle.TextChanged += TxtArticle_TextChanged;
            txtName.TextChanged += TxtName_TextChanged;
            txtDescription.TextChanged += TxtDescription_TextChanged;
            txtPrice.KeyPress += TxtPrice_KeyPress;

            // Настраиваем PictureBox для Zoom
            pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBox1.BorderStyle = BorderStyle.FixedSingle;
            pictureBox1.BackColor = Color.White;

            // Устанавливаем изображение по умолчанию
            pictureBox1.Image = GetDefaultImage();
        }

        // ----------------------------------------------------------
        // СОЗДАЕМ КНОПКУ "ОБЗОР" ПРОГРАММНО
        // ----------------------------------------------------------
        private void CreateBrowseButton()
        {
            btnBrowseImage = new Button
            {
                Text = "Обзор...",
                Location = new Point(298, 274), // Справа от pictureBox1
                Size = new Size(50, 29),
                BackColor = Color.LightBlue,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Microsoft Sans Serif", 10)
            };
            btnBrowseImage.Click += BtnBrowseImage_Click;
            this.Controls.Add(btnBrowseImage);

            // Увеличиваем ширину формы чтобы поместилась кнопка
            this.Width = 440;
        }

        private void FormAddProduct_Load(object sender, EventArgs e)
        {
            LoadCategories();
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
        // ЗАГРУЗКА КАТЕГОРИЙ
        // ----------------------------------------------------------
        private void LoadCategories()
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

                    if (cmbCategory.Items.Count > 0)
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
        // ПОЛУЧЕНИЕ ID КАТЕГОРИИ
        // ----------------------------------------------------------
        private int GetSelectedCategoryId()
        {
            if (cmbCategory.SelectedItem != null && cmbCategory.SelectedItem is CategoryItem selectedCategory)
            {
                return selectedCategory.Id;
            }
            throw new Exception("Категория не выбрана");
        }

        // ----------------------------------------------------------
        // ПРОВЕРКА УНИКАЛЬНОСТИ АРТИКУЛА
        // ----------------------------------------------------------
        private bool IsArticleExists(string article)
        {
            string query = "SELECT COUNT(*) FROM products WHERE article = @art AND is_deleted = 0";

            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                MySqlCommand cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@art", article);
                int count = Convert.ToInt32(cmd.ExecuteScalar());
                return count > 0;
            }
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
                dialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        // Загружаем и отображаем изображение
                        pictureBox1.Image = Image.FromFile(dialog.FileName);

                        // Преобразуем файл в массив байт и сохраняем
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
        // СОХРАНЕНИЕ ТОВАРА (ИЗМЕНЕНО ДЛЯ РАБОТЫ С BLOB)
        // ----------------------------------------------------------
        private void btnSave_Click_1(object sender, EventArgs e)
        {
            int catId;

            try
            {
                catId = GetSelectedCategoryId();
            }
            catch
            {
                MessageBox.Show(
                    "Выберите категорию товара",
                    "Ошибка",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );
                return;
            }

            try
            {
                // ВАЛИДАЦИЯ
                if (string.IsNullOrWhiteSpace(txtArticle.Text) ||
                    string.IsNullOrWhiteSpace(txtName.Text) ||
                    string.IsNullOrWhiteSpace(txtPrice.Text))
                {
                    MessageBox.Show("Заполните все поля", "Ошибка",
                                  MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // ПРОВЕРКА ЦЕНЫ
                if (!decimal.TryParse(txtPrice.Text, out decimal price) || price <= 0)
                {
                    MessageBox.Show("Введите корректную цену", "Ошибка",
                                  MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtPrice.Focus();
                    return;
                }

                // ПРОВЕРКА УНИКАЛЬНОСТИ АРТИКУЛА
                if (IsArticleExists(txtArticle.Text))
                {
                    MessageBox.Show(
                        "Артикул с таким товаром уже используется",
                        "Ошибка",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning
                    );
                    txtArticle.Focus();
                    return;
                }

                // SQL ЗАПРОС (image_path заменен на image_data)
                string query = @"INSERT INTO products 
                               (article, name, description, price, category_id, image_data, is_deleted) 
                               VALUES 
                               (@art, @name, @desc, @price, @catId, @imageData, 0)";

                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@art", txtArticle.Text.Trim());
                        cmd.Parameters.AddWithValue("@name", txtName.Text.Trim());
                        cmd.Parameters.AddWithValue("@desc", txtDescription.Text.Trim());
                        cmd.Parameters.AddWithValue("@price", price);
                        cmd.Parameters.AddWithValue("@catId", catId);

                        // Добавляем параметр для картинки
                        if (currentImageData == null || currentImageData.Length == 0)
                        {
                            cmd.Parameters.AddWithValue("@imageData", DBNull.Value);
                        }
                        else
                        {
                            cmd.Parameters.AddWithValue("@imageData", currentImageData);
                        }

                        cmd.ExecuteNonQuery();
                    }
                }

                MessageBox.Show("Товар успешно добавлен!", "Успех",
                              MessageBoxButtons.OK, MessageBoxIcon.Information);

                this.DialogResult = DialogResult.OK;
                ClearForm();
            }
            catch (MySqlException ex) when (ex.Number == 1062)
            {
                MessageBox.Show(
                    "Артикул с таким товаром уже используется",
                    "Ошибка",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка: " + ex.Message, "Ошибка",
                              MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
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

            // Разрешаем цифры и запятую/точку
            if (!char.IsDigit(e.KeyChar) && e.KeyChar != ',' && e.KeyChar != '.')
            {
                e.Handled = true;
            }

            // Заменяем точку на запятую
            if (e.KeyChar == '.')
            {
                e.KeyChar = ',';
            }

            // Проверяем, что запятая только одна
            if (e.KeyChar == ',' && txtPrice.Text.Contains(","))
            {
                e.Handled = true;
            }
        }

        // ----------------------------------------------------------
        // СБРОС ФОРМЫ
        // ----------------------------------------------------------
        private void btnReset_Click_1(object sender, EventArgs e)
        {
            ClearForm();
        }

        private void ClearForm()
        {
            txtArticle.Clear();
            txtName.Clear();
            txtDescription.Clear();
            txtPrice.Clear();
            pictureBox1.Image = GetDefaultImage();
            currentImageData = null;

            if (cmbCategory.Items.Count > 0)
                cmbCategory.SelectedIndex = 0;

            txtArticle.Focus();
        }

        // ----------------------------------------------------------
        // НАЗАД
        // ----------------------------------------------------------
        private void btnBack_Click_1(object sender, EventArgs e)
        {
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

        private void txtPrice_KeyPress_1(object sender, KeyPressEventArgs e)
        {
            // Этот метод дублирует TxtPrice_KeyPress, можно оставить пустым
            // или удалить из дизайнера
        }
    }
}