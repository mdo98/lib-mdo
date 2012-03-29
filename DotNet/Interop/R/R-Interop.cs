using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Reflection;
using System.IO;

namespace MDo.Interop.R
{
    public static class RInterop
    {
#if X86
        private const string R_HOME = @"lib\R";
        private const string R_DLL = @"bin\i386\R.dll";
        private const string RGraphApp_DLL = @"bin\i386\Rgraphapp.dll";
#else
        private const string R_HOME = @"lib\R";
        private const string R_DLL = @"bin\x64\R.dll";
        private const string RGraphApp_DLL = @"bin\x64\Rgraphapp.dll";
#endif

        
        #region Interop: R

        [StructLayout(LayoutKind.Sequential)]
        private struct RStartStruct
        {
            public RBool R_Quiet;
            public RBool R_Slave;
            public RBool R_Interactive;
            public RBool R_Verbose;
            public RBool LoadSiteFile;
            public RBool LoadInitFile;
            public RBool DebugInitFile;
            public RSaType RestoreAction;
            public RSaType SaveAction;
#if X86
            public uint vsize;
            public uint nsize;
            public uint max_vsize;
            public uint max_nsize;
            public uint ppsize;
#else
            public ulong vsize;
            public ulong nsize;
            public ulong max_vsize;
            public ulong max_nsize;
            public ulong ppsize;
#endif
            public int NoRenviron;
            [MarshalAs(UnmanagedType.LPStr)]
            public string rhome;
            [MarshalAs(UnmanagedType.LPStr)]
            public string home;
            [MarshalAs(UnmanagedType.FunctionPtr)]
            public R_ReadConsole ReadConsole;
            [MarshalAs(UnmanagedType.FunctionPtr)]
            public R_WriteConsole WriteConsole;
            [MarshalAs(UnmanagedType.FunctionPtr)]
            public R_CallBack CallBack;
            [MarshalAs(UnmanagedType.FunctionPtr)]
            public R_ShowMessage ShowMessage;
            [MarshalAs(UnmanagedType.FunctionPtr)]
            public R_YesNoCancel YesNoCancel;
            [MarshalAs(UnmanagedType.FunctionPtr)]
            public R_Busy Busy;
            [MarshalAs(UnmanagedType.I4)]
            public RUIMode CharacterMode;
            [MarshalAs(UnmanagedType.FunctionPtr)]
            public R_WriteConsoleEx WriteConsoleEx;
        };

        private delegate int R_ReadConsole(
            [MarshalAs(UnmanagedType.LPStr)]string prompt,
            IntPtr buf,
            int len,
            int addtohistory);

        private delegate void R_WriteConsole(
            [MarshalAs(UnmanagedType.LPStr,SizeParamIndex = 1)] string buf,
            int len);

        private delegate void R_WriteConsoleEx(
            [MarshalAs(UnmanagedType.LPStr,SizeParamIndex = 1)] string buf,
            int len,
            int otype);

        private delegate void R_CallBack();
        
        private delegate void R_ShowMessage(string msg);
        
        [return: MarshalAs(UnmanagedType.I4)]
        private delegate RYesNoCancel R_YesNoCancel(string msg);
        
        private delegate void R_Busy(int which);

        private enum RSaType
        {
            SA_NORESTORE,   /* = 0 */
            SA_RESTORE,
            SA_DEFAULT,     /* was == SA_RESTORE */
            SA_NOSAVE,
            SA_SAVE,
            SA_SAVEASK,
            SA_SUICIDE,
        };

        private enum RBool
        {
            False,
            True,
        };

        private enum RYesNoCancel
        {
            Yes     =  1,
            No      = -1,
            Cancel  =  0,
        };

        private enum RUIMode
        {
            RGui,
            RTerm,
            LinkDLL
        };

        private enum RParseStatus
        {
            PARSE_NULL,
            PARSE_OK,
            PARSE_INCOMPLETE,
            PARSE_ERROR,
            PARSE_EOF,
        };

