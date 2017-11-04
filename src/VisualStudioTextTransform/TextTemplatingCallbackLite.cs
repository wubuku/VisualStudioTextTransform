using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TextTemplating.VSHost;

namespace AIT.Tools.VisualStudioTextTransform
{
    public class TextTemplatingCallbackLite : ITextTemplatingCallback
    {
        public void ErrorCallback(bool warning, string message, int line, int column)
        {
            throw new NotImplementedException();
        }

        public void SetFileExtension(string extension)
        {
            throw new NotImplementedException();
        }

        public void SetOutputEncoding(Encoding encoding, bool fromOutputDirective)
        {
            throw new NotImplementedException();
        }
    }
}
