using System.Reflection;

namespace Utility
{
    public static class Helper
    {
        #region Application Version

        private static string _ApplicationVersion = null;
        public static string ApplicationVersion
        {
            get
            {
                if(_ApplicationVersion == null)
                    _ApplicationVersion = GetApplicationVersion();
                return _ApplicationVersion;
            }
        }

        private static string GetApplicationVersion()
        {
            string assemblyInfo = Assembly.GetExecutingAssembly().FullName;
            return assemblyInfo.Split('=')[1].Split(',')[0];
        }

        #endregion
    }
}
