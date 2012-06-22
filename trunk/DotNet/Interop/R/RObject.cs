using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MDo.Interop.R
{
    public abstract class RObject : IDisposable
    {
        #region Constructors

        protected RObject(IntPtr ptr, string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentNullException("name");
            this.Name = name;

            this.SetPtr(ptr);
        }

        #endregion Constructors


        #region Properties

        public string Name  { get; private set; }
        public IntPtr Ptr   { get; private set; }

        #endregion Properties


        #region Unmanaged Resource Management

        protected abstract void OnPtrSet();

        protected void SetPtr(IntPtr ptr)
        {
            this.Ptr = ptr;
            if (IntPtr.Zero != this.Ptr)
                RInterop.SetVariable(this.Name, this.Ptr);
            this.OnPtrSet();
        }

        public void Dispose()
        {
            RInterop.ClearVariable(this.Name);
            GC.SuppressFinalize(this);
        }

        ~RObject()
        {
            RInterop.ClearVariable(this.Name);
        }

        #endregion Unmanaged Resource Management


        #region Static Methods

        protected static string AutoName<T>() where T : RObject
        {
            return string.Format("{0}_{1}", typeof(T).Name, Guid.NewGuid().ToString("N"));
        }

        #endregion Static Methods
    }
}
