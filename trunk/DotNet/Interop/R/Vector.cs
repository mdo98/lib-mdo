using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MDo.Interop.R.Core
{
    public class Vector
    {
        public string   Name;
        public object[] Values;

        public Vector(string name, object[] values)
        {
            this.Name = name;
            this.Values = values;
        }
    }
}
