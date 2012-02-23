using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;
using Newtonsoft.Json;
using RestSharp;
using RestSharp.Extensions;

namespace SunriseSMS
{
    public partial class MainPage : PhoneApplicationPage
    {
        public RestClient client;

        public string baseURL = "http://your.hispeed.ch";
        public string loginDestination = "/setcookie.cgi";
        public string username = "lukas.diener@hispeed.ch";
        public string password = "!Qh7gR5W2";
        public string messengerURL = "https://your.hispeed.ch/glue.cgi?http://messenger.hispeed.ch/walrus/app/login.do?language=de&amp;hostname=your.hispeed.ch";

        // Constructor
        public MainPage()
        {
            
            InitializeComponent();
            login();
        }

        public void login() {

            client = new RestClient();
            client.BaseUrl = "http://your.hispeed.ch";
            var request = new RestRequest("/setcookie.cgi", Method.POST);
            request.AddHeader("Content-Type", "application/x-www-form-urlencoded");
            request.AddParameter("body", "url=https://your.hispeed.ch/glue.cgi?http://messenger.hispeed.ch/walrus/app/login.do?language=de&amp;hostname=your.hispeed.ch&mail=lukas.diener@hispeed.ch&password=!Qh7gR5W2", ParameterType.RequestBody);
  
            client.ExecuteAsync(request, (response) => 
            { 
                System.Diagnostics.Debug.WriteLine(response.Content);

                client.BaseUrl = "http://messenger.hispeed.ch";
                var sendRequest = new RestRequest("/walrus/app/sms_send.do", Method.GET);

                foreach(var c in response.Cookies)
                {
                    sendRequest.AddParameter(c.Name, c.Value, ParameterType.Cookie);
                    System.Diagnostics.Debug.WriteLine(c.Name + " " + c.Value);
                }

                sendRequest.AddHeader("Content-Type", "application/x-www-form-urlencoded");
                sendRequest.AddParameter("body", "hostname=your.hispeed.ch&action=send&groupName=%3A%3A__DEFAULTGROUP__%3A%3A&message=Test3dsfghjkhgdf&numCount=5&sendingMoment=now&sendDate=&sendTime=&notifAddress=notifNone&originator=originatorUser&recipientChecked=yes&recipient=0764822768", ParameterType.RequestBody);


                client.ExecuteAsync(sendRequest, ( response2 ) =>
                {
                    System.Diagnostics.Debug.WriteLine(response2.Content);
                }); 
            });

        }

        public void sendMessage() {
            client = new RestClient("http://messenger.hispeed.ch");
            var request = new RestRequest("/walrus/app/sms_send.do", Method.POST);
            request.AddHeader("Content-Type", "application/x-www-form-urlencoded");
            request.AddParameter("body", "hostname=your.hispeed.ch&action=send&groupName=%3A%3A__DEFAULTGROUP__%3A%3A&message=Test3dsfghjkhgdf&numCount=5&sendingMoment=now&sendDate=&sendTime=&notifAddress=notifNone&originator=originatorUser&recipientChecked=yes&recipient=0764822768", ParameterType.RequestBody);

            client.ExecuteAsync(request, ( response ) =>
            {
                System.Diagnostics.Debug.WriteLine(response.Content);
            });
             
        }


           

    }
}