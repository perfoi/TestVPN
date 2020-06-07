using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Xml.Linq;

namespace TestVPN
{
    internal static class Program
    {
        private static string Ip { get; } = GetLocalIpAddress();
        private static string Host { get; set; }
        private static string Login { get; set; }
        private static string Password { get; set; }
        private static string Path { get; set; } = Directory.GetCurrentDirectory();
        private static Status CurrentStatus { get; set; } = Status.Disconnected;
        private static Commands Command { get; set; } = Commands.About;

        private static void Main()
        {
            Console.SetWindowSize(125, 45);
            Console.WriteLine("VPN service for demonstration");
            Console.WriteLine(new string('-', 30));
            Console.WriteLine("Info:\r\n1. connect\r\n2. disconnect\r\n3. about\r\n4. show\r\n5. exit");
            Console.WriteLine(new string('-', 30));

            while (Command != Commands.Exit)
            {
                Console.Write("Enter the command number: ");
                if (int.TryParse(Console.ReadLine(), out var cmd))
                {
                    cmd--;
                    if (cmd >= 0 && cmd <= 4)
                    {
                        Command = (Commands)cmd;
                    }
                    else
                    {
                        Command = (Commands)10;
                    }
                }
                else
                {
                    Command = (Commands)10;
                }

                switch (Command)
                {
                    case Commands.Connect:
                        if (CurrentStatus != Status.Connected)
                        {
                            Auth();
                            if (Ip != GetLocalIpAddress())
                            {
                                Console.WriteLine("Connection has been established\r\n");
                                CurrentStatus = Status.Connected;
                            }
                            else
                            {
                                Console.WriteLine("Connection hasn't been established!");
                            }
                        }
                        else
                        {
                            Console.WriteLine("You need to disconnect\r\n");
                        }
                        break;
                    case Commands.Disconnect:
                        if (CurrentStatus != Status.Disconnected)
                        {
                            Disconnect();
                            Console.WriteLine("You've been disconnected\r\n");
                            CurrentStatus = Status.Disconnected;
                        }
                        else
                        {
                            Console.WriteLine("You don't have any vpn connections\r\n");
                        }

                        break;
                    case Commands.About:
                        DrawSplash();
                        break;
                    case Commands.ShowIp:
                        Console.WriteLine($"Your current ip address is: {GetLocalIpAddress()}\r\n");
                        break;
                    case Commands.Exit:
                        break;
                    default:
                        Console.WriteLine("1. connect\r\n2. disconnect\r\n3. about\r\n4. show\r\n5. exit\r\n");
                        break;
                }
            }
        }

        private static void Auth()
        {
            Console.Write("Do you want to read config file? [y/n] ");
            switch (Console.ReadLine())
            {
                case "yes":
                    XDocument confifFile = XDocument.Load($@"{Environment.CurrentDirectory}\config.xml");
                    XElement root = confifFile.Root;
                    List<Config> configs = new List<Config>();
                    foreach (var item in root.Elements())
                    {
                        try
                        {
                            configs.Add(
                                new Config
                                {
                                    Host = item.Element("host").Value,
                                    Login = item.Element("login").Value,
                                    Password = item.Element("password").Value,
                                });
                        }
                        catch (ArgumentNullException)
                        {
                            Console.WriteLine("Check config file credentials");
                            throw;
                        }
                    }
                    Console.WriteLine("Config file has been read");
                    if (configs.Count > 1)
                    {
                        Console.WriteLine($"What server do you want to use? [1-{configs.Count}]");
                        var i = 0;
                        foreach (var item in configs)
                        {
                            Console.WriteLine($"\t{++i}. {item.Host}");
                        }
                        if (int.TryParse(Console.ReadLine(), out int server))
                        {
                            try
                            {
                                Host = configs[server - 1].Host;
                                Login = configs[server - 1].Login;
                                Password = configs[server - 1].Password;
                            }
                            catch (IndexOutOfRangeException)
                            {
                                Console.WriteLine("Don't try to do this again! Bye");
                                throw;
                            }
                        }
                        Connect();
                        return;
                    }
                    break;
                default:
                    Console.Write("host: ");
                    Host = Console.ReadLine();
                    Console.Write("login: ");
                    Login = Console.ReadLine();
                    Console.Write("password: ");
                    Password = Console.ReadLine();
                    Connect();
                    break;
            }
        }

