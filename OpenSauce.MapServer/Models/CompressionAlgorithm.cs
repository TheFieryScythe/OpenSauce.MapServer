using System.Xml.Serialization;

namespace OpenSauce.MapServer.Models
{
	public enum CompressionAlgorithm
	{
		[XmlEnum("7zip")]
		SevenZip,

		[XmlEnum("zip")]
		Zip
	}
}