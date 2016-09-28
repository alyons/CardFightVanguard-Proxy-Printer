using System;
using System.Net;
using System.IO;
using System.Drawing;
using System.Drawing.Printing;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PrintTestApp
{
    class Program
    {
        static List<Card> cards;
        static List<Image> images;
        static IEnumerator<Image> iterator;

        static void Main(string[] args)
        {
            // Get list of cards

            cards = new List<Card>()
            {
                new Card() { Name = "Darkside Sword Master", Count = 4, Uri = "http://vignette4.wikia.nocookie.net/cardfight/images/8/83/G-BT05-021EN-RR.png/revision/latest?cb=20160120182915" },
                new Card() { Name = "Masquerade Bunny", Count = 4, Uri = "http://vignette2.wikia.nocookie.net/cardfight/images/a/a9/G-BT05-038EN-R.png/revision/latest?cb=20160120184358" },
                new Card() { Name = "Hoop Master", Count = 4, Uri = "http://vignette2.wikia.nocookie.net/cardfight/images/7/7d/G-BT06-018EN-RR.png/revision/latest?cb=20160317064603" },
                new Card() { Name = "Darkside Mirror Master", Count = 4, Uri = "http://vignette4.wikia.nocookie.net/cardfight/images/a/ae/G-BT05-020EN-RR.png/revision/latest?cb=20160120182843" }
            };
            images = new List<Image>();

            // Download images for cards
            foreach(var card in cards)
            {
                card.FileName = card.Name.Replace(' ', '_') + ".png";
                DownloadRemoteImage(card.Uri, card.FileName);

                Image image = Image.FromFile(card.FileName);
                for (int i = 0; i < card.Count; i++) images.Add(image);
            }

            // print document
            PrintDocument document = new PrintDocument();
            iterator = images.GetEnumerator();

            document.PrintPage += Document_PrintPage;
            if (iterator.MoveNext())
            {
                document.Print();
            }

            Console.Read();
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
                moreCards = iterator.MoveNext();
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