        private static void Connect()
        {
            Console.WriteLine("Connection process has been initiated");


            var sb = new StringBuilder();
            sb.AppendLine("[VPN]");
            sb.AppendLine("MEDIA=rastapi");
            sb.AppendLine("Port=VPN2-0");
            sb.AppendLine("Device=WAN Miniport (IKEv2)");
            sb.AppendLine("DEVICE=vpn");
            sb.AppendLine("PhoneNumber=" + Host);
            File.WriteAllText(Path + "\\VpnConnection.pbk", sb.ToString());

            sb = new StringBuilder();
            sb.AppendLine($"rasdial VPN {Login} {Password} /phonebook:{Path}\\VpnConnection.pbk");
            File.WriteAllText(Path + "\\VpnConnection.bat", sb.ToString());

            var connectProcess = new Process
            {
                StartInfo =
                {
                    FileName = Path + "\\VpnConnection.bat",
                    WindowStyle = ProcessWindowStyle.Normal
                }
            };

            connectProcess.Start();
            connectProcess.WaitForExit();
        }

        private static void Disconnect()
        {
            File.WriteAllText(Path + "\\VpnDisconnect.bat", "rasdial /d");

            var disconnectProcess = new Process
            {
                StartInfo =
                {
                    FileName = Path + "\\VpnDisconnect.bat",
                    WindowStyle = ProcessWindowStyle.Normal
                }
            };
            disconnectProcess.Start();
            disconnectProcess.WaitForExit();
        }

        private static string GetLocalIpAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip  in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }
            throw new Exception("No network adapters with an IPv4 address in the system!");
        }

        private static void DrawSplash()
        {
            var splash = new[]
            {
                @"                                                                           ",
                @"                         `..--:::::::::::----..``                          ",
                @"                     .-////:::::::::::::::::::::::-.`                      ",
                @"                  .:+/:::::::::::::::::::::::::::::::--`                   ",
                @"                ./+/::::::::::::::::::::::::::::::::::::-.                 ",
                @"             `.-++::::::::::::::::::::::::::::::::::::::::-``              ",
                @"            :hh++::/h:::::::::ss:hsooooso:::oh+:::::::/h::::yy/            ",
                @"            /dy+/:::ss:::::::+h::d/:::::oy::ssyo::::::/h::::+ds            ",
                @"            /dy+/::::h+:::::/h/::d/:::::/d::ss:sy:::::/h::::+ds            ",
                @"            :dy+/::::+h:::::yo:::d/::::/ss::ss::oy/:::/h::::+do            ",
                @"            -dh+/:::::ss:::oy::::dsooooo/:::ss:::/yo::/h::::od/            ",
                @"            `hd++:::::/h/:/h/::::d/:::::::::ss:::::ys:/h::::yd.                               Course Project, VSU, CSF",
                @"             sdo+::::::+y:y+:::::d/:::::::::ss::::::oy/h::::dy                                Perfilyeva Olga Ivanovna",
                @"             -dy+/::::::yys::::::d/:::::::::ss:::::::+yh:::od/                                TEST VPN SERVICE",
                @"              ydo+:::::::+:::::::+:::::::::://::::::::/+:::hh`             ",
                @"              -dy+/:::::::::::::::::::::::::::::::::::::::od/              ",
                @"               +ds+::::::::::::::::////::::::::::::::::::/ds               ",
                @"               `sdo+:::::::::::::/yhyyhhs:::::::::::::::/hy`               ",
                @"                `ydo+:::::::::::+dy+::/odh:::::::::::::/hy.                ",
                @"                 `sds+::::::::::sds+++++yd+:::::::::::+hy.                 ",
                @"                  `+hy+/::::::::sdddhyhddd+::::::::::oh+`                  ",
                @"                    -yhs/:::::::sddd:-oddd+::::::::+yy-                    ",
                @"                     `/hho/:::::sdddy/hddd+::::::+yh/`                     ",
                @"                       `+hho/:::sddddhdddd+::::+yh+`                       ",
                @"                         .+hhs/:+sssssssss/::+yy/.                         ",
                @"                           `/yhy+:::::::::/shy:`                           ",
                @"                              -ohhs/:::/oyy+-                              ",
                @"                                `:ohyyyyo-`                                ",
                @"                                    .-.                                    ",
                @"                                                                           "
            };

            foreach (var item in splash)
            {
                Console.WriteLine(item);
            }
        }

    }
}