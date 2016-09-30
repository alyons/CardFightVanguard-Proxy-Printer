using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CFV_ProxyPrinter
{
    public class Card : ViewModelBase
    {
        #region Variables
        private string name;
        private int count;
        private string uri;
        #endregion

        #region Properties
        public string Name
        {
            get { return name; }
            set
            {
                name = value;
                OnPropertyChanged("Name");
                if (!string.IsNullOrWhiteSpace(name))
                {
                    FileName = name.Replace(' ', '_') + ".png";
                }
            }
        }
        public int Count
        {
            get { return count; }
            set
            {
                if (value > 0 && value < 5 && count != value)
                {
                    count = value;
                    OnPropertyChanged("Count");
                }
            }
        }
        public string Uri
        {
            get { return uri; }
            set
            {
                if (uri == null || !uri.Equals(value))
                {
                    uri = value;
                    OnPropertyChanged("Uri");
                }
            }
        }
        public string FileName { get; set; }
        #endregion

        #region Constructors
        public Card()
        {
            Name = "Vanguard";
            Count = 1;
            Uri = "http://vignette2.wikia.nocookie.net/cardfight/images/3/37/Cfv_back.jpg/revision/20140801101556";
        }
        #endregion
    }
}
