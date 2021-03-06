﻿using OpenSauce.MapServer.Lib.Models;

namespace OpenSauce.MapServer.Lib.Storage
{
	public sealed class MapMetadata
	{
		public string UncompressedName { get; set; }
		public string UncompressedMD5 { get; set; }
		public long UncompressedSize { get; set; }
		public string CompressedName { get; set; }
		public string CompressedMD5 { get; set; }
		public long CompressedSize { get; set; }
		public MapPartModel[] Parts { get; set; }
	}
}