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
            init(NavigationContext.QueryString["fileName"]);
        }

        private void init( String fileName ) {
            System.Diagnostics.Debug.WriteLine(fileName);
            this.oldFilename =  fileName;
            /*try
            {*/
                using(IsolatedStorageFile myIsolatedStorage = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    if(!myIsolatedStorage.FileExists(fileName))
                    {
                        throw new IsolatedStorageException("File " + fileName + " not found");
                    }else{
                        DateTimeOffset creation = myIsolatedStorage.GetCreationTime(fileName);
                        //DateTimeOffset access = myIsolatedStorage.GetLastAccessTime(fileName);
                        //DateTimeOffset write = myIsolatedStorage.GetLastWriteTime(fileName);
                        long free = myIsolatedStorage.AvailableFreeSpace;
                        //long quota = myIsolatedStorage.Quota;

                        long size = 0;

                        using(IsolatedStorageFileStream stream = myIsolatedStorage.OpenFile(fileName, FileMode.Open))
                        {
                            size = stream.Length;
                        }

                        filename.Text = fileName;
                        creationDate.Text = creation.LocalDateTime.ToLongDateString() + " " + creation.LocalDateTime.ToLongTimeString();
                        double mb = size / (double)1048576;
                        fileSize.Text = mb.ToString("F2") + " MB";


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

        private void saveButton_Click( object sender, RoutedEventArgs e )
        {
            /*try
            {*/
                using(IsolatedStorageFile myIsolatedStorage = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    if(myIsolatedStorage.FileExists(this.oldFilename))
                    {
                        /*try
                        {*/
                            myIsolatedStorage.MoveFile(this.oldFilename, this.filename.Text);
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
    }
}