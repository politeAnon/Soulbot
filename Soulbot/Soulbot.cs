using Discord;
using Discord.WebSocket;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

//Soulbout v0.7
//Author: Polite
namespace ProjectSoulbot
{
    class Soulbot
    {
        //Define and instantiate DiscordClient Object.
        DiscordSocketClient client = new DiscordSocketClient(new DiscordSocketConfig { LogLevel = LogSeverity.Verbose });

        //Credentials Variables for reading from creds.txt
        ulong clientID;
        public string token;
        List<ulong> botOwners = new List<ulong>();
        string credsFileName = Path.Combine(Directory.GetCurrentDirectory(), "creds.txt");

        //Owner Variables for reading from ownerIDs.txt
        string ownerFileName = Path.Combine(Directory.GetCurrentDirectory(), "ownerIDs.txt");

        //Ban Message Information, ID is read from creds.json, messages are read from banMsgs.txt
        ulong banChannelID;
        List<string> banMsgs;
        int banMsgIndex;

        bool active = true; //Controls whether the bot should be doing things or just sitting online.
        ulong freeChannelID; //ID of a channel where the bot will respond to general commands regardless of who calls them.

        //Preset colors for embed messages.
        Color error = new Color(255, 0, 0);
        Color warning = new Color(200, 200, 0);
        Color ok = new Color(0, 255, 0);
        Color info = new Color(0, 0, 255);

        public async Task MainAsync()
        {
            Console.WriteLine("Soulbot v1.0 by Polite");
            //Read in values from creds.txt, the formatting of creds.txt is important since it is parsed naively. 
            string[] tempCredsReadIn = System.IO.File.ReadAllLines(@credsFileName);
            clientID = Convert.ToUInt64(tempCredsReadIn[0].Substring(tempCredsReadIn[0].IndexOf("=") + 1));
            token = tempCredsReadIn[1].Substring(tempCredsReadIn[1].IndexOf("=") + 1);
            banChannelID = Convert.ToUInt64(tempCredsReadIn[2].Substring(tempCredsReadIn[2].IndexOf("=") + 1));
            freeChannelID = Convert.ToUInt64(tempCredsReadIn[3].Substring(tempCredsReadIn[3].IndexOf("=") + 1));

            //Red in values from ownerIDs.txt
            string[] tempOwnerReadIn = System.IO.File.ReadAllLines(@ownerFileName);
            foreach (string line in tempOwnerReadIn) { botOwners.Add(Convert.ToUInt64(line)); }

            //Read in ban messages from banMsgs.txt source file and shuffle them initially.
            //Set banMsgIndex to 0 to start with, it will then increment as necessary.
            banMsgs = new List<string>(System.IO.File.ReadAllLines(@"banMsgs.txt"));
            banMsgs.Shuffle(); banMsgIndex = 0;

            //Add bot reaction to events.
            client.Log += Log;
            client.Connected += ModifyStatus;
            client.MessageReceived += MessageReceived;
            client.UserBanned += PostBanMsg;

            //Login and connect bot.
            await client.LoginAsync(TokenType.Bot, token);
            await client.StartAsync();

            //Hold on this task indefinitely.
            await Task.Delay(-1);
        }

        //Update bot game.
        private async Task ModifyStatus()
        {
            await client.SetGameAsync("Soulbot by Polite");
        }

