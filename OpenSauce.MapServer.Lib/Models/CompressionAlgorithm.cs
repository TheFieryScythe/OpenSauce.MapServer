using System.Xml.Serialization;

namespace OpenSauce.MapServer.Lib.Models
{
	public enum CompressionAlgorithm
	{
		[XmlEnum("7zip")]
		SevenZip,

		[XmlEnum("zip")]
		Zip
	}
}