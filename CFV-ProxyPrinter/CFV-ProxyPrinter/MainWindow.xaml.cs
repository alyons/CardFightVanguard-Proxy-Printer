using Newtonsoft.Json;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace CFV_ProxyPrinter
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Variables
        bool unsavedChanges = false;
        Command lastCommand;
        Stack<UndoableCommand> undoStack = new Stack<UndoableCommand>();
        Stack<UndoableCommand> redoStack = new Stack<UndoableCommand>();
        #endregion

        #region Properties
        public ObservableList<Card> Cards { get; set; }
        public String FilePath { get; set; }
        public bool UnsavedChanges
        {
            get { return unsavedChanges; }
            set
            {
                if (unsavedChanges != value)
                {
                    unsavedChanges = value;
                    if (unsavedChanges)
                        Title = Title + ((Title.Contains(" *")) ? "" : " *");
                    else
                        Title.Replace(" *", "");
                }
            }
        }
        #endregion

        public MainWindow()
        {
            InitializeComponent();

            RecentFileList.MenuClick += (s, e) =>
            {
                FilePath = e.Filepath;
                LoadFile(FilePath);
            };

            Cards = new ObservableList<Card>();
            Cards.CollectionChanged += Cards_CollectionChanged;
            cardListView.ItemsSource = Cards;
        }

        #region Methods

        #region Menu Items
        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            MenuItem item = sender as MenuItem;
            this.Title = "Info: " + item.Header;
        }

        private void OpenItem_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new System.Windows.Forms.OpenFileDialog();
            dialog.Filter = "JSON files (*.json)|*.json|All Files (*.*)|*.*";
            dialog.Title = "Choose a deck:";
            System.Windows.Forms.DialogResult result = dialog.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                FilePath = dialog.FileName;
            }

            LoadFile(FilePath);
        }
        
        private void SaveItem_Click(object sender, RoutedEventArgs e)
        {
            SaveFile(FilePath);
        }
        private void SaveAsItem_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.SaveFileDialog saveDialog = new System.Windows.Forms.SaveFileDialog();
            saveDialog.Filter = "JSON (*.json)|*.json";
            saveDialog.DefaultExt = ".json";
            saveDialog.Title = "Save proxy list";
            saveDialog.ShowDialog();

            if (saveDialog.FileName != "")
            {
                FilePath = saveDialog.FileName;
                SaveFile(FilePath);
            }
        }

        private void PrintItem_Click(object sender, RoutedEventArgs e)
        {

        }

        private void AddCardItem_Click(object sender, RoutedEventArgs e)
        {
            AddCardWindow addDialog = new AddCardWindow();
            addDialog.Closed += AddDialog_Closed;
            addDialog.ShowDialog();
        }

        private void AddDialog_Closed(object sender, EventArgs e)
        {
            AddCardWindow addDialog = sender as AddCardWindow;
            if (addDialog != null)
            {
                Card card = addDialog.Card;
                if (card != null)
                    ExecuteCommand(new AddCommand(this, card));
            }
        }

        private void UndoMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Undo();
        }
        private void RedoMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Redo();
        }
        #endregion
        #region Item Template Events

        private void IncrementCard_Click(object sender, RoutedEventArgs e)
        {
            Card item = (sender as Button).Tag as Card;
            var value = item.Count + 1;
            ExecuteCommand(new UpdateCountCommand(item, value));
        }

        private void DecrementCard_Click(object sender, RoutedEventArgs e)
        {
            Card item = (sender as Button).Tag as Card;
            var value = item.Count - 1;
            ExecuteCommand(new UpdateCountCommand(item, value));
        }

        private void RemoveCard_Click(object sender, RoutedEventArgs e)
        {
            Card item = (sender as Button).Tag as Card;
            ExecuteCommand(new RemoveCommand(this, item));
        }

        private void Cards_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add)
            {
                foreach (Card item in e.NewItems) item.PropertyChanged += SingleCardChanged;
            }
            else if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Remove)
            {
                foreach (Card item in e.OldItems) item.PropertyChanged -= SingleCardChanged;
            }
            AddToStack();
            UnsavedChanges = true;
            cardListView.Items.Refresh();
        }

        private void SingleCardChanged(object sender, PropertyChangedEventArgs e)
        {
            AddToStack();
            UnsavedChanges = true;
        }

        #endregion

        #region Helper Methods
        private bool LoadFile(string fileName)
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

            using (var stream = File.OpenRead(fileName))
            {
                byte[] data = new byte[stream.Length];
                stream.Read(data, 0, (int)stream.Length);
                string dataString = Encoding.UTF8.GetString(data);
                try
                {
                    var output = JsonConvert.DeserializeObject<List<Card>>(dataString);
                    Cards.AddRange(output);
                    foreach (Card item in Cards) item.PropertyChanged += SingleCardChanged;
                    Title = String.Format("CFV Proxy Printer - {0}", System.IO.Path.GetFileNameWithoutExtension(fileName));
                    Console.WriteLine("\tDone!");
                }
                catch (JsonReaderException jre)
                {
                    Console.WriteLine();
                    Console.WriteLine(jre.Message);
                    Console.WriteLine(jre.StackTrace);
                    success = false;
                }
            }

            if (success)
            {
                cardListView.Items.Refresh();
                RecentFileList.InsertFile(FilePath);
            }

            return success;
        }
        private void SaveFile(string fileName)
        {
            try
            {
                var data = JsonConvert.SerializeObject(Cards);
                File.WriteAllText(FilePath, data);
                Title = "CFV Proxy Printer";
                UnsavedChanges = false;
            }
            catch (ArgumentNullException) { System.Windows.Forms.MessageBox.Show("Cannot save file: Path is null. Try changing the name or location before saving again."); }
            catch (ArgumentException) { System.Windows.Forms.MessageBox.Show("Cannot save file: Path is invalid. Try changing the name or location before saving again."); }
            catch (DirectoryNotFoundException) { System.Windows.Forms.MessageBox.Show("Cannot save file: Cannot find the directory. Try changing the name or location before saving again."); }
            catch (PathTooLongException) { System.Windows.Forms.MessageBox.Show("Cannot save file: Path too long. Try changing the name or location before saving again."); }
            catch (IOException) { System.Windows.Forms.MessageBox.Show("Cannot save file: Couldn't open the file. Make sure the file isn't in use and try again."); }

        }

        // Undo and Redo
        private void ExecuteCommand(Command cmd)
        {
            lastCommand = cmd;
            cmd.Execute();
            CheckUndoRedoStatus();
        }
        private void Undo()
        {
            if (undoStack.Count > 0)
            {
                UndoableCommand cmd = undoStack.Pop();
                cmd.Undo();
                redoStack.Push(cmd);
            }
            CheckUndoRedoStatus();
        }
        private void Redo()
        {
            if (redoStack.Count > 0)
            {
                Command cmd = redoStack.Pop();
                ExecuteCommand(cmd);
            }
            CheckUndoRedoStatus();
        }
        private void CheckUndoRedoStatus()
        {
            undoMenuItem.IsEnabled = (undoStack.Count > 0);
            redoMenuItem.IsEnabled = (redoStack.Count > 0);
        }
        private void AddToStack()
        {
            if (lastCommand != null && lastCommand is UndoableCommand)
            {
                undoStack.Push((UndoableCommand)lastCommand);
                lastCommand = null;
                redoStack.Clear();
                CheckUndoRedoStatus();
            }
        }

        // Printing Things
        private void Print()
        {
            bool success = true;
            List<System.Drawing.Image> images = new List<System.Drawing.Image>();
            #region
            try
            {
                foreach (var card in Cards)
                {
                    card.FileName = card.Name.Replace(' ', '_') + ".png";
                    DownloadRemoteImage(card.Uri, card.FileName);

                    System.Drawing.Image image = System.Drawing.Image.FromFile(card.FileName);
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

            if (!success)
            {
                System.Windows.Forms.MessageBox.Show("Cannot print file: Failed to download all images.");
                return;
            }
            #endregion
        }
        private void DownloadRemoteImage(string uri, string fileName)
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
        #endregion

        #endregion
    }
}
