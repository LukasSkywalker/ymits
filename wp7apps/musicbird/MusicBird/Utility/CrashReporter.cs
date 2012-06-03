using System;
using System.Globalization;
using System.IO;
using System.IO.IsolatedStorage;
using System.Net;
using System.Text;
using System.Windows;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;

namespace MusicBird
{
    // CrashReporter
    // Matt Isenhower, Komodex Systems LLC
    // http://blog.ike.to/2011/02/02/wp7-application-crash-reporter/

    public static class CrashReporter
    {
        // Error Report URL parameters
        private const string ErrorReportURL = "http://musicdc.sourceforge.net/upload/musicbird/crashreporter/";
        private const string ProductName = "MusicBird";

        // The filename for crash reports in isolated storage 
        private const string ErrorLogFilename = "ApplicationErrorLog.log";

        private static readonly string LargeDashes = new string('=', 80);
        private static readonly string SmallDashes = new string('-', 80);
        private static string ErrorLogContent = null;
        private static PhoneApplicationFrame RootFrame = null;
        private static bool IsObscured = false;
        private static bool IsLocked = false;

        public static void Initialize( PhoneApplicationFrame frame )
        {
            RootFrame = frame;

            // Hook into exception events
            App.Current.UnhandledException += new EventHandler<ApplicationUnhandledExceptionEventArgs>(App_UnhandledException);
            RootFrame.NavigationFailed += new NavigationFailedEventHandler(RootFrame_NavigationFailed);

            // Hook into obscured/unobscured events
            RootFrame.Obscured += new EventHandler<ObscuredEventArgs>(RootFrame_Obscured);
            RootFrame.Unobscured += new EventHandler(RootFrame_Unobscured);

            // Send previous log if it exists
            SendExceptionLog();
        }

        #region Events

        static void App_UnhandledException( object sender, ApplicationUnhandledExceptionEventArgs e )
        {
            LogException(e.ExceptionObject, "Unhandled Exception");
        }

        static void RootFrame_NavigationFailed( object sender, NavigationFailedEventArgs e )
        {
            LogException(e.Exception, "Navigation Failed");
        }

        static void RootFrame_Obscured( object sender, ObscuredEventArgs e )
        {
            IsObscured = true;
            IsLocked = e.IsLocked;
        }

        static void RootFrame_Unobscured( object sender, EventArgs e )
        {
            IsObscured = false;
            IsLocked = false;
        }

        #endregion

        #region Methods

        private static void LogException( Exception e, string type = null )
        {
            try
            {
                using(IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    using(TextWriter writer = new StreamWriter(store.OpenFile(ErrorLogFilename, FileMode.Append)))
                    {
                        writer.WriteLine(LargeDashes);

                        // Error type
                        writer.WriteLine(type ?? "Application Error");

                        // Application name
                        writer.WriteLine("-> Product: " + ProductName);

                        // Application version
                        writer.Write("-> Version: " + Utility.Helper.ApplicationVersion);
#if DEBUG
                        writer.Write(" (Debug)");
#endif
                        writer.WriteLine();

                        // Date and time
                        writer.WriteLine("-> Date: " + DateTime.Now.ToString());

                        // Unique report ID
                        writer.WriteLine("-> Report ID: " + Guid.NewGuid().ToString());

                        writer.WriteLine(SmallDashes);

                        try
                        {
                            writer.WriteLine("-> OS Version: {0} ({1})", Environment.OSVersion, Microsoft.Devices.Environment.DeviceType);
                            writer.WriteLine("-> Framework: " + Environment.Version.ToString());
                            writer.WriteLine("-> Culture: " + CultureInfo.CurrentCulture);
                            writer.WriteLine("-> Current page: " + RootFrame.CurrentSource);
                        }
                        catch(Exception ex)
                        {
                            writer.WriteLine(" -> Error getting device/page info: " + ex.ToString());
                        }

                        writer.WriteLine("-> Obscured: " + ((IsObscured) ? "Yes" : "No"));
                        writer.WriteLine("-> Locked: " + ((IsLocked) ? "Yes" : "No"));

                        writer.WriteLine(SmallDashes);

                        // Exception Info
                        writer.WriteLine("Exception Information:");
                        writer.WriteLine();
                        writer.WriteLine(e.ToString());

                        writer.WriteLine();
                    }
                }
            }
            catch { }
        }

        #endregion

        #region HTTP Request and Response

        private static void SendExceptionLog()
        {
            try
            {
                using(IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    if(!store.FileExists(ErrorLogFilename))
                        return;

                    using(TextReader reader = new StreamReader(store.OpenFile(ErrorLogFilename, FileMode.Open, FileAccess.Read, FileShare.None)))
                    {
                        ErrorLogContent = reader.ReadToEnd();
                    }

                    if(ErrorLogContent == null)
                        return;

                    string url = ErrorReportURL + "?p=" + ProductName + "&v=" + Utility.Helper.ApplicationVersion;
#if DEBUG
                    url += "&d=1";
#endif

                    HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                    request.ContentType = "application/x-www-form-urlencoded";
                    request.Method = "POST";

                    request.BeginGetRequestStream(new AsyncCallback(GetRequestStringCallback), request);
                }
            }
            catch { }
        }

        private static void GetRequestStringCallback( IAsyncResult asyncResult )
        {
            HttpWebRequest request = (HttpWebRequest)asyncResult.AsyncState;

            string postData = "log=" + HttpUtility.UrlEncode(ErrorLogContent);

            byte[] bytes = Encoding.UTF8.GetBytes(postData);

            try
            {
                Stream postStream = request.EndGetRequestStream(asyncResult);
                postStream.Write(bytes, 0, bytes.Length);
                postStream.Close();

                request.BeginGetResponse(new AsyncCallback(GetResponseCallback), request);
            }
            catch { }
        }

        private static void GetResponseCallback( IAsyncResult asyncResult )
        {
            HttpWebRequest request = (HttpWebRequest)asyncResult.AsyncState;

            try
            {
                HttpWebResponse response = (HttpWebResponse)request.EndGetResponse(asyncResult);
                Stream streamResponse = response.GetResponseStream();
                StreamReader reader = new StreamReader(streamResponse);
                string responseString = reader.ReadToEnd();

                streamResponse.Close();
                reader.Close();
                response.Close();

                // Delete the log file
                using(IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    store.DeleteFile(ErrorLogFilename);
                }
            }
            catch { }
        }

        #endregion
    }
}