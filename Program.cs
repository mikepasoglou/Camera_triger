using System;

using System.Windows.Forms;
using System.Threading;

namespace Camera_triger
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {

            bool newinstance = true;
            using (Mutex mymutex = new Mutex(true, "Camera_triger", out newinstance))
            
            if (newinstance)
            {

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new Form1());
            }
            else
                MessageBox.Show("Η εφαρμογή ήδη εκτελείται");

        }
    }
}