        [StructLayout(LayoutKind.Sequential)]
        private struct RSEXPREC
        {
            // Header
            public uint SXPInfo;
            public IntPtr attrib;
            public IntPtr gengc_next_node;
            public IntPtr gengc_prev_node;
        }

        private enum RSEXPTYPE
        {
            NILSXP      = 0,	/* nil = NULL */
            SYMSXP      = 1,	/* symbols */
            LISTSXP     = 2,	/* lists of dotted pairs */
            CLOSXP      = 3,	/* closures */
            ENVSXP      = 4,	/* environments */
            PROMSXP     = 5,	/* promises: [un]evaluated closure arguments */
            LANGSXP     = 6,	/* language constructs (special lists) */
            SPECIALSXP  = 7,	/* special forms */
            BUILTINSXP  = 8,	/* builtin non-special forms */
            CHARSXP     = 9,	/* "scalar" string type (internal only)*/
            LGLSXP      = 10,	/* logical vectors */
            INTSXP      = 13,	/* integer vectors */
            REALSXP     = 14,	/* real variables */
            CPLXSXP     = 15,	/* complex variables */
            STRSXP      = 16,	/* string vectors */
            DOTSXP      = 17,	/* dot-dot-dot object */
            ANYSXP      = 18,	/* make "any" args work */
            VECSXP      = 19,	/* generic vectors */
            EXPRSXP     = 20,	/* expressions vectors */
            BCODESXP    = 21,	/* byte code */
            EXTPTRSXP   = 22,	/* external pointer */
            WEAKREFSXP  = 23,	/* weak reference */
            RAWSXP      = 24,	/* raw bytes */
            S4SXP       = 25,	/* S4 non-vector */

            NEWSXP      = 30,   /* fresh node creaed in new page */
            FREESXP     = 31,   /* node released by GC */

            FUNSXP      = 99	/* Closure or Builtin */
        };

        #region DLL Imports

        // DLL Information
        
        [DllImport(R_HOME + "\\" + R_DLL, CharSet = CharSet.Ansi, SetLastError = true)]
        private static extern IntPtr getDLLVersion();
        
        [DllImport(R_HOME + "\\" + R_DLL, CharSet = CharSet.Ansi, SetLastError = true)]
        private static extern IntPtr getRUser();

        
        // R Initialization & Cleanup

        [DllImport(R_HOME + "\\" + R_DLL, CharSet = CharSet.Ansi, SetLastError = true)]
        private static extern void R_setStartTime();

        [DllImport(R_HOME + "\\" + R_DLL, CharSet = CharSet.Ansi, SetLastError = true)]
        private static extern void R_DefParams(ref RStartStruct startInfo);
        
        [DllImport(R_HOME + "\\" + R_DLL, CharSet = CharSet.Ansi, SetLastError = true)]
        private static extern void R_SetParams(IntPtr startInfoPtr);
        
        [DllImport(R_HOME + "\\" + R_DLL, CharSet = CharSet.Ansi, SetLastError = true)]
        private static extern void R_set_command_line_arguments(int argc, string[] args);
        
        [DllImport(R_HOME + "\\" + RGraphApp_DLL, CharSet = CharSet.Ansi, SetLastError = true)]
        private static extern int GA_initapp(int argc, string[] args);
        
        [DllImport(R_HOME + "\\" + R_DLL, CharSet = CharSet.Ansi, SetLastError = true)]
        static extern void readconsolecfg();
        
        [DllImport(R_HOME + "\\" + R_DLL, CharSet = CharSet.Ansi, SetLastError = true)]
        private static extern void setup_Rmainloop();
        
        [DllImport(R_HOME + "\\" + R_DLL, CharSet = CharSet.Ansi, SetLastError = true)]
        private static extern void R_ReplDLLinit();

        [DllImport(R_HOME + "\\" + R_DLL, CharSet = CharSet.Ansi, SetLastError = true)]
        private static extern void Rf_endEmbeddedR(int fatal);


        // R Parsing & Expression Management

