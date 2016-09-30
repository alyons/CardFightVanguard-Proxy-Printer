using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace CFV_ProxyPrinter
{
    public class CardCountRule : ValidationRule
    {
        private int _min = 1;
        private int _max = 4;

        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            int count = 0;

			try
            {
                if (((string)value).Length > 0)
                    count = Int32.Parse((String)value);
            }
			catch (Exception e)
            {
                return new ValidationResult(false, "Illegal characters or " + e.Message);
            }

			if (count < _min || count > _max)
            {
                return new ValidationResult(false, "Please enter a value in the range of: " + _min + " and " + _max + ".");
            }
			else
            {
                return new ValidationResult(true, null);
            }
        }
    }
}
