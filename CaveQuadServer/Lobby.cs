using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CaveQuadServer
{
    class Lobby
    {
        public List<Player> myLobby;
        public string Name;
        public Lobby(string txt)
        {
            myLobby = new List<Player>();
            Name = txt;
        }
        public void RemovePlayer(Player p)
        {
            if (myLobby.Contains(p))
            {
                myLobby.Remove(p);
                p.CurrentLobby = null;
            }
        }
        public void AddPlayer(Player p)
        {
            p.CurrentLobby = this;
            myLobby.Add(p);
        }
    }
}
