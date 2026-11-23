using UnityEngine;

namespace ItHunts.Settings
{
    /// <summary>
    /// Effet d'une amélioration (nuit 4, point 2, D-100) : un membre par amélioration de docs/design/AMELIORATIONS.md, numéroté
    /// par famille (génériques 0 à 29, renard 30 à 39, caméléon 40 à 49, koala 50 à 59, hibou 60 à 69). Les numéros sont écrits
    /// dans les assets : ne pas les changer une fois publiés. Les anciens effets provisoires (balles lourdes, gâchette vive, long
    /// canon, rechargement éclair, barillet garni, balles incendiaires, œil du caméléon) sont devenus des rangs des 70.
    /// </summary>
    public enum UpgradeKind
    {
        // Génériques : parade (G1 à G4)
        Riposte = 0,                // G1 : une parade réussie renvoie une onde (PV, rayon ; Or repousse ; Mythique étourdit)
        GardeLarge = 1,             // G2 : fenêtre de parade + s (Or : tolérance tardive ×2 ; Mythique : pare les projectiles)
        SecondSouffle = 2,          // G3 : une parade rend des PV (Or : + une charge de mouvement ; Mythique : + une traction)
        RythmeDeFer = 3,            // G4 : parades d'affilée : +dégâts par pile (max) ; Mythique : le temps ralentit à 3
        // Orbe et âmes (G5 à G9)
        GrandeOrbe = 4,             // G5 : rayon de collecte × (1 + v) ; Mythique : à travers les murs
        Aspiration = 5,             // G6 : vitesse d'aspiration × (1 + v) ; Or : âmes au sol 45 s ; Mythique : 1 PV par âme
        AmesGourmandes = 6,         // G7 : âmes par kill × (1 + v) ; Or : +1 à plus de 14 m/s ; Mythique : doublées
        Marchandage = 7,            // G8 : prix du marchand × (1 − v) ; Or : hausse ÷ 2 ; Mythique : 1re montée gratuite par acte
        OrbeVorace = 8,             // G9 : chaque âme réduit les recharges de v s ; Or : aussi la répulsion ; Mythique : kill = traction
        // Traction (G10 à G13)
        TractionRapide = 9,         // G10 : recharge d'une charge − v s ; Mythique : arrivée à plus de 12 m/s rend la charge
        Harpon = 10,                // G11 : traction vers les ennemis (goules, puis volants) ; Or frappe ; Diamant étourdit ; Mythique explose
        ElanDeTraction = 11,        // G12 : accélération de traction × (1 + v) ; Mythique : tractions sans charge 4 s après une parade
        Sillage = 12,               // G13 : traînée qui blesse (PV) et ralentit (part) les ennemis traversés ; Mythique enflamme
        // Répulsion (G14 à G17)
        OndeBlessante = 13,         // G14 : l'onde blesse (PV) ; Or ralentit ; Mythique : double onde
        OndeDeChoc = 14,            // G15 : cône + v °, portée + v2 m ; Or : objets 20 % plus vite ; Mythique : renvoie les débris
        Rebond = 15,                // G16 : dégâts d'impact contre un mur × v ; Or étourdit 1 s ; Mythique : explose s'il meurt
        ChargeDeRepulsion = 16,     // G17 : charges de répulsion en plus (v), recharge − v2 s ; Mythique : saut au sol de 4 m
        // Dégâts et combat (G18 à G22)
        ProjectilesLourds = 17,     // G18 : dégâts des objets × (1 + v) ; Or : pas de casse au 1er impact ; Mythique : retour
        CoeurSolide = 18,           // G19 : + v PV max ; Or : régénération hors combat ; Mythique : une seconde chance par run
        Detonateur = 19,            // G20 : un étourdi ou paralysé qui meurt explose (PV, rayon) ; Or étourdit ; Mythique chaîne
        BriseurDOs = 20,            // G21 : dégâts × v contre étourdis / paralysés ; Or : +0.5 s d'étourdissement ; Mythique exécute
        Predateur = 21,             // G22 : +v par kill (5 s, max v2 piles) ; Or : +0.5 m/s ; Mythique : à 5 piles, étourdit
        // Mouvement (G23 à G27)
        ChargeEnPlus = 22,          // G23 : + v charges de mouvement ; Argent / Diamant : recharges ; Mythique : dashs gratuits
        SemellesDeVent = 23,        // G24 : course + v m/s ; Or : perte d'élan 30 % plus lente ; Mythique : plafond au sol +2
        PasDeChat = 24,             // G25 : glissade, wall slide, wall jump ; Mythique : même mur et charges rendues
        VifArgent = 25,             // G26 : gain de bunny hop × (1 + v) ; Or : contrôle en l'air ; Mythique : traverse les goules
        ChuteAmortie = 26,          // G27 : chute dans le vide − v PV ; atterrissage fort qui frappe (v2 PV, v3 m) ; Mythique explose
        // Synergies et utilitaires (G28 à G30)
        Allumette = 27,             // G28 : un ennemi en feu qui meurt enflamme ses voisins (m, s) ; Or ralentit ; Mythique étourdit
        SouffleCourt = 28,          // G29 : détection du traqueur × (1 − v) ; Mythique : 2 s d'invisibilité après une parade
        OeilDuCollectionneur = 29,  // G30 : + v choix aux coffres, portes × (1 − v2) ; Or : montées dès l'acte I ; Mythique : Argent direct

