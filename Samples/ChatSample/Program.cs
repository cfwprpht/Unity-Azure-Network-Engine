﻿namespace ChatSample
{
	using System;
	using System.Collections.Generic;
	using System.Runtime.CompilerServices;

	using Protocol;

	using ReliableUdp;
	using ReliableUdp.Enums;

	using Utility;

	using FactoryRegistrations = ReliableUdp.FactoryRegistrations;

	public static class Program
	{
		public static Dictionary<long, string> Names = new Dictionary<long, string>();

		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		public static void Main(string[] args)
		{
			if (args.Length == 0)
			{
				ShowHelp();
				return;
			}

			var settings = new ChatSettings();

			try
			{
				for (int i = 0; i < args.Length; i++)
				{
					string argumentTrimmed = args[i].Trim();

					switch (argumentTrimmed)
					{
						case "-s":
							settings.IsServer = true;
							break;
						case "-c":
							settings.IsServer = false;
							i++;
							settings.Host = args[i].Trim();
							break;
						case "-p":
							i++;
							settings.Port = int.Parse(args[i].Trim());
							break;
					}
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine("Error: " + ex);
				ShowHelp();
				return;
			}

			isRunning = Initialize(settings);

			while (isRunning)
			{
				string result = Console.ReadLine();

				result = result.Trim();
				if (result.ToLower() == "exit")
				{
					isRunning = false;
					break;
				}

				if (!settings.IsServer)
				{
					if(string.IsNullOrEmpty(result))
						continue;

					if (result.StartsWith("/nick "))
					{
						var message = new ClientChangeNick();
						message.NewNick = result.Replace("/nick ", String.Empty).Trim();
						listener.UdpManager.SendToAll(message, ChannelType.ReliableOrdered);
					}
					else if (result.StartsWith("/w "))
					{
						string toParse = result.Replace("/w ", string.Empty).Trim();
						var splittetResult = toParse.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
						if (splittetResult.Length != 2)
						{
							Console.WriteLine("Whisper Failure e.g. /w KDSBest Hi! How are you?");
							continue;
						}
						var message = new ClientWhisper();
						message.Receiver = splittetResult[0];
						message.Message.Message.Add(splittetResult[1]);
						listener.UdpManager.SendToAll(message, ChannelType.ReliableOrdered);
					}
					else
					{
						var message = new ClientChatMessage();
						message.Message.Add(result);
						listener.UdpManager.SendToAll(message, ChannelType.ReliableOrdered);
					}
				}
			}

			if (thread != null)
				thread.Stop();

			Console.WriteLine("Stopped Execution.");
		}

		private static bool isRunning = true;

		private static IUdpEventListener listener;

		private static bool isConnected = false;

		private static UdpThread thread;

		private static bool Initialize(ChatSettings settings)
		{
			FactoryRegistrations.Register();
			Protocol.FactoryRegistrations.Register();

			UdpManager manager;
			if (settings.IsServer)
			{
				listener = new ServerListener();

				manager = new UdpManager(listener, "chat");
				if (!manager.Start(settings.Port))
				{
					Console.WriteLine("Server start failed");
					return false;
				}
			}
			else
			{
				bool connecting = true;
				var clListener = new ClientListener((connectingResult) =>
															 {
																 isConnected = connectingResult;
																 connecting = false;
															 });
				clListener.HandleChatMessage = (message, peer, arg3) =>
					{
						string name = message.Sender.ToString();
						if (Names.ContainsKey(message.Sender))
							name = Names[message.Sender];
						Console.Write(name + ": ");
						foreach (string line in message.Message)
						{
							Console.WriteLine(line);
						}
					};
				clListener.HandleWhisperMessage = (message, peer, arg3) =>
				{
					string name = message.Sender.ToString();
					if (Names.ContainsKey(message.Sender))
						name = Names[message.Sender];
					Console.Write("[Whisper] " + name + ": ");
					foreach (string line in message.Message.Message)
					{
						Console.WriteLine(line);
					}
				};
				clListener.HandleNickMessage = (message, peer, arg3) =>
					{
						if (string.IsNullOrEmpty(message.NewNick))
							return;

						if (Names.ContainsKey(message.Sender))
							Names[message.Sender] = message.NewNick;
						else
							Names.Add(message.Sender, message.NewNick);
					};
				listener = clListener;
				manager = new UdpManager(listener, "chat");
				manager.Connect(settings.Host, settings.Port);
				Console.WriteLine("Connecting...");

				while (connecting)
				{
					manager.PollEvents();
					System.Threading.Thread.Sleep(100);
				}
			}

			thread = new UdpThread("Pool Events", 100,
										 () =>
											 {
												 manager.PollEvents();
											 });
			thread.Start();

			return true;
		}

		private static void ShowHelp()
		{
			Console.WriteLine("Server example: -s -p 2222");
			Console.WriteLine("Client example: -c localhost -p 2222");
		}
	}
}
