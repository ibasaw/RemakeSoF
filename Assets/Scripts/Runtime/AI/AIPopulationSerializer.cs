#if EANN_ENABLED
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

namespace Tolik.RemakeSoF.Runtime.AI
{
    /// <summary>
    /// Serialisiert und deserialisiert GA-Populationen (Genotype-Weights + Generationszaehler)
    /// als JSON-Dateien in StreamingAssets/AI/. Ermoeglicht persistentes Training ueber Neustarts.
    /// Speichert pro Rolle (Seeker/Hider) eine eigene Datei.
    /// Format: JSON mit Generation, PopulationSize, ParameterCount und float-Array pro Genotype.
    /// </summary>
    public static class AIPopulationSerializer
    {
        /// <summary>Unterverzeichnis in StreamingAssets fuer AI-Populationsdaten.</summary>
        private const string k_SubDirectory = "AI";

        /// <summary>Dateiname-Prefix fuer Seeker-Population.</summary>
        private const string k_SeekerFileName = "seeker_population.json";

        /// <summary>Dateiname-Prefix fuer Hider-Population.</summary>
        private const string k_HiderFileName = "hider_population.json";

        /// <summary>
        /// Gibt den vollstaendigen Dateipfad fuer eine Rolle zurueck.
        /// </summary>
        private static string GetFilePath(bool isSeeker)
        {
            string directory = Path.Combine(Application.streamingAssetsPath, k_SubDirectory);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            return Path.Combine(directory, isSeeker ? k_SeekerFileName : k_HiderFileName);
        }

        /// <summary>
        /// Speichert die Population eines GA als JSON-Datei.
        /// Wird nach jeder Generation aufgerufen (Auto-Save).
        /// </summary>
        /// <param name="ga">Der GeneticAlgorithm dessen Population gespeichert wird.</param>
        /// <param name="isSeeker">True = Seeker, False = Hider.</param>
        public static void Save(GeneticAlgorithm ga, bool isSeeker)
        {
            if (ga == null)
            {
                return;
            }

            List<Genotype> population = ga.GetPopulationCopy();
            if (population.Count == 0)
            {
                return;
            }

            PopulationData data = new()
            {
                Generation = ga.GenerationCount,
                PopulationSize = (uint)population.Count,
                ParameterCount = (uint)population[0].ParameterCount,
                Genotypes = new float[population.Count][],
            };

            for (int i = 0; i < population.Count; i++)
            {
                data.Genotypes[i] = population[i].GetParameterCopy();
            }

            string json = JsonUtility.ToJson(new PopulationDataWrapper(data), true);
            string filePath = GetFilePath(isSeeker);

            try
            {
                File.WriteAllText(filePath, json, Encoding.UTF8);
                Debug.Log($"[AI·Save] {(isSeeker ? "Seeker" : "Hider")} gespeichert: Gen={data.Generation} | Pop={data.PopulationSize} | Params={data.ParameterCount} | {filePath}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[AI·Save] Fehler: {e.Message}");
            }
        }

        /// <summary>
        /// Laedt eine gespeicherte Population aus der JSON-Datei.
        /// Gibt null zurueck wenn keine Datei existiert oder das Format ungueltig ist.
        /// </summary>
        /// <param name="isSeeker">True = Seeker, False = Hider.</param>
        /// <param name="genotypes">Geladene Genotypes (out).</param>
        /// <param name="generation">Gespeicherte Generationszahl (out).</param>
        /// <returns>True wenn erfolgreich geladen.</returns>
        public static bool TryLoad(bool isSeeker, out List<Genotype> genotypes, out uint generation)
        {
            genotypes = null;
            generation = 1;

            string filePath = GetFilePath(isSeeker);
            if (!File.Exists(filePath))
            {
                return false;
            }

            try
            {
                string json = File.ReadAllText(filePath, Encoding.UTF8);
                PopulationDataWrapper wrapper = JsonUtility.FromJson<PopulationDataWrapper>(json);
                if (wrapper == null || wrapper.Genotypes == null || wrapper.Genotypes.Length == 0)
                {
                    Debug.LogWarning($"[AI·Load] Ungueltige Datei: {filePath}");
                    return false;
                }

                genotypes = new List<Genotype>(wrapper.Genotypes.Length);
                for (int i = 0; i < wrapper.Genotypes.Length; i++)
                {
                    if (wrapper.Genotypes[i] == null || wrapper.Genotypes[i].Weights == null)
                    {
                        continue;
                    }
                    genotypes.Add(new Genotype(wrapper.Genotypes[i].Weights));
                }

                generation = wrapper.Generation;

                Debug.Log($"[AI·Load] {(isSeeker ? "Seeker" : "Hider")} geladen: Gen={generation} | Pop={genotypes.Count} | Params={wrapper.ParameterCount} | {filePath}");
                return genotypes.Count > 0;
            }
            catch (Exception e)
            {
                Debug.LogError($"[AI·Load] Fehler: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// Prueft ob eine gespeicherte Population fuer die Rolle existiert.
        /// </summary>
        public static bool HasSavedPopulation(bool isSeeker)
        {
            return File.Exists(GetFilePath(isSeeker));
        }

        /// <summary>
        /// Interne Datenstruktur fuer die Population (nicht serialisiert, nur intern).
        /// </summary>
        private struct PopulationData
        {
            public uint Generation;
            public uint PopulationSize;
            public uint ParameterCount;
            public float[][] Genotypes;
        }

        /// <summary>
        /// JsonUtility-kompatible Wrapper-Klasse (JsonUtility kann keine jagged Arrays direkt).
        /// Speichert Generation, PopSize, ParamCount und Array von GenotypeData.
        /// </summary>
        [Serializable]
        private class PopulationDataWrapper
        {
            /// <summary>Aktuelle Generationszahl.</summary>
            public uint Generation;

            /// <summary>Populationsgroesse.</summary>
            public uint PopulationSize;

            /// <summary>Anzahl Parameter pro Genotype.</summary>
            public uint ParameterCount;

            /// <summary>Array aller Genotypes mit ihren Weights.</summary>
            public GenotypeData[] Genotypes;

            /// <summary>Parameterloser Konstruktor fuer JsonUtility.</summary>
            public PopulationDataWrapper() { }

            /// <summary>Konstruktor aus interner PopulationData.</summary>
            public PopulationDataWrapper(PopulationData data)
            {
                Generation = data.Generation;
                PopulationSize = data.PopulationSize;
                ParameterCount = data.ParameterCount;
                Genotypes = new GenotypeData[data.Genotypes.Length];
                for (int i = 0; i < data.Genotypes.Length; i++)
                {
                    Genotypes[i] = new GenotypeData { Weights = data.Genotypes[i] };
                }
            }
        }

        /// <summary>
        /// JsonUtility-kompatible Klasse fuer einen einzelnen Genotype.
        /// </summary>
        [Serializable]
        private class GenotypeData
        {
            /// <summary>NN-Weights als float-Array.</summary>
            public float[] Weights;
        }
    }
}
#endif
