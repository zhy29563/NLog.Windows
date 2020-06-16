using System.Windows;

namespace TextBoxTargetTest
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        private readonly NLog.ILogger LogHelper = NLog.LogManager.GetCurrentClassLogger();
        public MainWindow()
        {
            InitializeComponent();
            
            NLog.Windows.WPF.TextBoxTarget.ReInitializeTarget(this);
        }

        private void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            
            LogHelper.Debug("Debug");
            LogHelper.Info("Info");
            LogHelper.Warn("Warn");
            LogHelper.Error("Error");
            LogHelper.Fatal("Fatal");
        }
    }
}