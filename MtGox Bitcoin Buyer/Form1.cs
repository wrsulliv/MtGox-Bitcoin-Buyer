using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using OpenQA.Selenium;
using System.Threading;
using OpenQA.Selenium.Chrome;
using System.IO;
using System.Net.Mail;

namespace MtGox_Bitcoin_Buyer
{
    public partial class Form1 : Form
    {
        delegate void SetLabelText_Del(String text);
        delegate void SetIntervalLabelText_Del(Double minutes, bool up);
        IWebDriver driver;
        List<ExchangeData> exchangeData = new List<ExchangeData>();
        Double tradeFee = 0.006; //  = Trade Fee Of 0.6%
        Double numberOfBitcoins = 0.1;
        int millisecondsPerDataPoint = 5000;
        int dataPointsPerPeriod = 12;
        Double minimumProfit = 0.2;
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {

            driver = new ChromeDriver();

        }
        private bool sellBitcoins(double number)
        {
            driver.Navigate().GoToUrl("http://mtgox.com/#sell");
            if (driver.FindElement(By.Name("marketOrderCheck")).GetAttribute("checked") != "true")
            {
                driver.FindElement(By.Name("marketOrderCheck")).Click();
            }
            driver.FindElement(By.Id("sellCost")).SendKeys((number * getCurrentSellPrice()).ToString());
            System.Threading.Thread.Sleep(1000); //  Make sure the site's javascript updates
            driver.FindElement(By.CssSelector("a[class='submitBtn button buttonLongest buttonBlue popup']")).Click();
            return true;
        }
        private bool buyBitCoins(double number)
        {
            driver.Navigate().GoToUrl("http://mtgox.com/#buy");
            if (driver.FindElement(By.Name("marketOrderCheck")).GetAttribute("checked") != "true")
            {
                driver.FindElement(By.Name("marketOrderCheck")).Click();
            }
            driver.FindElement(By.Id("buyCost")).SendKeys((number * getCurrentBuyPrice()).ToString());
            System.Threading.Thread.Sleep(1000); //  Make sure the site's javascript updates
            driver.FindElement(By.CssSelector("a[class='submitBtn button buttonLongest popup']")).Click();
            return true;
        }
        private double getCurrentBuyPrice()
        {

                return Double.Parse(driver.FindElement(By.Id("buyPrice")).GetAttribute("value"));

        }
           private double getCurrentSellPrice()
        {

            return Double.Parse(driver.FindElement(By.Id("sellPrice")).GetAttribute("value"));
            

        }
        private void loginToWebsite()
        {
            driver.Navigate().GoToUrl("http://mtgox.com/");
            System.Threading.Thread.Sleep(1000);
            driver.FindElement(By.Id("username")).SendKeys("mrblotchkins");
            loadPassword(driver);
            driver.FindElement(By.Name("LOGIN")).SendKeys(OpenQA.Selenium.Keys.Down + OpenQA.Selenium.Keys.Enter);
            System.Threading.Thread.Sleep(1000);
        }
        private void loadPassword(IWebDriver driver)
        {
            driver.FindElement(By.Id("password")).SendKeys("Emilycat5589");
            

        }

        private void button1_Click(object sender, EventArgs e)
        {
            loginToWebsite();
            lbl_Price.Text = "Test";

        }

        private void button2_Click(object sender, EventArgs e)
        {
            Double d = getCurrentBuyPrice();
            System.Windows.Forms.MessageBox.Show(d.ToString());
        }

        private void button3_Click(object sender, EventArgs e)
        {
            sellBitcoins(Double.Parse(textBox1.Text));
        }

        private void button4_Click(object sender, EventArgs e)
        {
            buyBitCoins(Double.Parse(textBox2.Text));
        }


