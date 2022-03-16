using System.Windows;

namespace ItalicPig.Bootstrap.View
{
    public partial class CloneDialog : Window
    {
        public CloneDialog()
        {
            InitializeComponent();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
