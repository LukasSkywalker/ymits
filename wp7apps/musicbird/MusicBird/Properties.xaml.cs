using System;
using System.IO.IsolatedStorage;
using System.IO;
using System.Windows;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Codeplex.OAuth;

namespace MusicBird
{
    public partial class Properties : PhoneApplicationPage
    {
        String oldFilename = "";

        public Properties()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo( NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            init(NavigationContext.QueryString["filename"]);
        }

        private void init( String filename ) {
            System.Diagnostics.Debug.WriteLine(filename);
            this.oldFilename =  filename;
            /*try
            {*/
                using(IsolatedStorageFile myIsolatedStorage = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    if(!myIsolatedStorage.FileExists(filename))
                    {
                        throw new IsolatedStorageException("File " + filename + " not found");
                    }else{
                        DateTimeOffset creation = myIsolatedStorage.GetCreationTime(filename);
                        //DateTimeOffset access = myIsolatedStorage.GetLastAccessTime(filename);
                        //DateTimeOffset write = myIsolatedStorage.GetLastWriteTime(filename);
                        long free = myIsolatedStorage.AvailableFreeSpace;
                        //long quota = myIsolatedStorage.Quota;

                        long size = 0;

                        using(IsolatedStorageFileStream stream = myIsolatedStorage.OpenFile(filename, FileMode.Open))
                        {
                            size = stream.Length;
                        }

                        fileName.Text = filename;
                        creationDate.Text = creation.LocalDateTime.ToLongDateString() + " " + creation.LocalDateTime.ToLongTimeString();
                        double mb = size/(double)1048576;
                        fileSize.Text = mb.ToString("F2")+" MB";


                        //memoryBar.Maximum = quota;
                        //memoryBar.Value = quota - free;
                        freeMemory.Text = "Free: " + (free / (double)1048576).ToString("F2") + " MB";
                        //usedMemory.Text = "Used: " + (quota - free) / 1024 / 1024 + "MB";
                        //System.Diagnostics.Debug.WriteLine("quota:" + quota + "\nfree:" + free);
                    }
                }
            /*}
            catch(Exception e) {
                System.Diagnostics.Debug.WriteLine("Properties.xaml.cs:init ___ IsolatedStorageException: "+e.Message);
            }*/
        }

        private void button1_Click( object sender, RoutedEventArgs e )
        {
            /*try
            {*/
                using(IsolatedStorageFile myIsolatedStorage = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    if(myIsolatedStorage.FileExists(oldFilename))
                    {
                        /*try
                        {*/
                            myIsolatedStorage.MoveFile(oldFilename, fileName.Text);
                        /*}
                        catch(Exception exc) {
                            System.Diagnostics.Debug.WriteLine(exc.Message);
                        }*/
                    }
                }
            /*}
            catch(Exception ex) {
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }*/
            NavigationService.Navigate(new Uri("/MainPage.xaml", UriKind.Relative));
        }

        private void dbUpload_Click( object sender, RoutedEventArgs e )
        {
            MessageBox.Show("Not implemented.");
        }
    }
}