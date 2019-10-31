using System.IO;
using System.Text;

namespace sfx_100_modbus_gui
{
    /// <summary>
    /// Helper class to write serialized UTF-8 encoded strings
    /// </summary>
    public sealed class StringWriterWithEncoding : StringWriter
    {
        private readonly Encoding _encoding;

        public StringWriterWithEncoding() { }

        public StringWriterWithEncoding(Encoding encoding)
        {
            this._encoding = encoding;
        }

        public override Encoding Encoding
        {
            get { return _encoding; }
        }
    }
}