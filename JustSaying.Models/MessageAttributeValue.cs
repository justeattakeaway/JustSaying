/* The code in this file is a subset of the MessageAttributeValue class as defined here:
 * https://github.com/aws/aws-sdk-net/blob/0b18c061daf81f4966fddd3a3cbe953101394282/sdk/src/Services/SimpleNotificationService/Generated/Model/MessageAttributeValue.cs
 */


using System.Collections.Generic;
using System.IO;


namespace JustSaying.Models
{
    /// <summary>
    /// The user-specified message attribute value. For string data types, the value attribute
    /// has the same restrictions on the content as the message body. For more information,
    /// see <a href="http://docs.aws.amazon.com/sns/latest/api/API_Publish.html">Publish</a> in the AWS docs.
    /// 
    ///  
    /// <para>
    /// Name, type, and value must not be empty or null. In addition, the message body should
    /// not be empty or null. All parts of the message attribute, including name, type, and
    /// value, are included in the message size restriction, which is currently 256 KB (262,144
    /// bytes). For more information, see <a href="http://docs.aws.amazon.com/sns/latest/dg/SNSMessageAttributes.html">Using
    /// Amazon SNS Message Attributes</a> in the AWS docs.
    /// </para>
    /// </summary>
    public class MessageAttributeValue
    {
        /// <summary>
        /// Gets and sets the property BinaryValue. 
        /// <para>
        /// Binary type attributes can store any binary data, for example, compressed data, encrypted
        /// data, or images.
        /// </para>
        /// </summary>
        public IReadOnlyCollection<byte> BinaryValue { get; set; }

        /// <summary>
        /// Gets and sets the property StringValue. 
        /// <para>
        /// Strings are Unicode with UTF8 binary encoding. For a list of code values, see <a href="http://en.wikipedia.org/wiki/ASCII#ASCII_printable_characters">http://en.wikipedia.org/wiki/ASCII#ASCII_printable_characters</a>.
        /// </para>
        /// </summary>
        public string StringValue { get; set; }

        /// <summary>
        /// Gets and sets the property DataType. 
        /// <para>
        /// Amazon SNS supports the following logical data types: String, String.Array, Number,
        /// and Binary. For more information, see <a href="http://docs.aws.amazon.com/sns/latest/dg/SNSMessageAttributes.html#SNSMessageAttributes.DataTypes">Message
        /// Attribute Data Types in the AWS docs</a>.
        /// </para>
        /// </summary>
        public string DataType { get; set; }
    }
}
