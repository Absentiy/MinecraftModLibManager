using Microsoft.Win32;
using MinecraftModLibManager;
using System;
using System.Collections.Generic;
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

namespace MinecraftModManager
{
    /// <summary>
    /// Логика взаимодействия для ModTypeSelectDialog.xaml
    /// </summary>
    public partial class ModTypeSelectDialog : Window
    {
        public ModType? SelectedType { get; set; }

        public ModTypeSelectDialog()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            SelectedType = ModType.Normal;
            DialogResult = true;
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            SelectedType = ModType.Addon;
            DialogResult = true;
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            SelectedType = ModType.Library;
            DialogResult = true;
        }

        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            SelectedType = null;
        }        
    }
}
