using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;

namespace MDo.Learning.Data
{
    [DataContract]
    internal class DataProperty
	{
        [DataMember]
		public string Name
		{
			get;
			internal set;
		}
		
        [DataMember]
		public string DataType
		{
            get
            {
                return this.Type == null ? string.Empty : this.Type.AssemblyQualifiedName;
            }
            internal set
            {
                this.Type = Type.GetType(value);
            }
		}
		
        [DataMember]
		public MemberTypes TypeBinding
		{
			get;
			internal set;
		}

		public Type Type
		{
			get;
            internal set;
		}

        public override string ToString()
        {
            return string.Format("{0} ({1})", this.Name, this.Type.FullName);
        }
	}

    [CollectionDataContract]
    internal class DataPropertyCollection : List<DataProperty>
    {
    }
}