        // Renard : lanterne de feu (R-P1, R-P2, R1 à R8)
        FourrureDeBraise = 30,      // R-P1 (passif) : insensible au feu ; qui vous frappe brûle v s ; Or : coups −10 % ; Mythique repousse
        Flair = 31,                 // R-P2 (passif) : coffres et âmes visibles à v m ; rayon de collecte × (1 + v2)
        LongSouffle = 32,           // R1 : portée du jet + v m ; Mythique : rebond sur le décor
        FlammeLarge = 33,           // R2 : cône + v ° ; Or : pousse ; Mythique : cercle complet
        MecheVive = 34,             // R3 : recharge − v s ; Or : un kill en feu rend 0.5 s ; Mythique : finit la recharge
        Braises = 35,               // R4 : brûlure + v s ; Or : 2 brûlures se cumulent ; Mythique : 3, et paralysie à 3
        FeuGregeois = 36,           // R5 : flaques de feu (s, m) ; Or ralentit ; Mythique étourdit à l'entrée
        Combustion = 37,            // R6 : un ennemi en feu qui meurt explose (PV, m) ; Or : voisins brûlent ; Mythique chaîne
        SouffleDouble = 38,         // R7 : charges de jet en plus (v), vitesse de la 2e (v2) ; Mythique : jet long
        FouleeArdente = 39,         // R8 : à plus de 14 m/s, traînée de feu (s, PV/s) ; Mythique : boule de feu au dash

        // Caméléon : barillet (C-P1, C-P2, C1 à C8)
        PeauChangeante = 40,        // C-P1 (passif) : immobile v s → détection × (1 − v2) ; Mythique : 1er tir critique ×3
        OeilFendu = 41,             // C-P2 (passif) : zone de tête × (1 + v), chaîne de critiques + v2 (max v3) ; corps critique Or / Mythique
        BallesParalysantes = 42,    // C1 : chaque touche paralyse v s ; Or : paralysé +15 % ; Mythique : 3 touches → projeté
        CadavreExplosif = 43,       // C2 : la balle qui tue explose (PV, m, chaîne v3) ; Mythique paralyse
        BallePerforante = 44,       // C3 : traverse v ennemis (v2 gardés) ; Or : objets ; Mythique : dernier paralysé
        Ricochet = 45,              // C4 : v rebonds (v2 dégâts) ; Or : portée 18 m ; Mythique : headshot gardé
        MainLeste = 46,             // C5 : + v balles par kill, rechargement − v2 ; Mythique : barillet de 8, headshot recharge tout
        SixiemeChambre = 47,        // C6 : dernière balle × v ; Argent étourdit ; Or perce ; Diamant explose ; Mythique recharge
        Eventail = 48,              // C7 : clic tenu = rafale (dispersion v °, dégâts × v2) ; Or pousse ; Mythique paralyse
        LangueDeCameleon = 49,      // C8 : balle-harpon : objet à v m ramené ; Or : volants ; Diamant / Mythique : goules tirées

