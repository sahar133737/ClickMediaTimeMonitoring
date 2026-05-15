using System;
using System.Threading;
using System.Windows.Forms;
using ClickMediaWorkTime.Forms;
using ClickMediaWorkTime.Services;

namespace ClickMediaWorkTime
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += Application_ThreadException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            try
            {
                DatabaseBootstrapper.EnsureDatabase();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Ошибка инициализации БД: " + ex.Message + Environment.NewLine +
                    "Проверьте строку подключения в App.config и доступность SQL Server.",
                    "Запуск",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }

            using (var login = new LoginForm())
            {
                if (login.ShowDialog() != DialogResult.OK)
                {
                    return;
                }
            }

            RolePermissionService.LoadCurrentRolePermissions();
            Application.Run(new MainMenuForm());
        }

        private static void Application_ThreadException(object sender, ThreadExceptionEventArgs e)
        {
            try
            {
                ErrorLogService.LogUiException("Application.ThreadException", e.Exception);
            }
            catch
            {
                // ignore
            }

            MessageBox.Show(
                "Необработанная ошибка интерфейса: " + e.Exception.Message,
                "Ошибка",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            try
            {
                var ex = e.ExceptionObject as Exception;
                if (ex != null)
                {
                    ErrorLogService.LogUiException("AppDomain.UnhandledException", ex);
                }
            }
            catch
            {
                // ignore
            }
        }
    }
}
