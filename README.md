<div align="center">

<img src="site/img/createur-avatar.jpg" width="96" height="96" alt="LD, créateur de LunaQuest">

# LunaQuest

**Joue à tes jeux PC VR sur ton Meta Quest par câble USB, sans l'appli Meta.**

[Site officiel](https://lunaquest-by-ld.netlify.app) · [Télécharger la dernière version](../../releases/latest) · [Mentions légales](https://lunaquest-by-ld.netlify.app/mentions-legales.html)

![Version](https://img.shields.io/badge/version-0.4.0-ff1f2e) ![Windows](https://img.shields.io/badge/Windows-10%20%7C%2011-2d5bff) ![Licence](https://img.shields.io/badge/licence-MIT-8fb2ff)

</div>

<p align="center"><img src="site/img/connexion.png" width="720" alt="Fenêtre de LunaQuest : état du casque, bouton Connecter et journal"></p>

LunaQuest est un lanceur pour Windows qui fait passer le streaming [ALVR](https://github.com/alvr-org/ALVR) **par le câble USB** au lieu du Wi-Fi.
Une superbe alternative si ta connexion sans fil n'est pas au top.

## Nouveau dans la 0.4.0

<p align="center"><img src="site/img/parametres.png" width="720" alt="Page Paramètres : langue et thème de LunaQuest"></p>

- **4 langues** : français, anglais, chinois et japonais.
- **8 thèmes** : noir, blanc, bleu, rouge, rose, violet, vert, jaune.
- **Animation de démarrage** (un coup de sabre ouvre l'écran) et **tutoriel guidé** qui présente chaque partie de l'appli.
- **Perfs dans le casque** : enfonce les deux joysticks en même temps pour voir images par seconde, latence, débit et pertes.
- **Filmer** : enregistre le flux du casque en vidéo.
- **Base stations** : allumage et veille par le Bluetooth du PC, à la main ou automatiquement en FBT.
- **Alertes de batterie**, **lancement avec Windows** (icône près de l'horloge) et **vérification des mises à jour**.
- **Correctif** : LunaQuest ne retire plus les autres pilotes SteamVR (Space Calibrator, Virtual Desktop…) quand il ajoute ALVR.
- Le mode FBE Mix est retiré.

## Le corps entier (FBE)

<p align="center"><img src="site/img/corps.png" width="720" alt="Page Corps et trackers : modes FBE et aperçu 3D en direct des appareils"></p>

- **FBE Unique** : tout le corps estimé par les caméras du Quest (torse, hanches, coudes, genoux, pieds), sans rien acheter.
- **FBT** : seulement tes vrais trackers (Vive, Tundra…) et tes bases.
- **Aperçu 3D en direct** de tous tes appareils : position, suivi (ou perte de suivi) et batterie.

## Fonctionnalités

- **Connexion en un clic** : tunnel USB, SteamVR et l'appli du casque se lancent tout seuls.
- **ALVR inclus** : la partie PC d'ALVR est installée avec LunaQuest, rien à télécharger à part.
- **Réglages ALVR intégrés** : images par seconde, débit, codec, résolution (jusqu'à 3840 par œil), son, micro.
- **Préréglages perso** : enregistre tes réglages par jeu (fournis : Beat Saber et VRChat).
- **Casque toujours éveillé**, pour éviter les jeux figés au lancement.
- **Écran du PC dans le casque** : double appui sur le bouton menu gauche.
- **Mémoire de zone** pour garder la calibration de tes trackers.
- **Appli LunaQuest pour le casque** : client ALVR au thème lunaire, couleurs plus vives.
- **Déconnexion automatique** quand tu quittes l'appli du casque.

## Installation

1. Télécharge `LunaQuest-0.4.0-Setup.zip` dans les [releases](../../releases/latest).
2. Décompresse-le et lance `LunaQuest-Setup.exe`.
   L'installateur n'est pas encore signé : si Windows affiche « Windows a protégé votre ordinateur »,
   clique sur **Informations complémentaires** puis **Exécuter quand même**.
3. Ouvre LunaQuest depuis le menu Démarrer et suis l'onglet **Guide**.

Il te faut : Windows 10 ou 11, un Meta Quest 2, 3, 3S ou Pro en mode développeur, un câble USB-C (USB 3 conseillé),
Steam et SteamVR.

## Compiler depuis les sources

Le lanceur est écrit en C# et se compile avec le compilateur fourni avec Windows (.NET Framework 4.x), sans rien installer :

```powershell
powershell -ExecutionPolicy Bypass -File build.ps1
```

Résultat : `bin\LunaQuest.exe`, `bin\LunaQuest-Setup.exe` et `dist\LunaQuest-0.4.0-Setup.zip`.
L'appli du casque et la partie PC d'ALVR se compilent à part : voir [`alvr/`](alvr/README.md).

| Dossier | Contenu |
|---|---|
| `src/` | Code du lanceur (`LunaQuest.cs`), de l'écran PC et des perfs dans le casque (`DesktopViewer.cs`), des base stations (`BaseStations.cs`), du lien avec ALVR (`AlvrLink.cs`), des traductions (`Lang.cs`), de l'animation et du tutoriel (`Intro.cs`) et de l'installateur (`Setup.cs`) |
| `alvr/` | Modifications d'ALVR : appli du casque et partie PC (suivi du corps FBE) |
| `site/` | Site web de LunaQuest |
| `res/` | Licences des composants tiers et réglages ALVR de départ |

## Crédits

- **Idée, conception et tests :** LD
- **Réalisation :** développé avec l'IA [Claude](https://www.anthropic.com/claude) d'Anthropic, sous la direction de LD
- **Streaming :** basé sur [ALVR](https://github.com/alvr-org/ALVR) (licence MIT) et OpenVR de Valve (licence BSD 3-Clause)

## Licence

LunaQuest est distribué sous [licence MIT](LICENSE). Les composants tiers gardent leur licence : voir [`THIRD-PARTY-NOTICES.txt`](res/THIRD-PARTY-NOTICES.txt).

LunaQuest est un projet indépendant, **non affilié** à Meta, Valve, au projet ALVR ni à Anthropic.
Meta Quest est une marque de Meta Platforms, Inc. Steam et SteamVR sont des marques de Valve Corporation.
