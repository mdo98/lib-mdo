using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using MDo.Common.App.CLI;

namespace MDo.Interop.R.Test
{
    static class Program
    {
        static void Main(string[] args)
        {
            (new CmdLineBootstrapper()).Run(args);
        }
    }
}
