using CSScriptLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OnlySQL
{
    class Program
    {
        static void Main(string[] args)
        {
            Translate.Setup();

            if(args != null && args.Length > 0)
            {
                if(System.IO.File.Exists(args[0]))
                {
                    var x = new Translate();
                    x.Run( 
                        System.IO.File.ReadAllText(args[0]));
                }                
            }
            else
            {
                //var x = new Translate();
                //x.Run(Properties.Resources.demoSource);
            }            
        }
    }
}
