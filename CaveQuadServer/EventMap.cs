using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CaveQuadServer
{
    class EventMap
    {
        private int[,] Teleports;//{currentx,currenty,newx,newy,newlobby}

        public EventMap(int[,] m)
        {
            Teleports = m;
        }

        public int getTeleport(int x, int y)
        {
            for (int i = 0; i < Teleports.GetLength(0); i++)
            {
                if (x == Teleports[i, 0] && y == Teleports[i, 1])
                    return i;
            }
            return -1;
        }
        public int getTeleportX(int ind)
        {
            return Teleports[ind, 2];
        }
        public int getTeleportY(int ind)
        {
            return Teleports[ind, 3];
        }
        public int getTeleportLobby(int ind)
        {
            return Teleports[ind, 4];
        }
    }
}
