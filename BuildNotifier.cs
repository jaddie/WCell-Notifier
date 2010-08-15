using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using settings = WCellNotifier.Properties.Settings;
using System.Timers;
using Squishy.Irc;
using Squishy.Network;
using OpenPOP.POP3;
using System.Net;
using Squishy.Irc.Commands;

namespace WCellNotifier
{
    public class BuildNotifier : IrcClient
    {
        #region IRC Connection info

        private const int Port = 6667;
        public static int SendQueue
        {
            get { return ThrottledSendQueue.CharsPerSecond; }
            set { ThrottledSendQueue.CharsPerSecond = value; }
        }
        public static readonly BuildNotifier Irc = new BuildNotifier
        {
            Nicks = new[] { "WCellNotifier", "WCell|Notifier" },
            UserName = "Jad_WCellNotifier",
            Info = "WCell's Notifier",
            _network = Dns.GetHostAddresses("irc.quakenet.org")
        };
        public static System.Timers.Timer SpamTimer = new System.Timers.Timer();
        private IPAddress[] _network;
        public static readonly List<string> ChannelList = new List<string> { settings.Default.NotificationChannel };
        public static Timer emailNotifications = new Timer(30000);
        #endregion
        static void Main()
        {
            emailNotifications.Elapsed += new ElapsedEventHandler(EmailNotifications_Elapsed);
            if (string.IsNullOrEmpty(settings.Default.EmailFromAddress) || string.IsNullOrEmpty(settings.Default.EmailHost) || string.IsNullOrEmpty(settings.Default.EmailPassword) || string.IsNullOrEmpty(settings.Default.EmailUsername) || string.IsNullOrEmpty(settings.Default.NotificationChannel) )
            {
                WriteErrorSystem.WriteError(null, "Error null email system value, check config!");
                Console.ReadLine();
                return;
            }
            else
            {
                #region IRC Connecting

                Irc.Client.Connecting += OnConnecting;
                Irc.Client.Connected += Client_Connected;
                Irc.BeginConnect(Irc._network[0].ToString(), Port);
                #endregion
           //     emailNotifications.Start();
            }
            System.Windows.Forms.Application.Run();
        }
        protected override void OnUnknownCommandUsed(CmdTrigger trigger)
        {
            return;
        }
        public static void Client_Connected(Connection con)
        {
            WriteErrorSystem.WriteError(null,"Connected to IRC Server");
        }

        public static void OnConnecting(Connection con)
        {
            WriteErrorSystem.WriteError(null,DateTime.Now + " : Connecting to server");
        }

        protected override void Perform()
        {
            try
            {
                IrcCommandHandler.Initialize();
                CommandHandler.Msg("Q@CServe.quakenet.org", " auth jaddie outlive1");
                Irc.CommandHandler.Mode("x",Irc.Me);
                CommandHandler.RemoteCommandPrefix = "~";
                foreach (var chan in ChannelList)
                {
                    if (chan.Contains(","))
                    {
                        var chaninfo = chan.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                        if (chaninfo.Length > 1)
                            CommandHandler.Join(chaninfo[0], chaninfo[1]);
                        else
                            CommandHandler.Join(chaninfo[0]);
                    }
                    else
                    {
                        CommandHandler.Join(chan);
                    }
                }
                if (emailNotifications.Enabled != true)
                CommandHandler.Msg(settings.Default.NotificationChannel, "Warning Email notification system is offline");
                TracBackend.TracMain();
            }
            catch (Exception e)
            {
                WriteErrorSystem.WriteError(null,e.Data + e.StackTrace);
            }
        }

        static void EmailNotifications_Elapsed(object sender, ElapsedEventArgs e)
        {
            CheckMail();
        }
        static void CheckMail()
        {
            try
            {
                Console.WriteLine("Checking Mailbox");
                POPClient pop3 = new POPClient();
                pop3.Connect(settings.Default.EmailHost, 110, false);
                pop3.Authenticate(settings.Default.EmailUsername, settings.Default.EmailPassword);
                int i = 0;
                while (i <= pop3.GetMessageCount())
                {
                    Console.WriteLine(pop3.GetMessageCount());
                    var msg = pop3.GetMessage(i);
                    if (msg != null && msg.MessageBody.Capacity > 0 && !string.IsNullOrEmpty(msg.MessageBody[0]) && msg.Headers.From.ToString() == settings.Default.EmailFromAddress)
                    {
                        Console.WriteLine("Marking message for deletion..");
                        pop3.DeleteMessage(i);
                        string msgbody = msg.MessageBody[0].Replace("\n", " ");
                        msgbody = msgbody.Replace("\r", " ");
                        var msgsplit = new StringStream(msgbody);
                        msgbody = msgsplit.NextWord("=====");
                        BuildNotifier.Irc.CommandHandler.Msg(settings.Default.NotificationChannel, msgbody);
                        Console.WriteLine(msgbody);
                    }
                    i++;
                }
                Console.WriteLine("Closing connection, deleting messages");
                pop3.Disconnect();
            }
            catch (Exception e)
            {
                WriteErrorSystem.WriteError(null, e.Message + e.Data + e.Source + e.StackTrace);
            }
        }
    }
}