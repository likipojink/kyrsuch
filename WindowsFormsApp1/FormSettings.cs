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
    public partial class FormSettings : Form
    {
        public FormSettings()
        {
            InitializeComponent();
            LoadCurrentSettings();
            // Загружаем текущие настройки
           
        }
        private void LoadCurrentSettings()
        {
            txtServer.Text = DatabaseConfig.Server;
            txtDatabase.Text = DatabaseConfig.Database;
            txtUserId.Text = DatabaseConfig.UserId;
            txtPassword.Text = DatabaseConfig.Password;
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            // Проверка заполнения
            if (string.IsNullOrWhiteSpace(txtServer.Text) ||
                string.IsNullOrWhiteSpace(txtDatabase.Text) ||
                string.IsNullOrWhiteSpace(txtUserId.Text))
            {
                MessageBox.Show("Заполните все поля (пароль может быть пустым).",
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Сохраняем настройки
            DatabaseConfig.Save(
                txtServer.Text.Trim(),
                txtDatabase.Text.Trim(),
                txtUserId.Text.Trim(),
                txtPassword.Text.Trim()
            );

            MessageBox.Show("Настройки сохранены. Приложение будет перезапущено.",
                "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);

            // Перезапускаем приложение, чтобы новые настройки применились во всех формах
            Application.Restart();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();

        }
    }
}
