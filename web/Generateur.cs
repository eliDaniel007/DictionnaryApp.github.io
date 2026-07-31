using System.Text;

namespace DicoWeb;

/// <summary>
/// Génération de dictionnaires de mots de passe, reproduite depuis l'application
/// WPF « Générateur de Dictionnaire de Mots de Passe » (Oumar Diogo Bah &
/// Eli Daniel Senyo). Construit le jeu de caractères autorisés puis énumère
/// toutes les combinaisons pour chaque longueur (compteur type « odomètre »).
/// L'énumération est paresseuse (yield) : comme l'app d'origine qui écrit dans
/// un fichier, elle peut produire des millions de mots sans tout garder en mémoire.
/// </summary>
public static class Generateur
{
    public record Options(
        int Min, int Max,
        bool Lower, bool Upper, bool Digits,
        string? LowerCustom, string? UpperCustom, string? NumberCustom,
        string? Specials, string? CustomSpecial);

    // Reproduit GetAllowedCharacters() : un champ perso remplace le set de base.
    public static string ConstruireCharset(Options o)
    {
        var chars = new StringBuilder();
        chars.Append(!string.IsNullOrWhiteSpace(o.LowerCustom) ? o.LowerCustom!.Trim()
            : (o.Lower ? "abcdefghijklmnopqrstuvwxyz" : ""));
        chars.Append(!string.IsNullOrWhiteSpace(o.UpperCustom) ? o.UpperCustom!.Trim()
            : (o.Upper ? "ABCDEFGHIJKLMNOPQRSTUVWXYZ" : ""));
        chars.Append(!string.IsNullOrWhiteSpace(o.NumberCustom) ? o.NumberCustom!.Trim()
            : (o.Digits ? "0123456789" : ""));
        if (!string.IsNullOrEmpty(o.Specials)) chars.Append(o.Specials);
        if (!string.IsNullOrWhiteSpace(o.CustomSpecial)) chars.Append(o.CustomSpecial!.Trim());
        return new string(chars.ToString().Distinct().ToArray());
    }

    // Reproduit CalculateTotalCombinations(). Renvoie aussi si le total dépasse
    // la capacité d'un entier 64 bits (nombres astronomiques).
    public static (long total, bool astronomique) CalculerTotal(int min, int max, int taille)
    {
        double t = 0;
        for (int len = min; len <= max; len++)
            t += Math.Pow(taille, len);
        if (t > (double)long.MaxValue) return (long.MaxValue, true);
        return ((long)t, false);
    }

    /// <summary>Énumère paresseusement toutes les combinaisons (reproduit la boucle + IncrementIndices).</summary>
    public static IEnumerable<string> Enumerer(string charset, int min, int max)
    {
        if (charset.Length == 0) yield break;
        for (int length = min; length <= max; length++)
        {
            var indices = new int[length];
            do
            {
                var c = new char[length];
                for (int i = 0; i < length; i++) c[i] = charset[indices[i]];
                yield return new string(c);
            }
            while (Incrementer(indices, charset.Length));
        }
    }

    // Reproduit IncrementIndices().
    private static bool Incrementer(int[] indices, int maxValue)
    {
        for (int i = indices.Length - 1; i >= 0; i--)
        {
            if (indices[i] < maxValue - 1) { indices[i]++; return true; }
            indices[i] = 0;
        }
        return false;
    }
}