        // Koala : épée en os (K-P1, K-P2, K1 à K8)
        PeauEpaisse = 50,           // K-P1 (passif) : dégâts reçus × (1 − v), + v2 PV max ; Or : pas de recul ; Mythique : sous 25 %
        GriffesGrimpantes = 51,     // K-P2 (passif) : wall slide × (1 − v), hissage + v2 m, wall jump + v3 m ; Mythique : accroche
        LameLongue = 52,            // K1 : portée + v m, arc + v2 ° ; Or : aide à 8 m ; Mythique : 3e coup en cercle
        Enchainement = 53,          // K2 : combo × (1 − v), fenêtre + v2 s ; Or : pas de pause ; Mythique : 4e coup tourbillon
        Fente = 54,                 // K3 : fente + v m, dégâts × v2 ; Mythique : traverse et étourdit
        OndeDeTaille = 55,          // K4 : coup à plus de 12 m/s → onde (m, PV) ; Mythique étourdit
        OsBrise = 56,               // K5 : saignement (PV/s, s) ; Or : 2 se cumulent ; Mythique : un saignant qui meurt soigne 8
        GardeDOs = 57,              // K6 : parade + v ms, coup suivant × v2 ; Or : la parade frappe ; Mythique : riposte
        BriseGarde = 58,            // K7 : coups × v sur étourdi / paralysé / saignant ; Or prolonge ; Mythique exécute
        ElanDuBucheron = 59,        // K8 : kill à l'épée : charge + v m/s pendant v2 s ; Or : dash gratuit ; Mythique : traction

        // Hibou : livre à lien (H-P1, H-P2, H1 à H8)
        VisionNocturne = 60,        // H-P1 (passif) : contour des ennemis à v m ; Mythique : traqueur à travers les murs
        Plumes = 61,                // H-P2 (passif) : plané (gravité × v) ; pas de dégâts de chute ; Mythique : +1 saut
        LienMultiple = 62,          // H1 : v liens (v2 de drain chacun) ; Diamant : +10 % par lien ; Mythique : dégâts partagés
        Chainage = 63,              // H2 : v sauts à v3 m, v2 du drain ; Mythique : la cible morte passe le lien
        LienVorace = 64,            // H3 : drain + v PV/s ; Or : ralentit 25 % ; Mythique : 50 % et plus de bond
        Sangsue = 65,               // H4 : part rendue v ; Or : bouclier (max v2) ; Mythique : absorbe un coup du boss
        LonguePortee = 66,          // H5 : portées + v m ; Or : vue perdue 1.5 s ; Mythique : à travers les murs 1 s
        LienDAncrage = 67,          // H6 : la cible liée est une ancre de traction ; Argent freine ; Or frappe ; Mythique cloue
        PagesBrulantes = 68,        // H7 : mort sous le lien : + v âmes, recharge − v2 ; Or : relance ; Mythique : onde
        SoinLie = 69,               // H8 : soin v PV/s, + v2 PV/s pour vous ; Mythique : ressuscite un allié
    }

    /// <summary>Personnage d'une amélioration (ses 10 pour son porteur seulement) ; Aucun : générique, pour tous.</summary>
    public enum UpgradeCharacter : byte { Aucun = 0, Renard = 1, Cameleon = 2, Koala = 3, Hibou = 4 }

    /// <summary>
    /// Une amélioration (étape 7, D-043 ; rangs : nuit 4, D-100) : nom, icône (sprite de la DA, sinon symbole court et couleur),
    /// effet, **5 rangs** (Bronze, Argent, Or, Diamant, Mythique) avec une valeur par rang pour un, deux ou trois paramètres et
    /// une description courte par rang ; personnage (vide : générique) ; passif (un seul porté à la fois). Un asset par
    /// amélioration, 70 au total (docs/design/AMELIORATIONS.md) ; une amélioration possédée **monte de rang** au lieu de se
    /// cumuler. Les anciens champs (amount, forArtifact, rarity, merchantPrice) restent lus : amount vaut la valeur Bronze quand
    /// rankValues est vide.
    /// </summary>
    [CreateAssetMenu(fileName = "Upgrade", menuName = "ItHunts/Amélioration")]
    public sealed class UpgradeDefinition : ScriptableObject
    {
        /// <summary>Rangs : 0 Bronze, 1 Argent, 2 Or, 3 Diamant, 4 Mythique.</summary>
        public const int MaxRank = 4;
        public const int RankCount = 5;

