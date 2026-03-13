using FlowerShopApp;
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
    public partial class MainFormSalesman : Form
    {
        public MainFormSalesman(int userId, string fullName, int roleId, string roleName)
        {
            InitializeComponent();
            // Ваш код инициализации для продавца
            this.Text = $"Главная форма - Продавец: {fullName}";
        }
        public MainFormSalesman()
        {
            InitializeComponent();
        }

        private void button5_Click(object sender, EventArgs e)
        {
            ProductSeller tovarForm = new ProductSeller();
            tovarForm.ShowDialog();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            FormOrderCreation ordercreation = new FormOrderCreation();
            ordercreation.ShowDialog();
        }

        private void button7_Click(object sender, EventArgs e)
        {
            FormOrdersSeller ordercreation = new FormOrdersSeller();
            ordercreation.ShowDialog();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            A loginForm = new A();
            loginForm.Show();      // показываем её
            this.Close();          // закрываем текущую главную форму
        }
    }
}