        //Parses commands made to the bot in text channels.
        private async Task MessageReceived(SocketMessage m)
        {
            bool isFree = m.Channel.Id == freeChannelID;
            bool isOwner = botOwners.Contains(m.Author.Id);
            if (isFree || isOwner)
            {
                var eb = new EmbedBuilder(); //Prepare embed for message posting.
                string command; int spacePos = m.Content.IndexOf(" "); //Find index of space in message if there is one.
                //If there is a space get the part prior to it. If there is no space get entire message.
                if (spacePos == -1) { command = m.Content; } else { command = m.Content.Substring(0, spacePos); }

                switch (command)
                {
                    //Provides helpful information for commands to use with the bot.
                    case ",help":
                        if (active) //Bot only responds to help command when on.
                        {
                            eb.WithDescription("COMMANDS HELP\n" +
                                               ",on : Will turn the bot on causing it to process commands and events.\n" +
                                               ",off : Will turn the bot off causing it to essentially just sit online till turned back on.\n" +
                                               ",abm %string% : Adds a ban message to the list of possible messages the bot will say when someone is banned where %string% is the message to add.\n");
                            eb.WithColor(info);
                            await m.Channel.SendMessageAsync("", false, eb);
                        }
                        break;
                    //Pings the bot and it replies with pong.
                    case ",ping":
                        if (active) //Only perform when on.
                        {
                            eb.WithDescription("Pong!"); eb.WithColor(info);
                            await m.Channel.SendMessageAsync("", false, eb);
                        }
                        break;
                    //Tells the user their uid.
                    case ",uid":
                        if (active) //Only perform when on.
                        {
                            eb.WithDescription(m.Author.Username + " your UID is: " + m.Author.Id); eb.WithColor(info);
                            await m.Channel.SendMessageAsync("", false, eb);
                        }
                        break;
                    //Tells the user whether the bot considers them an admin.
                    case ",amadmin":
                        if (active) //Only perform when on.
                        {
                            if (isOwner) { eb.WithDescription(m.Author.Username + ", I recognize you as an admin."); eb.WithColor(ok); }
                            else { eb.WithDescription(m.Author.Username + ", you are not an admin."); eb.WithColor(error); }
                            await m.Channel.SendMessageAsync("", false, eb);
                        }
                        break;
                    //Lists all recognized admins UIDs and their usernames.
                    case ",lad":
                        if (active) //Only perform when on.
                        {
                            string msg = "";
                            foreach (var owner in botOwners)
                            {
                                IUser tmp = await m.Channel.GetUserAsync(owner);
                                msg = msg + tmp.Username + ": " + tmp.Id + "\n";
                            }
                            eb.WithDescription(msg); eb.WithColor(info);
                            await m.Channel.SendMessageAsync("", false, eb);
                        }
                        break;
                    //List all ban messages along with their index.
                    case ",lbm":
                        if (active) //Only perform when on.
                        {
                            string msg = "";
                            for (int i = 0; i < banMsgs.Count; i++)
                            {
                                msg = msg + "[" + i + "] : " + banMsgs[i] + "\n";
                            }
                            eb.WithDescription(msg); eb.WithColor(info);
                            await m.Channel.SendMessageAsync("", false, eb);
                        }
                        break;
                    //Tell the bot to start doing things again if it was turned off.
                    case ",on":
                        if (isOwner) //Only parse command if the author is an owner.
                        {
                            if (active) //Only perform when on.
                            {
                                eb.WithDescription("I am already on!"); //Bot announces it is already on.
                                eb.WithColor(warning);
                                await m.Channel.SendMessageAsync("", false, eb);
                            }
                            else
                            {
                                active = true;
                                eb.WithDescription("Turning on!"); //Bot acknowledges successful turn on. One of two response/actions bot should make while off.
                                eb.WithColor(ok);
                                await m.Channel.SendMessageAsync("", false, eb);
                            }
                        }
                        break;
                    //Tell the bot to stop doing anything like announcing bans and so on.
                    case ",off":
                        if (isOwner) //Only parse command if the author is an owner.
                        {
                            if (active) //Only perform when on.
                            {
                                active = false;
                                eb.WithDescription("Turning off!"); //Bot acknowledges successful turn off.
                                eb.WithColor(ok);
                                await m.Channel.SendMessageAsync("", false, eb);
                            }
                            else
                            {
                                eb.WithDescription("I am already off!"); //Bot announces it is already off. One of two response/actions bot should make while off.
                                eb.WithColor(warning);
                                await m.Channel.SendMessageAsync("", false, eb);
                            }
                        }
                        break;
                    //Adds a discord user as a bot admin.
                    case ",aau":
                        if (isOwner) //Only parse command if the author is an owner.
                        {
                            if (active) //Only perform when on.
                            {
                                ulong uid = Convert.ToUInt64(m.Content.Substring(spacePos + 1)); //Parse out the value part of the command.
                                IUser tmp = await m.Channel.GetUserAsync(uid);
                                if (tmp == null)
                                {
                                    eb.WithDescription("The provided UID is not a user on this server!"); eb.WithColor(warning);
                                    await m.Channel.SendMessageAsync("", false, eb);
                                }
                                else if (botOwners.Contains(uid))
                                {
                                    eb.WithDescription(tmp.Username + ": " + uid + " is already an admin!"); eb.WithColor(warning);
                                    await m.Channel.SendMessageAsync("", false, eb);
                                }
                                else
                                {
                                    botOwners.Add(uid);
                                    using (System.IO.StreamWriter file = new System.IO.StreamWriter(@"ownerIDs.txt", true)) { file.Write("\n" + m.Content.Substring(spacePos + 1)); }
                                    eb.WithDescription(tmp.Username + ": " + uid + " added as a bot admin!"); eb.WithColor(ok);
                                    await m.Channel.SendMessageAsync("", false, eb);
                                }
                            }
                        }
                        break;
                    //Removes a discord user as a bot admin.
                    case ",rma":
                        if (isOwner) //Only parse command if the author is an owner.
                        {
                            if (active) //Only perform when on.
                            {
                                ulong uid = Convert.ToUInt64(m.Content.Substring(spacePos + 1)); //Parse out the value part of the command.
                                IUser tmp = await m.Channel.GetUserAsync(uid);
                                if (!botOwners.Contains(uid))
                                {
                                    eb.WithDescription("The provided UID is not a bot admin!"); eb.WithColor(warning);
                                    await m.Channel.SendMessageAsync("", false, eb);
                                }
                                else
                                {
                                    botOwners.RemoveAll(item => item == uid);

                                    using (System.IO.StreamWriter writer = new StreamWriter(@"ownerIDs.txt", false))
                                    {
                                        writer.Write(botOwners[0]);
                                        for (int i = 1; i < botOwners.Count; i++) { writer.Write("\n" + botOwners[i]); }
                                    }

                                    eb.WithDescription(tmp.Username + ": " + uid + " removed as a bot admin!"); eb.WithColor(ok);
                                    await m.Channel.SendMessageAsync("", false, eb);
                                }
                            }
                        }
                        break;
                    //Add a new ban message to the bot from discord itself.
                    case ",abm":
                        if (isOwner) //Only parse command if the author is an owner.
                        {
                            if (active) //Only perform when on.
                            {
                                string msg = m.Content.Substring(spacePos + 1); //Parse out the value part of the command.
                                using (System.IO.StreamWriter file = new System.IO.StreamWriter(@"banMsgs.txt", true)) { file.Write("\n" + msg); } //Write to the banMsgs.txt file so this addition is persistent over bot restarts.
                                banMsgs.Add(msg); //Add this ban message to the list of possible ones. 
                                banMsgs.Shuffle(); banMsgIndex = 0; //Re-shuffle the list with the new addition.
                                eb.WithDescription("Successfully added new ban message: USER " + msg); eb.WithColor(ok);
                                await m.Channel.SendMessageAsync("", false, eb); //Provide feedback of success for the user.
                            }
                        }
                        break;
                    case ",rbm":
                        if (isOwner) //Only parse command if the author is an owner.
                        {
                            if (active) //Only perform when on.
                            {
                                int index = Convert.ToInt16(m.Content.Substring(spacePos + 1)); //Parse out the value part of the command.
                                if (index < 0 || index >= banMsgs.Count)
                                {
                                    eb.WithDescription("Invalid index to delete. Try again."); eb.WithColor(warning);
                                    await m.Channel.SendMessageAsync("", false, eb);
                                }
                                else
                                {
                                    banMsgs.RemoveAt(index);

                                    using (System.IO.StreamWriter writer = new StreamWriter(@"banMsgs.txt", false))
                                    {
                                        if (banMsgs.Count >= 1)
                                        {
                                            writer.Write(banMsgs[0]);
                                            for (int i = 1; i < botOwners.Count; i++) { writer.Write("\n" + botOwners[i]); }
                                        }
                                        else
                                        {
                                            //Nothing to write.
                                        }
                                    }

                                    eb.WithDescription("Ban message with index " + index + " has been removed!"); eb.WithColor(ok);
                                    await m.Channel.SendMessageAsync("", false, eb);
                                }
                            }
                        }
                        break;
                }
            }
        }

