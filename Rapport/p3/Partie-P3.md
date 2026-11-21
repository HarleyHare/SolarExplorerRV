# Personne 3 — Textures, lumière, son et licences

> Brouillon. Les passages *à compléter* et les captures sont à faire dans Unity.

## 1. Textures

### Source et licence
Les textures viennent de **Solar System Scope** (https://www.solarsystemscope.com/textures/). Elles sont basées sur des images de la NASA et distribuées sous licence **CC BY 4.0** : utilisation et modification autorisées, à condition de citer la source (voir `CREDITS.md`).

### Choix de la résolution 2K
Chaque texture fait 2048 × 1024 pixels (projection équirectangulaire, prévue pour être enroulée sur une sphère). Le 2K est un compromis :
- assez détaillé pour reconnaître chaque planète en VR ;
- assez léger pour la fluidité en VR et pour le quota Git LFS du dépôt.

### Intégration dans Unity
1. Les 11 textures utilisées sont placées dans `Assets/Textures` avec des noms précis (`2k_earth_daymap`, `2k_mars`...) : le constructeur de scène les retrouve par leur nom et crée un matériau par astre (`M_Terre`, `M_Mars`...).
2. Pour les anneaux de Saturne (`2k_saturn_ring_alpha.png`), l'option **Alpha Is Transparency** est cochée : la transparence de l'image laisse voir les espaces entre les anneaux.
3. Le ciel étoilé (`2k_stars_milky_way`) est appliqué comme **skybox** avec le shader *Skybox/Panoramic* : il entoure le joueur à 360°.

*Capture à faire : la fenêtre Project avec les textures, et l'Inspector de la texture des anneaux.*

## 2. Lumière

### Principe
Le Soleil contient une **lumière ponctuelle** (*Point Light*) qui éclaire dans toutes les directions. Avec URP, son intensité diminue avec le carré de la distance : une planète deux fois plus loin reçoit quatre fois moins de lumière.

### Réglages retenus

| Réglage | Valeur | Raison |
|---|---|---|
| Intensité de la lumière du Soleil | 1000 | Valeur d'origine (300) trop faible : les planètes lointaines étaient presque noires |
| Lumière ambiante | (0,55 ; 0,55 ; 0,6) | Les faces de nuit restent lisibles au lieu d'être totalement noires |
| Auto-illumination des planètes (`PlanetGlow`) | 0,3 | La texture reste visible de loin et du côté nuit |
| Émission du Soleil | Texture du Soleil, intensité 1 | Le Soleil « brille » au lieu d'être une sphère éclairée de l'intérieur |
| Brouillard | Désactivé | Voir les difficultés ci-dessous |

### Lisibilité de la vue générale
Pour que les textures soient reconnaissables depuis la vue d'ensemble, les tailles ont été agrandies plus que les distances (représentation volontairement **pas à l'échelle**, comme prévu dans le cahier des charges) :

| Astre | Rayon dans Unity | Distance au Soleil dans Unity |
|---|---|---|
| Soleil | 15 | — |
| Mercure | 1,5 | 24 |
| Vénus | 2,7 | 33 |
| Terre | 3 | 42 |
| Mars | 2 | 51 |
| Jupiter | 9 | 70 |
| Saturne | 7,5 | 98 |
| Uranus | 5 | 124 |
| Neptune | 5 | 140 |

La vue générale a été rapprochée (35 m de haut, 110 m du Soleil) : en VR, l'utilisateur tourne la tête pour voir l'ensemble du système.

*Captures à faire : la scène avant et après les réglages de lumière.*

## 3. Son

*À compléter : musique choisie, source, licence, volume (0,4), lecture en boucle.*

## 4. Difficultés rencontrées et solutions

| Problème observé | Cause | Solution |
|---|---|---|
| Planètes grises et sans texture dès qu'elles sont un peu éloignées | Le modèle VR de Unity active un **brouillard** gris et dense, prévu pour une petite pièce | Brouillard désactivé dans le constructeur (`RenderSettings.fog = false`) |
| Soleil gris au lieu de lumineux | URP retirait l'émission du matériau à l'enregistrement (réglage d'illumination globale par défaut) | `globalIlluminationFlags = RealtimeEmissive` sur les matériaux émissifs |
| Certaines planètes perdaient leur texture | Des images avaient été glissées directement sur des objets : Unity crée alors des matériaux parasites qui remplacent les bons | Matériaux parasites supprimés ; règle d'équipe : seules les textures sont ajoutées, le constructeur crée les matériaux |
| Le joueur tombait sans fin au lancement | Le joueur du modèle VR a une **gravité** (prévue pour marcher sur un sol) et la vue générale est en hauteur, sans sol | Gravité désactivée au démarrage dans `TravelManager` |
| Une grille apparaissait sur les planètes | La salle de démonstration du modèle VR (sol quadrillé) était encore dans la scène | Salle masquée par le constructeur |

*Captures à faire : avant/après le brouillard (la différence est très visible).*

## 5. Captures à placer dans ce dossier

- [ ] `textures-project.png` : les textures dans la fenêtre Project
- [ ] `anneaux-import.png` : l'Inspector de la texture des anneaux (Alpha Is Transparency)
- [ ] `avant-brouillard.png` / `apres-brouillard.png`
- [ ] `vue-generale.png` : le système solaire en Play depuis la vue générale
- [ ] `saturne-proche.png` : Saturne et ses anneaux de près
