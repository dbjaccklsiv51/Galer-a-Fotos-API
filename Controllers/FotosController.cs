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
            // 1. Validación
            if (albumId < 1 || albumId > 100)
            {
                return BadRequest(new
                {
                    error = "El albumId debe estar entre 1 y 100.",
                    valoresValidos = "1-100"
                });
            }

            // 2. Obtener cliente HTTP
            var client = _httpClientFactory.CreateClient("JSONPlaceholder");
            var response = await client.GetAsync($"photos?albumId={albumId}");

            if (!response.IsSuccessStatusCode)
            {
                // Si la API externa falla, devolvemos error 500 (o lo que corresponda)
                return StatusCode((int)response.StatusCode, "Error al consultar la API externa.");
            }

            var json = await response.Content.ReadAsStringAsync();
            var fotos = JsonSerializer.Deserialize<List<Foto>>(json, _jsonOptions);

            // 3. Si no hay fotos
            if (fotos == null || fotos.Count == 0)
            {
                return NotFound($"El álbum {albumId} no contiene fotos.");
            }

            // 4. Transformar
            var items = fotos.Select(f => new FotoAlbumItem
            {
                Id = f.Id,
                Titulo = f.Title,
                AlbumId = f.AlbumId,
                UrlCompleta = f.Url,
                Miniatura = f.ThumbnailUrl,
                Tamanio = "Completa"
            }).ToList();

            // 5. Respuesta
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
            // 1. Validación
            if (string.IsNullOrWhiteSpace(palabra))
            {
                return BadRequest(new { error = "Debe proporcionar una palabra de búsqueda." });
            }

            var client = _httpClientFactory.CreateClient("JSONPlaceholder");

            // 2. Obtener todas las fotos (5000)
            var response = await client.GetAsync("photos");
            if (!response.IsSuccessStatusCode)
            {
                return StatusCode((int)response.StatusCode, "Error al consultar la API externa.");
            }

            var json = await response.Content.ReadAsStringAsync();
            var todasLasFotos = JsonSerializer.Deserialize<List<Foto>>(json, _jsonOptions) ?? new List<Foto>();

            // 3. Filtrar por título (case-insensitive)
            var palabraLower = palabra.ToLower();
            var fotosFiltradas = todasLasFotos
                .Where(f => f.Title != null && f.Title.ToLower().Contains(palabraLower))
                .ToList();

            int totalEncontradas = fotosFiltradas.Count;

            // 4. Tomar máximo 20
            var fotosLimitadas = fotosFiltradas.Take(20).ToList();

            // 5. Crear items con título destacado
            var items = fotosLimitadas.Select(f =>
            {
                // Resaltar la palabra en el título (reemplazo simple, sensible a mayúsculas)
                // Usamos Regex para reemplazar todas las ocurrencias, ignorando mayúsculas
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

            // 6. Respuesta
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
                    break; // éxito, salimos del bucle
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
            // Validar albumId (opcional, podemos permitir cualquier número)
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

            // Tomar las primeras 5 miniaturas
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
        // Métodos de los endpoints (se desarrollan a continuación)
    }
}