        [DllImport(R_HOME + "\\" + R_DLL, CharSet = CharSet.Ansi, SetLastError = true)]
        private static extern IntPtr Rf_mkString([MarshalAs(UnmanagedType.LPStr)] string str);

        [DllImport(R_HOME + "\\" + R_DLL, CharSet = CharSet.Ansi, SetLastError = true)]
        private static extern IntPtr Rf_protect(IntPtr ptr);

        [DllImport(R_HOME + "\\" + R_DLL, CharSet = CharSet.Ansi, SetLastError = true)]
        private static extern void Rf_unprotect_ptr(IntPtr ptr);

        [DllImport(R_HOME + "\\" + R_DLL, CharSet = CharSet.Ansi, SetLastError = true)]
        private static extern IntPtr R_ParseVector(
            IntPtr text,
            int n,
            [MarshalAs(UnmanagedType.I4)] ref RParseStatus status,
            IntPtr srcfile);

        [DllImport(R_HOME + "\\" + R_DLL, CharSet = CharSet.Ansi, SetLastError = true)]
        private static extern IntPtr R_tryEval(IntPtr expr, IntPtr env, ref int errorOccurred);
        
        #endregion DLL Imports

        #endregion Interop: R


        #region Interop: System

        [DllImport("kernel32.dll", CharSet = CharSet.Ansi, SetLastError = true)]
        private static extern IntPtr LoadLibraryEx(string lpFileName, IntPtr hReservedNull, LoadLibraryFlags dwFlags);

        [Flags]
        private enum LoadLibraryFlags : uint
        {
            DONT_RESOLVE_DLL_REFERENCES         = 0x00000001,
            LOAD_IGNORE_CODE_AUTHZ_LEVEL        = 0x00000010,
            LOAD_LIBRARY_AS_DATAFILE            = 0x00000002,
            LOAD_LIBRARY_AS_DATAFILE_EXCLUSIVE  = 0x00000040,
            LOAD_LIBRARY_AS_IMAGE_RESOURCE      = 0x00000020,
            LOAD_LIBRARY_SEARCH_APPLICATION_DIR = 0x00000200,
            LOAD_LIBRARY_SEARCH_DEFAULT_DIRS    = 0x00001000,
            LOAD_LIBRARY_SEARCH_DLL_LOAD_DIR    = 0x00000100,
            LOAD_LIBRARY_SEARCH_SYSTEM32        = 0x00000800,
            LOAD_LIBRARY_SEARCH_USER_DIRS       = 0x00000400,
            LOAD_WITH_ALTERED_SEARCH_PATH       = 0x00000008,
        }

