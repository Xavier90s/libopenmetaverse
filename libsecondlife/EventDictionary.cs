using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using libsecondlife.Packets;

namespace libsecondlife
{
    /// <summary>
    /// Registers, unregisters, and fires events generated by incoming packets
    /// </summary>
    public class PacketEventDictionary
    {
        /// <summary>
        /// Object that is passed to worker threads in the ThreadPool for
        /// firing packet callbacks
        /// </summary>
        private struct PacketCallbackWrapper
        {
            /// <summary>Callback to fire for this packet</summary>
            public NetworkManager.PacketCallback Callback;
            /// <summary>Reference to the simulator that this packet came from</summary>
            public Simulator Simulator;
            /// <summary>The packet that needs to be processed</summary>
            public Packet Packet;
        }

        /// <summary>Reference to the SecondLife client</summary>
        public SecondLife Client;

        private Dictionary<PacketType, NetworkManager.PacketCallback> _EventTable = 
            new Dictionary<PacketType,NetworkManager.PacketCallback>();
        private WaitCallback _ThreadPoolCallback;

        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="client"></param>
        public PacketEventDictionary(SecondLife client)
        {
            Client = client;
            _ThreadPoolCallback = new WaitCallback(ThreadPoolDelegate);
        }

        /// <summary>
        /// Register an event handler
        /// </summary>
        /// <remarks>Use PacketType.Default to fire this event on every 
        /// incoming packet</remarks>
        /// <param name="packetType">Packet type to register the handler for</param>
        /// <param name="eventHandler">Callback to be fired</param>
        public void RegisterEvent(PacketType packetType, NetworkManager.PacketCallback eventHandler)
        {
            lock (_EventTable)
            {
                if (_EventTable.ContainsKey(packetType))
                    _EventTable[packetType] += eventHandler;
                else
                    _EventTable[packetType] = eventHandler;
            }
        }

        /// <summary>
        /// Unregister an event handler
        /// </summary>
        /// <param name="packetType">Packet type to unregister the handler for</param>
        /// <param name="eventHandler">Callback to be unregistered</param>
        public void UnregisterEvent(PacketType packetType, NetworkManager.PacketCallback eventHandler)
        {
            lock (_EventTable)
            {
                if (_EventTable.ContainsKey(packetType) && _EventTable[packetType] != null)
                    _EventTable[packetType] -= eventHandler;
            }
        }

        /// <summary>
        /// Fire the events registered for this packet type synchronously
        /// </summary>
        /// <param name="packetType">Incoming packet type</param>
        /// <param name="packet">Incoming packet</param>
        /// <param name="simulator">Simulator this packet was received from</param>
        internal void RaiseEvent(PacketType packetType, Packet packet, Simulator simulator)
        {
            NetworkManager.PacketCallback callback;

            if (_EventTable.TryGetValue(packetType, out callback))
            {
                try
                {
                    callback(packet, simulator);
                }
                catch (Exception ex)
                {
                    Client.Log("Packet Event Handler: " + ex.ToString(), Helpers.LogLevel.Error);
                }
            }
            else if (packetType != PacketType.Default && packetType != PacketType.PacketAck)
            {
                Client.Log("No handler registered for packet event " + packetType, Helpers.LogLevel.Debug);
            }
        }

        /// <summary>
        /// Fire the events registered for this packet type asynchronously
        /// </summary>
        /// <param name="packetType">Incoming packet type</param>
        /// <param name="packet">Incoming packet</param>
        /// <param name="simulator">Simulator this packet was received from</param>
        internal void BeginRaiseEvent(PacketType packetType, Packet packet, Simulator simulator)
        {
            NetworkManager.PacketCallback callback;

            if (_EventTable.TryGetValue(packetType, out callback))
            {
                if (callback != null)
                {
                    PacketCallbackWrapper wrapper;
                    wrapper.Callback = callback;
                    wrapper.Packet = packet;
                    wrapper.Simulator = simulator;
                    ThreadPool.QueueUserWorkItem(_ThreadPoolCallback, wrapper);

                    return;
                }
            }

            if (packetType != PacketType.Default && packetType != PacketType.PacketAck)
            {
                Client.Log("No handler registered for packet event " + packetType, Helpers.LogLevel.Debug);
            }
        }

        private void ThreadPoolDelegate(Object state)
        {
            PacketCallbackWrapper wrapper = (PacketCallbackWrapper)state;

            try
            {
                wrapper.Callback(wrapper.Packet, wrapper.Simulator);
            }
            catch (Exception ex)
            {
                Client.Log("Async Packet Event Handler: " + ex.ToString(), Helpers.LogLevel.Error);
            }
        }
    }

    /// <summary>
    /// Registers, unregisters, and fires events generated by the Capabilities
    /// event queue
    /// </summary>
    public class CapsEventDictionary
    {
        /// <summary>
        /// Object that is passed to worker threads in the ThreadPool for
        /// firing CAPS callbacks
        /// </summary>
        private struct CapsCallbackWrapper
        {
            /// <summary>Callback to fire for this packet</summary>
            public Capabilities.EventQueueCallback Callback;
            /// <summary>Name of the CAPS event</summary>
            public string CapsEvent;
            /// <summary>Decoded body of the CAPS event</summary>
            public System.Collections.Hashtable Body;
            /// <summary>Reference to the event queue that generated this event</summary>
            public CapsEventQueue EventQueue;
        }

