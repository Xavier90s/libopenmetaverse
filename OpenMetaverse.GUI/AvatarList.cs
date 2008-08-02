/*
 * Copyright (c) 2007-2008, openmetaverse.org
 * All rights reserved.
 *
 * - Redistribution and use in source and binary forms, with or without 
 *   modification, are permitted provided that the following conditions are met:
 *
 * - Redistributions of source code must retain the above copyright notice, this
 *   list of conditions and the following disclaimer.
 * - Neither the name of the openmetaverse.org nor the names 
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

using OpenMetaverse;
using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace OpenMetaverse.GUI
{
    public class AvatarList : ListView
    {
        private GridClient _Client;
        private List<uint> _Avatars = new List<uint>();

        public delegate void AvatarDoubleClickCallback(Avatar avatar);
        public event AvatarDoubleClickCallback OnAvatarDoubleClick;

        /// <summary>
        /// Gets or sets the GridClient associated with this control
        /// </summary>
        public GridClient Client
        {
            get { return _Client; }
            set { if (value != null) InitializeClient(value); }
        }

        /// <summary>
        /// TreeView control for an unspecified client's nearby avatar list
        /// </summary>
        public AvatarList()
        {
            ColumnHeader header1 = this.Columns.Add("Name");
            header1.Width = this.Width - 20;

            ColumnHeader header2 = this.Columns.Add(" ");
            header2.Width = 40;

            this.View = View.Details;
            this.DoubleClick += new EventHandler(AvatarList_DoubleClick);
        }

        /// <summary>
        /// TreeView control for the specified client's nearby avatar list
        /// </summary>
        /// <param name="client"></param>
        public AvatarList(GridClient client)
        {
            InitializeClient(client);
            new InventoryTree();
        }

        /// <summary>
        /// Thread-safe method for clearing the TreeView control
        /// </summary>
        public void ClearNodes()
        {
            if (this.InvokeRequired) this.BeginInvoke((MethodInvoker)delegate { ClearNodes(); });
            else this.Items.Clear();
        }

        private void InitializeClient(GridClient client)
        {
            _Client = client;
            _Client.Network.OnCurrentSimChanged += new NetworkManager.CurrentSimChangedCallback(Network_OnCurrentSimChanged);
            _Client.Objects.OnNewAvatar += new ObjectManager.NewAvatarCallback(Objects_OnNewAvatar);
            _Client.Objects.OnObjectKilled += new ObjectManager.KillObjectCallback(Objects_OnObjectKilled);
            _Client.Objects.OnObjectUpdated += new ObjectManager.ObjectUpdatedCallback(Objects_OnObjectUpdated);
        }

        void Objects_OnObjectUpdated(Simulator simulator, ObjectUpdate update, ulong regionHandle, ushort timeDilation)
        {
            lock(_Avatars)
            {
                if (_Avatars.Contains(update.LocalID))
                {
                    Avatar av;
                    if (simulator.ObjectsAvatars.TryGetValue(update.LocalID, out av))
                        UpdateAvatar(av);
                }
            }
        }

        private void UpdateAvatar(Avatar avatar)
        {
            if (this.InvokeRequired) this.BeginInvoke((MethodInvoker)delegate { UpdateAvatar(avatar); });
            else
            {
                lock (_Avatars)
                {
                    ListViewItem item;
                    if (_Avatars.Contains(avatar.LocalID))
                    {
                        item = this.Items[avatar.LocalID.ToString()];
                        item.SubItems[1].Text = (int)Vector3.Dist(_Client.Self.SimPosition, avatar.Position) + "m";
                    }
                    else
                    {
                        _Avatars.Add(avatar.LocalID);
                        string key = avatar.LocalID.ToString();
                        item = this.Items.Add(key, avatar.Name, null);
                        item.SubItems.Add((int)Vector3.Dist(_Client.Self.SimPosition, avatar.Position) + "m");
                    }
                    item.Tag = avatar;
                }
            }
        }

        private void RemoveAvatar(uint localID)
        {
            if (this.InvokeRequired) this.BeginInvoke((MethodInvoker)delegate { RemoveAvatar(localID); });
            else
            {
                lock (_Avatars)
                {
                    if (_Avatars.Contains(localID))
                    {
                        _Avatars.Remove(localID);
                        string key = localID.ToString();
                        int index = this.Items.IndexOfKey(key);
                        if (index > -1) this.Items.RemoveAt(index);
                    }
                }
            }
        }

        private void AvatarList_DoubleClick(object sender, EventArgs e)
        {
            ListView list = (ListView)sender;
            if (list.SelectedItems.Count > 0)
            {
                if (list.SelectedItems[0].Tag is Avatar)
                {
                    Avatar av = (Avatar)list.SelectedItems[0].Tag;
                    if (OnAvatarDoubleClick != null)
                    {
                        try { OnAvatarDoubleClick(av); }
                        catch (Exception ex) { Logger.Log(ex.Message, Helpers.LogLevel.Error, Client, ex); }
                    }
                }
            }
        }

        private void Network_OnCurrentSimChanged(Simulator PreviousSimulator)
        {
            lock (_Avatars) _Avatars.Clear();
            ClearNodes();
        }

        private void Objects_OnNewAvatar(Simulator simulator, Avatar avatar, ulong regionHandle, ushort timeDilation)
        {
            lock (_Avatars)
            {
                if (!_Avatars.Contains(avatar.LocalID)) UpdateAvatar(avatar);
            }
        }

        private void Objects_OnObjectKilled(Simulator simulator, uint objectID)
        {
            lock (_Avatars)
            {
                if (_Avatars.Contains(objectID)) RemoveAvatar(objectID);
            }
        }

    }

}
