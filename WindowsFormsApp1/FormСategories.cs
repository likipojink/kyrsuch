using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WindowsFormsApp1
{
    public partial class FormСategories : Form
    {
        private MySqlConnection connection;
        private DataGridView dataGridView;
        private TextBox txtCategoryName;

        // Строка подключения - настройте под свою БД
        private string connectionString = DatabaseConfig.ConnectionString;

        public FormСategories()
        {
            InitializeComponent1();
            LoadCategories();
        }

        private void InitializeComponent1()
        {
            this.Text = "Управление категориями";
            this.Size = new Size(800, 600);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Font = new Font("Segoe UI", 10);

            // Основной контейнер
            var mainPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(10),
                RowCount = 3,
                ColumnCount = 1
            };
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 40)); // Панель кнопок
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 80)); // Панель ввода
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // DataGridView

            // 1. Панель кнопок управления
            var buttonPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                Height = 40
            };

            var btnAdd = CreateButton("Добавить", Color.LightGreen, AddCategory);
            var btnEdit = CreateButton("Изменить", Color.LightBlue, EditCategory);
            var btnDelete = CreateButton("Удалить", Color.LightCoral, DeleteCategory);
            var btnBack = CreateButton("Назад", Color.LightGray, GoBack);

            buttonPanel.Controls.AddRange(new[] { btnAdd, btnEdit, btnDelete, btnBack });
            mainPanel.Controls.Add(buttonPanel, 0, 0);

            // 2. Панель для ввода данных
            var inputPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.FixedSingle
            };

            var inputTable = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(10),
                ColumnCount = 2,
                RowCount = 1
            };
            inputTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
            inputTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            var lblCategoryName = new Label
            {
                Text = "Название категории:",
                TextAlign = ContentAlignment.MiddleLeft,
                Dock = DockStyle.Fill
            };

            txtCategoryName = new TextBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 10),
                MaxLength = 100
            };

            inputTable.Controls.Add(lblCategoryName, 0, 0);
            inputTable.Controls.Add(txtCategoryName, 1, 0);
            inputPanel.Controls.Add(inputTable);
            mainPanel.Controls.Add(inputPanel, 0, 1);

            // 3. DataGridView для отображения категорий
            dataGridView = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = SystemColors.ControlLightLight,
                RowHeadersVisible = false,
                Font = new Font("Segoe UI", 10)
            };

            // Настройка колонок
            dataGridView.Columns.Add("id", "ID");
            dataGridView.Columns.Add("name", "Название категории");
            dataGridView.Columns["id"].Width = 50;
            dataGridView.Columns["id"].ReadOnly = true;
            dataGridView.Columns["name"].MinimumWidth = 200;

            dataGridView.SelectionChanged += (s, e) =>
            {
                if (dataGridView.SelectedRows.Count > 0)
                {
                    txtCategoryName.Text = dataGridView.SelectedRows[0].Cells["name"].Value?.ToString() ?? "";
                }
            };

            mainPanel.Controls.Add(dataGridView, 0, 2);
            this.Controls.Add(mainPanel);
        }

        private Button CreateButton(string text, Color backColor, Action action)
        {
            return new Button
            {
                Text = text,
                Size = new Size(100, 35),
                BackColor = backColor,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Margin = new Padding(5)
            }.WithClick (action);
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
                MessageBox.Show($"Ошибка подключения к БД: {ex.Message}", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                throw;
            }
        }

        private void LoadCategories()
        {
            try
            {
                ConnectToDatabase();

                string query = "SELECT id, name FROM categories ORDER BY name";
                var adapter = new MySqlDataAdapter(query, connection);
                var dataTable = new DataTable();
                adapter.Fill(dataTable);

                // Очищаем DataGridView
                dataGridView.Rows.Clear();

                // Заполняем данными
                foreach (DataRow row in dataTable.Rows)
                {
                    dataGridView.Rows.Add(row["id"], row["name"]);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки категорий: {ex.Message}", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                connection?.Close();
            }
        }

        // НОВЫЙ МЕТОД: Проверка на дублирование названия категории
        private bool IsCategoryNameExists(string categoryName, int? excludeId = null)
        {
            try
            {
                ConnectToDatabase();

                string query;
                MySqlCommand cmd;

                if (excludeId.HasValue)
                {
                    // Для редактирования: проверяем все категории кроме текущей
                    query = "SELECT COUNT(*) FROM categories WHERE LOWER(name) = LOWER(@name) AND id != @id";
                    cmd = new MySqlCommand(query, connection);
                    cmd.Parameters.AddWithValue("@name", categoryName.Trim());
                    cmd.Parameters.AddWithValue("@id", excludeId.Value);
                }
                else
                {
                    // Для добавления: проверяем все категории
                    query = "SELECT COUNT(*) FROM categories WHERE LOWER(name) = LOWER(@name)";
                    cmd = new MySqlCommand(query, connection);
                    cmd.Parameters.AddWithValue("@name", categoryName.Trim());
                }

                int count = Convert.ToInt32(cmd.ExecuteScalar());
                return count > 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка проверки названия категории: {ex.Message}", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return true; // В случае ошибки считаем, что категория существует
            }
            finally
            {
                connection?.Close();
            }
        }

        private void AddCategory()
        {
            string categoryName = txtCategoryName.Text.Trim();

            if (string.IsNullOrWhiteSpace(categoryName))
            {
                MessageBox.Show("Введите название категории!", "Предупреждение",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // 1. Клиентская проверка (без обращения к БД)
            bool existsInGrid = false;
            foreach (DataGridViewRow row in dataGridView.Rows)
            {
                if (row.Cells["name"].Value != null &&
                    string.Equals(row.Cells["name"].Value.ToString(), categoryName, StringComparison.OrdinalIgnoreCase))
                {
                    existsInGrid = true;
                    break;
                }
            }

            if (existsInGrid)
            {
                MessageBox.Show($"Категория '{categoryName}' уже существует в списке!", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // 2. Серверная проверка (для подстраховки)
            if (IsCategoryNameExists(categoryName))
            {
                MessageBox.Show($"Категория '{categoryName}' уже существует в базе данных!", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {
                ConnectToDatabase();

                string query = "INSERT INTO categories (name) VALUES (@name)";
                var cmd = new MySqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@name", categoryName);

                int result = cmd.ExecuteNonQuery();

                if (result > 0)
                {
                    MessageBox.Show("Категория успешно добавлена!", "Успех",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    txtCategoryName.Clear();
                    LoadCategories();
                }
            }
            catch (MySqlException ex)
            {
                if (ex.Number == 1062) // Duplicate entry (дополнительная проверка)
                {
                    MessageBox.Show("Категория с таким названием уже существует!", "Ошибка",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    MessageBox.Show($"Ошибка добавления категории: {ex.Message}", "Ошибка",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка добавления категории: {ex.Message}", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                connection?.Close();
            }
        }

        private void EditCategory()
        {
            if (dataGridView.SelectedRows.Count == 0)
            {
                MessageBox.Show("Выберите категорию для редактирования!", "Предупреждение",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string newCategoryName = txtCategoryName.Text.Trim();

            if (string.IsNullOrWhiteSpace(newCategoryName))
            {
                MessageBox.Show("Введите новое название категории!", "Предупреждение",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var selectedRow = dataGridView.SelectedRows[0];
            int categoryId = Convert.ToInt32(selectedRow.Cells["id"].Value);
            string currentName = selectedRow.Cells["name"].Value.ToString();

            // Проверяем, изменилось ли название
            if (string.Equals(currentName, newCategoryName, StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show("Название не изменилось!", "Информация",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // 1. Клиентская проверка (без обращения к БД)
            bool existsInGrid = false;
            foreach (DataGridViewRow row in dataGridView.Rows)
            {
                if (row.Cells["id"].Value != null &&
                    Convert.ToInt32(row.Cells["id"].Value) != categoryId && // Исключаем текущую категорию
                    row.Cells["name"].Value != null &&
                    string.Equals(row.Cells["name"].Value.ToString(), newCategoryName, StringComparison.OrdinalIgnoreCase))
                {
                    existsInGrid = true;
                    break;
                }
            }

            if (existsInGrid)
            {
                MessageBox.Show($"Категория '{newCategoryName}' уже существует в списке!", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // 2. Серверная проверка
            if (IsCategoryNameExists(newCategoryName, categoryId))
            {
                MessageBox.Show($"Категория '{newCategoryName}' уже существует в базе данных!", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (MessageBox.Show($"Изменить категорию '{currentName}' на '{newCategoryName}'?",
                "Подтверждение изменения",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                try
                {
                    ConnectToDatabase();

                    string query = "UPDATE categories SET name = @name WHERE id = @id";
                    var cmd = new MySqlCommand(query, connection);
                    cmd.Parameters.AddWithValue("@name", newCategoryName);
                    cmd.Parameters.AddWithValue("@id", categoryId);

                    int result = cmd.ExecuteNonQuery();

                    if (result > 0)
                    {
                        MessageBox.Show("Категория успешно изменена!", "Успех",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                        LoadCategories();

                        // Выделяем отредактированную строку
                        foreach (DataGridViewRow row in dataGridView.Rows)
                        {
                            if (row.Cells["id"].Value != null &&
                                Convert.ToInt32(row.Cells["id"].Value) == categoryId)
                            {
                                row.Selected = true;
                                break;
                            }
                        }
                    }
                }
                catch (MySqlException ex)
                {
                    if (ex.Number == 1062) // Duplicate entry
                    {
                        MessageBox.Show("Категория с таким названием уже существует!", "Ошибка",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    else
                    {
                        MessageBox.Show($"Ошибка изменения категории: {ex.Message}", "Ошибка",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка изменения категории: {ex.Message}", "Ошибка",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally
                {
                    connection?.Close();
                }
            }
        }

        private void DeleteCategory()
        {
            if (dataGridView.SelectedRows.Count == 0)
            {
                MessageBox.Show("Выберите категорию для удаления!", "Предупреждение",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var selectedRow = dataGridView.SelectedRows[0];
            int categoryId = Convert.ToInt32(selectedRow.Cells["id"].Value);
            string categoryName = selectedRow.Cells["name"].Value.ToString();

            // Проверяем, используется ли категория в товарах
            if (IsCategoryInUse(categoryId))
            {
                MessageBox.Show($"Категория '{categoryName}' используется в товарах и не может быть удалена!\n" +
                    "Сначала удалите или переместите все товары из этой категории.",
                    "Ошибка удаления",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (MessageBox.Show($"Удалить категорию '{categoryName}'?",
                "Подтверждение удаления",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                try
                {
                    ConnectToDatabase();

                    string query = "DELETE FROM categories WHERE id = @id";
                    var cmd = new MySqlCommand(query, connection);
                    cmd.Parameters.AddWithValue("@id", categoryId);

                    int result = cmd.ExecuteNonQuery();

                    if (result > 0)
                    {
                        MessageBox.Show("Категория успешно удалена!", "Успех",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                        txtCategoryName.Clear();
                        LoadCategories();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка удаления категории: {ex.Message}", "Ошибка",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally
                {
                    connection?.Close();
                }
            }
        }

        private bool IsCategoryInUse(int categoryId)
        {
            try
            {
                ConnectToDatabase();

                string query = "SELECT COUNT(*) FROM products WHERE category_id = @categoryId";
                var cmd = new MySqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@categoryId", categoryId);

                int count = Convert.ToInt32(cmd.ExecuteScalar());
                return count > 0;
            }
            catch
            {
                return true; // Если ошибка - лучше не удалять
            }
        }

        private void GoBack()
        {
            this.Close(); // Возврат к форме справочников
        }

       
        }
    }


