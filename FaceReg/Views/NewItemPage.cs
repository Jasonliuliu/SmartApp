using System;

using Xamarin.Forms;

namespace FaceReg.Views
{
    public class NewItemPage : ContentPage
    {
        public NewItemPage()
        {
            Content = new StackLayout
            {
                Children = {
                    new Label { Text = "Hello ContentPage" }
                }
            };
        }
    }
}