        private void SetLabelText(String text)
        {
            lbl_Price.Text = text;
        }
        private void SetIntervalLabelText(Double minutes, bool up)
        {
            lbl_Interval.Text = minutes.ToString() + "Minutes\n" + up.ToString();
        }
        private Double determineMinSellPrice(Double numOfBitcoins, Double buyPricePerBitcoin, Double desiredProfit, Double tradeFee)
        { 
           return ((desiredProfit / numOfBitcoins) + buyPricePerBitcoin)/(1 - 2*tradeFee + tradeFee*tradeFee); //  Equation to determine minimum required profit
        }
        private StockAction getMarketStatus(List<Double> priceHistory, int numberPerGroup)
        { 
            //  We will determine this by taking the most recent 5 data points, and comparing to the previous 5.
            //  This may give some sort of indication of the state of the market
            Double recent = 0;
            for(int x = 1; x <= numberPerGroup; x++)
            {
                recent += priceHistory[priceHistory.Count - x];
            }
            recent = recent / (Double)numberPerGroup;

            Double previous = 0;
            for (int x = 1; x <= numberPerGroup; x++)
            {
                previous += priceHistory[priceHistory.Count - numberPerGroup - x];
            }
            previous = previous / (Double)numberPerGroup;

            if (recent > previous) 
            {
                return  StockAction.Up;
            }
            else if (recent == previous)
            {
                return StockAction.Steady;
            }
            else
            {
                return StockAction.Down;
            }
        }
        private void showCurrentPrice()
        {
            Double minAmount = Double.Parse(textBox3.Text);
            Double currentPrice = getCurrentBuyPrice();
            SetLabelText_Del labelDel = SetLabelText;
            SetIntervalLabelText_Del intervalLabelDel = SetIntervalLabelText;
            List<Double> priceHistory = new List<double>();
            Double totalTime = 0;
            Double initialPrice = currentPrice;
            while (currentPrice < minAmount)
            {
                object[] obj = new object[1];
                obj[0] = "$" + currentPrice.ToString();
                lbl_Price.Invoke(labelDel, obj);
                System.Threading.Thread.Sleep(millisecondsPerDataPoint);
                currentPrice = getCurrentBuyPrice();
                totalTime += millisecondsPerDataPoint;
                priceHistory.Add(currentPrice);
                if (Math.Abs(currentPrice - initialPrice) >= 3)
                {
                    obj = new object[2];
                    obj[0] = (totalTime / (Double)1000) / (Double)60; // Get number of minutes for interval
                    if (currentPrice > initialPrice) //  Determine if interval was +/-
                    {
                        obj[1] = true;
                    }
                    else
                    {
                        obj[1] = false;
                    }
                    lbl_Interval.Invoke(intervalLabelDel, obj);
                    totalTime = 0;
                    initialPrice = currentPrice;
                }
            }
            System.Windows.Forms.MessageBox.Show(getCurrentBuyPrice().ToString());
        }
        private Double getMarketAverage(List<Double> priceHistory, int dataPointsPerPeriod, int numberOfPeriodsToAverage)
        {
            Double periodAverage = 0;
            for (int i = 0; i < numberOfPeriodsToAverage; i++)
            {
                Double dataPointAverage = 0;
                for (int j = 1; j <= dataPointsPerPeriod; j++)
                {
                    dataPointAverage += priceHistory[priceHistory.Count - i*dataPointsPerPeriod - j];
                }
                dataPointAverage = dataPointAverage / (Double)dataPointsPerPeriod;
                periodAverage += dataPointAverage;
            }
            periodAverage = periodAverage / (Double)numberOfPeriodsToAverage;
            return periodAverage;
        }
        private void runSystem()
        {
            loginToWebsite();

            //  Variables to hold the market history
            List<Double> priceHistory = new List<double>();
            
            //  Capture 6 initial periods
            for (int i = 0; i < (6 * dataPointsPerPeriod); i++)
            {
                System.Threading.Thread.Sleep(millisecondsPerDataPoint);
                priceHistory.Add(getCurrentBuyPrice());
                
            }

            while (1 == 1)
            {
                //  Loop until the market goes up
                while (!((getMarketStatus(priceHistory, dataPointsPerPeriod) == StockAction.Up) & (getCurrentBuyPrice() <= getMarketAverage(priceHistory, dataPointsPerPeriod, 6))))
                {
                    //  Continue capturing data points
                    for (int i = 0; i < dataPointsPerPeriod; i++)
                    {
                        System.Threading.Thread.Sleep(millisecondsPerDataPoint);
                        priceHistory.Add(getCurrentBuyPrice());
                        
                    }
                }

                //  Since the market is going up, and the price is equal or below the average, buy some bitcoins! Also start the timer
                Double MillisecondTimer = 0;
                Double initialPrice = getCurrentBuyPrice();
                Double currentPrice = initialPrice;
                try
                {
                    buyBitCoins(numberOfBitcoins);
                }
                catch (Exception ex)
                {
                    loginToWebsite();
                    buyBitCoins(numberOfBitcoins);
                }
                //  Keep capturing points until the market does down
                while (!(getMarketStatus(priceHistory, dataPointsPerPeriod) == StockAction.Down))
                {
                    for (int i = 0; i < dataPointsPerPeriod; i++)
                    {
                        System.Threading.Thread.Sleep(millisecondsPerDataPoint);
                        priceHistory.Add(getCurrentSellPrice());
                        MillisecondTimer += millisecondsPerDataPoint;
                        
                    }
               }

                //  Now that the market's shown signs of declining, just wait until the price is at least at the minimum to profit
                //  ... Probably a better algorithm for this =)
                currentPrice = priceHistory[priceHistory.Count - 1]; // Get most recent price
                while (currentPrice < determineMinSellPrice(numberOfBitcoins, initialPrice, minimumProfit, tradeFee))
                {
                    System.Threading.Thread.Sleep(millisecondsPerDataPoint);
                    currentPrice = getCurrentSellPrice();
                    priceHistory.Add(currentPrice);
                    MillisecondTimer += millisecondsPerDataPoint;
                }


                try
                {
                    sellBitcoins(getBitcoinCountAfterFee(numberOfBitcoins, tradeFee));
                }
                catch (Exception ex)
                {
                    loginToWebsite();
                    sellBitcoins(getBitcoinCountAfterFee(numberOfBitcoins, tradeFee));
                }
                Double boughtPrice = getActualBuyMoneySpent(initialPrice, numberOfBitcoins);
                Double sellPrice = getActualSellMoneyEarned(currentPrice, numberOfBitcoins, tradeFee);
                writeTextToFile("Time Sold: " + DateTime.Now.ToString());
                writeTextToFile("Elapsed Minutes: " + ((MillisecondTimer / 1000) / 60).ToString());
                writeTextToFile("Profit: " + (sellPrice - boughtPrice).ToString());
                writeTextToFile("Buy Price (Fee from BTC): " + boughtPrice.ToString());
                writeTextToFile("Actual # of BTC Bought: " + getBitcoinCountAfterFee(numberOfBitcoins, tradeFee));
                writeTextToFile("Sell Price: " + sellPrice.ToString());
                writeTextToFile("");
                exchangeData.Add(new ExchangeData(boughtPrice, sellPrice, (MillisecondTimer / 1000) / 60, sellPrice - boughtPrice)); //  TODO, make functions for real selling / buying prices / profit

            }

        }
        private void showPlot()
        {
           // Chart c = new Chart();
        }
        private void writeTextToFile(String text)
        {
           StreamWriter sw = File.AppendText("D:/BTC_TradeHistory.txt");
            sw.WriteLine(text);
            sw.Close();
        }
        private Double getActualBuyMoneySpent(Double buyPricePerBitcoin, Double numberOfBitcoins)
        {
            return (numberOfBitcoins * buyPricePerBitcoin);
        }

