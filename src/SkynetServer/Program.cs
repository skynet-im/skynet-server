using SkynetServer.Entities;
using System;
using System.Linq;

namespace SkynetServer
{
    class Program
    {
        static void Main(string[] args)
        {
            using (ChannelContext ctx = new ChannelContext())
            {
                //ctx.Database.EnsureDeleted();
                ctx.Database.EnsureCreated();
            }

            //using (ChannelContext ctx = new ChannelContext())
            //{
            //    ctx.Channels.Add(new Channel() { ChannelId = 153 });
            //    ctx.SaveChanges();

            //    Random random = new Random();
            //    Span<byte> value = stackalloc byte[8];
            //    random.NextBytes(value);
            //    BitConverter.ToInt64(value);
            //}

            using (ChannelContext ctx = new ChannelContext())
            using (MessageContext mtx = new MessageContext())
            {
                foreach (Channel c in ctx.Channels)
                {
                    Console.WriteLine($"Channel with id {c.ChannelId}");

                    foreach (Message m in mtx.Messages.Where(x => x.ChannelId == c.ChannelId))
                    {
                        Console.WriteLine($"\tMessage with id {m.MessageId}");
                    }
                }
            }

            Console.WriteLine("Press any key to exit");
            Console.ReadKey();
        }
    }
}
