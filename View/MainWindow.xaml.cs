using System;
using System.ComponentModel;
using System.Windows;

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
    }
}