        [DllImport("kernel32", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
        private static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

        #endregion Interop: System


        #region Fields

        private static RStartStruct RStartInfo;
        private static IntPtr RStartInfoPtr;

        #endregion Fields


        static RInterop()
        {
            InitRSession();
            AppDomain.CurrentDomain.DomainUnload += EndRSession;
        }


        #region RInterop Methods

        private static void InitRSession()
        {
            unsafe
            {
                RVersion = new string((sbyte*)getDLLVersion().ToPointer());
                RUserHome = new string((sbyte*)getRUser().ToPointer());
            }
            RHome = MakeAbsolutePath(R_HOME);

            RStartInfo = new RStartStruct();
            R_setStartTime();
            R_DefParams(ref RStartInfo);

            RStartInfo.rhome = RHome;
            RStartInfo.home = RUserHome;

            // Setup R in embedded mode
            RStartInfo.CharacterMode = RUIMode.LinkDLL;
            RStartInfo.R_Quiet = RBool.True;
            RStartInfo.R_Interactive = RBool.False;
            RStartInfo.RestoreAction = RSaType.SA_RESTORE;
            RStartInfo.SaveAction = RSaType.SA_NOSAVE;

            // Setup R callbacks
            RStartInfo.ReadConsole = IReadConsole;
            RStartInfo.WriteConsole = null;
            RStartInfo.CallBack = ICallBack;
            RStartInfo.ShowMessage = IShowMessage;
            RStartInfo.YesNoCancel = IYesNoCancel;
            RStartInfo.Busy = IBusy;
            RStartInfo.WriteConsoleEx = IWriteConsoleEx;

            // Pass RStartInfo to R
            RStartInfoPtr = Marshal.AllocHGlobal(Marshal.SizeOf(RStartInfo));
            Marshal.StructureToPtr(RStartInfo, RStartInfoPtr, false);
            R_SetParams(RStartInfoPtr);

            R_set_command_line_arguments(0, new string[] { });
            GA_initapp(0, new string[] { });
            readconsolecfg();
            setup_Rmainloop();
            R_ReplDLLinit();
        }


        private static void EndRSession(object sender, EventArgs e)
        {
            Rf_endEmbeddedR(0);

            Marshal.FreeHGlobal(RStartInfoPtr);
            RStartInfoPtr = IntPtr.Zero;
        }

        public static void Evaluate(string statement)
        {
            RParseStatus status = RParseStatus.PARSE_NULL;
            IntPtr expr = R_ParseVector(Rf_mkString(statement), 0, ref status, IntPtr.Zero);
            if (status != RParseStatus.PARSE_OK)
                throw new RInteropException(string.Format(
                    "Error {0} while parsing R statement: {1}",
                    status.ToString(),
                    statement));

            IntPtr pExpr = Rf_protect(expr);
            int evalError = 0;
            IntPtr ans = R_tryEval(pExpr, IntPtr.Zero, ref evalError);
        }

        #endregion RInterop Methods


        #region Events

        public static event EventHandler<RMessageEventArgs> MessageAvailable;

        #endregion Events


        #region Properties

        public static string Verion     { get { return "2.14.2"; } }
        public static string RVersion   { get; private set; }
        public static string RHome      { get; private set; }
        public static string RUserHome  { get; private set; }

        #endregion Properties


        #region R Callbacks

        private static int IReadConsole(string prompt, IntPtr buf, int len, int addtohistory)
        {
            // We don't use the console to interact with R. Return 0 to force R to exit any event loop.
            return 0;
        }

        private static void IWriteConsoleEx(string buf, int len, int otype)
        {
            OnMessageAvailable(new RMessageEventArgs(buf, otype != 0));
        }

        private static void ICallBack()
        {
            // No-Op
        }

        private static void IBusy(int which)
        {
            // No-Op
        }

        private static void IShowMessage(string msg)
        {
            OnMessageAvailable(new RMessageEventArgs(msg, true));
        }

        private static RYesNoCancel IYesNoCancel(string msg)
        {
            OnMessageAvailable(new RMessageEventArgs(string.Format("Yes/No/*CANCEL*: {0}", msg), false));
            return RYesNoCancel.Cancel;
        }

        #endregion R Callbacks


        #region Helper Methods

        private static void OnMessageAvailable(RMessageEventArgs e)
        {
            if (MessageAvailable != null)
            {
                MessageAvailable(null, e);
            }
            else
            {
                if (e.IsError)
                    Console.Error.WriteLine(e.Message);
                else
                    Console.WriteLine(e.Message);
            }
        }

        private static IntPtr LoadDll(string dllPath)
        {
            return LoadLibraryEx(MakeAbsolutePath(dllPath), IntPtr.Zero, LoadLibraryFlags.LOAD_LIBRARY_SEARCH_DLL_LOAD_DIR | LoadLibraryFlags.LOAD_LIBRARY_SEARCH_DEFAULT_DIRS);
        }

        private static string MakeAbsolutePath(string path)
        {
            if (Path.IsPathRooted(path))
                return path;
            else
                return Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), path);
        }

        #endregion Helper Methods
    }


    #region Helper Classes

    public class RMessageEventArgs : EventArgs
    {
        public RMessageEventArgs(string msg, bool err)
        {
            this.Message = msg;
            this.IsError = err;
        }
        public string Message   { get; private set; }
        public bool IsError     { get; private set; }
    }

    #endregion Helper Classes
}
