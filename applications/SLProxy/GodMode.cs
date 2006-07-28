/*
 * GodMode.cs: Enables client-side God Mode privileges
 *
 * Copyright (c) 2006 John Hurliman
 * All rights reserved.
 *
 * - Redistribution and use in source and binary forms, with or without 
 *   modification, are permitted provided that the following conditions are met:
 *
 * - Redistributions of source code must retain the above copyright notice, this
 *   list of conditions and the following disclaimer.
 * - Neither the name of the Second Life Reverse Engineering Team nor the names 
 *   of its contributors may be used to endorse or promote products derived from
 *   this software without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" 
 * AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE 
 * IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE 
 * ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE 
 * LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR 
 * CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF 
 * SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS 
 * INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN 
 * CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) 
 * ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE 
 * POSSIBILITY OF SUCH DAMAGE.
 */

using System;
using System.Net;
using System.Collections;
using SLProxy;
using libsecondlife;
using Nwc.XmlRpc;

public class GodMode {
	private static ProtocolManager protocolManager;
	private static Proxy proxy;
	private static LLUUID agentID;
	private static LLUUID sessionID;

	public static void Main(string[] args) {
		protocolManager = new ProtocolManager("keywords.txt", "protocol.txt");
		ProxyConfig proxyConfig = new ProxyConfig("GodMode", "jhurliman@wsu.edu", protocolManager, args);
		proxy = new Proxy(proxyConfig);

		// register login delegate
		proxy.SetLoginResponseDelegate(new XmlRpcResponseDelegate(Login));

		// register delegates for all packets
		proxy.AddDelegate("ChatFromViewer", Direction.Outgoing, new PacketDelegate(ChatFromViewer));

		proxy.Start();
	}

	private static void Login(XmlRpcResponse response) {
		Hashtable values = (Hashtable)response.Value;
		if (values.Contains("agent_id") && values.Contains("session_id")) {
			agentID = new LLUUID((string)values["agent_id"]);
			sessionID = new LLUUID((string)values["session_id"]);
		}
	}

	// delegate for movement packet: log the packet and return it unharmed
	private static Packet ChatFromViewer(Packet packet, IPEndPoint endPoint) {
		// deconstruct the packet
		Hashtable blocks = PacketUtility.Unbuild(packet);

		// return the packet unmodified unless they said /god
		if (PacketUtility.VariableToString((byte[])PacketUtility.GetField(blocks, "ChatData", "Message")) != "/god")
			return packet;

		// construct a GrantGodlikePowers packet
		blocks = new Hashtable();
		Hashtable fields;
		fields = new Hashtable();
		fields["GodLevel"] = (byte)255;
		fields["Token"] = LLUUID.GenerateUUID();
		blocks[fields] = "GrantData";

		fields = new Hashtable();
		fields["AgentID"] = agentID;
		fields["SessionID"] = sessionID;
		blocks[fields] = "AgentData";

		Packet godPacket = PacketBuilder.BuildPacket("GrantGodlikePowers", 
			protocolManager, blocks, Helpers.MSG_RELIABLE | Helpers.MSG_ZEROCODED);

		// inject the packet
		proxy.InjectPacket(godPacket, Direction.Incoming);

		Console.WriteLine("Injected GrantGodlikePowers packet");

		// drop the packet
		return null;
	}
}