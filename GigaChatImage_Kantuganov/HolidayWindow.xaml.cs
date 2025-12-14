using System.Collections.Generic;
using System.Windows;

namespace GigaChatImage_Kantuganov
{
    public partial class HolidayWindow : Window
    {
        public string SelectedPrompt { get; private set; }

        public HolidayWindow()
        {
            InitializeComponent();
            HolidayList.ItemsSource = new List<Holiday>
            {
                new Holiday { Name = "Новый год", Date = "1 января", Prompt = "Новогодняя ёлка" },
                new Holiday { Name = "Рождество", Date = "7 января", Prompt = "Рождественская сцена" },
                new Holiday { Name = "8 марта", Date = "8 марта", Prompt = "Весенние цветы" },
                new Holiday { Name = "9 мая", Date = "9 мая", Prompt = "Салют Победы" },
                new Holiday { Name = "Осень", Date = "Сентябрь-Ноябрь", Prompt = "Осенний лес" },
                new Holiday { Name = "Зима", Date = "Декабрь-Февраль", Prompt = "Зимний лес" },
                new Holiday { Name = "Весна", Date = "Март-Май", Prompt = "Весенние цветы" },
                new Holiday { Name = "Лето", Date = "Июнь-Август", Prompt = "Летний пляж" }
            };
        }

        private void SelectButton_Click(object sender, RoutedEventArgs e)
        {
            if (HolidayList.SelectedItem is Holiday selected)
            {
                SelectedPrompt = selected.Prompt;
                DialogResult = true;
                Close();
            }
            else
            {
                MessageBox.Show("Выберите праздник");
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }

    public class Holiday
    {
        public string Name { get; set; }
        public string Date { get; set; }
        public string Prompt { get; set; }
    }
}