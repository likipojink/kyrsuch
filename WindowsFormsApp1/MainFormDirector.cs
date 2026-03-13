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
    public partial class MainFormDirector : Form
    {
        public MainFormDirector(int userId, string fullName, int roleId, string roleName)
        {
            InitializeComponent();
            // Ваш код инициализации для менеджера
            this.Text = $"Главная форма - Менеджер: {fullName}";
        }
        public MainFormDirector()
        {
            InitializeComponent();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            FormOrders orders = new FormOrders();
            orders.ShowDialog();
        }

        private void button5_Click(object sender, EventArgs e)
        {
            Product tovarForm = new Product();
            tovarForm.ShowDialog();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            A loginForm = new A();
            loginForm.Show();      // показываем её
            this.Close();          // закрываем текущую главную форму
        }
    }
}
