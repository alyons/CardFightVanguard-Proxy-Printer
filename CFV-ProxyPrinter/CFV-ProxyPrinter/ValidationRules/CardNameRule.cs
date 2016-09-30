using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace CFV_ProxyPrinter
{
    public class CardNameRule : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            if (String.IsNullOrWhiteSpace((String)value))
            {
                return new ValidationResult(false, "The card must have a name.");
            }
            else
            {
                return new ValidationResult(true, null);
            }
        }
    }
}
