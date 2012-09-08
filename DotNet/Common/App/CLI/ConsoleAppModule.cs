using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Reflection;

namespace System
{
    public interface IConsoleAppModule
    {
        string Name { get; }
        int Run(string[] args);
        void PrintUsage();
    }

    public abstract class ConsoleAppModule : IConsoleAppModule
    {
        public virtual string Name
        {
            get { return this.GetType().Name; }
        }

        public abstract int Run(string[] args);

        public void PrintUsage()
        {
            Console.WriteLine(this.Usage);
        }

        public virtual string Usage
        {
            get { return string.Format("{0}: No cmdline parameters required or accepted.", this.Name); }
        }
    }
}
