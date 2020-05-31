using System.Windows;

namespace MinDistanceWpf
{
    /// <summary>
    /// Interaction logic for DistanceDialog.xaml
    /// </summary>
    public partial class DistanceDialog : Window
    {
        private double _distance;

        public DistanceDialog()
        {
            InitializeComponent();
            _distance = 0.0;
        }

        private void OkBtn_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(distanceTextBox.Text) && double.TryParse(distanceTextBox.Text, out _distance))           
                this.DialogResult = true;
        }

        public double Distance
        {
            get { return _distance; }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (_distance == 0.0)
                this.DialogResult = false;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            distanceTextBox.Focus();
        }
    }
}
