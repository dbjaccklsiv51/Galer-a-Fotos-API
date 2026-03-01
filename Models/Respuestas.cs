namespace GaleriaFotosAPI.Models
{
    // Para Endpoint 1: Álbum completo
    public class AlbumResponse
    {
        public int AlbumId { get; set; }
        public int TotalFotos { get; set; }
        public List<FotoAlbumItem> Fotos { get; set; }
    }

    public class FotoAlbumItem
    {
        public int Id { get; set; }
        public string Titulo { get; set; }
        public int AlbumId { get; set; }
        public string UrlCompleta { get; set; }
        public string Miniatura { get; set; }
        public string Tamanio { get; set; } = "Completa";
    }

    // Para Endpoint 2: Búsqueda
    public class BusquedaResponse
    {
        public string PalabraBuscada { get; set; }
        public int TotalEncontradas { get; set; }
        public int TotalMostradas { get; set; }
        public List<FotoBusquedaItem> Fotos { get; set; }
    }

    public class FotoBusquedaItem
    {
        public int Id { get; set; }
        public string Titulo { get; set; }
        public string TituloDestacado { get; set; }
        public int AlbumId { get; set; }
        public string Miniatura { get; set; }
    }

    // Para Endpoint 3: Foto aleatoria
    public class FotoAleatoriaResponse
    {
        public string Mensaje { get; set; }
        public FotoAleatoriaItem Foto { get; set; }
        public string Sugerencia { get; set; }
    }

    public class FotoAleatoriaItem
    {
        public int Id { get; set; }
        public string Titulo { get; set; }
        public int AlbumId { get; set; }
        public string UrlCompleta { get; set; }
        public string Miniatura { get; set; }
    }

    // Para Endpoint 4: Resumen del álbum
    public class ResumenAlbumResponse
    {
        public int AlbumId { get; set; }
        public int TotalFotos { get; set; }
        public List<string> Muestras { get; set; }
        public string PrimeraFoto { get; set; }
    }
}