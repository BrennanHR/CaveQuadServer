using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace CaveQuadServer
{
    class Player
    {
        public byte id;
        public byte X;
        public byte Y;
        public byte Gem_green = 0;
        public byte Gem_blue = 0;
        public byte Gem_red = 0;
        public byte Gem_purple = 0;
        public byte Gem_black = 0;
        public int Coin;
        public string Name;
        public bool Flagged;
        public int joining;
        public string Fash;
        public bool loggedin;
        public Lobby CurrentLobby { get; set; }
        public Map CurrentMap { get; set; }
        TcpClient socket;

        public Player(TcpClient s)
        {
            socket = s;
            Name = "";
            loggedin = false;
        }
        public TcpClient GetSocket()
        {
            return socket;
        }
        public void Destroy()
        {
            socket.Close();
        }
        public void Flag()
        {
            Flagged = true;
        }
    }
}
