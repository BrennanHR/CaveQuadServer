using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CaveQuadServer
{
    class Market
    {
        private int price_green;
        private int price_blue;
        private int price_red;
        private int price_purple;
        private int price_black;
        private Random rand = new Random();

        public Market()
        {
            price_green = rand.Next(1, 5);//4
            price_blue = rand.Next(5, 15);//10
            price_red = rand.Next(15, 30);//15
            price_purple = rand.Next(70, 110);//40
            price_black = rand.Next(900, 1000);//100
        }
        public void MarketShift()
        {
            price_green = rand.Next(1, 5);//4
            price_blue = rand.Next(5, 15);//10
            price_red = rand.Next(15, 30);//15
            price_purple = rand.Next(70, 110);//40
            price_black = rand.Next(900, 1000);//100
        }
        public String getMarket()
        {
            String ret = "green:" + price_green + "\nblue:" + price_blue + "\nred:" + price_red + "\npurple:" + price_purple + "\nblack:" + price_black;
            return ret;
        }
    }
}
