using System;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using ItalicPig.Bootstrap.ViewModel;

namespace ItalicPig.Bootstrap.View
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            RestorePlacement();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
            if (((ProjectCollection)DataContext).Projects.Any(project => project.IsBusy))
            {
                if (MessageBox.Show("Are you sure you want to quit while Git is busy? This can cause repository corruption.", "Quit", MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No) == MessageBoxResult.No)
                {
                    e.Cancel = true;
                    return;
                }
            }
            SavePlacement();
        }

        private void SavePlacement()
        {
            Properties.Settings.Default.WindowPlacement = WindowPlacement.GetPlacement(this);
            Properties.Settings.Default.Save();
        }

        private void RestorePlacement()
        {
            WindowPlacement.SetPlacement(this, Properties.Settings.Default.WindowPlacement);
        }

        private void CloneButton_Click(object sender, RoutedEventArgs e)
        {
            var Dialog = new CloneDialog
            {
                Owner = this,
                DataContext = new ProjectClone((ProjectCollection)DataContext)
            };
            Dialog.ShowDialog();
        }
    }
}
