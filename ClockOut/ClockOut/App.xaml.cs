using System;
using System.Windows;
using Serilog;

namespace ClockOut
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Serilog 구성
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.File("logs\\app-.log", rollingInterval: RollingInterval.Day)
                .CreateLogger();

            Log.Information("애플리케이션 시작됨.");

            try
            {
                this.InitializeComponent();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "애플리케이션 시작 중 치명적인 오류 발생.");
                throw;
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }
    }
}