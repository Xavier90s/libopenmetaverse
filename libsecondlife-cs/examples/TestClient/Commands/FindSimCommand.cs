using System;
using System.Collections.Generic;
using System.Text;
using libsecondlife;
using libsecondlife.Packets;

namespace libsecondlife.TestClient
{
    public class FindSimCommand : Command
    {
        Dictionary<SecondLife, bool> GridDataCached = new Dictionary<SecondLife, bool>();

        public FindSimCommand(TestClient testClient)
        {
            Name = "findsim";
            Description = "Searches for a simulator and returns information about it. Usage: findsim [Simulator Name]";
        }

        public override string Execute(string[] args, LLUUID fromAgentID)
        {
            if (args.Length < 1)
                return "Usage: findsim [Simulator Name]";

            string simName = string.Empty;
            for (int i = 0; i < args.Length; i++)
                simName += args[i] + " ";
            simName = simName.TrimEnd().ToLower();

            if (!GridDataCached.ContainsKey(Client))
            {
                GridDataCached[Client] = false;
            }

            if (!GridDataCached[Client])
            {
                Client.Grid.AddAllSims();
                System.Threading.Thread.Sleep(5000);
                GridDataCached[Client] = true;
            }

            int attempts = 0;
            GridRegion region = null;
            while (region == null && attempts++ < 5)
            {
                region = Client.Grid.GetGridRegion(simName);
            }

            if (region != null)
                return "Found " + region.Name + ": handle=" + region.RegionHandle +
                    "(" + region.X + "," + region.Y + ")";
            else
                return "Lookup of " + simName + " failed";
        }
    }
}
