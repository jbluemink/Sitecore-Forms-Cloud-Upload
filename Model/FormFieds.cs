using System.Collections.Generic;

namespace Stockpick.Form.Cloud.Model
{
    public class FormFields
    {
        public string FormId { set; get; }
        public IList<FormFieldSmall> Fields { set; get; }
    }
}