﻿namespace ReliableUdp.Const
{
	public static class Mtu
	{
		public static readonly int[] PossibleValues =
		{
				576 - HeaderSize.MAX_UDP,  //Internet Path MTU for X.25 (RFC 879)
            1492 - HeaderSize.MAX_UDP, //Ethernet with LLC and SNAP, PPPoE (RFC 1042)
            1500 - HeaderSize.MAX_UDP, //Ethernet II (RFC 1191)
            4352 - HeaderSize.MAX_UDP, //FDDI
            4464 - HeaderSize.MAX_UDP, //Token ring
            7981 - HeaderSize.MAX_UDP  //WLAN
        };

		public static int MaxPacketSize = PossibleValues[PossibleValues.Length - 1];
	}
}
