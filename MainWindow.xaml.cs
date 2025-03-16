/*
 * Projet: Générateur de Dictionnaire de Mots de Passe
 * Auteurs: Oumar Diogo Bah et Eli Daniel Senyo
 * Description: Classe principale gérant la génération de dictionnaires de mots de passe
 *              avec différentes combinaisons de caractères et longueurs
 */

using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace PasswordDictionaryGenerator
{
    public partial class MainWindow : Window
    {
        // Variables membres pour suivre la progression
        private long totalPasswordsGenerated = 0;  // Nombre total de mots de passe générés
        private Stopwatch generationTimer;         // Chronomètre pour mesurer le temps écoulé
        private DateTime startTime;                // Heure de début de la génération

        // Constructeur
        public MainWindow()
        {
            InitializeComponent();
            generationTimer = new Stopwatch();
        }

        /// <summary>
        /// Gestionnaire d'événement pour le bouton de génération
        /// </summary>
        private async void GenerateButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Validation des entrées utilisateur
                if (!ValidateInputs())
                    return;

                // Récupération et validation des caractères autorisés
                string allowedChars = GetAllowedCharacters();
                if (allowedChars.Length == 0)
                {
                    MessageBox.Show("Veuillez sélectionner au moins un type de caractères.", "Erreur de validation");
                    return;
                }

                // Initialisation de la génération
                SetControlsEnabled(false);
                totalPasswordsGenerated = 0;
                startTime = DateTime.Now;
                generationTimer.Restart();

                int minLength = int.Parse(MinLengthTextBox.Text);
                int maxLength = int.Parse(MaxLengthTextBox.Text);

                // Affichage des paramètres de génération
                LogTextBox.Clear();
                LogTextBox.AppendText($"Début de la génération avec les caractères: {allowedChars}\n");
                LogTextBox.AppendText($"Longueur min: {minLength}, max: {maxLength}\n");

                // Lancement de la génération
                await GenerateDictionaryAsync(minLength, maxLength, allowedChars, OutputFileTextBox.Text);

                // Affichage des résultats
                generationTimer.Stop();
                LogTextBox.AppendText($"\nGénération terminée en {generationTimer.Elapsed.ToString(@"hh\:mm\:ss")}\n");
                LogTextBox.AppendText($"Total des mots de passe générés : {totalPasswordsGenerated:N0}\n");

                MessageBox.Show("Génération terminée avec succès!", "Succès", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de la génération: {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                // Réinitialisation de l'interface
                SetControlsEnabled(true);
                GenerationProgress.Value = 0;
                TimeRemainingTextBlock.Text = "--:--:--";
            }
        }

        /// <summary>
        /// Valide les entrées utilisateur (longueurs et fichier de sortie)
        /// </summary>
        private bool ValidateInputs()
        {
            // Validation de la longueur minimale
            if (!int.TryParse(MinLengthTextBox.Text, out int minLength) || minLength < 1)
            {
                MessageBox.Show("La longueur minimale doit être un nombre positif.", "Erreur de validation");
                return false;
            }

            // Validation de la longueur maximale
            if (!int.TryParse(MaxLengthTextBox.Text, out int maxLength) || maxLength < minLength)
            {
                MessageBox.Show("La longueur maximale doit être supérieure ou égale à la longueur minimale.", "Erreur de validation");
                return false;
            }

            // Validation du fichier de sortie
            if (string.IsNullOrEmpty(OutputFileTextBox.Text))
            {
                MessageBox.Show("Veuillez sélectionner un fichier de sortie.", "Erreur de validation");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Construit la chaîne de caractères autorisés en fonction des sélections utilisateur
        /// </summary>
        private string GetAllowedCharacters()
        {
            StringBuilder chars = new StringBuilder();

            // Ajout des lettres minuscules
            if (!string.IsNullOrWhiteSpace(CustomLowercaseTextBox.Text) &&
                CustomLowercaseTextBox.Foreground != System.Windows.Media.Brushes.Gray)
            {
                chars.Append(CustomLowercaseTextBox.Text.Trim());
            }
            else if (LowercaseCheckBox.IsChecked == true)
            {
                chars.Append("abcdefghijklmnopqrstuvwxyz");
            }

            // Ajout des lettres majuscules
            if (!string.IsNullOrWhiteSpace(CustomUppercaseTextBox.Text) &&
                CustomUppercaseTextBox.Foreground != System.Windows.Media.Brushes.Gray)
            {
                chars.Append(CustomUppercaseTextBox.Text.Trim());
            }
            else if (UppercaseCheckBox.IsChecked == true)
            {
                chars.Append("ABCDEFGHIJKLMNOPQRSTUVWXYZ");
            }

            // Ajout des chiffres
            if (!string.IsNullOrWhiteSpace(CustomNumbersTextBox.Text) &&
                CustomNumbersTextBox.Foreground != System.Windows.Media.Brushes.Gray)
            {
                chars.Append(CustomNumbersTextBox.Text.Trim());
            }
            else if (NumbersCheckBox.IsChecked == true)
            {
                chars.Append("0123456789");
            }

            // Ajout des caractères spéciaux sélectionnés
            if (SpecialHash.IsChecked == true) chars.Append("#");
            if (SpecialDollar.IsChecked == true) chars.Append("$");
            if (SpecialPercent.IsChecked == true) chars.Append("%");
            if (SpecialAnd.IsChecked == true) chars.Append("&");
            if (SpecialStar.IsChecked == true) chars.Append("*");
            if (SpecialQuestion.IsChecked == true) chars.Append("?");

            // Ajout des caractères personnalisés
            if (!string.IsNullOrWhiteSpace(CustomCharsTextBox.Text) &&
                CustomCharsTextBox.Foreground != System.Windows.Media.Brushes.Gray)
            {
                chars.Append(CustomCharsTextBox.Text.Trim());
            }

            // Retourne la chaîne sans doublons
            return new string(chars.ToString().Distinct().ToArray());
        }

        /// <summary>
        /// Active/désactive les contrôles de l'interface pendant la génération
        /// </summary>
        private void SetControlsEnabled(bool enabled)
        {
            GenerateButton.IsEnabled = enabled;
            MinLengthTextBox.IsEnabled = enabled;
            MaxLengthTextBox.IsEnabled = enabled;
            OutputFileTextBox.IsEnabled = enabled;
            BrowseButton.IsEnabled = enabled;
        }

        /// <summary>
        /// Gestionnaire du bouton Parcourir pour sélectionner le fichier de sortie
        /// </summary>
        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = "Fichiers texte (*.txt)|*.txt|Tous les fichiers (*.*)|*.*",
                DefaultExt = ".txt"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                OutputFileTextBox.Text = saveFileDialog.FileName;
            }
        }

        /// <summary>
        /// Méthode principale de génération du dictionnaire
        /// </summary>
        private async Task GenerateDictionaryAsync(int minLength, int maxLength, string allowedChars, string outputFile)
        {
            using (StreamWriter writer = new StreamWriter(outputFile))
            {
                // Calcul du nombre total de combinaisons pour toutes les longueurs
                long totalCombinations = CalculateTotalCombinations(minLength, maxLength, allowedChars.Length);
                long currentCombinations = 0;  // Compteur des combinaisons déjà générées
                LogTextBox.AppendText($"Nombre total de combinaisons à générer : {totalCombinations:N0}\n\n");

                // Génération pour chaque longueur de mot de passe
                for (int length = minLength; length <= maxLength; length++)
                {
                    // Calcul du nombre de combinaisons pour la longueur actuelle
                    long combinationsForLength = (long)Math.Pow(allowedChars.Length, length);

                    // Génération des mots de passe pour la longueur actuelle
                    await GeneratePasswordsOfLengthAsync(length, allowedChars, writer,
                                                       totalCombinations, currentCombinations);

                    // Mise à jour du compteur total et du log
                    currentCombinations += combinationsForLength;
                    LogTextBox.AppendText($"Génération des mots de passe de longueur {length} terminée\n");
                    LogTextBox.ScrollToEnd();
                }
            }
        }

        /// <summary>
        /// Calcule le nombre total de combinaisons possibles
        /// </summary>
        private long CalculateTotalCombinations(int minLength, int maxLength, int charSetLength)
        {
            long total = 0;
            for (int len = minLength; len <= maxLength; len++)
            {
                total += (long)Math.Pow(charSetLength, len);
            }
            return total;
        }

        /// <summary>
        /// Met à jour l'estimation du temps restant
        /// </summary>
        private void UpdateTimeRemaining(int currentStep, int totalSteps)
        {
            if (currentStep == 0) return;

            TimeSpan elapsedTime = generationTimer.Elapsed;

            // Calcul du nombre total de mots de passe
            long totalPasswords = CalculateTotalCombinations(
                int.Parse(MinLengthTextBox.Text),
                int.Parse(MaxLengthTextBox.Text),
                GetAllowedCharacters().Length);

            // Calcul de la progression
            double progressPercentage = (double)totalPasswordsGenerated / totalPasswords;

            if (progressPercentage > 0 && progressPercentage < 1)
            {
                // Estimation du temps restant
                double estimatedTotalSeconds = elapsedTime.TotalSeconds / progressPercentage;
                double remainingSeconds = estimatedTotalSeconds - elapsedTime.TotalSeconds;

                if (remainingSeconds > 0)
                {
                    TimeSpan remainingTime = TimeSpan.FromSeconds(remainingSeconds);

                    Dispatcher.Invoke(() =>
                    {
                        TimeRemainingTextBlock.Text = remainingTime.ToString(@"hh\:mm\:ss");
                        PasswordCountTextBlock.Text = totalPasswordsGenerated.ToString("N0");
                    });
                }
            }
        }

        /// <summary>
        /// Génère tous les mots de passe d'une longueur donnée
        /// </summary>

        private async Task GeneratePasswordsOfLengthAsync(int length, string allowedChars,
            StreamWriter writer, long totalCombinations, long previousCombinations)
        {
            // Initialisation des variables de suivi
            int[] indices = new int[length];  // Tableau pour générer les combinaisons
            long combinationsGenerated = 0;    // Compteur pour la longueur actuelle
            long combinationsForLength = (long)Math.Pow(allowedChars.Length, length);  // Total pour cette longueur

            do
            {
                // Génération du mot de passe actuel
                char[] password = new char[length];
                for (int i = 0; i < length; i++)
                {
                    password[i] = allowedChars[indices[i]];
                }

                // Écriture dans le fichier et mise à jour des compteurs
                await writer.WriteLineAsync(new string(password));
                totalPasswordsGenerated++;
                combinationsGenerated++;

                // Mise à jour de l'interface toutes les 1000 combinaisons
                if (combinationsGenerated % 1000 == 0)
                {
                    // Calcul de la progression globale
                    double progress = ((double)(previousCombinations + combinationsGenerated) / totalCombinations) * 100;

                    await Dispatcher.InvokeAsync(() =>
                    {
                        // Mise à jour de l'interface utilisateur
                        GenerationProgress.Value = progress;
                        PasswordCountTextBlock.Text = totalPasswordsGenerated.ToString("N0");

                        // Calcul et affichage du temps restant
                        TimeSpan elapsed = generationTimer.Elapsed;
                        double progressRatio = (double)(previousCombinations + combinationsGenerated) / totalCombinations;
                        if (progressRatio > 0)
                        {
                            // Estimation du temps total et restant
                            double totalSeconds = elapsed.TotalSeconds / progressRatio;
                            double remainingSeconds = totalSeconds - elapsed.TotalSeconds;
                            TimeSpan remaining = TimeSpan.FromSeconds(remainingSeconds);
                            TimeRemainingTextBlock.Text = remaining.ToString(@"hh\:mm\:ss");
                        }
                    });
                }

            } while (IncrementIndices(indices, allowedChars.Length));  // Génération de la combinaison suivante

            // S'assurer que toutes les données sont écrites dans le fichier
            await writer.FlushAsync();
        }

        /// <summary>
        /// Incrémente les indices pour générer la combinaison suivante
        /// </summary>
        private bool IncrementIndices(int[] indices, int maxValue)
        {
            for (int i = indices.Length - 1; i >= 0; i--)
            {
                if (indices[i] < maxValue - 1)
                {
                    indices[i]++;
                    return true;
                }
                indices[i] = 0;
            }
            return false;
        }

        /// <summary>
        /// Gère le focus sur les zones de texte personnalisées
        /// </summary>
        private void TextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            TextBox textBox = (TextBox)sender;
            if (textBox.Foreground == System.Windows.Media.Brushes.Gray)
            {
                textBox.Text = "";
                textBox.Foreground = System.Windows.Media.Brushes.Black;
            }
        }

        /// <summary>
        /// Gère la perte de focus sur les zones de texte personnalisées
        /// </summary>
        private void TextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            TextBox textBox = (TextBox)sender;
            if (string.IsNullOrWhiteSpace(textBox.Text))
            {
                string placeholderText = "";
                if (textBox == CustomLowercaseTextBox)
                    placeholderText = "Exemple: abcd (lettres minuscules supplémentaires)";
                else if (textBox == CustomUppercaseTextBox)
                    placeholderText = "Exemple: ABCD (lettres majuscules supplémentaires)";
                else if (textBox == CustomNumbersTextBox)
                    placeholderText = "Exemple: 123 (chiffres supplémentaires)";
                else if (textBox == CustomCharsTextBox)
                    placeholderText = "Exemple: @!+ (caractères spéciaux supplémentaires)";

                textBox.Text = placeholderText;
                textBox.Foreground = System.Windows.Media.Brushes.Gray;
            }
        }
    }
}