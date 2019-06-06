using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace CaveQuadServer
{
    class Program
    {
        static IPAddress mIP;
        static TcpListener mTCPListener;
        static List<Player> mClients;
        static List<Lobby> mLobbies;
        static MessageBuffer SBuff;
        static MessageBuffer RBuff;
        static Map quadmap;
        static Map gemmap;
        static EventMap eventmap;
        static List<string> banlist;
        static List<byte> idhandouts;
        static byte[] msBuff;
        static byte[] mrBuff;
        static int mPort;
        static int maxPlayers = 20;
        static int serverSecond = 0;
        static Timer serverTimer;
        static bool detailedExceptions;
        static bool KeepRunning { get; set; }
        //static string directory = @"C:\Users\Brennan\AppData\Roaming\CaveQuadServer\";
        static string directory = @"C:\Users\billy\AppData\Roaming\CaveQuadServer\";
        static byte spawnx = 5;
        static byte spawny = 5;
        static void Main(string[] args)
        {
            //init Collision map
            //int[,] maparr= new int[191,177];
            int[,] maparr = new int[191, 191];
            if (!File.Exists(directory + @"maps\collisionmap.txt"))
            {
                Console.WriteLine("collisionmap.txt not found... aborting...");
                return;
            }
            String input = File.ReadAllText(directory+ @"maps\collisionmap.txt");
            int i = 0, j = 0;
            foreach (var row in input.Split('\n'))
            {
                j = 0;
                foreach (var col in row.Trim().Split(' '))
                {
                    maparr[j, i] = int.Parse(col.Trim());//switched i and j orig maparr[i, j]
                    j++;
                }
                i++;
            }
            //init gem map
            int[,] gmaparr = new int[191, 191];
            Random rand = new Random();
            for (i = 0; i < 191; i++)
                for (j = 0; j < 191; j++)
                {
                    if (rand.Next(0, 16) == 0)
                        gmaparr[i, j] = 1;
                    else
                        gmaparr[i, j] = 0;
                }
            //init event map
              //{currentx,currenty,newx,newy,newlobby}
            int[,] emaparr = new int[,] { { 32,38,32,44,4},{ 32,44,32,38,0}, { 105,35,117,35,1}, { 117,35,105,35,4},
            { 88,116,88,99,5},{ 88,99,88,116,4}, { 85,141,73,141,2},{ 73,141,85,141,4},{ 152,131,152,137,3},
            { 152,137,152,131,4} };

            //finalize
            quadmap = new Map(maparr);
            gemmap = new Map(gmaparr);
            eventmap = new EventMap(emaparr);
            //init lobbies
            mLobbies = new List<Lobby>();
            for(i = 0; i < 6; i++)
            {
                mLobbies.Add(new Lobby(i.ToString()));//lobbies 0-5 in order
            }
            //init vars
            mPort = 7778;
            detailedExceptions = false;
            bool runloop = true;
            string inpt = "";
            string tInpt = "";
            int itInpt = -1;
            msBuff = new byte[1024];
            mrBuff = new byte[1024];
            mClients = new List<Player>();
            banlist = new List<string>();
            idhandouts = new List<byte>();
            for (byte ii = 0; ii < maxPlayers + 1; ii++)
                idhandouts.Add(ii);
            
            SBuff = new MessageBuffer(msBuff);
            RBuff = new MessageBuffer(mrBuff);

            //init timer
            serverTimer = new Timer();
            serverTimer.Elapsed += new ElapsedEventHandler(TickServerSecond);
            serverTimer.Interval = 1000;
            serverTimer.Enabled = true;

            Console.WriteLine("Review code for instructions or enter 'help'");
            while (runloop)
            {
                inpt = Console.ReadLine();
                switch (inpt)
                {
                    case "start":
                        Console.Clear();
                        StartListeningForIncomingConnection();
                        break;
                    case "stop":
                        StopServer();
                        break;
                    case "clear":
                        Console.Clear();
                        break;
                    case "help":
                        Console.WriteLine("start : starts the server");
                        Console.WriteLine("stop : ends the server");
                        Console.WriteLine("exit : terminiates the program");
                        Console.WriteLine("clear : clears the console");
                        Console.WriteLine("broadcast : broadcasts a message to all player chat windows");
                        Console.WriteLine("snapshot : views a player");
                        Console.WriteLine("ban : bans a player by IP");
                        Console.WriteLine("banlist : views the list of bans");
                        Console.WriteLine("seconds : server uptime");
                        Console.WriteLine("playerstats : views player account info");
                        break;
                    case "broadcast":
                        Console.WriteLine("Enter a string to broadcast, or type cancel to not broadcast anything");
                        tInpt = Console.ReadLine();
                        if (!tInpt.Equals("cancel"))
                        {
                            SBuff.SeekStart();
                            SBuff.WriteByte(1);
                            SBuff.WriteString(tInpt);
                            SendPacketAll();
                        }
                        break;
                    case "snapshot":
                        Console.WriteLine("Enter the player ID of who you want to check up on");
                        itInpt = Convert.ToInt32(Console.ReadLine());
                        if (itInpt >= 0 && itInpt < mClients.Count)
                        {
                            Player p = mClients[itInpt];
                            int radius = 20;
                            int left = Math.Max(0, p.X-radius);
                            int right = Math.Min(p.X+radius,p.CurrentMap.GetLength());
                            int up = Math.Max(0, p.Y-radius);
                            int down = Math.Min(p.Y+radius,p.CurrentMap.GetLength());
                            Console.Clear();
                            for(int y = up; y< down; y++)
                            {
                                for (int x = left; x < right; x++)
                                    if (!(x == p.X && y == p.Y))
                                        Console.Write(p.CurrentMap.GetSpot(x, y));
                                    else
                                        Console.Write(" ");
                                Console.WriteLine();
                            }
                        }
                        else
                            Console.WriteLine("Invalid client id");
                        break;
                    case "ban":
                        Console.WriteLine("Enter the player ID of who you want to ban");
                        itInpt = Convert.ToInt32(Console.ReadLine());
                        if (itInpt >= 0 && itInpt < mClients.Count)
                        {
                            Player p = mClients[itInpt];
                            banlist.Add(p.GetSocket().Client.RemoteEndPoint.ToString());
                            RemoveClient(p);
                        }
                        else
                            Console.WriteLine("Invalid client id");
                        break;
                    case "banlist":
                        for(i = 0; i < banlist.Count; i++)
                        {
                            Console.WriteLine(banlist[i]);
                        }
                        break;
                    case "seconds":
                        Console.WriteLine("The server has been up "+serverSecond.ToString()+" seconds.");
                        break;
                    case "playerstats":
                        Console.WriteLine("Enter the player ID of who you want to check up on");
                        itInpt = Convert.ToInt32(Console.ReadLine());
                        if (itInpt >= 0 && itInpt < mClients.Count)
                        {
                            Player p = mClients[itInpt];
                            Console.WriteLine(p.Name);
                            Console.WriteLine("X: "+p.X+" Y: "+p.Y);
                            Console.WriteLine("Loggedin: "+p.loggedin.ToString());
                            Console.WriteLine("Coin: "+p.Coin);
                            Console.WriteLine("RedGem: "+p.Gem_red);
                            if (p.CurrentLobby != null)
                            {
                                Console.WriteLine("Current Lobby: " + p.CurrentLobby.Name);
                            }
                            else
                                Console.WriteLine("Current Lobby is null");
                        }
                        break;
                    case "exit":
                        runloop = false;
                        break;
                }
            }
        }
        public static async void StartListeningForIncomingConnection(IPAddress ipaddr = null, int port = 7778)
        {
            if (ipaddr == null)
            {
                ipaddr = IPAddress.Any;
            }
            if (port <= 0)
            {
                port = 7778;
            }
            mIP = ipaddr;
            mPort = port;
            Console.WriteLine(string.Format("IP Address: {0} - Port: {1}", mIP.ToString(), mPort));
            mTCPListener = new TcpListener(mIP, mPort);
            try
            {
                mTCPListener.Start();
                KeepRunning = true;
                while (KeepRunning)
                {
                    var returnedByAccept = await mTCPListener.AcceptTcpClientAsync();
                    if (!banlist.Contains(returnedByAccept.Client.RemoteEndPoint.ToString()))
                    {
                        if (mClients.Count < maxPlayers)
                        {
                            Player newp = new Player(returnedByAccept);
                            mClients.Add(newp);
                            TakeCareOfTCPClient(newp);
                            Console.WriteLine(String.Format("Client connected successfully, count: {0} - {1}", mClients.Count, returnedByAccept.Client.RemoteEndPoint));
                        }
                        else
                        {
                            SBuff.SeekStart();
                            SBuff.WriteByte(5);//server is full
                            SendPacket(returnedByAccept);
                            returnedByAccept.Close();
                        }
                    }
                    else
                    {
                        SBuff.SeekStart();
                        SBuff.WriteByte(6);//banned
                        SendPacket(returnedByAccept);
                        returnedByAccept.Close();
                    }
                }
            }
            catch (Exception e)
            {
                if(detailedExceptions)
                Console.WriteLine(e.ToString());
                else
                Console.WriteLine("An exception has been caught in StartListeningForIncomingConnection()");
            }
        }
        public static void StopServer()
        {
            try
            {
                if (mTCPListener != null)
                {
                    mTCPListener.Stop();
                }
                foreach (Player c in mClients)
                {
                    c.Destroy();
                }
                mClients.Clear();
            }
            catch (Exception e)
            {
                if (detailedExceptions)
                    Console.WriteLine(e.ToString());
                else
                    Console.WriteLine("An exception has been caught in StopServer()");
            }
        }
        private static async void TakeCareOfTCPClient(Player paramClient)
        {
            NetworkStream stream = null;
            try
            {
                stream = paramClient.GetSocket().GetStream();
                while (KeepRunning)
                {
                    int nRet = await stream.ReadAsync(mrBuff, 0, mrBuff.Length);
                    if (nRet == 0)
                    {
                        RemoveClient(paramClient);
                        Console.WriteLine("Socket disconnected");
                        break;
                    }
                    RBuff.Position = 0;
                    string temp = "";
                    string temp2 = "";
                    if (paramClient.loggedin == false)
                        switch(RBuff.ReadByte())
                        {
                            case 4://logindata
                                temp = RBuff.ReadString();//username
                                temp2 = RBuff.ReadString();//hash password
                                string ppath = directory + temp + @".txt";
                                if (File.Exists(ppath))
                                {
                                    string[] tread = File.ReadAllLines(ppath);
                                    if (temp2.Equals(tread[1])) // successfull login
                                    {
                                        paramClient.Fash = temp2;
                                        paramClient.X = Convert.ToByte(Convert.ToInt32(tread[2]));
                                        paramClient.Y = Convert.ToByte(Convert.ToInt32(tread[3]));
                                        paramClient.Name = temp;
                                        paramClient.Coin = Convert.ToByte(Convert.ToInt32(tread[4]));
                                        paramClient.Gem_red = Convert.ToByte(Convert.ToInt32(tread[6]));
                                        Console.WriteLine("Successful Login for {0}", temp);
                                        paramClient.id = idhandouts[0];
                                        idhandouts.RemoveAt(0);
                                        paramClient.CurrentMap = quadmap;
                                        paramClient.joining = 0;
                                        paramClient.loggedin = true;
                                        for (int i = 0; i < mLobbies.Count; i++)
                                        {
                                            if (mLobbies[i].Name.Equals(tread[5]))
                                            {
                                                paramClient.joining = i;
                                                break;
                                            }
                                        }
                                        //okay to start
                                        SBuff.SeekStart();
                                        SBuff.WriteByte(10);
                                        SBuff.WriteByte(paramClient.X);
                                        SBuff.WriteByte(paramClient.Y);
                                        SBuff.WriteByte(paramClient.Gem_red);
                                        SendPacket(paramClient.GetSocket());
                                    }
                                    else
                                    {
                                        SBuff.SeekStart();
                                        SBuff.WriteByte(7);
                                        SendPacket(paramClient.GetSocket());
                                        Console.WriteLine("Failed Login for {0}", temp);
                                    }
                                }
                                else //create new account as current doesnt exist
                                {
                                    string[] tfill = new string[] { temp, temp2, spawnx.ToString(), spawny.ToString(), 0.ToString(), mLobbies[0].Name, 0.ToString() };
                                    File.WriteAllLines(ppath, tfill);
                                    paramClient.Fash = temp2;
                                    paramClient.X = spawnx;
                                    paramClient.Y = spawny;
                                    paramClient.Coin = 0;
                                    paramClient.Gem_red = 0;
                                    paramClient.Name = temp;
                                    Console.WriteLine("New account created: {0}", temp);
                                    paramClient.id = idhandouts[0];
                                    idhandouts.RemoveAt(0);
                                    paramClient.CurrentMap = quadmap;
                                    paramClient.joining = 0;//default lobby
                                    paramClient.loggedin = true;
                                    //okay to start and some starting info
                                    SBuff.SeekStart();
                                    SBuff.WriteByte(10);
                                    SBuff.WriteByte(spawnx);
                                    SBuff.WriteByte(spawny);
                                    SBuff.WriteByte(paramClient.Gem_red);
                                    SendPacket(paramClient.GetSocket());
                                }
                                break;
                        }
                    else
                        switch (RBuff.ReadByte())
                        {
                            case 1://joining lobby
                                EnterLobby(paramClient,paramClient.joining);
                                break;
                            case 2://player is moving
                                byte dir = RBuff.ReadByte();
                                bool movesuccess = false;
                                switch (dir) {
                                    case 1://up
                                        if (!quadmap.CheckCol(paramClient.X, paramClient.Y - 1))
                                        {
                                            paramClient.Y -= 1;
                                            movesuccess = true;
                                        }
                                        else
                                        {
                                            RefreshPlayer(paramClient);
                                            Console.WriteLine(paramClient.Name + " Tried to move illegally and has been flagged.");
                                            paramClient.Flag();
                                        }
                                        break;
                                    case 2://left
                                        if (!quadmap.CheckCol(paramClient.X - 1, paramClient.Y))
                                        {
                                            paramClient.X -= 1;
                                            movesuccess = true;
                                        }
                                        else
                                        {
                                            RefreshPlayer(paramClient);
                                            Console.WriteLine(paramClient.Name + " Tried to move illegally and has been flagged.");
                                            paramClient.Flag();
                                        }
                                        break;
                                    case 3://down
                                        if (!quadmap.CheckCol(paramClient.X, paramClient.Y + 1))
                                        {
                                            paramClient.Y += 1;
                                            movesuccess = true;
                                        }
                                        else
                                        {
                                            RefreshPlayer(paramClient);
                                            Console.WriteLine(paramClient.Name + " Tried to move illegally and has been flagged.");
                                            paramClient.Flag();
                                        }
                                        break;
                                    case 4://right
                                        if (!quadmap.CheckCol(paramClient.X + 1, paramClient.Y))
                                        {
                                            paramClient.X += 1;
                                            movesuccess = true;
                                        }
                                        else
                                        {
                                            RefreshPlayer(paramClient);
                                            Console.WriteLine(paramClient.Name + " Tried to move illegally and has been flagged.");
                                            paramClient.Flag();
                                        }
                                        break;
                                }
                                if (movesuccess)
                                {
                                    foreach(Player c in paramClient.CurrentLobby.myLobby)
                                    {
                                        if (c != paramClient)
                                        {
                                            SBuff.SeekStart();
                                            SBuff.WriteByte(8);
                                            SBuff.WriteByte(paramClient.id);
                                            SBuff.WriteByte(dir);
                                            SendPacket(c.GetSocket());
                                        }
                                    }

                                }
                                break;
                            case 3://chat
                                string txt = paramClient.Name+": "+RBuff.ReadString();
                                Console.WriteLine("Returned: " + nRet);
                                Console.WriteLine(txt);
                                Console.WriteLine(txt.Length);
                                SBuff.SeekStart();
                                SBuff.WriteByte(2);
                                SBuff.WriteString(txt);
                                SendPacketAll();
                                break;
                            case 5://event interaction

                                int telecall = eventmap.getTeleport(paramClient.X, paramClient.Y);
                                if (telecall > -1)
                                {
                                    paramClient.X = (byte)eventmap.getTeleportX(telecall);
                                    paramClient.Y = (byte)eventmap.getTeleportY(telecall);
                                    LeaveLobby(paramClient);
                                    EnterLobby(paramClient, eventmap.getTeleportLobby(telecall));
                                }

                                break;
                            case 6: //player scanning the gem map
                                if (paramClient.CurrentLobby != null)
                                {
                                    SBuff.SeekStart();
                                    SBuff.WriteByte(10);
                                    for(int xx=paramClient.X-3;xx<paramClient.X+4;xx++)
                                        for(int yy = paramClient.Y - 3; yy < paramClient.Y + 4; yy++)
                                        {
                                            SBuff.WriteByte(Convert.ToByte(gemmap.GetSpot(xx,yy)));
                                        }
                                    SendPacket(paramClient.GetSocket());
                                }
                                break;
                            case 7://player collecting a gem
                                byte tx = RBuff.ReadByte();
                                byte ty = RBuff.ReadByte();
                                if (gemmap.GetSpot(tx, ty) == 1)
                                {
                                    paramClient.Gem_red += 1;
                                    gemmap.SetSpot(tx, ty, 0);
                                    SBuff.SeekStart();
                                    SBuff.WriteByte(11);//message id
                                    SBuff.WriteByte(0);//item id
                                    SendPacket(paramClient.GetSocket());
                                }
                                break;
                        }
                    Array.Clear(mrBuff, 0, mrBuff.Length);
                    //Console.WriteLine(new string(tBuff));
                    //Array.Clear(tBuff, 0, tBuff.Length);
                }
            }
            catch (Exception excp)
            {
                RemoveClient(paramClient);
                if (detailedExceptions)
                    Console.WriteLine(excp.ToString());
                else
                    Console.WriteLine("An exception has been caught in TakeCareOfTCPClient()");
            }
        }
        private static void RemoveClient(Player paramClient)
        {
            foreach(Player c in mClients)
            {
                if (c == paramClient)
                {
                    LeaveLobby(c);
                    idhandouts.Add(c.id);
                    c.Destroy();
                    mClients.Remove(c);
                    Console.WriteLine(String.Format("Client removed, count: {0}", mClients.Count));
                    break;
                }
            }
        }
        public static async void SendPacket(TcpClient p)
        {
            try
            {
                await p.GetStream().WriteAsync(msBuff, 0, msBuff.Length);
                //await p.GetStream().WriteAsync(msBuff, 0, msBuff.Length);
            }
            catch (Exception e)
            {
                if (detailedExceptions)
                    Console.WriteLine(e.ToString());
                else
                    Console.WriteLine("An exception has been caught in SendPacket(TcpClient p)");
            }
        }
        public static async void SendPacketAll()
        {
            try
            {
                foreach (Player c in mClients)
                {
                    await c.GetSocket().GetStream().WriteAsync(msBuff, 0, msBuff.Length);
                    //c.GetSocket().GetStream().WriteAsync(msBuff, 0, msBuff.Length);
                }
            }
            catch (Exception e)
            {
                if (detailedExceptions)
                    Console.WriteLine(e.ToString());
                else
                    Console.WriteLine("An exception has been caught in SendPacketAll()");
            }
        }
        public static void RefreshPlayer(Player p)
        {
            SBuff.SeekStart();
            SBuff.WriteByte(3);
            SBuff.WriteByte(p.X);
            SBuff.WriteByte(p.Y);
            SendPacket(p.GetSocket());
        }
        public static void LeaveLobby(Player p)
        {
            SavePlayer(p);
            Lobby plobby = p.CurrentLobby;
            if (plobby != null)
            {
                plobby.RemovePlayer(p);
                byte pid = p.id;
                foreach (Player c in plobby.myLobby)
                {
                    SBuff.SeekStart();
                    SBuff.WriteByte(7);
                    SBuff.WriteByte(pid);
                    SendPacket(c.GetSocket());
                    Console.WriteLine("Told "+c.Name+" that "+p.Name+" has left the lobby.");
                }
            }
        }
        public static void EnterLobby(Player paramClient, int lobindex)
        {
            mLobbies[lobindex].AddPlayer(paramClient);
            //tell player where they should be in the lobby
            RefreshPlayer(paramClient);
            //send data about other players in the lobby
            SBuff.SeekStart();
            SBuff.WriteByte(4);//message id
            SBuff.WriteByte(Convert.ToByte(mLobbies[lobindex].myLobby.Count - 1));//number of other players
            foreach (Player p in mLobbies[lobindex].myLobby)
            {
                if (p != paramClient)
                {
                    SBuff.WriteByte(p.id);
                    SBuff.WriteByte(p.X);
                    SBuff.WriteByte(p.Y);
                    SBuff.WriteString(p.Name);
                }
            }
            SendPacket(paramClient.GetSocket());
            Console.WriteLine("Sent "+paramClient.Name+" Lobby data");
            //Tell the others there is a new player and then note it
            foreach (Player p in mLobbies[lobindex].myLobby)
            {
                if (p != paramClient)
                {
                    SBuff.SeekStart();
                    SBuff.WriteByte(9);//message id
                    SBuff.WriteByte(paramClient.id);
                    SBuff.WriteByte(paramClient.X);
                    SBuff.WriteByte(paramClient.Y);
                    SBuff.WriteString(paramClient.Name);
                    SendPacket(p.GetSocket());
                }
            }
        }
        public static void SavePlayer(Player p)
        {
            string ppath = directory + p.Name + @".txt";
            if (File.Exists(ppath))
            {
                string plob = "-1";
                if (p.CurrentLobby != null)
                    plob = p.CurrentLobby.Name;
                string[] tfill = new string[] { p.Name, p.Fash, p.X.ToString(), p.Y.ToString(), p.Coin.ToString() , plob , p.Gem_red.ToString()};
                File.WriteAllLines(ppath, tfill);
            }
            else
            {
                Console.WriteLine("Severe error. No file to write for player {0}",p.Name);
            }
        }
        public static void TickServerSecond(object source, ElapsedEventArgs e)
        {
            serverSecond += 1;
            //change price of certain gems
        }
    }
}

/*
 * tread
 * 0: Name, 1: Fash, 2: X, 3: Y, 4: Coin, 5: Lobby, 6: Gem_red
 * 
 */