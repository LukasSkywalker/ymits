using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.IO.IsolatedStorage;
using System.IO;

namespace Helper
{
    public static class Preferences
    {
        public static void write( string name, string value )
        {
            string filename = "pref-" + name + ".txt";
            /*try
            {*/
            using(IsolatedStorageFile myIsolatedStorage = IsolatedStorageFile.GetUserStoreForApplication())
            {
                if(myIsolatedStorage.FileExists(filename)) myIsolatedStorage.DeleteFile(filename);
                using(StreamWriter writeFile = new StreamWriter(new IsolatedStorageFileStream(filename,
                    FileMode.Create, FileAccess.Write, myIsolatedStorage)))
                {
                    string someTextData = value;
                    writeFile.WriteLine(someTextData);
                    writeFile.Close();
                }
            }
            System.Diagnostics.Debug.WriteLine("Pref " + name + " written successfully. Value is " + value);
            /*}
            catch(Exception e) {
                System.Diagnostics.Debug.WriteLine("Failed to write pref "+name+": "+value+". Error is "+e.Message);
            }*/

        }

        public static void write( string name, int value )
        {
            write(name, value.ToString());
        }

        public static void write( string name, bool value )
        {
            write(name, value.ToString());
        }

        public static string read( string name )
        {
            string filename = "pref-" + name + ".txt";
            string output = null;
            /*try
            {*/
            using(IsolatedStorageFile myIsolatedStorage = IsolatedStorageFile.GetUserStoreForApplication())
            {
                try
                {
                    if(!myIsolatedStorage.FileExists(filename)) return null;
                    IsolatedStorageFileStream fileStream = myIsolatedStorage.OpenFile(filename, FileMode.Open, FileAccess.Read);
                    using(StreamReader reader = new StreamReader(fileStream))
                    {
                        output = reader.ReadLine();
                    }
                }
                catch(IsolatedStorageException)
                {
                    // TODO
                }
            }
            /*}
            catch(Exception e) { System.Diagnostics.Debug.WriteLine(e.Message); }*/
            return output;
        }

        public static bool readBool( string name )
        {
            string value = read(name);
            bool tr = true;
            if(value != null && value.Equals(tr.ToString()))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public static int readInt( string name )
        {
            string value = read(name);
            try
            {
                return Convert.ToInt32(value);

            }
            catch(FormatException)
            {
                return 0;
            }
        }
    }
}
