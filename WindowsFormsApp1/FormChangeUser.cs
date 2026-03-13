using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WindowsFormsApp1
{
    public partial class FormChangeUser : Form
    {

        private string connectionString = DatabaseConfig.ConnectionString;
        private int userId;
        private string currentLogin;

        public class RoleItem
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }

        // Конструктор с параметрами для передачи данных пользователя
        public FormChangeUser(int userId, string fullName, string login, int roleId)
        {
            InitializeComponent();

            this.userId = userId;
            this.currentLogin = login;

            // Заполняем поля данными пользователя
            txtFullName.Text = fullName;
            txtLogin.Text = login;

            // Привязка событий
            btnSave.Click += btnSave_Click;
            btnReset.Click += btnReset_Click_1;
            btnBack.Click += btnBack_Click;



            // Загружаем роли и устанавливаем выбранную
            LoadRolesAndSelect(roleId);
        }

        // Стандартный конструктор (опционально)
        public FormChangeUser() : this(0, "", "", 0)
        {
        }

        private void LoadRolesAndSelect(int selectedRoleId)
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string sql = "SELECT id, name FROM roles ORDER BY name";
                    MySqlCommand cmd = new MySqlCommand(sql, conn);
                    MySqlDataReader reader = cmd.ExecuteReader();

                    cmbRole.Items.Clear();

                    while (reader.Read())
                    {
                        cmbRole.Items.Add(new RoleItem
                        {
                            Id = reader.GetInt32("id"),
                            Name = reader.GetString("name")
                        });
                    }
                    reader.Close();

                    cmbRole.DisplayMember = "Name";
                    cmbRole.ValueMember = "Id";

                    // Устанавливаем выбранную роль
                    bool roleSelected = false;
                    if (selectedRoleId > 0)
                    {
                        foreach (RoleItem item in cmbRole.Items)
                        {
                            if (item.Id == selectedRoleId)
                            {
                                cmbRole.SelectedItem = item;
                                roleSelected = true;
                                break;
                            }
                        }
                    }

                    // Если роль не найдена или не указана, выбираем первую
                    if (!roleSelected && cmbRole.Items.Count > 0)
                    {
                        cmbRole.SelectedIndex = 0;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при загрузке ролей: " + ex.Message,
                                "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Метод хэширования пароля
        private string HashPassword(string password)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                StringBuilder builder = new StringBuilder();
                foreach (byte b in bytes)
                {
                    builder.Append(b.ToString("x2"));
                }
                return builder.ToString();
            }
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            if (!ValidateInput())
                return;

            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();

                    // Проверяем, не существует ли уже такой логин (кроме текущего пользователя)
                    if (txtLogin.Text.Trim() != currentLogin)
                    {
                        string checkSql = "SELECT COUNT(*) FROM users WHERE login = @login AND id != @userId";
                        using (MySqlCommand checkCmd = new MySqlCommand(checkSql, conn))
                        {
                            checkCmd.Parameters.AddWithValue("@login", txtLogin.Text.Trim());
                            checkCmd.Parameters.AddWithValue("@userId", userId);
                            int count = Convert.ToInt32(checkCmd.ExecuteScalar());

                            if (count > 0)
                            {
                                MessageBox.Show("Пользователь с таким логином уже существует!",
                                              "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                txtLogin.Focus();
                                txtLogin.SelectAll();
                                return;
                            }
                        }
                    }

                    // Получаем ID выбранной роли
                    int roleId = 0;
                    if (cmbRole.SelectedItem != null && cmbRole.SelectedItem is RoleItem selectedRole)
                    {
                        roleId = selectedRole.Id;
                    }

                    // Определяем, обновлять ли пароль
                    bool updatePassword = !string.IsNullOrWhiteSpace(txtPassword.Text);

                    // SQL запрос для обновления
                    if (updatePassword)
                    {
                        string hashedPassword = HashPassword(txtPassword.Text);
                        string updateSql = @"UPDATE users 
                                         SET full_name = @fullName, 
                                             login = @login, 
                                             password_hash = @password, 
                                             role_id = @roleId 
                                         WHERE id = @id";

                        using (MySqlCommand updateCmd = new MySqlCommand(updateSql, conn))
                        {
                            updateCmd.Parameters.AddWithValue("@fullName", txtFullName.Text.Trim());
                            updateCmd.Parameters.AddWithValue("@login", txtLogin.Text.Trim());
                            updateCmd.Parameters.AddWithValue("@password", hashedPassword);
                            updateCmd.Parameters.AddWithValue("@roleId", roleId);
                            updateCmd.Parameters.AddWithValue("@id", userId);

                            int rowsAffected = updateCmd.ExecuteNonQuery();

                            if (rowsAffected > 0)
                            {
                                MessageBox.Show("Данные пользователя успешно обновлены!", "Успех",
                                              MessageBoxButtons.OK, MessageBoxIcon.Information);
                                this.DialogResult = DialogResult.OK;
                                this.Close();
                            }
                            else
                            {
                                MessageBox.Show("Не удалось обновить данные пользователя", "Ошибка",
                                              MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                        }
                    }
                    else
                    {
                        // Без изменения пароля
                        string updateSql = @"UPDATE users 
                                         SET full_name = @fullName, 
                                             login = @login, 
                                             role_id = @roleId 
                                         WHERE id = @id";

                        using (MySqlCommand updateCmd = new MySqlCommand(updateSql, conn))
                        {
                            updateCmd.Parameters.AddWithValue("@fullName", txtFullName.Text.Trim());
                            updateCmd.Parameters.AddWithValue("@login", txtLogin.Text.Trim());
                            updateCmd.Parameters.AddWithValue("@roleId", roleId);
                            updateCmd.Parameters.AddWithValue("@id", userId);

                            int rowsAffected = updateCmd.ExecuteNonQuery();

                            if (rowsAffected > 0)
                            {
                                MessageBox.Show("Данные пользователя успешно обновлены!", "Успех",
                                              MessageBoxButtons.OK, MessageBoxIcon.Information);
                                this.DialogResult = DialogResult.OK;
                                this.Close();
                            }
                            else
                            {
                                MessageBox.Show("Не удалось обновить данные пользователя", "Ошибка",
                                              MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                        }
                    }
                }
            }
            catch (MySqlException ex)
            {
                MessageBox.Show($"Ошибка базы данных: {ex.Message}",
                              "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Произошла ошибка: {ex.Message}",
                              "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private bool ValidateInput()
        {
            // Проверка ФИО
            if (string.IsNullOrWhiteSpace(txtFullName.Text))
            {
                MessageBox.Show("Введите ФИО пользователя", "Ошибка",
                              MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtFullName.Focus();
                return false;
            }

            if (txtFullName.Text.Trim().Length < 3)
            {
                MessageBox.Show("ФИО должно содержать не менее 3 символов", "Ошибка",
                              MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtFullName.Focus();
                txtFullName.SelectAll();
                return false;
            }

            // Проверка логина
            if (string.IsNullOrWhiteSpace(txtLogin.Text))
            {
                MessageBox.Show("Введите логин пользователя", "Ошибка",
                              MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtLogin.Focus();
                return false;
            }

            if (txtLogin.Text.Trim().Length < 3)
            {
                MessageBox.Show("Логин должен содержать не менее 3 символов", "Ошибка",
                              MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtLogin.Focus();
                txtLogin.SelectAll();
                return false;
            }

            // Проверка роли
            if (cmbRole.SelectedIndex == -1 || cmbRole.Items.Count == 0)
            {
                MessageBox.Show("Выберите роль пользователя", "Ошибка",
                              MessageBoxButtons.OK, MessageBoxIcon.Warning);
                cmbRole.Focus();
                return false;
            }

            // Проверка пароля (если введен)
            if (!string.IsNullOrWhiteSpace(txtPassword.Text) && txtPassword.Text.Length < 4)
            {
                MessageBox.Show("Пароль должен содержать не менее 4 символов", "Ошибка",
                              MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtPassword.Focus();
                txtPassword.SelectAll();
                return false;
            }

            return true;
        }



        private void btnBack_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }



        // Метод для установки данных (если нужен для вызова без конструктора)
        public void SetUserData(int userId, string fullName, string login, int roleId)
        {
            this.userId = userId;
            this.currentLogin = login;
            txtFullName.Text = fullName;
            txtLogin.Text = login;
            LoadRolesAndSelect(roleId);
        }

        private void btnReset_Click_1(object sender, EventArgs e)
        {
            txtLogin.Clear();
            txtFullName.Clear();
            txtPassword.Clear();
            txtPassword.Focus();
        }

        private void txtFullName_KeyPress(object sender, KeyPressEventArgs e)
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
