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
    public partial class FormOrderStatus : Form
    {
        private MySqlConnection connection;
        private DataGridView dataGridView;
        private TextBox txtStatusName;

        private string connectionString = DatabaseConfig.ConnectionString;
        private int? currentEditingId = null; // ID редактируемого статуса

        public FormOrderStatus()
        {
            InitializeComponent1();
            LoadStatuses();
        }

        private void InitializeComponent1()
        {
            this.Text = "Управление статусами заказов";
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

            var btnAdd = CreateButton("Добавить", Color.LightGreen, AddStatus);
            var btnEdit = CreateButton("Изменить", Color.LightBlue, EditStatus);
            var btnDelete = CreateButton("Удалить", Color.LightCoral, DeleteStatus);
            var btnBack = CreateButton("Назад", Color.LightGray, GoBack);
            var btnCancel = CreateButton("Отмена", Color.LightYellow, CancelEdit);

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
            inputTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
            inputTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            var lblStatusName = new Label
            {
                Text = "Название статуса:",
                TextAlign = ContentAlignment.MiddleLeft,
                Dock = DockStyle.Fill
            };

            txtStatusName = new TextBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 10),
                MaxLength = 50
            };

            inputTable.Controls.Add(lblStatusName, 0, 0);
            inputTable.Controls.Add(txtStatusName, 1, 0);
            inputPanel.Controls.Add(inputTable);
            mainPanel.Controls.Add(inputPanel, 0, 1);

            // 3. DataGridView для отображения статусов
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
            dataGridView.Columns.Add("name", "Название статуса");
            dataGridView.Columns["id"].Width = 50;
            dataGridView.Columns["id"].ReadOnly = true;

            dataGridView.SelectionChanged += (s, e) =>
            {
                if (dataGridView.SelectedRows.Count > 0)
                {
                    currentEditingId = Convert.ToInt32(dataGridView.SelectedRows[0].Cells["id"].Value);
                    txtStatusName.Text = dataGridView.SelectedRows[0].Cells["name"].Value?.ToString() ?? "";
                }
            };

            // Двойной клик для редактирования
            dataGridView.CellDoubleClick += (s, e) =>
            {
                if (e.RowIndex >= 0)
                {
                    currentEditingId = Convert.ToInt32(dataGridView.Rows[e.RowIndex].Cells["id"].Value);
                    txtStatusName.Text = dataGridView.Rows[e.RowIndex].Cells["name"].Value?.ToString() ?? "";
                    EditStatus();
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

        private void LoadStatuses()
        {
            try
            {
                ConnectToDatabase();

                string query = "SELECT id, name FROM order_statuses ORDER BY id";
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
                MessageBox.Show($"Ошибка загрузки статусов: {ex.Message}", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                connection?.Close();
            }
        }

        private bool IsStatusNameUnique(string statusName, int? excludeId = null)
        {
            try
            {
                ConnectToDatabase();

                string query;
                if (excludeId.HasValue)
                {
                    query = "SELECT COUNT(*) FROM order_statuses WHERE name = @name AND id != @id";
                }
                else
                {
                    query = "SELECT COUNT(*) FROM order_statuses WHERE name = @name";
                }

                var cmd = new MySqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@name", statusName.Trim());

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

        private void AddStatus()
        {
            if (string.IsNullOrWhiteSpace(txtStatusName.Text))
            {
                MessageBox.Show("Введите название статуса!", "Предупреждение",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtStatusName.Focus();
                return;
            }

            // Проверка уникальности
            if (!IsStatusNameUnique(txtStatusName.Text))
            {
                MessageBox.Show("Статус с таким названием уже существует!\nПожалуйста, введите другое название.",
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                txtStatusName.Focus();
                txtStatusName.SelectAll();
                return;
            }

            try
            {
                ConnectToDatabase();

                string query = "INSERT INTO order_statuses (name) VALUES (@name)";
                var cmd = new MySqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@name", txtStatusName.Text.Trim());

                int result = cmd.ExecuteNonQuery();

                if (result > 0)
                {
                    MessageBox.Show("Статус успешно добавлен!", "Успех",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    ClearInput();
                    LoadStatuses();
                }
            }
            catch (MySqlException ex)
            {
                if (ex.Number == 1062) // Duplicate entry (дополнительная защита)
                {
                    MessageBox.Show("Статус с таким названием уже существует!", "Ошибка",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    MessageBox.Show($"Ошибка добавления статуса: {ex.Message}", "Ошибка",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка добавления статуса: {ex.Message}", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                connection?.Close();
            }
        }

        private void EditStatus()
        {
            if (!currentEditingId.HasValue)
            {
                MessageBox.Show("Выберите статус для редактирования!", "Предупреждение",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(txtStatusName.Text))
            {
                MessageBox.Show("Введите новое название статуса!", "Предупреждение",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtStatusName.Focus();
                return;
            }

            // Получаем текущее название статуса для сравнения
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
            if (currentName.Equals(txtStatusName.Text.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show("Название статуса не изменилось.", "Информация",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Проверка уникальности (исключая текущий статус)
            if (!IsStatusNameUnique(txtStatusName.Text, currentEditingId.Value))
            {
                MessageBox.Show("Статус с таким названием уже существует!\nПожалуйста, введите другое название.",
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                txtStatusName.Focus();
                txtStatusName.SelectAll();
                return;
            }

            if (MessageBox.Show($"Изменить статус '{currentName}' на '{txtStatusName.Text}'?",
                "Подтверждение изменения",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                try
                {
                    ConnectToDatabase();

                    string query = "UPDATE order_statuses SET name = @name WHERE id = @id";
                    var cmd = new MySqlCommand(query, connection);
                    cmd.Parameters.AddWithValue("@name", txtStatusName.Text.Trim());
                    cmd.Parameters.AddWithValue("@id", currentEditingId.Value);

                    int result = cmd.ExecuteNonQuery();

                    if (result > 0)
                    {
                        MessageBox.Show("Статус успешно изменен!", "Успех",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                        ClearInput();
                        LoadStatuses();
                    }
                }
                catch (MySqlException ex)
                {
                    if (ex.Number == 1062) // Duplicate entry (дополнительная защита)
                    {
                        MessageBox.Show("Статус с таким названием уже существует!", "Ошибка",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    else
                    {
                        MessageBox.Show($"Ошибка изменения статуса: {ex.Message}", "Ошибка",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка изменения статуса: {ex.Message}", "Ошибка",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally
                {
                    connection?.Close();
                }
            }
        }

        private void DeleteStatus()
        {
            if (!currentEditingId.HasValue || dataGridView.SelectedRows.Count == 0)
            {
                MessageBox.Show("Выберите статус для удаления!", "Предупреждение",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var selectedRow = dataGridView.SelectedRows[0];
            int statusId = Convert.ToInt32(selectedRow.Cells["id"].Value);
            string statusName = selectedRow.Cells["name"].Value.ToString();

            // Проверяем, используется ли статус
            if (IsStatusInUse(statusId))
            {
                MessageBox.Show($"Статус '{statusName}' используется в заказах и не может быть удален!\n" +
                    "Сначала измените статус во всех заказах.",
                    "Ошибка удаления",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (MessageBox.Show($"Удалить статус '{statusName}'?",
                "Подтверждение удаления",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                try
                {
                    ConnectToDatabase();

                    string query = "DELETE FROM order_statuses WHERE id = @id";
                    var cmd = new MySqlCommand(query, connection);
                    cmd.Parameters.AddWithValue("@id", statusId);

                    int result = cmd.ExecuteNonQuery();

                    if (result > 0)
                    {
                        MessageBox.Show("Статус успешно удален!", "Успех",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                        ClearInput();
                        LoadStatuses();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка удаления статуса: {ex.Message}", "Ошибка",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally
                {
                    connection?.Close();
                }
            }
        }


        private bool IsStatusInUse(int statusId)
        {
            try
            {
                ConnectToDatabase();

                string query = "SELECT COUNT(*) FROM orders WHERE status_id = @statusId";
                var cmd = new MySqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@statusId", statusId);

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
            txtStatusName.Clear();
            txtStatusName.Focus();
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

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                connection?.Dispose();
            }
            base.Dispose(disposing);
        }
    }

}

