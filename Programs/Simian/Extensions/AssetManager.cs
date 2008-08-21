using System;
using System.Collections.Generic;
using System.IO;
using OpenMetaverse;
using OpenMetaverse.Imaging;
using OpenMetaverse.Packets;

namespace Simian.Extensions
{
    public class AssetManager : ISimianExtension
    {
        Simian Server;
        Dictionary<ulong, Asset> CurrentUploads = new Dictionary<ulong, Asset>();

        public AssetManager(Simian server)
        {
            Server = server;
        }

        public void Start()
        {
            LoadDefaultAssets(Server.DataDir);

            Server.UDPServer.RegisterPacketCallback(PacketType.AssetUploadRequest, new UDPServer.PacketCallback(AssetUploadRequestHandler));
            Server.UDPServer.RegisterPacketCallback(PacketType.SendXferPacket, new UDPServer.PacketCallback(SendXferPacketHandler));
            Server.UDPServer.RegisterPacketCallback(PacketType.AbortXfer, new UDPServer.PacketCallback(AbortXferHandler));
            Server.UDPServer.RegisterPacketCallback(PacketType.TransferRequest, new UDPServer.PacketCallback(TransferRequestHandler));
        }

        public void Stop()
        {
        }

        #region Xfer System

        void AssetUploadRequestHandler(Packet packet, Agent agent)
        {
            AssetUploadRequestPacket request = (AssetUploadRequestPacket)packet;
            UUID assetID = UUID.Combine(request.AssetBlock.TransactionID, agent.SecureSessionID);

            if (request.AssetBlock.AssetData.Length != 0)
            {
                // Create a new asset from the upload
                Asset asset = CreateAsset((AssetType)request.AssetBlock.Type, assetID, request.AssetBlock.AssetData);
                if (asset == null)
                {
                    Logger.Log("Failed to create asset from uploaded data", Helpers.LogLevel.Warning);
                }

                Logger.DebugLog(String.Format("Storing uploaded asset {0} ({1})", assetID, asset.AssetType));

                // Store the asset
                lock (Server.AssetStore)
                    Server.AssetStore[assetID] = asset;

                // Send a success response
                AssetUploadCompletePacket complete = new AssetUploadCompletePacket();
                complete.AssetBlock.Success = true;
                complete.AssetBlock.Type = request.AssetBlock.Type;
                complete.AssetBlock.UUID = request.AssetBlock.TransactionID;
                agent.SendPacket(complete);
            }
            else
            {
                // Create a new asset for the upload
                Asset asset = CreateAsset((AssetType)request.AssetBlock.Type, assetID, null);
                if (asset == null)
                {
                    Logger.Log("Failed to create asset from uploaded data", Helpers.LogLevel.Warning);
                }

                Logger.DebugLog(String.Format("Starting upload for {0} ({1})", assetID, asset.AssetType));

                RequestXferPacket xfer = new RequestXferPacket();
                xfer.XferID.DeleteOnCompletion = request.AssetBlock.Tempfile;
                xfer.XferID.FilePath = 0;
                xfer.XferID.Filename = new byte[0];
                xfer.XferID.ID = request.AssetBlock.TransactionID.GetULong();
                xfer.XferID.UseBigPackets = false;
                xfer.XferID.VFileID = asset.AssetID;
                xfer.XferID.VFileType = request.AssetBlock.Type;

                // Add this asset to the current upload list
                lock (CurrentUploads)
                    CurrentUploads[xfer.XferID.ID] = asset;

                agent.SendPacket(xfer);
            }
        }

        void SendXferPacketHandler(Packet packet, Agent agent)
        {
            SendXferPacketPacket xfer = (SendXferPacketPacket)packet;

            Asset asset;
            if (CurrentUploads.TryGetValue(xfer.XferID.ID, out asset))
            {
                if (asset.AssetData == null)
                {
                    if (xfer.XferID.Packet != 0)
                    {
                        Logger.Log(String.Format("Received Xfer packet {0} before the first packet!",
                            xfer.XferID.Packet), Helpers.LogLevel.Error);
                        return;
                    }

                    uint size = Helpers.BytesToUInt(xfer.DataPacket.Data);
                    asset.AssetData = new byte[size];

                    Buffer.BlockCopy(xfer.DataPacket.Data, 4, asset.AssetData, 0, xfer.DataPacket.Data.Length - 4);

                    // Confirm the first upload packet
                    ConfirmXferPacketPacket confirm = new ConfirmXferPacketPacket();
                    confirm.XferID.ID = xfer.XferID.ID;
                    confirm.XferID.Packet = xfer.XferID.Packet;
                    agent.SendPacket(confirm);
                }
                else
                {
                    Buffer.BlockCopy(xfer.DataPacket.Data, 0, asset.AssetData, (int)xfer.XferID.Packet * 1000,
                        xfer.DataPacket.Data.Length);

                    // Confirm this upload packet
                    ConfirmXferPacketPacket confirm = new ConfirmXferPacketPacket();
                    confirm.XferID.ID = xfer.XferID.ID;
                    confirm.XferID.Packet = xfer.XferID.Packet;
                    agent.SendPacket(confirm);

                    if ((xfer.XferID.Packet & (uint)0x80000000) != 0)
                    {
                        // Asset upload finished
                        Logger.DebugLog("Completed asset upload");

                        lock (CurrentUploads)
                            CurrentUploads.Remove(xfer.XferID.ID);

                        lock (Server.AssetStore)
                            Server.AssetStore[asset.AssetID] = asset;

                        AssetUploadCompletePacket complete = new AssetUploadCompletePacket();
                        complete.AssetBlock.Success = true;
                        complete.AssetBlock.Type = (sbyte)asset.AssetType;
                        complete.AssetBlock.UUID = asset.AssetID;
                        agent.SendPacket(complete);
                    }
                }
            }
            else
            {
                Logger.DebugLog("Received a SendXferPacket for an unknown upload");
            }
        }

