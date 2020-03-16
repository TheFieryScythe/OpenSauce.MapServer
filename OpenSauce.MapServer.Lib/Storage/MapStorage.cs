using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Azure;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using OpenSauce.MapServer.Lib.Models;

namespace OpenSauce.MapServer.Lib.Storage
{
	public sealed class MapStorage
	{
		public MapStorage(ILogger logger, BlobContainerClient blobContainerClient)
		{
			_logger = logger;
			_containerClient = blobContainerClient;
		}

		public async Task<MapPartDefinitionModel> GetMapPartDefinitionAsync(string map)
		{
			_logger.LogInformation("MapRequested:{0}", map);

			var metadata = await GetMapMetadataAsync(map);
			return new MapPartDefinitionModel
			{
				MapDownload = new MapDownloadModel
				{
					Algorithm = CompressionAlgorithm.Zip,
					Name = metadata.UncompressedName,
					MD5 = metadata.UncompressedMD5,
					UncompressedSize = metadata.UncompressedSize,
					CompressedSize = metadata.CompressedSize,
					Parts = metadata.Parts
				}
			};
		}

		public async Task<Stream> GetMapPartStreamAsync(string map, string part)
		{
			var mapMetadata = await GetMapMetadataAsync(map);

			var partMetadata = mapMetadata.Parts.FirstOrDefault(p => p.Name.Equals(part, StringComparison.OrdinalIgnoreCase));
			if (partMetadata == null)
			{
				_logger.Log(LogLevel.Error, "MapPartNotFound:{0}", part);
				throw new FileNotFoundException($"A map part was not found matching {part}");
			}

			var blobClient = await GetBlobClientAsync(mapMetadata.CompressedName);
			if (blobClient == null || !await blobClient.ExistsAsync())
			{
				_logger.Log(LogLevel.Error, "MapPartNotFound:{0}", part);
				throw new FileNotFoundException($"A map part was not found matching {part}");
			}

			var downloadInfo = await blobClient.DownloadAsync(new HttpRange(partMetadata.StartOffset, partMetadata.Size));
			return downloadInfo.Value.Content;
		}

		private async Task<MapMetadata> GetMapMetadataAsync(string map)
		{
			var blobClient = await GetBlobClientAsync($"{map}.json");
			if (blobClient == null || !await blobClient.ExistsAsync())
			{
				_logger.Log(LogLevel.Error, "MapNotFound:{0}", map);
				throw new FileNotFoundException($"A map was not found matching {map}");
			}

			var downloadInfo = await blobClient.DownloadAsync();
			using var reader = new StreamReader(downloadInfo.Value.Content);
			return JsonConvert.DeserializeObject<MapMetadata>(await reader.ReadToEndAsync());
		}

		private async Task<BlobClient> GetBlobClientAsync(string itemName)
		{
			await foreach (var item in _containerClient.GetBlobsAsync())
			{
				if (!item.Name.Equals(itemName, StringComparison.OrdinalIgnoreCase))
				{
					continue;
				}

				return _containerClient.GetBlobClient(item.Name);
			}

			return null;
		}

		private readonly ILogger _logger;
		private readonly BlobContainerClient _containerClient;
	}
}