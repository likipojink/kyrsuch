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
    public partial class FormUsers : Form
    {
        private string connectionString = DatabaseConfig.ConnectionString;
        private DataTable usersTable;

        private int currentUserId; // 1. Добавить поле

        public FormUsers(int currentUserId) // 2. Добавить конструктор
        {
            InitializeComponent();
            this.currentUserId = currentUserId;
        }

        public FormUsers()
        {
            InitializeComponent();
        }

        private void FormUsers_Load(object sender, EventArgs e)
        {
            LoadUsers();
        }

        private void LoadUsers()
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string sql = @"SELECT 
                         u.id AS ID, 
                         u.full_name AS 'ФИО', 
                         u.login AS 'Логин',
                         r.name AS 'Роль'
                  FROM users u
                  LEFT JOIN roles r ON u.role_id = r.id
                  ORDER BY u.full_name";

                    MySqlDataAdapter da = new MySqlDataAdapter(sql, conn);
                    usersTable = new DataTable();
                    da.Fill(usersTable);

                    dataGridView1.DataSource = usersTable;

                    // Скрываем техническое поле ID
                    if (dataGridView1.Columns.Contains("ID"))
                        dataGridView1.Columns["ID"].Visible = false;

                    // Настраиваем ширину колонок
                    dataGridView1.Columns["ФИО"].Width = 200;
                    dataGridView1.Columns["Логин"].Width = 120;
                    dataGridView1.Columns["Роль"].Width = 150;

                    // Автоматическое изменение размеров
                    dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
                    dataGridView1.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;

                    // Настраиваем отображение
                    dataGridView1.ColumnHeadersDefaultCellStyle.Font = new Font("Arial", 10, FontStyle.Bold);
                    dataGridView1.EnableHeadersVisualStyles = false;
                    dataGridView1.ColumnHeadersDefaultCellStyle.BackColor = Color.LightGray;

                    // Подсветка четных строк
                    dataGridView1.AlternatingRowsDefaultCellStyle.BackColor = Color.Lavender;

                    // Отключаем возможность редактирования
                    dataGridView1.ReadOnly = true;
                    dataGridView1.AllowUserToAddRows = false;
                    dataGridView1.AllowUserToDeleteRows = false;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при загрузке пользователей: " + ex.Message,
                               "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Кнопка обновления (если нужна)
        private void btnRefresh_Click(object sender, EventArgs e)
        {
            LoadUsers();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            FormAddUser addUserForm = new FormAddUser();
            addUserForm.ShowDialog();

            if (addUserForm.DialogResult == DialogResult.OK)
            {
                LoadUsers();
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void btnEdit_Click(object sender, EventArgs e)
        {
            if (dataGridView1.SelectedRows.Count == 0)
            {
                MessageBox.Show("Выберите пользователя для редактирования",
                               "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            DataGridViewRow selectedRow = dataGridView1.SelectedRows[0];

            // Получаем данные пользователя
            int userId = GetSelectedUserId();

            if (userId == 0)
            {
                MessageBox.Show("Не удалось получить данные пользователя",
                               "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            string fullName = selectedRow.Cells["ФИО"].Value?.ToString() ?? "";
            string login = selectedRow.Cells["Логин"].Value?.ToString() ?? "";

            // Получаем ID роли
            int roleId = 0;
            if (dataGridView1.Columns.Contains("role_id"))
            {
                object roleIdObj = selectedRow.Cells["role_id"].Value;
                if (roleIdObj != null)
                {
                    roleId = Convert.ToInt32(roleIdObj);
                }
            }
            else
            {
                // Получаем ID роли по названию
                string roleName = selectedRow.Cells["Роль"].Value?.ToString() ?? "";
                roleId = GetRoleIdByName(roleName);
            }

            // Открываем форму редактирования
            FormChangeUser editForm = new FormChangeUser(userId, fullName, login, roleId);
            editForm.ShowDialog();

            // Обновляем список пользователей после закрытия формы
            if (editForm.DialogResult == DialogResult.OK)
            {
                LoadUsers();
            }

        }
        private int GetSelectedUserId()
        {
            if (dataGridView1.SelectedRows.Count == 0)
                return 0;

            DataGridViewRow selectedRow = dataGridView1.SelectedRows[0];

            if (dataGridView1.Columns.Contains("ID"))
            {
                object idValue = selectedRow.Cells["ID"].Value;
                if (idValue != null)
                {
                    return Convert.ToInt32(idValue);
                }
            }

            return 0;
        }
        private int GetRoleIdByName(string roleName)
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string sql = "SELECT id FROM roles WHERE name = @name";
                    using (MySqlCommand cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@name", roleName);
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

       
        private void DeleteUser(int userId)
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string sql = "DELETE FROM users WHERE id = @id";

                    using (MySqlCommand cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@id", userId);
                        int rowsAffected = cmd.ExecuteNonQuery();

                        if (rowsAffected > 0)
                        {
                            MessageBox.Show("Пользователь успешно удален!", "Успех",
                                          MessageBoxButtons.OK, MessageBoxIcon.Information);
                            LoadUsers(); // Обновляем список
                        }
                        else
                        {
                            MessageBox.Show("Не удалось удалить пользователя", "Ошибка",
                                          MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
            }
            catch (MySqlException ex)
            {
                if (ex.Message.Contains("foreign key constraint"))
                {
                    MessageBox.Show("Нельзя удалить пользователя, так как с ним связаны другие записи " +
                                  "(например, заказы).\n\nСначала удалите связанные записи.",
                                  "Ошибка удаления", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    MessageBox.Show($"Ошибка базы данных: {ex.Message}",
                                  "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Произошла ошибка: {ex.Message}",
                              "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnDelete_Click_1(object sender, EventArgs e)
        {
            if (dataGridView1.SelectedRows.Count == 0)
            {
                MessageBox.Show("Выберите пользователя для удаления",
                               "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            int userId = GetSelectedUserId();

            if (userId == 0)
            {
                MessageBox.Show("Не удалось получить данные пользователя",
                               "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Проверяем, не пытается ли пользователь удалить сам себя
            if (userId == currentUserId)
            {
                MessageBox.Show("Вы не можете удалить свою собственную учетную запись!",
                              "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Получаем данные пользователя для подтверждения
            DataGridViewRow selectedRow = dataGridView1.SelectedRows[0];
            string fullName = selectedRow.Cells["ФИО"].Value?.ToString() ?? "";
            string login = selectedRow.Cells["Логин"].Value?.ToString() ?? "";
            string role = selectedRow.Cells["Роль"].Value?.ToString() ?? "";

            // Проверяем, не является ли пользователь администратором
            // (можно разрешить удалять администраторов, если нужно)
            if (role == "Администратор")
            {
                DialogResult adminConfirm = MessageBox.Show(
                    $"Вы пытаетесь удалить администратора:\n\n" +
                    $"ФИО: {fullName}\n" +
                    $"Логин: {login}\n\n" +
                    "Вы уверены, что хотите удалить администратора?",
                    "Подтверждение удаления администратора",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning,
                    MessageBoxDefaultButton.Button2);

                if (adminConfirm != DialogResult.Yes)
                {
                    return;
                }
            }

            // Запрос подтверждения
            DialogResult result = MessageBox.Show(
                $"Вы действительно хотите удалить пользователя:\n\n" +
                $"ФИО: {fullName}\n" +
                $"Логин: {login}\n" +
                $"Роль: {role}\n\n" +
                "Это действие нельзя отменить!",
                "Подтверждение удаления",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning,
                MessageBoxDefaultButton.Button2);

            if (result == DialogResult.Yes)
            {
                DeleteUser(userId);
            }
        }
    }
}



