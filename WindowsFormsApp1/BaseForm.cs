using System;
using System.Drawing;
using System.Windows.Forms;

namespace WindowsFormsApp1
{
    public class BaseForm : Form
    {
        private Timer activityTimer;
        private DateTime lastActivityTime;
        private int timeoutSeconds;      // время бездействия в секундах
        private bool enabled;            // включена ли блокировка
        private bool isLocked = false;   // заблокирована ли форма
        private Form notificationForm = null; // окно предупреждения

        public BaseForm()
        {
            // Значения по умолчанию (можно изменить или читать из настроек)
            timeoutSeconds = 6000;
            enabled = true;

            // Центрируем форму
            this.StartPosition = FormStartPosition.CenterScreen;

            if (enabled)
            {
                activityTimer = new Timer();
                activityTimer.Interval = 1000; // проверка каждую секунду
                activityTimer.Tick += CheckInactivity;

                // Подписываемся на события активности
                this.MouseMove += (s, e) => ResetTimer();
                this.KeyPress += (s, e) => ResetTimer();
                this.MouseClick += (s, e) => ResetTimer();
                this.KeyDown += (s, e) => ResetTimer();

                this.Load += (s, e) =>
                {
                    lastActivityTime = DateTime.Now;
                    activityTimer.Start();
                };

                this.FormClosed += (s, e) =>
                {
                    activityTimer?.Stop();
                    activityTimer?.Dispose();
                    CloseNotification();
                };
            }
        }

        // Сброс таймера при активности
        private void ResetTimer()
        {
            lastActivityTime = DateTime.Now;
            CloseNotification();
        }

        // Закрыть окно предупреждения
        private void CloseNotification()
        {
            if (notificationForm != null && !notificationForm.IsDisposed)
            {
                try
                {
                    notificationForm.Close();
                    notificationForm.Dispose();
                }
                catch { }
                notificationForm = null;
            }
        }

        // Проверка бездействия
        private void CheckInactivity(object sender, EventArgs e)
        {
            if (!enabled || isLocked) return;

            TimeSpan inactiveTime = DateTime.Now - lastActivityTime;

            // Если осталось 5 секунд до блокировки и предупреждение ещё не показано
            if (inactiveTime.TotalSeconds >= timeoutSeconds - 5 &&
                inactiveTime.TotalSeconds < timeoutSeconds &&
                notificationForm == null)
            {
                ShowWarning();
            }

            // Если время вышло – блокируем
            if (inactiveTime.TotalSeconds >= timeoutSeconds)
            {
                this.Invoke(new Action(() => {
                    ShowLoginForm();
                }));
            }
        }

        // Показать предупреждение
        private void ShowWarning()
        {
            if (notificationForm != null) return;

            notificationForm = new Form
            {
                Text = "Предупреждение",
                Size = new Size(300, 150),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                TopMost = true,
                ControlBox = false
            };

            Label lblMessage = new Label
            {
                Text = "⚠ Внимание!\n\nЧерез 5 секунд произойдет автоматический выход\nиз-за длительного бездействия.",
                TextAlign = ContentAlignment.MiddleCenter,
                Size = new Size(280, 80),
                Location = new Point(10, 10)
            };

            Button btnOk = new Button
            {
                Text = "Продолжить работу",
                Size = new Size(150, 30),
                Location = new Point(75, 80)
            };
            btnOk.Click += (s, args) => {
                lastActivityTime = DateTime.Now;
                CloseNotification();
            };

            notificationForm.Controls.Add(lblMessage);
            notificationForm.Controls.Add(btnOk);
            notificationForm.Show();
        }

        // Показать форму входа (блокировка)
        protected virtual void ShowLoginForm()
        {
            activityTimer.Stop();
            isLocked = true;
            CloseNotification();

            MessageBox.Show(
                "Вы были возвращены на экран регистрации из-за длительного бездействия.",
                "Информация",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            using (var loginForm = new A())   // используем форму авторизации A
            {
                bool wasVisible = this.Visible;
                this.Hide();

                if (loginForm.ShowDialog() == DialogResult.OK)
                {
                    isLocked = false;
                    lastActivityTime = DateTime.Now;

                    if (wasVisible)
                    {
                        this.Show();
                        MessageBox.Show("Добро пожаловать обратно!", "Успешно",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }

                    activityTimer.Start();
                }
                else
                {
                    Application.Exit();
                }
            }
        }

        // Свойства для управления извне
        public void UpdateInactivityTimeout(int newTimeout)
        {
            timeoutSeconds = newTimeout;
        }

        public bool IsLocked => isLocked;

        // Необходимо для дизайнера (можно оставить пустым)
        private void InitializeComponent()
        {
            this.SuspendLayout();
            // 
            // BaseForm
            // 
            this.ClientSize = new System.Drawing.Size(284, 261);
            this.Name = "BaseForm";
            this.ResumeLayout(false);
        }
    }
}