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
    public partial class FormAddUser : Form
    {
        private string connectionString = DatabaseConfig.ConnectionString;
        public FormAddUser()
        {

            InitializeComponent();
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
                        cmbRole.Items.Add(new
                        {
                            Id = reader.GetInt32("id"),
                            Name = reader.GetString("name")
                        });
                    }
                    reader.Close();

                    cmbRole.DisplayMember = "Name";
                    cmbRole.ValueMember = "Id";

                    if (cmbRole.Items.Count > 0)
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

            // Проверка пароля
            if (string.IsNullOrWhiteSpace(txtPassword.Text))
            {
                MessageBox.Show("Введите пароль пользователя", "Ошибка",
                              MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtPassword.Focus();
                return false;
            }

            if (txtPassword.Text.Length < 4)
            {
                MessageBox.Show("Пароль должен содержать не менее 4 символов", "Ошибка",
                              MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtPassword.Focus();
                txtPassword.SelectAll();
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

            return true;
        }



        // Автоматическая генерация логина из ФИО (опционально)


        private void button3_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void buttonRresset_Click(object sender, EventArgs e)
        {
            txtFullName.Clear();
            txtLogin.Clear();
            txtPassword.Clear();

            if (cmbRole.Items.Count > 0)
                cmbRole.SelectedIndex = 0;

            txtFullName.Focus();
        }

        private void buttonSave_Click(object sender, EventArgs e)
        {
            if (!ValidateInput())
                return;

            try
            {
                string hashedPassword = HashPassword(txtPassword.Text);

                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();

                    // Проверяем, не существует ли уже такой логин
                    string checkSql = "SELECT COUNT(*) FROM users WHERE login = @login";
                    using (MySqlCommand checkCmd = new MySqlCommand(checkSql, conn))
                    {
                        checkCmd.Parameters.AddWithValue("@login", txtLogin.Text.Trim());
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

                    // Получаем ID выбранной роли
                    int roleId = 0;
                    if (cmbRole.SelectedItem != null)
                    {
                        dynamic selectedItem = cmbRole.SelectedItem;
                        roleId = selectedItem.Id;
                    }

                    // Добавляем пользователя
                    string insertSql = @"INSERT INTO users 
                                       (full_name, login, password_hash, role_id) 
                                       VALUES (@fullName, @login, @password, @roleId)";

                    using (MySqlCommand insertCmd = new MySqlCommand(insertSql, conn))
                    {
                        insertCmd.Parameters.AddWithValue("@fullName", txtFullName.Text.Trim());
                        insertCmd.Parameters.AddWithValue("@login", txtLogin.Text.Trim());
                        insertCmd.Parameters.AddWithValue("@password", hashedPassword);
                        insertCmd.Parameters.AddWithValue("@roleId", roleId);

                        int rowsAffected = insertCmd.ExecuteNonQuery();

                        if (rowsAffected > 0)
                        {
                            MessageBox.Show("Пользователь успешно добавлен!", "Успех",
                                          MessageBoxButtons.OK, MessageBoxIcon.Information);
                            txtFullName.Clear();
                            txtLogin.Clear();
                            txtPassword.Clear();

                            if (cmbRole.Items.Count > 0)
                                cmbRole.SelectedIndex = 0;

                            txtFullName.Focus();
                        }
                        else
                        {
                            MessageBox.Show("Не удалось добавить пользователя", "Ошибка",
                                          MessageBoxButtons.OK, MessageBoxIcon.Error);
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
