﻿using Discord.WebSocket;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace RainBorg
{
    public partial class RainBorg
    {
        internal static decimal GetBalance()
        {
            try
            {
                using (WebClient client = new WebClient())
                {
                    string dl = client.DownloadString(balanceUrl);
                    JObject j = JObject.Parse(dl);
                    return (decimal)j["balance"];
                }
            }
            catch
            {
                return -1;
            }
        }

        public static decimal Floor(decimal Input)
        {
            var r = Convert.ToDecimal(Math.Pow(10, decimalPlaces));
            return Math.Floor(Input * r) / r;
        }

        public static string Format(decimal Input)
        {
            Input = Floor(Input);
            string f = "{0:#,##0.#############}";
            return string.Format(f, Input);
        }
        public static string Format(double Input)
        {
            decimal I = Floor((decimal)Input);
            string f = "{0:#,##0.#############}";
            return string.Format(f, I);
        }

        // Log
        public static void Log(string Source, string Message, params object[] Objects)
        {
            // Log to console
            Console.WriteLine("{0} {1}\t{2}", DateTime.Now.ToString("HH:mm:ss"), Source, string.Format(Message, Objects));

            // If log file is specified
            if (!string.IsNullOrEmpty(logFile))
                using (StreamWriter w = File.AppendText(logFile))
                    w.WriteLine(string.Format("{0} {1} {2}\t{3}", DateTime.Now.ToLongDateString(), DateTime.Now.ToLongTimeString(), Source, string.Format(Message, Objects)));
        }

        // On exit
        public static bool ConsoleEventCallback(int eventType)
        {
            // Exiting
            if (eventType == 2)
            {
                if (exitMessage != "") foreach (KeyValuePair<ulong, List<ulong>> Entry in UserPools)
                        (_client.GetChannel(Entry.Key) as SocketTextChannel).SendMessageAsync(exitMessage).GetAwaiter().GetResult();
                Config.Save().GetAwaiter().GetResult();
            }
            return false;
        }

        // Remove a user from all user pools
        public static Task RemoveUserAsync(SocketUser User, ulong ChannelId)
        {
            // 0 = all channels
            if (ChannelId == 0)
                foreach (KeyValuePair<ulong, List<ulong>> Entry in UserPools)
                {
                    if (Entry.Value.Contains(User.Id))
                        Entry.Value.Remove(User.Id);
                }

            // Specific channel pool
            else if (UserPools.ContainsKey(ChannelId))
            {
                if (UserPools[ChannelId].Contains(User.Id))
                    UserPools[ChannelId].Remove(User.Id);
            }

            return Task.CompletedTask;
        }

        // Grab eligible channels
        private static List<ulong> EligibleChannels()
        {
            List<ulong> Output = new List<ulong>();
            foreach (KeyValuePair<ulong, List<ulong>> Entry in UserPools)
            {
                if (Entry.Value.Count >= userMin)
                {
                    Output.Add(Entry.Key);
                }
            }
            return Output;
        }

        // Relaunch bot
        public static void Relaunch()
        {
            Log("RainBorg", "Relaunching bot...");
            Paused = true;
            JObject Resuming = new JObject
            {
                ["userPools"] = JToken.FromObject(UserPools),
                ["greylist"] = JToken.FromObject(Greylist),
                ["userMessages"] = JToken.FromObject(UserMessages)
            };
            File.WriteAllText(resumeFile, Resuming.ToString());
            Process.Start("RelaunchUtility.exe", "RainBorg.exe");
            ConsoleEventCallback(2);
            Environment.Exit(0);
        }
    }
}
