/*
 * Enveloppe web autour de l'application WPF « Générateur de Dictionnaire de
 * Mots de Passe ». Reproduit sa logique (voir Generateur.cs) et l'expose via
 * une API JSON + une page web.
 *
 * - /api/generer   : renvoie le nombre RÉEL de combinaisons + un aperçu.
 * - /api/telecharger : génère et STREAME le fichier complet (jusqu'à plusieurs
 *   millions de mots) sans tout charger en mémoire, comme l'app d'origine.
 */
using DicoWeb;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

const int MAX_LONGUEUR = 8;          // borne la longueur (le total reste énorme)
const long MAX_TELECHARGEMENT = 5_000_000; // lignes max dans le .txt (démo)

var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
app.Urls.Clear();
app.Urls.Add($"http://0.0.0.0:{port}");

app.UseDefaultFiles();
app.UseStaticFiles();

string? Valider(int min, int max, string charset)
{
    if (min < 1) return "La longueur minimale doit être au moins 1.";
    if (max < min) return "La longueur maximale doit être ≥ à la minimale.";
    if (max > MAX_LONGUEUR) return $"Longueur maximale limitée à {MAX_LONGUEUR} pour la démo.";
    if (charset.Length == 0) return "Sélectionne au moins un type de caractères.";
    return null;
}

app.MapPost("/api/generer", (GenererRequest req) =>
{
    var options = new Generateur.Options(
        req.Min, req.Max, req.Lower, req.Upper, req.Digits,
        req.LowerCustom, req.UpperCustom, req.NumberCustom, req.Specials, req.CustomSpecial);
    var charset = Generateur.ConstruireCharset(options);

    var erreur = Valider(req.Min, req.Max, charset);
    if (erreur != null) return Results.BadRequest(new { error = erreur });

    var (total, astronomique) = Generateur.CalculerTotal(req.Min, req.Max, charset.Length);
    var apercu = Generateur.Enumerer(charset, req.Min, req.Max).Take(500).ToList();

    return Results.Ok(new
    {
        total,
        astronomique,
        charset,
        apercu,
        maxTelechargement = MAX_TELECHARGEMENT
    });
});

// Téléchargement streamé du dictionnaire complet (jusqu'à MAX_TELECHARGEMENT lignes).
app.MapGet("/api/telecharger", async (HttpContext ctx,
    int min, int max, bool lower, bool upper, bool digits,
    string? lowerCustom, string? upperCustom, string? numberCustom,
    string? specials, string? customSpecial) =>
{
    var options = new Generateur.Options(min, max, lower, upper, digits,
        lowerCustom, upperCustom, numberCustom, specials, customSpecial);
    var charset = Generateur.ConstruireCharset(options);

    var erreur = Valider(min, max, charset);
    if (erreur != null)
    {
        ctx.Response.StatusCode = 400;
        await ctx.Response.WriteAsync(erreur);
        return;
    }

    ctx.Response.ContentType = "text/plain; charset=utf-8";
    ctx.Response.Headers["Content-Disposition"] = "attachment; filename=\"dictionnaire.txt\"";

    await using var writer = new StreamWriter(ctx.Response.Body);
    long n = 0;
    foreach (var mot in Generateur.Enumerer(charset, min, max))
    {
        await writer.WriteLineAsync(mot);
        if (++n >= MAX_TELECHARGEMENT) break;
    }
    await writer.FlushAsync();
});

app.Run();

record GenererRequest(
    int Min, int Max,
    bool Lower, bool Upper, bool Digits,
    string? LowerCustom, string? UpperCustom, string? NumberCustom,
    string? Specials, string? CustomSpecial);
