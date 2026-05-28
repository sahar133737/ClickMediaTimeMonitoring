using System;
using System.Configuration;
using System.Threading;
using System.Windows.Forms;
using ClickMediaWorkTime.Forms;
using ClickMediaWorkTime.Infrastructure;
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
                var configPath = AppDomain.CurrentDomain.SetupInformation.ConfigurationFile;
                var server = "(сервер не определён)";
                try
                {
                    server = ConnectionStringHelper.DescribeServer(
                        ConfigurationManager.ConnectionStrings["MasterConnection"]?.ConnectionString ?? string.Empty);
                }
                catch
                {
                    // ignore
                }

                var detail = ex.InnerException != null ? ex.InnerException.Message : string.Empty;
                MessageBox.Show(
                    ex.Message + Environment.NewLine + Environment.NewLine +
                    (string.IsNullOrWhiteSpace(detail) ? string.Empty : detail + Environment.NewLine + Environment.NewLine) +
                    "Сервер из конфигурации: " + server + Environment.NewLine +
                    "Файл настроек: " + configPath + Environment.NewLine + Environment.NewLine +
                    "На другом ПК откройте ClickMediaWorkTime.exe.config и укажите свой Server " +
                    "(например (localdb)\\MSSQLLocalDB или .\\SQLEXPRESS). " +
                    "Папка Sql с скриптами должна лежать рядом с .exe.",
                    "Ошибка базы данных",
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