        void AbortXferHandler(Packet packet, Agent agent)
        {
            AbortXferPacket abort = (AbortXferPacket)packet;
            
            lock (CurrentUploads)
            {
                if (CurrentUploads.ContainsKey(abort.XferID.ID))
                {
                    Logger.DebugLog(String.Format("Aborting Xfer {0}, result: {1}", abort.XferID.ID,
                        (TransferError)abort.XferID.Result));

                    CurrentUploads.Remove(abort.XferID.ID);
                }
                else
                {
                    Logger.DebugLog(String.Format("Received an AbortXfer for an unknown xfer {0}",
                        abort.XferID.ID));
                }
            }
        }

        #endregion Xfer System

        #region Transfer System

        void TransferRequestHandler(Packet packet, Agent agent)
        {
            TransferRequestPacket request = (TransferRequestPacket)packet;

            ChannelType channel = (ChannelType)request.TransferInfo.ChannelType;
            SourceType source = (SourceType)request.TransferInfo.SourceType;

            if (channel == ChannelType.Asset)
            {
                // Construct the response packet
                TransferInfoPacket response = new TransferInfoPacket();
                response.TransferInfo = new TransferInfoPacket.TransferInfoBlock();
                response.TransferInfo.TransferID = request.TransferInfo.TransferID;

                if (source == SourceType.Asset)
                {
                    // Parse the request
                    UUID assetID = new UUID(request.TransferInfo.Params, 0);
                    AssetType type = (AssetType)(sbyte)Helpers.BytesToInt(request.TransferInfo.Params, 16);

                    // Set the response channel type
                    response.TransferInfo.ChannelType = (int)ChannelType.Asset;

                    // Params
                    response.TransferInfo.Params = new byte[20];
                    Buffer.BlockCopy(assetID.GetBytes(), 0, response.TransferInfo.Params, 0, 16);
                    Buffer.BlockCopy(Helpers.IntToBytes((int)type), 0, response.TransferInfo.Params, 16, 4);

                    // Check if we have this asset
                    Asset asset;
                    if (Server.AssetStore.TryGetValue(assetID, out asset))
                    {
                        if (asset.AssetType == type)
                        {
                            Logger.DebugLog(String.Format("Transferring asset {0} ({1})", asset.AssetID, asset.AssetType));

                            // Asset found
                            response.TransferInfo.Size = asset.AssetData.Length;
                            response.TransferInfo.Status = (int)StatusCode.OK;
                            response.TransferInfo.TargetType = (int)TargetType.Unknown; // Doesn't seem to be used by the client

                            agent.SendPacket(response);

                            // Transfer system does not wait for ACKs, just sends all of the
                            // packets for this transfer out
                            const int MAX_CHUNK_SIZE = Settings.MAX_PACKET_SIZE - 100;
                            int processedLength = 0;
                            int packetNum = 0;
                            while (processedLength < asset.AssetData.Length)
                            {
                                TransferPacketPacket transfer = new TransferPacketPacket();
                                transfer.TransferData.ChannelType = (int)ChannelType.Asset;
                                transfer.TransferData.TransferID = request.TransferInfo.TransferID;
                                transfer.TransferData.Packet = packetNum++;

                                int chunkSize = Math.Min(asset.AssetData.Length - processedLength, MAX_CHUNK_SIZE);
                                transfer.TransferData.Data = new byte[chunkSize];
                                Buffer.BlockCopy(asset.AssetData, processedLength, transfer.TransferData.Data, 0, chunkSize);
                                processedLength += chunkSize;

                                if (processedLength >= asset.AssetData.Length)
                                    transfer.TransferData.Status = (int)StatusCode.Done;
                                else
                                    transfer.TransferData.Status = (int)StatusCode.OK;

                                agent.SendPacket(transfer);
                            }
                        }
                        else
                        {
                            Logger.Log(String.Format(
                                "Request for asset {0} with type {1} does not match actual asset type {2}",
                                asset.AssetID, type, asset.AssetType), Helpers.LogLevel.Warning);
                        }
                    }
                    else
                    {
                        // Asset not found
                        response.TransferInfo.Size = 0;
                        response.TransferInfo.Status = (int)StatusCode.UnknownSource;
                        response.TransferInfo.TargetType = (int)TargetType.Unknown;

                        agent.SendPacket(response);
                    }
                }
                else if (source == SourceType.SimEstate)
                {
                    UUID agentID = new UUID(request.TransferInfo.Params, 0);
                    UUID sessionID = new UUID(request.TransferInfo.Params, 16);
                    EstateAssetType type = (EstateAssetType)Helpers.BytesToInt(request.TransferInfo.Params, 32);

                    Logger.Log("Please implement estate asset transfers", Helpers.LogLevel.Warning);
                }
                else if (source == SourceType.SimInventoryItem)
                {
                    UUID agentID = new UUID(request.TransferInfo.Params, 0);
                    UUID sessionID = new UUID(request.TransferInfo.Params, 16);
                    UUID ownerID = new UUID(request.TransferInfo.Params, 32);
                    UUID taskID = new UUID(request.TransferInfo.Params, 48);
                    UUID itemID = new UUID(request.TransferInfo.Params, 64);
                    UUID assetID = new UUID(request.TransferInfo.Params, 80);
                    AssetType type = (AssetType)(sbyte)Helpers.BytesToInt(request.TransferInfo.Params, 96);

                    if (taskID != UUID.Zero)
                    {
                        // Task (prim) inventory request
                        Logger.Log("Please implement task inventory transfers", Helpers.LogLevel.Warning);
                    }
                    else
                    {
                        // Agent inventory request
                        Logger.Log("Please implement agent inventory transfer", Helpers.LogLevel.Warning);
                    }
                }
                else
                {
                    Logger.Log(String.Format(
                        "Received a TransferRequest that we don't know how to handle. Channel: {0}, Source: {1}",
                        channel, source), Helpers.LogLevel.Warning);
                }
            }
            else
            {
                Logger.Log(String.Format(
                    "Received a TransferRequest that we don't know how to handle. Channel: {0}, Source: {1}",
                    channel, source), Helpers.LogLevel.Warning);
            }
        }

