using System.Xml.Serialization;

namespace OpenSauce.MapServer.Lib.Models
{
	public sealed class MapDownloadModel
	{
		[XmlAttribute("algorithm")]
		public CompressionAlgorithm Algorithm { get; set; }

		[XmlAttribute("name")]
		public string Name { get; set; }

		[XmlAttribute("md5")]
		public string MD5 { get; set; }

		[XmlAttribute("uncompressed_size")]
		public long UncompressedSize { get; set; }

		[XmlAttribute("compressed_size")]
		public long CompressedSize { get; set; }

		[XmlElement("part")]
		public MapPartModel[] Parts { get; set; }
	}
}