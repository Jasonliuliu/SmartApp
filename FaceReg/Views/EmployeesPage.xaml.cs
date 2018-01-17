using System;
using System.Collections.Generic;

using Xamarin.Forms;

namespace FaceReg
{
    public partial class EmployeesPage : ContentPage
    {
        public EmployeesPage()
        {
            InitializeComponent();


            BindingContext = new ViewModels.EmployeesViewModel();
        }
    }
}
