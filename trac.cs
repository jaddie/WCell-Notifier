using System;
using Squishy.Irc.Commands;
namespace WCellNotifier
{

    public class GetTicketCommand : Command
    {
        public GetTicketCommand()
            : base("gt", "getticket")
        {
            Usage = "getticket id";
            Description = "Gets the information of the ticket you specify";
        }

        public override void Process(CmdTrigger trigger)
        {
            try
            {
                string reply = "";
                foreach (var value in TracBackend.GetTicket(trigger.Args.NextInt(1)))
                {
                    reply += value;
                    reply += "   ";
                }
                reply = reply.Replace("\n", " ");
                reply = reply.Replace("\r", " ");
                trigger.Reply(reply);
            }
            catch (Exception e)
            {
                // Error handling
            }
        }
    }
    
    public class CreateTicketCommand : Command
    {
        public CreateTicketCommand()
            : base("ct")
        {
            Usage = "ct -summary summary here -description description here -attr none -notify false";
            Description = "Add a new ticket";
        }

        public override void Process(CmdTrigger trigger)
        {
            try
            {
                string summary = "", description = "";
                PageAttributes attributes = new PageAttributes();
                bool notify = false;
                while (trigger.Args.HasNext)
                {
                    var next = trigger.Args.NextModifiers();
                    switch (next)
                    {
                        case "s":
                        case "summary":
                            summary = trigger.Args.NextWord("\"");
                            break;
                        case "d":
                        case "description":
                            description = trigger.Args.NextWord("\"");
                            break;
                        case "a":
                        case "attr":
                            attributes.comment = trigger.Args.NextWord("\"");
                            break;
                        case "n":
                        case "notify":
                            notify = Convert.ToBoolean(trigger.Args.NextWord());
                            break;
                        default:
                            Console.WriteLine(trigger.Args.NextWord("\""));
                            break;
                    }
                }
                trigger.Reply(TracBackend.CreateTicket(summary, description, attributes, notify));
            }
            catch (Exception e)
            {
                // Error handling
            }
        }
    }
}