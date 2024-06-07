﻿using prbd_2324_a03.Model;
using prbd_2324_a03.ViewModel;
using PRBD_Framework;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace prbd_2324_a03.View
{
    /// <summary>
    /// Interaction logic for AddOperationView.xaml
    /// </summary>
    public partial class AddOperationView : DialogWindowBase
    {
        private readonly AddOperationViewModel _vm;
        public AddOperationView(Tricounts tricounts, Operations operations, bool IsNew) {
            InitializeComponent();
            DataContext = _vm = new AddOperationViewModel(tricounts, operations, IsNew);
        }

        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e) {
            foreach (char c in e.Text) {
                if (!char.IsDigit(c) && c != '.' && c != ',') // Permet également les décimales
                {
                    e.Handled = true;
                    return;
                }
            }
        }


    }
}
