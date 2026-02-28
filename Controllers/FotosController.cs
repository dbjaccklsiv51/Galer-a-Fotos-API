using Json_Demo;  // Para JsonHelper
using Json_Demo.Models;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace Json_Demo.Controllers
{
    [Route("api/fotos")]
    [ApiController]
    public class FotosController : ControllerBase
    {
        private readonly HttpClient _httpClient;
        private const string BaseUrl = "https://jsonplaceholder.typicode.com";

        public FotosController()
        {
            _httpClient = new HttpClient();
            _httpClient.BaseAddress = new Uri(BaseUrl);
        }

        // GET: api/fotos/album/1
        [HttpGet("album/{albumId}")]
        public async Task<IActionResult> GetFotosPorAlbum(int albumId)
        {
            // Validar albumId
            if (albumId < 1 || albumId > 100)
            {
                return BadRequest(new
                {
                    error = "El albumId debe estar entre 1 y 100",
                    sugerencia = "Valores válidos: 1, 2, 3, ..., 100"
                });
            }

            // Consultar API externa
            var response = await _httpClient.GetAsync($"photos?albumId={albumId}");
            if (!response.IsSuccessStatusCode)
            {
                return StatusCode(500, "Error al consultar el servicio externo");
            }

            var json = await response.Content.ReadAsStringAsync();
            var fotos = JsonSerializer.Deserialize<List<Foto>>(json);

            if (fotos == null || fotos.Count == 0)
            {
                return NotFound(new
                {
                    mensaje = $"El álbum {albumId} no tiene fotos o no existe."
                });
            }

            var fotosTransformadas = fotos.Select(f => new
            {
                f.Id,
                titulo = f.Title,
                albumId = f.AlbumId,
                urlCompleta = f.Url,
                miniatura = f.ThumbnailUrl,
                tamanio = "Completa"
            }).ToList();

            var resultado = new
            {
                albumId = albumId,
                totalFotos = fotosTransformadas.Count,
                fotos = fotosTransformadas
            };

            return Ok(JsonHelper.ToJson(resultado));
        }

        // GET: api/fotos/buscar?palabra=accusamus
        [HttpGet("buscar")]
        public async Task<IActionResult> BuscarFotos([FromQuery] string palabra)
        {
            if (string.IsNullOrWhiteSpace(palabra))
            {
                return BadRequest(new { error = "Debe proporcionar una palabra para buscar." });
            }

            var response = await _httpClient.GetAsync("photos");
            if (!response.IsSuccessStatusCode)
            {
                return StatusCode(500, "Error al obtener las fotos del servicio externo.");
            }

            var json = await response.Content.ReadAsStringAsync();
            var todasLasFotos = JsonSerializer.Deserialize<List<Foto>>(json);

            var fotosFiltradas = todasLasFotos
                .Where(f => f.Title.Contains(palabra, StringComparison.OrdinalIgnoreCase))
                .ToList();

            int totalEncontradas = fotosFiltradas.Count;

            var fotosLimitadas = fotosFiltradas
                .Take(20)
                .Select(f => new
                {
                    f.Id,
                    titulo = f.Title,
                    tituloDestacado = f.Title.Replace(palabra, $"**{palabra}**", StringComparison.OrdinalIgnoreCase),
                    albumId = f.AlbumId,
                    miniatura = f.ThumbnailUrl
                })
                .ToList();

            var resultado = new
            {
                palabraBuscada = palabra,
                totalEncontradas = totalEncontradas,
                totalMostradas = fotosLimitadas.Count,
                fotos = fotosLimitadas
            };

            return Ok(JsonHelper.ToJson(resultado));
        }

        // GET: api/fotos/aleatoria
        [HttpGet("aleatoria")]
        public async Task<IActionResult> FotoAleatoria()
        {
            const int maxIntentos = 3;
            int intentos = 0;
            Foto foto = null;

            while (intentos < maxIntentos && foto == null)
            {
                intentos++;
                int idAleatorio = Random.Shared.Next(1, 5001);
                var response = await _httpClient.GetAsync($"photos/{idAleatorio}");

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    foto = JsonSerializer.Deserialize<Foto>(json);
                }
                else if ((int)response.StatusCode != 404)
                {
                    return StatusCode(500, "Error al consultar la API externa.");
                }
            }

            if (foto == null)
            {
                return StatusCode(500, "No se pudo obtener una foto válida después de varios intentos.");
            }

            var resultado = new
            {
                mensaje = "¡Foto del día!",
                foto = new
                {
                    foto.Id,
                    titulo = foto.Title,
                    albumId = foto.AlbumId,
                    urlCompleta = foto.Url,
                    miniatura = foto.ThumbnailUrl
                },
                sugerencia = $"También puedes ver el álbum completo: /api/fotos/album/{foto.AlbumId}"
            };

            return Ok(JsonHelper.ToJson(resultado));
        }

        // GET: api/fotos/album/1/resumen
        [HttpGet("album/{albumId}/resumen")]
        public async Task<IActionResult> ResumenAlbum(int albumId)
        {
            var response = await _httpClient.GetAsync($"photos?albumId={albumId}");
            if (!response.IsSuccessStatusCode)
            {
                return StatusCode(500, "Error al consultar el servicio externo.");
            }

            var json = await response.Content.ReadAsStringAsync();
            var fotos = JsonSerializer.Deserialize<List<Foto>>(json);

            if (fotos == null || fotos.Count == 0)
            {
                return NotFound(new { mensaje = $"El álbum {albumId} no existe o no tiene fotos." });
            }

            var muestras = fotos.Take(5).Select(f => f.ThumbnailUrl).ToList();
            var primeraFoto = fotos.First().Url;

            var resultado = new
            {
                albumId = albumId,
                totalFotos = fotos.Count,
                muestras = muestras,
                primeraFoto = primeraFoto
            };

            return Ok(JsonHelper.ToJson(resultado));
        }
    }
}