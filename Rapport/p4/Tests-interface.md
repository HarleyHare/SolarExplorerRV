# Personne 4 — interface et interactions

## Configuration à consigner

- Version Unity : 6000.6.0f1
- Scène : `Assets/Scenes/SolarSystem.unity`
- Mode de test : XR Device Simulator (sans casque)
- Date et machine : à compléter pendant le test dans Unity

## Vérifications en Play

Pour chaque ligne, noter le résultat observé et joindre une capture dans ce dossier. Ne pas marquer un test réussi sans l’avoir exécuté dans Unity.

| Test | Résultat | Capture / message Console |
|---|---|---|
| Pointer une planète et afficher sa fiche | À tester | |
| Lire le panneau depuis la position du joueur | À tester | |
| Bouton Visiter | À tester | |
| Bouton Vue générale | À tester | |
| Bouton Orbites, puis second clic | À tester | |
| Sélectionner deux planètes puis Comparer | À tester | |
| Second clic sur Comparer pour fermer les copies | À tester | |

## État vérifié dans le dépôt

- `PlanetInfo.OnSelected()` transmet la planète sélectionnée à `SolarSystemManager`.
- `SolarSystemManager` contient les actions Visiter, Vue générale, Orbites et Comparer.
- Le constructeur crée un Canvas en espace monde, un `TrackedDeviceGraphicRaycaster` si le type XR est disponible et un `XRUIInputModule` si le type est disponible.
- Le panneau a été agrandi à 650 × 440 unités UI ; le titre passe à 48 et le texte à 28. Cette amélioration doit être confirmée visuellement dans Unity.
- Les faits des huit planètes présents dans `SolarExplorerBuilder.cs` respectent déjà la limite de deux phrases.

## Procédure de diagnostic du pointage

Si une planète ne peut pas être sélectionnée, vérifier `XR Origin Hands` dans la Hierarchy et l’Inspector de la manette. Rechercher un `Near-Far Interactor` ou un `Ray Interactor`, puis relever le message complet de la Console et capturer la Hierarchy et l’Inspector.

## Bugs constatés

À compléter uniquement après les essais : pour chaque bug, décrire l’action effectuée, le résultat attendu, le résultat obtenu, le message rouge complet de la Console et le nom de la capture associée.
