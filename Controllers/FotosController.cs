using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using GaleriaFotosAPI.Models;

namespace GaleriaFotosAPI.Controllers
{
    [Route("api/fotos")]
    [ApiController]
    public class FotosController : ControllerBase
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

        public FotosController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        [HttpGet("album/{albumId}")]
        public async Task<ActionResult<AlbumResponse>> GetAlbum(int albumId)
        {
            
            if (albumId < 1 || albumId > 100)
            {
                return BadRequest(new
                {
                    error = "El albumId debe estar entre 1 y 100.",
                    valoresValidos = "1-100"
                });
            }

            
            var client = _httpClientFactory.CreateClient("JSONPlaceholder");
            var response = await client.GetAsync($"photos?albumId={albumId}");

            if (!response.IsSuccessStatusCode)
            {
                
                return StatusCode((int)response.StatusCode, "Error al consultar la API externa.");
            }

            var json = await response.Content.ReadAsStringAsync();
            var fotos = JsonSerializer.Deserialize<List<Foto>>(json, _jsonOptions);

            
            if (fotos == null || fotos.Count == 0)
            {
                return NotFound($"El álbum {albumId} no contiene fotos.");
            }

            
            var items = fotos.Select(f => new FotoAlbumItem
            {
                Id = f.Id,
                Titulo = f.Title,
                AlbumId = f.AlbumId,
                UrlCompleta = f.Url,
                Miniatura = f.ThumbnailUrl,
                Tamanio = "Completa"
            }).ToList();

            
            var resultado = new AlbumResponse
            {
                AlbumId = albumId,
                TotalFotos = items.Count,
                Fotos = items
            };

            return Ok(resultado);
        }
        [HttpGet("buscar")]
        public async Task<ActionResult<BusquedaResponse>> BuscarFotos([FromQuery] string palabra)
        {
            
            if (string.IsNullOrWhiteSpace(palabra))
            {
                return BadRequest(new { error = "Debe proporcionar una palabra de búsqueda." });
            }

            var client = _httpClientFactory.CreateClient("JSONPlaceholder");

            
            var response = await client.GetAsync("photos");
            if (!response.IsSuccessStatusCode)
            {
                return StatusCode((int)response.StatusCode, "Error al consultar la API externa.");
            }

            var json = await response.Content.ReadAsStringAsync();
            var todasLasFotos = JsonSerializer.Deserialize<List<Foto>>(json, _jsonOptions) ?? new List<Foto>();

            
            var palabraLower = palabra.ToLower();
            var fotosFiltradas = todasLasFotos
                .Where(f => f.Title != null && f.Title.ToLower().Contains(palabraLower))
                .ToList();

            int totalEncontradas = fotosFiltradas.Count;

            
            var fotosLimitadas = fotosFiltradas.Take(20).ToList();

            
            var items = fotosLimitadas.Select(f =>
            {
                var tituloDestacado = System.Text.RegularExpressions.Regex.Replace(
                    f.Title,
                    palabra,
                    $"**{palabra}**",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase
                );

                return new FotoBusquedaItem
                {
                    Id = f.Id,
                    Titulo = f.Title,
                    TituloDestacado = tituloDestacado,
                    AlbumId = f.AlbumId,
                    Miniatura = f.ThumbnailUrl
                };
            }).ToList();

            
            var resultado = new BusquedaResponse
            {
                PalabraBuscada = palabra,
                TotalEncontradas = totalEncontradas,
                TotalMostradas = items.Count,
                Fotos = items
            };

            return Ok(resultado);
        }
        [HttpGet("aleatoria")]
        public async Task<ActionResult<FotoAleatoriaResponse>> FotoAleatoria()
        {
            var client = _httpClientFactory.CreateClient("JSONPlaceholder");
            var random = new Random();
            const int maxIntentos = 3;
            Foto foto = null;

            for (int intento = 0; intento < maxIntentos; intento++)
            {
                int id = random.Next(1, 5001); // 1 a 5000 inclusive
                var response = await client.GetAsync($"photos/{id}");

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    foto = JsonSerializer.Deserialize<Foto>(json, _jsonOptions);
                    break; 
                }
            }

            if (foto == null)
            {
                return StatusCode(500, "No se pudo obtener una foto aleatoria después de varios intentos.");
            }

            var resultado = new FotoAleatoriaResponse
            {
                Mensaje = "¡Foto del día!",
                Foto = new FotoAleatoriaItem
                {
                    Id = foto.Id,
                    Titulo = foto.Title,
                    AlbumId = foto.AlbumId,
                    UrlCompleta = foto.Url,
                    Miniatura = foto.ThumbnailUrl
                },
                Sugerencia = $"También puedes ver el álbum completo: /api/fotos/album/{foto.AlbumId}"
            };

            return Ok(resultado);
        }
        [HttpGet("album/{albumId}/resumen")]
        public async Task<ActionResult<ResumenAlbumResponse>> ResumenAlbum(int albumId)
        {
            if (albumId < 1)
            {
                return BadRequest("El albumId debe ser un número positivo.");
            }

            var client = _httpClientFactory.CreateClient("JSONPlaceholder");
            var response = await client.GetAsync($"photos?albumId={albumId}");

            if (!response.IsSuccessStatusCode)
            {
                return StatusCode((int)response.StatusCode, "Error al consultar la API externa.");
            }

            var json = await response.Content.ReadAsStringAsync();
            var fotos = JsonSerializer.Deserialize<List<Foto>>(json, _jsonOptions);

            if (fotos == null || fotos.Count == 0)
            {
                return NotFound($"El álbum {albumId} no contiene fotos.");
            }

            var muestras = fotos.Take(5).Select(f => f.ThumbnailUrl).ToList();

            var resultado = new ResumenAlbumResponse
            {
                AlbumId = albumId,
                TotalFotos = fotos.Count,
                Muestras = muestras,
                PrimeraFoto = fotos.First().Url
            };

            return Ok(resultado);
        }
    }
}