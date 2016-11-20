using System;
using System.Windows.Forms;

namespace JohnSkosnik.Imagetender
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            frmImagetender frmImagetender = new frmImagetender();
            if (args.Length >= 1)
            {
                frmImagetender.ShowImages(args[0]);
            }
            Application.Run(frmImagetender);
        }
    }
}
