using Newtonsoft.Json;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Printing;
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
        bool fromRedo = false;
        Stack<UndoableCommand> undoStack = new Stack<UndoableCommand>();
        Stack<UndoableCommand> redoStack = new Stack<UndoableCommand>();
        IEnumerator<System.Drawing.Image> iterator;
        List<System.Drawing.Image> images;
        #endregion

        #region Properties
        public ObservableList<Card> Cards { get; set; }
        public string FilePath { get; set; }
        public bool UnsavedChanges
        {
            get { return unsavedChanges; }
            set
            {
                if (unsavedChanges != value)
                {
                    unsavedChanges = value;
                    SetTitle();
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

        #region Application Commands
        private void NewCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            throw new NotImplementedException();
        }
        private void NewCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void OpenCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            throw new NotImplementedException();
        }
        private void OpenCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            throw new NotImplementedException();
        }

        // Save Command
        private void SaveCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = Cards.Count > 0 && !string.IsNullOrWhiteSpace(FilePath);
        }
        private void SaveCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            SaveFile(FilePath);
        }


        // Save As Command
        private void SaveAsCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = Cards.Count > 0;
        }
        private void SaveAsCommand_Executed(object sender, ExecutedRoutedEventArgs e)
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

        private void PrintPreviewCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            throw new NotImplementedException();
        }
        private void PrintPreviewCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void PrintCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            throw new NotImplementedException();
        }
        private void PrintCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            throw new NotImplementedException();
        }

        // Exit Command
        private void ExitCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }
        private void ExitCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (UnsavedChanges)
            {
                var result = MessageBox.Show("There are unsaved changes. Would you like to still close the application? (The pending changes will be lost)", "Closing Application", MessageBoxButton.OKCancel);
                if (result == MessageBoxResult.OK)
                {
                    Close();
                }
            }
            else
            {
                Close();
            }
        }
        #endregion

        #region Menu Items
        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            MenuItem item = sender as MenuItem;
            Title = "Info: " + item.Header;
        }

        private void NewItem_Click(object sender, RoutedEventArgs e)
        {
            Cards.Clear();
            cardListView.Items.Refresh();
            FilePath = null;
            undoStack.Clear();
            redoStack.Clear();
            UnsavedChanges = false;
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
            Print();
        }
        private void PrintPreviewItem_Click(object sender, RoutedEventArgs e)
        {
            PrintPreview();
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
                    SetTitle();
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
                UnsavedChanges = false;
            }
            catch (ArgumentNullException) { System.Windows.Forms.MessageBox.Show("Cannot save file: Path is null. Try changing the name or location before saving again."); }
            catch (ArgumentException) { System.Windows.Forms.MessageBox.Show("Cannot save file: Path is invalid. Try changing the name or location before saving again."); }
            catch (DirectoryNotFoundException) { System.Windows.Forms.MessageBox.Show("Cannot save file: Cannot find the directory. Try changing the name or location before saving again."); }
            catch (PathTooLongException) { System.Windows.Forms.MessageBox.Show("Cannot save file: Path too long. Try changing the name or location before saving again."); }
            catch (IOException) { System.Windows.Forms.MessageBox.Show("Cannot save file: Couldn't open the file. Make sure the file isn't in use and try again."); }

        }

        private void SetTitle()
        {
            Title = "CFV Proxy Printer";

            if (!string.IsNullOrWhiteSpace(FilePath)) Title += " - " + System.IO.Path.GetFileNameWithoutExtension(FilePath);
            if (UnsavedChanges) Title += " *";
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
                fromRedo = true;
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
                if (!fromRedo) redoStack.Clear();
                CheckUndoRedoStatus();
                fromRedo = false;
            }
        }

        // Printing Things
        private void Print()
        {
            DownloadImages();

            PrintDocument document = new PrintDocument();
            iterator = images.GetEnumerator();

            document.PrintPage += Document_PrintPage;

            System.Windows.Forms.PrintDialog printDialog = new System.Windows.Forms.PrintDialog();
            printDialog.Document = document;
            if (printDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                if (iterator.MoveNext())
                {
                    document.Print();
                }
            }
        }
        private void PrintPreview()
        {
            DownloadImages();

            PrintDocument document = new PrintDocument();
            iterator = images.GetEnumerator();

            document.PrintPage += Document_PrintPage;

            System.Windows.Forms.PrintPreviewDialog printPreviewDialog = new System.Windows.Forms.PrintPreviewDialog();
            if (iterator.MoveNext())
            {
                printPreviewDialog.Document = document;
                printPreviewDialog.ShowDialog();
            }
        }
        private bool DownloadImages()
        {
            bool success = true;
            images = new List<System.Drawing.Image>();
            try
            {
                foreach (var card in Cards)
                {
                    card.FileName = card.Name.Replace(' ', '_') + ".png";
                    DownloadRemoteImage(card.Uri, card.FileName);

                    System.Drawing.Image image = System.Drawing.Image.FromFile(card.FileName);
                    for (int i = 0; i < card.Count; i++) images.Add(image);
                }

                success = true;
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
            }
            return success;
        }
        private void Document_PrintPage(object sender, PrintPageEventArgs args)
        {
            int width = 231;
            int height = 338;
            int maxX = 838;
            int maxY = 1088;
            System.Drawing.Rectangle rect = new System.Drawing.Rectangle(0, 0, width, height);
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
