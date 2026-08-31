using System;
using System.Windows;

namespace ModbusFluentTest
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        protected override void OnClosed(EventArgs e)
        {
            Server?.Shutdown();
            Client?.Shutdown();
            base.OnClosed(e);
        }
    }
}
