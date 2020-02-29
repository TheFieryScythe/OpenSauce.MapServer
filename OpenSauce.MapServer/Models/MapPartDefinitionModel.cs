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

	[XmlRoot("osHTTPServer")]
	public sealed class MapPartDefinitionModel
	{
		[XmlElement("map_download")]
		public MapDownloadModel MapDownload { get; set; }
	}

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

	public sealed class MapPartModel
	{
		[XmlAttribute("name")]
		public string Name { get; set; }

		[XmlAttribute("index")]
		public int Index { get; set; }

		[XmlAttribute("md5")]
		public string MD5 { get; set; }

		[XmlAttribute("size")]
		public long Size { get; set; }
	}
}