        /// <summary>Reference to the SecondLife client</summary>
        public SecondLife Client;

        private Dictionary<string, Capabilities.EventQueueCallback> _EventTable = 
            new Dictionary<string, Capabilities.EventQueueCallback>();
        private WaitCallback _ThreadPoolCallback;

        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="client">Reference to the SecondLife client</param>
        public CapsEventDictionary(SecondLife client)
        {
            Client = client;
            _ThreadPoolCallback = new WaitCallback(ThreadPoolDelegate);
        }

        /// <summary>
        /// Register an event handler
        /// </summary>
        /// <remarks>Use String.Empty to fire this event on every CAPS event</remarks>
        /// <param name="capsEvent">Capability event name to register the 
        /// handler for</param>
        /// <param name="eventHandler">Callback to fire</param>
        public void RegisterEvent(string capsEvent, Capabilities.EventQueueCallback eventHandler)
        {
            lock (_EventTable)
            {
                if (_EventTable.ContainsKey(capsEvent))
                    _EventTable[capsEvent] += eventHandler;
                else
                    _EventTable[capsEvent] = eventHandler;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="capsEvent">Capability event name unregister the 
        /// handler for</param>
        /// <param name="eventHandler">Callback to unregister</param>
        public void UnregisterEvent(string capsEvent, Capabilities.EventQueueCallback eventHandler)
        {
            lock (_EventTable)
            {
                if (_EventTable.ContainsKey(capsEvent) && _EventTable[capsEvent] != null)
                    _EventTable[capsEvent] -= eventHandler;
            }
        }

        /// <summary>
        /// Fire the events registered for this event type synchronously
        /// </summary>
        /// <param name="capsEvent">Capability name, or String.Empty for a
        /// default handler</param>
        /// <param name="eventName">Actual capability name</param>
        /// <param name="body">Decoded event body</param>
        /// <param name="eventQueue">Reference to the event queue that 
        /// generated this event</param>
        internal void RaiseEvent(string capsEvent, string eventName, System.Collections.Hashtable body, CapsEventQueue eventQueue)
        {
            Capabilities.EventQueueCallback callback;

            // Explicit handlers first
            if (_EventTable.TryGetValue(capsEvent, out callback))
            {
                if (callback != null)
                {
                    try
                    {
                        callback(eventName, body, eventQueue);
                    }
                    catch (Exception ex)
                    {
                        Client.Log("CAPS Event Handler: " + ex.ToString(), Helpers.LogLevel.Error);
                    }

                    return;
                }
            }

            if (capsEvent != String.Empty)
            {
                // Generic handler second
                Packet packet = Packet.BuildPacket(capsEvent, body);
                if (packet != null)
                {
                    NetworkManager.IncomingPacket incomingPacket;
                    incomingPacket.Simulator = eventQueue.Simulator;
                    incomingPacket.Packet = packet;

                    Client.Network.PacketInbox.Enqueue(incomingPacket);
                }
                else
                {
                    Client.Log("No handler registered for CAPS event " + capsEvent, Helpers.LogLevel.Debug);
                }
            }
        }

        /// <summary>
        /// Fire the events registered for this event type asynchronously
        /// </summary>
        /// <param name="capsEvent">Capability name, or String.Empty for a
        /// default handler</param>
        /// <param name="eventName">Actual capability name</param>
        /// <param name="body">Decoded event body</param>
        /// <param name="eventQueue">Reference to the event queue that 
        /// generated this event</param>
        internal void BeginRaiseEvent(string capsEvent, string eventName, System.Collections.Hashtable body, CapsEventQueue eventQueue)
        {
            Capabilities.EventQueueCallback callback;

            // Explicit handlers first
            if (_EventTable.TryGetValue(capsEvent, out callback))
            {
                if (callback != null)
                {
                    CapsCallbackWrapper wrapper;
                    wrapper.Callback = callback;
                    wrapper.CapsEvent = eventName;
                    wrapper.Body = body;
                    wrapper.EventQueue = eventQueue;
                    ThreadPool.QueueUserWorkItem(_ThreadPoolCallback, wrapper);

                    return;
                }
            }

            if (capsEvent != String.Empty)
            {
                // Generic handler second
                Packet packet = Packet.BuildPacket(capsEvent, body);
                if (packet != null)
                {
                    NetworkManager.IncomingPacket incomingPacket;
                    incomingPacket.Simulator = eventQueue.Simulator;
                    incomingPacket.Packet = packet;

                    Client.Network.PacketInbox.Enqueue(incomingPacket);
                }
                else
                {
                    Client.Log("No handler registered for CAPS event " + capsEvent, Helpers.LogLevel.Debug);
                }
            }
        }

        private void ThreadPoolDelegate(Object state)
        {
            CapsCallbackWrapper wrapper = (CapsCallbackWrapper)state;

            try
            {
                wrapper.Callback(wrapper.CapsEvent, wrapper.Body, wrapper.EventQueue);
            }
            catch (Exception ex)
            {
                Client.Log("Async CAPS Event Handler: " + ex.ToString(), Helpers.LogLevel.Error);
            }
        }
    }
}
