﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Net;
using System.Net.Sockets;


namespace BattleShipsServer
{
    class Player
    {
        public bool ShipSent = false;
        public char[,] Board = new char[10, 10];
        public List<string> Ships = new List<string>();
        public Game GameIn;
        public TcpClient client;
        public string name;

        string sentmessage;
        public void Send(string message)
        {
            try
            {
                sentmessage = $"¬{message}`";
                NetworkStream SendDataStream = client.GetStream();
                byte[] msg = System.Text.Encoding.Unicode.GetBytes(sentmessage);
                SendDataStream.Write(msg, 0, msg.Length);
                Program.Log($"Sent to {name} - {message}");
            }
            catch (Exception ex)
            {
                Program.Log($"[Error]SEND {name}-{ex.ToString()}");
                if (GameIn != null)
                {
                    GameIn.EndGame(this);
                }
            }
        }
        Game temp = null;
        public void RecieveData()
        {
            string data = "";
            NetworkStream RecieveDataStream = client.GetStream();
            byte[] bytes = new byte[256];
            int i;
            try
            {
                while (true)
                {
                    if ((i = RecieveDataStream.Read(bytes, 0, bytes.Length)) != 0)
                    {
                        string DataBunched = System.Text.Encoding.Unicode.GetString(bytes, 0, i);
                        string[] messages = DataBunched.Split('¬').Where(x => string.IsNullOrWhiteSpace(x) == false && x != "¬").ToArray();
                        foreach (var msg in messages)
                        {
                            data = msg.Substring(0, msg.IndexOf("`"));
                            Program.Log($"Recieved from {name} - {data}");
                            if (data.StartsWith("UN:"))
                            {
                                var splitlist = data.Split(':');
                                name = splitlist[1];
                            }
                            else if (data.StartsWith("NewGame:"))
                            {
                                var splitlist = data.Split(':');
                                bool uniquename = true;
                                foreach (Game g in Program.CurrentGames)
                                {
                                    if (g.Name == splitlist[1])
                                    {
                                        uniquename = false;
                                    }
                                }
                                if (uniquename == false)
                                {
                                    Send("InvalidName");
                                }
                                else
                                {
                                    Game newgame = new Game();
                                    Program.CurrentGames.Add(newgame);
                                    newgame.Name = splitlist[1];
                                    GameIn = newgame;
                                    newgame.p1 = this;
                                    Send("JoinedGame:" + newgame.Name);
                                }
                            }
                            else if (data == "CurrentGames")
                            {
                                if (Program.CurrentGames.Count != 0)
                                {
                                    string Games = "Games:";
                                    foreach (Game g in Program.CurrentGames)
                                    {
                                        if (g == Program.CurrentGames[0])
                                        {
                                            Games += g.Name;
                                        }
                                        else
                                        {
                                            Games += "," + g.Name;
                                        }
                                    }
                                    Send(Games);
                                }
                            }
                            else if (data.StartsWith("JoinGame:"))
                            {
                                var splitlist = data.Split(':');
                                foreach (Game g in Program.CurrentGames)
                                {
                                    if (g.Name == splitlist[1])
                                    {
                                        temp = g;
                                    }
                                }
                                if (temp != null)
                                {
                                    GameIn = temp;
                                    temp.p2 = this;
                                    Send("JoinedGame:" + temp.Name);
                                    temp.StartGame();
                                }
                            }
                            else if (data.StartsWith("Ships:"))
                            {
                                var SL = data.Split(':');
                                var splitlist = SL[1].Split(',');
                                char ShipNum = FindShipNumber(splitlist.Length);
                                foreach (var c in splitlist)
                                {
                                    Ships.Add($"{ShipNum}:{c}");
                                    Board[int.Parse(c[0].ToString()), int.Parse(c[1].ToString())] = ShipNum;
                                }
                            }
                            else if (data == "ShipsConfirmed")
                            {
                                PrintShips();
                                ShipSent = true;
                                GameIn.Play();
                            }
                            else if (data.StartsWith("OShip:"))
                            {
                                var SL = data.Split(':');
                                GameIn.CheckShip(SL[1], this);
                            }
                            else if (data.StartsWith("Message:"))
                            {
                                GameIn.Messaging(this, data);
                            }
                            else if (data == "LeftG")
                            {
                                GameIn.EndGame(this);
                                GameIn = null;
                            }
                        }
                    }
                }
            }catch(Exception ex)
            {
                Program.Log($"[ERROR]REC {name} - {ex}");
                if (GameIn != null)
                {
                    GameIn.EndGame(this);
                }
            }
        }
        public bool ThreeShip = false;
        private char FindShipNumber(int len)
        {
            int shipnum= -1;
            if(len == 2)
            {
                shipnum =0;
            }
            else if(len == 4)
            {
                shipnum =3;
            }else if (len == 5)
            {
                shipnum =4;
            }else if(len == 3)
            {
                if(ThreeShip == false)
                {
                    ThreeShip = true;
                    shipnum =1;
                }
                else
                {
                    ThreeShip = false;
                    shipnum =2;
                }
            }
            return Convert.ToChar(shipnum.ToString());
        }
        private void PrintShips()
        {
            for(int y = 0; y < 10; y++)
            {
                for(int x = 0; x < 10; x++)
                {
                    Console.Write(Board[x, y]);
                }
                Console.Write("\n");
            }
        }
    }
}
