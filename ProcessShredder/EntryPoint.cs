using System;
using System.Windows.Forms;

namespace ProcessShredder
{
    static class EntryPoint
    {
        /// <summary>The entry point for the application</summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new ProcessShredderGUI());
        }
    }
}
