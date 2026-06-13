using System;

namespace LumyxCheatsV2
{
    public static class Program
    {
        [STAThread]
        public static void Main()
        {
            // Tworzymy silnik aplikacji WPF bezpośrednio w kodzie
            var app = new System.Windows.Application();

            // Tworzymy i uruchamiamy Twoje fioletowe menu
            var glowneOkno = new MainWindow();

            app.Run(glowneOkno);
        }
    }
}
