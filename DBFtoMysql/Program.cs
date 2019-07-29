using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

using System.Globalization;
using System.Threading;


namespace DBFtoMysql
{
    static class Program
    {
        /// <summary>
        /// Punto de entrada principal para la aplicación.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // For test culture
            //string nameCulture = "es";
            //Thread.CurrentThread.CurrentCulture = new CultureInfo(nameCulture);
            //Thread.CurrentThread.CurrentUICulture = new CultureInfo(nameCulture);


            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new ui_Main());
        }
    }
}
