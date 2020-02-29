using System;
using System.IO;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using OpenSauce.MapServer.Models;

namespace OpenSauce.MapServer.Controllers
{
	[Route("api/[controller]")]
	public class MapDownloadController : Controller
	{
		public MapDownloadController(BlobServiceClient blobServiceClient)
		{
			_mapBlobContainerClient = blobServiceClient.GetBlobContainerClient("opensauce-mapserver-maps");
		}

		[HttpGet]
		public async Task<IActionResult> Get([FromQuery] MapDownloadParameters parameters)
		{
			if (!parameters.IsValid())
			{
				return NotFound();
			}

			return parameters.GetRequestType() switch
			{
				RequestType.MapDefinition => await GetMapDefinitionAsync(parameters.Map),
				RequestType.MapPart => await GetMapPartAsync(parameters.Map, parameters.Part),
				_ => throw new ArgumentOutOfRangeException()
			};
		}

		private async Task<IActionResult> GetMapDefinitionAsync(string map)
		{
			try
			{
				var mapMetadata = await GetMapMetadataAsync(map);

				Response.ContentType = "application/xml";
				return Ok(
					new MapPartDefinitionModel
					{
						MapDownload = new MapDownloadModel
						{
							Algorithm = CompressionAlgorithm.Zip,
							Name = mapMetadata.UncompressedName,
							MD5 = mapMetadata.UncompressedMD5,
							UncompressedSize = mapMetadata.UncompressedSize,
							CompressedSize = mapMetadata.CompressedSize,
							Parts = new[]
							{
								new MapPartModel
								{
									Name = mapMetadata.CompressedName,
									Index = 0,
									MD5 = mapMetadata.CompressedMD5,
									Size = mapMetadata.CompressedSize
								}
							}
						}
					});
			}
			catch (FileNotFoundException)
			{
				return NotFound($"Unknown map \"{map}\"");
			}
		}

		private async Task<IActionResult> GetMapPartAsync(string map, string part)
		{
			try
			{
				var mapMetadata = await GetMapMetadataAsync(map);

				var blobClient = await GetBlobClientAsync(mapMetadata.CompressedName);
				if (blobClient == null)
				{
					throw new FileNotFoundException($"A map part was not found matching {part}");
				}

				var downloadInfo = await blobClient.DownloadAsync();
				return new FileStreamResult(downloadInfo.Value.Content, new MediaTypeHeaderValue("application/octet-stream"))
				{
					FileDownloadName = part
				};
			}
			catch (FileNotFoundException)
			{
				return NotFound($"Unknown map part \"{part}\"");
			}
		}

		private async Task<MapMetadata> GetMapMetadataAsync(string mapName)
		{
			var blobClient = await GetBlobClientAsync(Path.ChangeExtension(mapName, "zip"));
			if (blobClient == null)
			{
				throw new FileNotFoundException($"A map was not found matching {mapName}");
			}

			var blobProperties = (await blobClient.GetPropertiesAsync()).Value;
			return new MapMetadata
			{
				UncompressedName = blobProperties.Metadata[nameof(MapMetadata.UncompressedName)],
				UncompressedMD5 = blobProperties.Metadata[nameof(MapMetadata.UncompressedMD5)],
				UncompressedSize = long.Parse(blobProperties.Metadata[nameof(MapMetadata.UncompressedSize)]),
				CompressedName = blobProperties.Metadata[nameof(MapMetadata.CompressedName)],
				CompressedMD5 = blobProperties.Metadata[nameof(MapMetadata.CompressedMD5)],
				CompressedSize = long.Parse(blobProperties.Metadata[nameof(MapMetadata.CompressedSize)])
			};
		}

		private async Task<BlobClient> GetBlobClientAsync(string itemName)
		{
			var containerClient = _mapBlobContainerClient;
			await foreach (var item in containerClient.GetBlobsAsync())
			{
				if (!item.Name.Equals(itemName, StringComparison.OrdinalIgnoreCase))
				{
					continue;
				}

				return containerClient.GetBlobClient(item.Name);
			}

			return null;
		}

		private readonly BlobContainerClient _mapBlobContainerClient;
	}
}