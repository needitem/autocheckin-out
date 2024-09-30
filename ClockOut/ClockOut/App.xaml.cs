using System;
using System.Windows;

namespace ClockOut
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            // Serilog 구성
            try
            {
                this.InitializeComponent();
            }
            catch (Exception ex)
            {
                throw;
            }
            finally
            {

            }
        }
    }
}