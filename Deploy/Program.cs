using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Deploy
{
    class Program
    {
        static void Main(string[] args)
        {
            var settings = Setting.GetSettings();
            foreach (var setting in settings)
            {
                var importManager = new ImportManager(setting.User, setting.Password, setting.Url);
                importManager.Import(setting.Dist, setting.Name, null, setting.Solution, null);

                Console.WriteLine("Upload done - press [ENTER]");
                Console.ReadLine();
            }
        }
    }
}
