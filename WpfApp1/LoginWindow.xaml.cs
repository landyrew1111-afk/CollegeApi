using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace WpfApp1
{
    public partial class LoginWindow : Window
    {
        public LoginWindow() => InitializeComponent();

        private void PbPass_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) BtnLogin_Click(sender, e);
        }

        private async void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            TbError.Text = "";
            if (string.IsNullOrWhiteSpace(TbLogin.Text) || string.IsNullOrWhiteSpace(PbPass.Password))
            {
                TbError.Text = "Введите логин и пароль";
                return;
            }

            BtnLogin.IsEnabled = false;
            try
            {
                if (!await Api.EnsureAvailableAsync())
                {
                    TbError.Text = "Сервер недоступен. Запустите проект CollegeApi (F5 в Visual Studio).";
                    return;
                }

                var result = await Api.PostAsync<LoginResponse>("/Auth/login", new
                {
                    login = TbLogin.Text.Trim(),
                    parol = PbPass.Password
                });

                var main = new MainWindow(result);
                main.Show();
                Close();
            }
            catch (ApiException ex)
            {
                TbError.Text = ex.Message;
            }
            catch (Exception ex)
            {
                TbError.Text = "Не удалось подключиться к серверу: " + ex.Message;
            }
            finally
            {
                BtnLogin.IsEnabled = true;
            }
        }
    }
}
