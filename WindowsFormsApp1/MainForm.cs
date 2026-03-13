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
    public partial class MainForm : BaseForm
    {
        private int currentUserId;
        public MainForm(int userId, string fullName, int roleId, string roleName)
        {
            InitializeComponent();

            this.currentUserId = userId;

            // Ваш код инициализации для администратора
            this.Text = $"Главная форма - Администратор: {fullName}";

            
        }

        private void button1_Click(object sender, EventArgs e)
        {
            FormUsers users = new FormUsers(currentUserId);
            users.ShowDialog();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            FormDirectoryMenu directoryMenu = new FormDirectoryMenu();
            directoryMenu.ShowDialog();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            A loginForm = new A();
            loginForm.Show();      // показываем её
            this.Close();          // закрываем текущую главную форму
        }
    }
}