        private Double getActualSellMoneyEarned(Double sellPricePerBitcoin, Double numberOfBitcoinsPurchased, Double tradeFee)
        {
            Double actualBitcoinsBought = getBitcoinCountAfterFee(numberOfBitcoinsPurchased, tradeFee);
            return actualBitcoinsBought * sellPricePerBitcoin - actualBitcoinsBought * sellPricePerBitcoin * tradeFee;
        }

        private Double getBitcoinCountAfterFee(Double numberOfBitcoins, Double tradeFee)
        {
            return numberOfBitcoins - numberOfBitcoins * tradeFee;
        }
        private void button5_Click(object sender, EventArgs e)
        {
            ThreadStart ts = new ThreadStart(showCurrentPrice);
            Thread th = new Thread(showCurrentPrice);
            th.Start();

        }
        private void sendEmail()
        {
            string server = "smtp-sl.vtext.com";
            string to = "4134547707@vtext.com";
            string from = "4134547707@vtext.com";
            MailMessage message = new MailMessage(from, to);
            message.Subject = "Bitcoin Alert!";
            message.Body = "The price of bitcoins has hit " + getCurrentBuyPrice().ToString();
            SmtpClient client = new SmtpClient(server);
            client.UseDefaultCredentials = true;
            client.Send(message);
        }

        private void button6_Click(object sender, EventArgs e)
        {
            sendEmail();
        }
        private enum StockAction
        { 
           Up, Down, Steady
        }

        private void button7_Click(object sender, EventArgs e)
        {
            runSystem();
        }

        private struct ExchangeData
        {
            public Double BuyPrice;
            public Double SellPrice;
            public Double ElapsedMinutes;
            public Double Profit;

            public ExchangeData(Double buyPrice, Double sellPrice, Double elapsedMinutes, Double profit)
            {
                this.BuyPrice = buyPrice;
                this.SellPrice = sellPrice;
                this.ElapsedMinutes = elapsedMinutes;
                this.Profit = profit;
            }
        }
    }
}
