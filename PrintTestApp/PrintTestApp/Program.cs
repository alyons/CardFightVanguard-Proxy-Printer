using System;
using System.Net;
using System.IO;
using System.Drawing;
using System.Drawing.Printing;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CommandLine;
using CommandLine.Text;

using Newtonsoft.Json;

namespace PrintTestApp
{
    class Options
    {
        [Option('f', "file", HelpText="Input file to read.")]
        public string InputFile { get; set; }

        [Option("defaultPrinter", DefaultValue = true, HelpText ="Use the default printer.")]
        public bool DefaultPrinter { get; set; }

        [Option("print", DefaultValue = true, HelpText ="Print the file.")]
        public bool Print { get; set; }
    }

    class Program
    {
        static List<Card> cards;
        static List<Image> images;
        static IEnumerator<Image> iterator;

        static void Main(string[] args)
        {
            var options = new Options();
            Parser parser = new Parser();
            bool success = true;

            cards = new List<Card>();
            images = new List<Image>();

            if (parser.ParseArguments(args, options))
            {
                success = GetCardList(options.InputFile);
                if (success) success = DownloadCards();
                if (success) PrintCards();
            }
            else
            {
                Console.WriteLine("Failed to parse options...");
            }

            Console.Read();
        }

        private static bool GetCardList(string fileName)
        {
            bool success = false;

            Console.Write("Loading Cards...");

            List<Exception> errors = new List<Exception>();

            success = File.Exists(fileName);

            if (!success)
            {
                Console.WriteLine("\tFile {0} does not exist.", fileName);
                return success;
            }

            using(var stream = File.OpenRead(fileName))
            {
                byte[] data = new byte[stream.Length];
                stream.Read(data, 0, (int)stream.Length);
                string dataString = Encoding.UTF8.GetString(data);
                try
                {
                    var output = JsonConvert.DeserializeObject<List<Card>>(dataString);
                    cards.AddRange(output);
                }
                catch (JsonReaderException jre)
                {
                    Console.WriteLine();
                    Console.WriteLine(jre.Message);
                    Console.WriteLine(jre.StackTrace);
                }
            }

            Console.WriteLine("\tDone!");
            return success;
        }

        private static bool DownloadCards()
        {
            bool success = true;

            Console.Write("Downloading images...");

            try
            {
                foreach (var card in cards)
                {
                    card.FileName = card.Name.Replace(' ', '_') + ".png";
                    DownloadRemoteImage(card.Uri, card.FileName);

                    Image image = Image.FromFile(card.FileName);
                    for (int i = 0; i < card.Count; i++) images.Add(image);
                }

                Console.WriteLine("\tDone!");
            }
            catch (Exception e)
            {
                Console.WriteLine();
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
                success = false;
            }

            return success;
        }

        private static void PrintCards()
        {
            Console.Write("Printing cards...");

            PrintDocument document = new PrintDocument();
            iterator = images.GetEnumerator();

            document.PrintPage += Document_PrintPage;
            document.EndPrint += Document_EndPrint;

            if (iterator.MoveNext())
            {
                document.Print();
            }
        }

        private static void Document_EndPrint(object sender, PrintEventArgs e)
        {
            Console.WriteLine("\tDone!");
        }

        private static void Document_PrintPage(object sender, PrintPageEventArgs args)
        {
            int width = 231;
            int height = 338;
            int maxX = 838;
            int maxY = 1088;
            Rectangle rect = new Rectangle(0, 0, width, height);
            bool moreCards = true;
            do
            {
                var image = iterator.Current;
                args.Graphics.DrawImage(image, rect);
                moreCards = iterator.MoveNext();
                rect.X += width;
                if (rect.X + width > maxX)
                {
                    rect.X = 0;
                    rect.Y += height;
                    if (rect.Y + height > maxY)
                    {
                        break;
                    }
                }
            } while (moreCards);

            args.HasMorePages = moreCards;
        }

        private static void DownloadRemoteImage(string uri, string fileName)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri);
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();

            if ((response.StatusCode == HttpStatusCode.OK || 
                 response.StatusCode == HttpStatusCode.Moved || 
                 response.StatusCode == HttpStatusCode.Redirect) && 
                response.ContentType.StartsWith("image", StringComparison.OrdinalIgnoreCase))
            {
                using (Stream inputStream = response.GetResponseStream())
                using (Stream outputStream = File.OpenWrite(fileName))
                {
                    byte[] buffer = new byte[4096];
                    int bytesRead;
                    do
                    {
                        bytesRead = inputStream.Read(buffer, 0, buffer.Length);
                        outputStream.Write(buffer, 0, bytesRead);
                    } while (bytesRead != 0);
                }
            }
        }
    }

    class Card
    {
        public string Name;
        public int Count;
        public string Uri;
        public string FileName;
    }
}
