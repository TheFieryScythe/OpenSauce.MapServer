using System.Xml.Serialization;

namespace OpenSauce.MapServer.Models
{
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

		[XmlIgnore]
		public long StartOffset { get; set; }
	}
}