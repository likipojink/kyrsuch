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
    public partial class FormRole : Form
    {
        private MySqlConnection connection;
        private DataGridView dataGridView;
        private TextBox txtRoleName;
        private int? currentEditingId = null; // ID редактируемой роли

        private string connectionString = "server=127.0.0.1;database=flower_shop;uid=root;pwd=;charset=utf8mb4";

        public FormRole()
        {
            InitializeComponents();
            LoadRoles();
        }

        private void InitializeComponents()
        {
            this.Text = "Управление ролями";
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
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 80));
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            // 1. Панель кнопок управления
            var buttonPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                Height = 40
            };

            var btnAdd = CreateButton("Добавить", Color.LightGreen, AddRole);
            var btnEdit = CreateButton("Изменить", Color.LightBlue, EditRole);
            var btnDelete = CreateButton("Удалить", Color.LightCoral, DeleteRole);
            var btnCancel = CreateButton("Отмена", Color.LightYellow, CancelEdit);
            var btnBack = CreateButton("Назад", Color.LightGray, GoBack);

            buttonPanel.Controls.AddRange(new[] { btnAdd, btnEdit, btnDelete, btnCancel, btnBack });
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
            inputTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100));
            inputTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            var lblRoleName = new Label
            {
                Text = "Название роли:",
                TextAlign = ContentAlignment.MiddleLeft,
                Dock = DockStyle.Fill
            };

            txtRoleName = new TextBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 10),
                MaxLength = 50
            };

            inputTable.Controls.Add(lblRoleName, 0, 0);
            inputTable.Controls.Add(txtRoleName, 1, 0);
            inputPanel.Controls.Add(inputTable);
            mainPanel.Controls.Add(inputPanel, 0, 1);

            // 3. DataGridView для отображения ролей
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
            dataGridView.Columns.Add("name", "Название роли");
            dataGridView.Columns["id"].Width = 50;
            dataGridView.Columns["id"].ReadOnly = true;

            dataGridView.SelectionChanged += (s, e) =>
            {
                if (dataGridView.SelectedRows.Count > 0)
                {
                    currentEditingId = Convert.ToInt32(dataGridView.SelectedRows[0].Cells["id"].Value);
                    txtRoleName.Text = dataGridView.SelectedRows[0].Cells["name"].Value?.ToString() ?? "";
                }
            };

            // Двойной клик для редактирования
            dataGridView.CellDoubleClick += (s, e) =>
            {
                if (e.RowIndex >= 0)
                {
                    currentEditingId = Convert.ToInt32(dataGridView.Rows[e.RowIndex].Cells["id"].Value);
                    txtRoleName.Text = dataGridView.Rows[e.RowIndex].Cells["name"].Value?.ToString() ?? "";
                    EditRole();
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
            }.WithClick(action);
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

        private void LoadRoles()
        {
            try
            {
                ConnectToDatabase();

                string query = "SELECT id, name FROM roles ORDER BY id";
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

                // Сбрасываем ID редактирования
                currentEditingId = null;
                ClearInput();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки ролей: {ex.Message}", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                connection?.Close();
            }
        }

        private bool IsRoleNameUnique(string roleName, int? excludeId = null)
        {
            try
            {
                ConnectToDatabase();

                string query;
                if (excludeId.HasValue)
                {
                    query = "SELECT COUNT(*) FROM roles WHERE name = @name AND id != @id";
                }
                else
                {
                    query = "SELECT COUNT(*) FROM roles WHERE name = @name";
                }

                var cmd = new MySqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@name", roleName.Trim());

                if (excludeId.HasValue)
                {
                    cmd.Parameters.AddWithValue("@id", excludeId.Value);
                }

                int count = Convert.ToInt32(cmd.ExecuteScalar());
                return count == 0; // true если уникально
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка проверки уникальности: {ex.Message}", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            finally
            {
                connection?.Close();
            }
        }

        private void AddRole()
        {
            if (string.IsNullOrWhiteSpace(txtRoleName.Text))
            {
                MessageBox.Show("Введите название роли!", "Предупреждение",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtRoleName.Focus();
                return;
            }

            // Проверка уникальности
            if (!IsRoleNameUnique(txtRoleName.Text))
            {
                MessageBox.Show("Роль с таким названием уже существует!\nПожалуйста, введите другое название.",
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                txtRoleName.Focus();
                txtRoleName.SelectAll();
                return;
            }

            try
            {
                ConnectToDatabase();

                string query = "INSERT INTO roles (name) VALUES (@name)";
                var cmd = new MySqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@name", txtRoleName.Text.Trim());

                int result = cmd.ExecuteNonQuery();

                if (result > 0)
                {
                    MessageBox.Show("Роль успешно добавлена!", "Успех",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    ClearInput();
                    LoadRoles();
                }
            }
            catch (MySqlException ex)
            {
                if (ex.Number == 1062) // Duplicate entry (дополнительная защита)
                {
                    MessageBox.Show("Роль с таким названием уже существует!", "Ошибка",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    MessageBox.Show($"Ошибка добавления роли: {ex.Message}", "Ошибка",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка добавления роли: {ex.Message}", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                connection?.Close();
            }
        }

        private void EditRole()
        {
            if (!currentEditingId.HasValue)
            {
                MessageBox.Show("Выберите роль для редактирования!", "Предупреждение",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(txtRoleName.Text))
            {
                MessageBox.Show("Введите новое название роли!", "Предупреждение",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtRoleName.Focus();
                return;
            }

            // Получаем текущее название роли для сравнения
            string currentName = "";
            foreach (DataGridViewRow row in dataGridView.Rows)
            {
                if (Convert.ToInt32(row.Cells["id"].Value) == currentEditingId.Value)
                {
                    currentName = row.Cells["name"].Value.ToString();
                    break;
                }
            }

            // Если название не изменилось, ничего не делаем
            if (currentName.Equals(txtRoleName.Text.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show("Название роли не изменилось.", "Информация",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Проверка уникальности (исключая текущую роль)
            if (!IsRoleNameUnique(txtRoleName.Text, currentEditingId.Value))
            {
                MessageBox.Show("Роль с таким названием уже существует!\nПожалуйста, введите другое название.",
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                txtRoleName.Focus();
                txtRoleName.SelectAll();
                return;
            }

            if (MessageBox.Show($"Изменить роль '{currentName}' на '{txtRoleName.Text}'?",
                "Подтверждение изменения",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                try
                {
                    ConnectToDatabase();

                    string query = "UPDATE roles SET name = @name WHERE id = @id";
                    var cmd = new MySqlCommand(query, connection);
                    cmd.Parameters.AddWithValue("@name", txtRoleName.Text.Trim());
                    cmd.Parameters.AddWithValue("@id", currentEditingId.Value);

                    int result = cmd.ExecuteNonQuery();

                    if (result > 0)
                    {
                        MessageBox.Show("Роль успешно изменена!", "Успех",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                        ClearInput();
                        LoadRoles();
                    }
                }
                catch (MySqlException ex)
                {
                    if (ex.Number == 1062) // Duplicate entry (дополнительная защита)
                    {
                        MessageBox.Show("Роль с таким названием уже существует!", "Ошибка",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    else
                    {
                        MessageBox.Show($"Ошибка изменения роли: {ex.Message}", "Ошибка",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка изменения роли: {ex.Message}", "Ошибка",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally
                {
                    connection?.Close();
                }
            }
        }

        private void DeleteRole()
        {
            if (!currentEditingId.HasValue || dataGridView.SelectedRows.Count == 0)
            {
                MessageBox.Show("Выберите роль для удаления!", "Предупреждение",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var selectedRow = dataGridView.SelectedRows[0];
            int roleId = Convert.ToInt32(selectedRow.Cells["id"].Value);
            string roleName = selectedRow.Cells["name"].Value.ToString();

            // Проверяем, используется ли роль
            if (IsRoleInUse(roleId))
            {
                MessageBox.Show($"Роль '{roleName}' используется и не может быть удалена!\n" +
                    "Сначала удалите всех пользователей с этой ролью.",
                    "Ошибка удаления",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (MessageBox.Show($"Удалить роль '{roleName}'?",
                "Подтверждение удаления",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                try
                {
                    ConnectToDatabase();

                    string query = "DELETE FROM roles WHERE id = @id";
                    var cmd = new MySqlCommand(query, connection);
                    cmd.Parameters.AddWithValue("@id", roleId);

                    int result = cmd.ExecuteNonQuery();

                    if (result > 0)
                    {
                        MessageBox.Show("Роль успешно удалена!", "Успех",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                        ClearInput();
                        LoadRoles();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка удаления роли: {ex.Message}", "Ошибка",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally
                {
                    connection?.Close();
                }
            }
        }

        private bool IsRoleInUse(int roleId)
        {
            try
            {
                ConnectToDatabase();

                string query = "SELECT COUNT(*) FROM users WHERE role_id = @roleId";
                var cmd = new MySqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@roleId", roleId);

                int count = Convert.ToInt32(cmd.ExecuteScalar());
                return count > 0;
            }
            catch
            {
                return true; // Если ошибка - лучше не удалять
            }
        }

        private void ClearInput()
        {
            txtRoleName.Clear();
            txtRoleName.Focus();
            currentEditingId = null;

            // Снимаем выделение в DataGridView
            if (dataGridView.SelectedRows.Count > 0)
            {
                dataGridView.ClearSelection();
            }
        }

        private void CancelEdit()
        {
            ClearInput();
        }

        private void GoBack()
        {
            this.Close();
        }


    }

    public static class ButtonExtensions
    {
        public static Button WithClick(this Button button, Action action)
        {
            button.Click += (s, e) => action();
            return button;
        }
    }
}