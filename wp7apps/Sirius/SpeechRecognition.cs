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
using System.Windows.Threading;
using System.Threading;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Hawaii.Services.Client;
using Hawaii.Services.Client.Speech;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Microsoft.Xna.Framework;
using System.ComponentModel;
using Microsoft.Xna.Framework.Audio;

namespace Sirius
{
    public class SpeechRecognition
    {
        public delegate void SpeechRecognizedHandler( String result );//ResultParsed
        public event SpeechRecognizedHandler SpeechRecognized;

        public const string HawaiiApplicationId = "e8d2031f-030f-49f3-b86d-7309981f53b8";
        
        public SpeechRecognition(){
        }

        public void start( MemoryStream memStream )
        {
            /*
             * SpeechService.GetGrammarsAsync(HawaiiClient.HawaiiApplicationId,
                ( result ) =>
                {
                    this.OnSpeechGrammarsReceived(result);
                });
             *
             * This returns only "Dictation" now. (at 2011-12-13). No need to do that.
             */

            SpeechService.RecognizeSpeechAsync(HawaiiClient.HawaiiApplicationId,
                "Dictation",
                memStream.ToArray(),
                ( result ) =>
                {
                    this.OnSpeechRecognitionCompleted(result);
                });
        }

        private void OnSpeechRecognitionCompleted( SpeechServiceResult speechResult )
        {
            System.Diagnostics.Debug.WriteLine("SpeechRecognition: "+speechResult.Status);
            if (speechResult.Status == Status.Success)
            {
                // Use the response from the service. In this case the relevant 
                // data is in speechResult.SpeechResult.Items
                // Each item is a string that represents one possible text translation.
                List<String> items = speechResult.SpeechResult.Items;
                if(items.Count > 0)
                {
                    System.Diagnostics.Debug.WriteLine("SpeechRecognition: "+items.Count+" results.");
                    foreach(String result in items)
                    {
                        System.Diagnostics.Debug.WriteLine(result);
                        if(SpeechRecognized != null) {
                            SpeechRecognized(result);
                        }
                    }

                }
                else {
                    System.Diagnostics.Debug.WriteLine("SpeechRecognition: No results.");
                }
            }
            else
            {
                // Display the error state.
            }
        }

        private void OnSpeechGrammarsReceived( SpeechServiceResult result )
        {
            System.Diagnostics.Debug.WriteLine("SpeechGrammars: " + result.Status);
            if (result.Status == Status.Success)
            {
                // Use the response from the service.
                // The list of grammars is in result.SpeechResult.Items;
                foreach(String grammar in result.SpeechResult.Items) {
                    System.Diagnostics.Debug.WriteLine(grammar);
                }
            }
            else
            {
                // Display the error state.
            }
            
        }
    }

    public class XNAAsyncDispatcher : IApplicationService
    {
        private DispatcherTimer frameworkDispatcherTimer;

        public XNAAsyncDispatcher( TimeSpan dispatchInterval )
        {
            this.frameworkDispatcherTimer = new DispatcherTimer();
            this.frameworkDispatcherTimer.Tick += new EventHandler(frameworkDispatcherTimer_Tick);
            this.frameworkDispatcherTimer.Interval = dispatchInterval;
        }

        void IApplicationService.StartService( ApplicationServiceContext context ) { this.frameworkDispatcherTimer.Start(); }
        void IApplicationService.StopService() { this.frameworkDispatcherTimer.Stop(); }
        void frameworkDispatcherTimer_Tick( object sender, EventArgs e ) { FrameworkDispatcher.Update(); }
    }  
}
