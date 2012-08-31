using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace System
{
    public class Sandbox : MarshalByRefObject
    {
        private static readonly object SyncRoot = new object();
        private static readonly IDictionary<string, AppDomain> ActiveDomains = new Dictionary<string, AppDomain>();

        public string Name { get; private set; }

        private Sandbox(string name)
        {
            this.Name = name;
        }

        internal bool LoadFrom(string assemblyFile)
        {
            try
            {
                Assembly.LoadFrom(assemblyFile);
                return true;
            }
            catch
            {
                return false;
            }
        }

        internal SerializableObject DoCallback(SerializableDelegate<Func<object, object>> callback, SerializableObject state)
        {
            try
            {
                if (null == callback || null == callback.Action)
                    throw new ArgumentNullException("callback");

                var returnValue = callback.Action(state.BaseObject);
                return (null == returnValue ? SerializableObject.NullVoid : new SerializableObject(returnValue.GetType(), returnValue));
            }
            catch (Exception ex)
            {
                throw new SerializableException(ex);
            }
        }

        public static object ExecuteInSandbox(string friendlyName, Func<object, object> action, object state)
        {
            Sandbox newAppDomain = Sandbox.Create(friendlyName);
            try
            {
                SerializableObject serializableState = (null == state ? SerializableObject.NullVoid : new SerializableObject(state.GetType(), state));
                SerializableObject obj = newAppDomain.DoCallback(new SerializableDelegate<Func<object, object>>(action), serializableState);
                return obj.BaseObject;
            }
            catch (SerializableException ex)
            {
                throw ex.Exception;
            }
            finally
            {
                Sandbox.Dispose(newAppDomain);
            }
        }

        internal static Sandbox Create(string friendlyName)
        {
            if (string.IsNullOrWhiteSpace(friendlyName))
                throw new ArgumentNullException("friendlyName");

            string name = string.Format("{0}_{1}", friendlyName.Trim(), Guid.NewGuid().ToString("N"));
            AppDomain newDomain = AppDomain.CreateDomain(name, null, AppDomain.CurrentDomain.SetupInformation);

            Sandbox sandbox;
            try
            {
                Type thisType = typeof(Sandbox);
                sandbox = newDomain.CreateInstanceFromAndUnwrap(thisType.Assembly.Location, thisType.FullName,
                    false, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                    null, new object[] { name }, null, null) as Sandbox;

                foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    string asmPath = asm.Location;
                    if (!string.IsNullOrWhiteSpace(asmPath))
                        sandbox.LoadFrom(asmPath);
                }

                lock (SyncRoot)
                {
                    ActiveDomains.Add(name, newDomain);
                }
            }
            catch
            {
                AppDomain.Unload(newDomain);
                throw;
            }
            return sandbox;
        }

        internal static void Dispose(Sandbox sandbox)
        {
            if (null == sandbox)
                throw new ArgumentNullException("sandbox");

            string name = sandbox.Name;

            AppDomain appDomain = null;
            lock (SyncRoot)
            {
                if (ActiveDomains.ContainsKey(name))
                {
                    appDomain = ActiveDomains[name];
                    ActiveDomains.Remove(name);
                }
            }

            if (null != appDomain)
                AppDomain.Unload(appDomain);
        }

        #region MarshalByRefObject

        public override object InitializeLifetimeService()
        {
            return null;
        }

        #endregion MarshalByRefObject
    }
}
