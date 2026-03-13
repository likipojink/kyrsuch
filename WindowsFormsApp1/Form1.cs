using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using MySql.Data.MySqlClient;
using System.Security.Cryptography;
using WindowsFormsApp1;


namespace WindowsFormsApp1
{




    public partial class A : Form
    {
        private string connectionString = DatabaseConfig.ConnectionString;

        public A()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Login(txtLogin.Text, txtPassword.Text);
        }

        private void Login(string login, string password)
        {
            if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Введите логин и пароль", "Ошибка",
                              MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();

                    // Получаем пользователя и название его роли
                    string query = @"SELECT u.id, u.full_name, u.role_id, u.password_hash, r.name as role_name
                                   FROM users u 
                                   INNER JOIN roles r ON u.role_id = r.id
                                   WHERE u.login = @Login AND u.password_hash = @Password";

                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Login", login);
                        command.Parameters.AddWithValue("@Password", password);

                        using (MySqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                // Успешная авторизация
                                int userId = Convert.ToInt32(reader["id"]);
                                string fullName = reader["full_name"].ToString();
                                int roleId = Convert.ToInt32(reader["role_id"]);
                                string roleName = reader["role_name"].ToString();

                                MessageBox.Show($"Добро пожаловать, {fullName}!\nРоль: {roleName}", "Успех",
                                              MessageBoxButtons.OK, MessageBoxIcon.Information);

                                // Открываем соответствующую форму в зависимости от роли
                                OpenRoleBasedForm(userId, fullName, roleId, roleName);
                            }
                            else
                            {
                                MessageBox.Show("Неверный логин или пароль", "Ошибка авторизации",
                                              MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                        }
                    }
                }
            }
            catch (MySqlException ex)
            {
                MessageBox.Show($"Ошибка подключения к базе данных: {ex.Message}", "Ошибка",
                              MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Произошла ошибка: {ex.Message}", "Ошибка",
                              MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OpenRoleBasedForm(int userId, string fullName, int roleId, string roleName)
        {
            Form roleForm = null;

            // Выбираем форму в зависимости от роли
            switch (roleName.ToLower())
            {
                case "администратор":
                    roleForm = new MainForm(userId, fullName, roleId, roleName);
                    break;
                case "менеджер":
                    roleForm = new MainFormDirector(userId, fullName, roleId, roleName);
                    break;
                case "продавец":
                    roleForm = new MainFormSalesman(userId, fullName, roleId, roleName);
                    break;
                default:
                    MessageBox.Show($"Неизвестная роль: {roleName}", "Ошибка",
                                  MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
            }

            if (roleForm != null)
            {
                roleForm.Show();
                this.Hide(); // Скрываем форму авторизации
            }
        }

        private void A_Load(object sender, EventArgs e)
        {
            txtLogin.Focus();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void txtPassword_KeyPress_1(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Enter)
            {
                Login(txtLogin.Text, txtPassword.Text);
                e.Handled = true;
            }
        }

        private void txtLogin_KeyPress(object sender, KeyPressEventArgs e)
        {
            
            
        }

        private void btnSettings_Click(object sender, EventArgs e)
        {
            using (var FormSettings = new FormSettings())
            {
                FormSettings.ShowDialog();
            }
        }
    }
}