        [Tooltip("Code du document (G1, R-P1, C3…) : repère pour la DA et les tests.")]
        public string code = "";
        public string displayName = "Amélioration";
        [Tooltip("Une ligne : ce que fait l'amélioration (sans les chiffres, qui dépendent du rang).")]
        public string description = "";
        [Tooltip("Icône de la DA (70 icônes) ; vide : le symbole court sur fond sombre.")]
        public Sprite icon;
        [Tooltip("Icône greybox : symbole court affiché sur l'orbe, et couleur de l'orbe.")]
        public string iconSymbol = "+";
        [ColorUsage(false, true)] public Color color = new Color(1f, 0.8f, 0.3f);
        public UpgradeKind kind;
        [Tooltip("Personnage (ses 10 pour son porteur seulement) ; Aucun : pour tous.")]
        public UpgradeCharacter character = UpgradeCharacter.Aucun;
        [Tooltip("Passif de personnage : un seul porté à la fois ; en prendre un autre remplace le premier (rang perdu).")]
        public bool isPassive;
        [Tooltip("Paramètre principal, une valeur par rang (Bronze, Argent, Or, Diamant, Mythique).")]
        public float[] rankValues = new float[0];
        [Tooltip("Second paramètre (optionnel), une valeur par rang.")]
        public float[] rankValues2 = new float[0];
        [Tooltip("Troisième paramètre (optionnel), une valeur par rang.")]
        public float[] rankValues3 = new float[0];
        [Tooltip("Description courte de chaque rang (celle du tableau) ; le Mythique décrit son effet nouveau.")]
        public string[] rankTexts = new string[0];
        [Tooltip("Synergies (texte) : avec quelles autres améliorations elle se combine.")]
        public string synergies = "";
        [Tooltip("Ancienne valeur unique (avant les rangs) : valeur Bronze si rankValues est vide.")]
        public float amount = 1f;
        [Tooltip("Ancien champ : réservée à cet artefact. Remplacé par character ; encore respecté par les tirages.")]
        public ArtifactDefinition forArtifact;
        [Tooltip("Ancien champ : rareté (0 commune, 1 rare, 2 légendaire). Sans effet depuis les rangs.")]
        [Range(0, 2)] public int rarity;
        [Tooltip("Ancien prix fixe : remplacé par EconomySettings.rankPrices.")]
        [Min(0)] public int merchantPrice = 60;

        public bool Generic => character == UpgradeCharacter.Aucun;

        /// <summary>Valeur du paramètre principal au rang donné (Bronze = 0) ; amount si les rangs ne sont pas renseignés.</summary>
        public float Value(int rank) => At(rankValues, rank, amount);

        /// <summary>Second paramètre au rang donné (0 s'il n'existe pas).</summary>
        public float Value2(int rank) => At(rankValues2, rank, 0f);

        /// <summary>Troisième paramètre au rang donné (0 s'il n'existe pas).</summary>
        public float Value3(int rank) => At(rankValues3, rank, 0f);

        /// <summary>Texte du rang (celui du tableau) ; la description générale si le rang n'en a pas.</summary>
        public string RankText(int rank)
        {
            if (rankTexts != null && rankTexts.Length > 0)
                return rankTexts[Mathf.Clamp(rank, 0, rankTexts.Length - 1)];
            return description;
        }

        /// <summary>Nom du rang : Bronze, Argent, Or, Diamant, Mythique.</summary>
        public static string RankName(int rank) => Mathf.Clamp(rank, 0, MaxRank) switch
        {
            0 => "Bronze",
            1 => "Argent",
            2 => "Or",
            3 => "Diamant",
            _ => "Mythique",
        };

        /// <summary>« Nom · Bronze → Argent » (montée) ou « Nom · Bronze » (nouvelle).</summary>
        public string OfferTitle(int fromRank, int toRank) =>
            fromRank < 0 ? $"{displayName} · {RankName(toRank)}" : $"{displayName} · {RankName(fromRank)} → {RankName(toRank)}";

        private static float At(float[] values, int rank, float fallback)
        {
            if (values == null || values.Length == 0)
                return fallback;
            return values[Mathf.Clamp(rank, 0, values.Length - 1)];
        }
    }
}
