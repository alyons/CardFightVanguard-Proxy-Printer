using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace CFV_ProxyPrinter
{
    /// <summary>
    /// Interaction logic for AddCardWindow.xaml
    /// </summary>
    public partial class AddCardWindow : Window
    {
        public Card Card { get; set; }
        bool saveCard = false;

        public AddCardWindow()
        {
            InitializeComponent();

            Card = new Card();
        }

        private void Add_Click(object sender, RoutedEventArgs e)
        {
            saveCard = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            if (!saveCard) Card = null;

            base.OnClosing(e);
        }
    }
}