        #endregion Transfer System

        Asset CreateAsset(AssetType type, UUID assetID, byte[] data)
        {
            switch (type)
            {
                case AssetType.Bodypart:
                    return new AssetBodypart(assetID, data);
                case AssetType.Clothing:
                    return new AssetClothing(assetID, data);
                case AssetType.LSLBytecode:
                    return new AssetScriptBinary(assetID, data);
                case AssetType.LSLText:
                    return new AssetScriptText(assetID, data);
                case AssetType.Notecard:
                    return new AssetNotecard(assetID, data);
                case AssetType.Texture:
                    return new AssetTexture(assetID, data);
                case AssetType.Animation:
                case AssetType.CallingCard:
                case AssetType.Folder:
                case AssetType.Gesture:
                case AssetType.ImageJPEG:
                case AssetType.ImageTGA:
                case AssetType.Landmark:
                case AssetType.LostAndFoundFolder:
                case AssetType.Object:
                case AssetType.RootFolder:
                case AssetType.Simstate:
                case AssetType.SnapshotFolder:
                case AssetType.Sound:
                case AssetType.SoundWAV:
                case AssetType.TextureTGA:
                case AssetType.TrashFolder:
                case AssetType.Unknown:
                default:
                    Logger.Log("Asset type " + type.ToString() + " not implemented!", Helpers.LogLevel.Warning);
                    return null;
            }
        }

        void LoadDefaultAssets(string path)
        {
            string[] textures = Directory.GetFiles(path, "*.jp2", SearchOption.TopDirectoryOnly);
            string[] clothing = Directory.GetFiles(path, "*.clothing", SearchOption.TopDirectoryOnly);
            string[] bodyparts = Directory.GetFiles(path, "*.bodypart", SearchOption.TopDirectoryOnly);

            for (int i = 0; i < textures.Length; i++)
            {
                UUID assetID = ParseUUIDFromFilename(textures[i]);
                AssetTexture item = new AssetTexture(assetID, File.ReadAllBytes(textures[i]));
                Server.AssetStore[assetID] = item;
            }

            for (int i = 0; i < clothing.Length; i++)
            {
                UUID assetID = ParseUUIDFromFilename(clothing[i]);
                AssetClothing item = new AssetClothing(assetID, File.ReadAllBytes(clothing[i]));
                item.Decode();
                Server.AssetStore[assetID] = item;
            }

            for (int i = 0; i < bodyparts.Length; i++)
            {
                UUID assetID = ParseUUIDFromFilename(bodyparts[i]);
                AssetBodypart item = new AssetBodypart(assetID, File.ReadAllBytes(bodyparts[i]));
                item.Decode();
                Server.AssetStore[assetID] = item;
            }
        }

        static UUID ParseUUIDFromFilename(string filename)
        {
            int dot = filename.LastIndexOf('.');

            if (dot > 35)
            {
                // Grab the last 36 characters of the filename
                string uuidString = filename.Substring(dot - 36, 36);
                UUID uuid;
                UUID.TryParse(uuidString, out uuid);
                return uuid;
            }
            else
            {
                return UUID.Zero;
            }
        }
    }
}
