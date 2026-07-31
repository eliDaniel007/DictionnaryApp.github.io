/*
 * Enveloppe web autour de l'application WPF « Générateur de Dictionnaire de
 * Mots de Passe ». Reproduit sa logique (voir Generateur.cs) et l'expose via
 * une API JSON + une page web. Génération plafonnée pour rester fluide.
 */
using DicoWeb;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

const int PLAFOND = 200000;

var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
app.Urls.Clear();
app.Urls.Add($"http://0.0.0.0:{port}");

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapPost("/api/generer", (GenererRequest req) =>
{
    int min = req.Min, max = req.Max;
    if (min < 1) return Results.BadRequest(new { error = "La longueur minimale doit être au moins 1." });
    if (max < min) return Results.BadRequest(new { error = "La longueur maximale doit être ≥ à la minimale." });
    if (max > 12) return Results.BadRequest(new { error = "Longueur maximale limitée à 12 pour la démo." });

    var options = new Generateur.Options(
        min, max, req.Lower, req.Upper, req.Digits,
        req.LowerCustom, req.UpperCustom, req.NumberCustom, req.Specials, req.CustomSpecial);

    var charset = Generateur.ConstruireCharset(options);
    if (charset.Length == 0)
        return Results.BadRequest(new { error = "Sélectionne au moins un type de caractères." });

    var (mots, total, tropGrand) = Generateur.Generer(charset, min, max, PLAFOND);

    if (tropGrand)
        return Results.Ok(new
        {
            tropGrand = true,
            total,
            charset,
            plafond = PLAFOND,
            message = $"Trop de combinaisons ({total:N0}). Avec {charset.Length} caractères et une longueur max de {max}, le total dépasse la limite de {PLAFOND:N0}."
        });

    return Results.Ok(new { tropGrand = false, total, charset, mots });
});

app.Run();

record GenererRequest(
    int Min, int Max,
    bool Lower, bool Upper, bool Digits,
    string? LowerCustom, string? UpperCustom, string? NumberCustom,
    string? Specials, string? CustomSpecial);
