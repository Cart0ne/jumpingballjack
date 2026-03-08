# Haptic Feedback - Rollback Instructions

## Problema
La classe `static class HapticFeedback` causava stack overflow su iOS (EXC_BAD_ACCESS, 6412 frame in `GetTypeInfoFromTypeDefinitionIndex` - ricorsione infinita IL2CPP).

## Fix applicato
Cambiato `HapticFeedback.cs` da `public static class` a `public class` (classe normale con metodi statici). Questo evita il loop di risoluzione tipi IL2CPP.

## Come tornare indietro se il gioco non parte

### 1. Rimuovere le chiamate haptic dai 3 file:

**Assets/Scripts/BallController.cs** (riga ~496):
- Commentare: `HapticFeedback.TriggerLight();`

**Assets/Scripts/BallScoreTracker.cs** (riga ~88):
- Commentare: `HapticFeedback.TriggerMedium();`

**Assets/Scripts/GameOverManager.cs** (riga ~108):
- Commentare: `HapticFeedback.TriggerHeavy();`

### 2. Disabilitare i file haptic:
- Rinominare `Assets/Scripts/HapticFeedback.cs` → `HapticFeedback.cs.bak`
- Rinominare `Assets/Plugins/iOS/HapticFeedback.mm` → `HapticFeedback.mm.bak`

### 3. Rebuild:
- Chiudere Xcode
- Cancellare cartella build iOS dal Desktop
- Unity → File → Build Settings → Build (nuova cartella)
- Aprire .xcodeproj → Signing & Capabilities → Automatically manage signing → Team → Run

## File coinvolti
- `Assets/Scripts/HapticFeedback.cs` (wrapper C#)
- `Assets/Plugins/iOS/HapticFeedback.mm` (plugin nativo)
- `Assets/Scripts/BallController.cs` (TriggerLight)
- `Assets/Scripts/BallScoreTracker.cs` (TriggerMedium)
- `Assets/Scripts/GameOverManager.cs` (TriggerHeavy)
