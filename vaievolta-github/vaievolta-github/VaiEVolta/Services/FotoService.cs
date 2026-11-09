namespace VaiEVolta.Services;

// Salva as fotos enviadas em wwwroot/uploads com um nome único
public class FotoService
{
    private static readonly string[] Extensoes = [".jpg", ".jpeg", ".png", ".webp"];
    private const long TamanhoMaximo = 5 * 1024 * 1024; // 5 MB

    private readonly string _pasta;

    public FotoService(IWebHostEnvironment env)
    {
        _pasta = Path.Combine(env.WebRootPath, "uploads");
        Directory.CreateDirectory(_pasta);
    }

    public string? Validar(IFormFile? arquivo)
    {
        if (arquivo is null || arquivo.Length == 0) return null;
        var ext = Path.GetExtension(arquivo.FileName).ToLowerInvariant();
        if (!Extensoes.Contains(ext)) return "Envie uma imagem JPG, PNG ou WEBP.";
        if (arquivo.Length > TamanhoMaximo) return "A foto pode ter no máximo 5 MB.";
        return null;
    }

    public async Task<string> SalvarAsync(IFormFile arquivo)
    {
        var nome = $"{Guid.NewGuid():N}{Path.GetExtension(arquivo.FileName).ToLowerInvariant()}";
        await using var stream = File.Create(Path.Combine(_pasta, nome));
        await arquivo.CopyToAsync(stream);
        return $"/uploads/{nome}";
    }

    public void Apagar(string? url)
    {
        // Só apaga fotos enviadas pelos usuários, nunca as ilustrações de exemplo
        if (url is null || !url.StartsWith("/uploads/")) return;
        var caminho = Path.Combine(_pasta, Path.GetFileName(url));
        if (File.Exists(caminho)) File.Delete(caminho);
    }
}