        //Post ban message when UserBanned event is detected.
        private async Task PostBanMsg(SocketUser u, SocketGuild g)
        {
            if (active) //Bot only announces ban messages when it is on.
            {
                IReadOnlyCollection<Discord.Rest.RestBan> bCollection = await g.GetBansAsync();
                var b = bCollection.ToImmutableList().Find(item => item.User.Username == u.Username);

                //Send ban message to channel then increment index.
                var channel = g.GetTextChannel(banChannelID);
                var msg = u.Username + " " + banMsgs[banMsgIndex];
                var eb = new EmbedBuilder();
                if (b.Reason != null) { eb.WithDescription(msg + " BAN REASON: " + b.Reason); }
                else { eb.WithDescription(msg); }

                await channel.SendMessageAsync("", false, eb);
                banMsgIndex++;

                //If we've used all ban messages shuffle them and start using them over.
                if (banMsgIndex >= banMsgs.Count) { banMsgs.Shuffle(); banMsgIndex = 0; }
            }
        }

        //Log Function
        private Task Log(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }
    }

    //Helper Class to allow multiple calling of Shuffle without recreating the RNG.
    public static class ThreadSafeRandom
    {
        [ThreadStatic]
        private static Random Local;
        public static Random ThisThreadsRandom
        {
            get { return Local ?? (Local = new Random(unchecked(Environment.TickCount * 31 + Thread.CurrentThread.ManagedThreadId))); }
        }
    }

    //Helper Class to provide extension method to the IList<T> container for shuffling capability.
    static class helperExtensions
    {
        public static void Shuffle<T>(this IList<T> list)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int i = ThreadSafeRandom.ThisThreadsRandom.Next(n + 1);
                T value = list[i];
                list[i] = list[n];
                list[n] = value;
            }
        }
    }
}