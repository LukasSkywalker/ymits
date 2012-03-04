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
using System.Xml;
using System.Xml.Serialization;
using System.Runtime.Serialization;
using System.IO;
using System.Collections.Generic;
using System.IO.IsolatedStorage;

namespace MusicBird
{
    public static class MySystem
    {
        /*public static void writePref(String name, String value){
            List<Preference> prefs;
            XmlWriterSettings xmlWriterSettings = new XmlWriterSettings();
            xmlWriterSettings.Indent = true;

            using(IsolatedStorageFile myIsolatedStorage = IsolatedStorageFile.GetUserStoreForApplication())
            {
                if(myIsolatedStorage.FileExists("Preferences.xml"))
                {
                    prefs = readPrefs();
                    if(prefs.ContainsKey(name)){
                        prefs[name] = value;
                    }else{
                        prefs.Add(name, value);
                    }
                }
                else {
                    prefs = new List<Preference>();
                }
                using(IsolatedStorageFileStream stream = myIsolatedStorage.OpenFile("Preferences.xml", FileMode.Create))
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(List<Preference>));
                    using(XmlWriter xmlWriter = XmlWriter.Create(stream, xmlWriterSettings))
                    {
                        serializer.Serialize(xmlWriter, prefs);
                    }
                }
            }    
        }

        public static List<Preference> readPrefs()
        {
            try
            {
                using(IsolatedStorageFile myIsolatedStorage = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    if(!myIsolatedStorage.FileExists("Preferences.xml"))
                    {
                        return new List<Preference>();
                    }
                    using(IsolatedStorageFileStream stream = myIsolatedStorage.OpenFile("Preferences.xml", FileMode.Open))
                    {
                        XmlSerializer serializer = new XmlSerializer(typeof(List<Preference>));
                        List<Preference> data = (List<Preference>)serializer.Deserialize(stream);
                        return data;
                    }
                }
            }
            catch
            {
                // add some code here
                throw new IsolatedStorageException("Could not get Pref file from UserStore");
            }
        }*/
    }

    [DataContract]
    public class Preference {
        [DataMember]
        public string name { get; set; }

        [DataMember]
        public string value { get; set; }
        

        public Preference(String name, String value)
        {
            this.name = name;
            this.value = value;
        }
    }
}
