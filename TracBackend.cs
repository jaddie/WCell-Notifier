using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using CookComputing.XmlRpc;
using settings = WCellNotifier.Properties.Settings;

namespace WCellNotifier
{
    public interface ITrac : IXmlRpcProxy
    {
        [XmlRpcMethod("wiki.getAllPages")]
        string[] WikiGetAllPages();
        [XmlRpcMethod("wiki.putPage")]
        bool WikiPutPage(string pagename, string content, PageAttributes attr);
        [XmlRpcMethod("ticket.query")]
        int[] TicketQuery(string query);
        [XmlRpcMethod("ticket.get")]
        Array TicketGet(int id);
        [XmlRpcMethod("ticket.getTicketFields")]
        Array TicketGetFields();
        [XmlRpcMethod("ticket.create")]
        int TicketCreate(string summary, string description, PageAttributes attr, bool notify);
    }
    public struct TicketAttributes
    {
        public int id;
        public string DateCreated;
        public string TimeChanged;
    }
    // define the structure needed by the putPage method
    public struct PageAttributes
    {
        public string comment;
    }
    class TracBackend
    {
        public static ITrac proxy = XmlRpcProxyGen.Create<ITrac>();
        public static void TracMain()
        {
            // Should be in a config file
            string user = "James";
            string password = "outlive1";

            // If you wish point this to the trac url, if you don't
            // it will use the one as set in the service decleration
            proxy.Url = "http://tracker.wcell.org/wcellmaster/login/xmlrpc";

            // Attach credentials
            proxy.Credentials = new System.Net.NetworkCredential(user, password);
            var rc = proxy.TicketQuery("status!=closed");
            string output = "";
            foreach (var s in rc)
            {
                output += s;
                output += ",";
            }
            BuildNotifier.Irc.CommandHandler.Msg(settings.Default.NotificationChannel.ToString(), "Open Tickets:" + output);
        }
        public static List<object> GetTicket(int id)
        {
            Array response = proxy.TicketGet(id);
            List<object> output = new List<object>();
            int length = response.GetLength(0);
            for (int i = 0; i < length; i++)
            {
                if (i < 3)
                {
                    output.Add(response.GetValue(i));
                }
                else
                {
                    var meh = (XmlRpcStruct)response.GetValue(3);
                    var values = meh.Values;
                    foreach (var value in values)
                    {
                        output.Add(value);
                    }
                }
            }
            return output;
        }
        public  static int CreateTicket(string summary,string description,PageAttributes attr,bool notify=false)
        {
            return proxy.TicketCreate(summary, description, attr, notify);
        }
        public static List<object> GetTicketFields()
        {
            Array fields = proxy.TicketGetFields();
            List<object> output = new List<object>();
            foreach (var field in fields)
            {
                var meh = (XmlRpcStruct)field;
                foreach (var o1 in meh.Values)
                {
                    output.Add(o1);
                }
            }
            return output;
        }
      /*  public static List<string> ReadSpeechMarks(string line)
        {
            var l = new StringReader(line);
            do
            {
                continue;
            } while (l.Read() != '"');

        }*/
    }
}
