using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CaveQuadServer
{
    class Map
    {
        public int[,] MyMap;

        public Map(int[,] m)
        {
            MyMap = m;
        }
        public bool CheckCol(int x, int y)
        {
            if (x > 0 && x < MyMap.Length && y > 0 && y < MyMap.Length)
                return (MyMap[x, y] == 1);
            else
                return true;
        }
        public int GetSpot(int x, int y)
        {
            if (x > 0 && x < MyMap.Length && y > 0 && y < MyMap.Length)
                return MyMap[x, y];
            else
                return 1;
        }
        public void SetSpot(int x, int y, int v)
        {
            if (x > 0 && x < MyMap.Length && y > 0 && y < MyMap.Length)
                MyMap[x, y] = v;
        }
        public int GetLength()
        {
            return MyMap.Length;
        }
        
    }
}
