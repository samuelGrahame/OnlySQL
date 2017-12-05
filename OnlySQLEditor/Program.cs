using OnlySQL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace OnlySQLEditor
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();

            Translate.Setup();

            using (frmEditor fe = new frmEditor())
            {                
                Application.Run(fe);
            }
        }
    }
}
