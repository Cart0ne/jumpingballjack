# Jumping Ball Jack - Piano di Progetto

## Architettura del Gioco

Endless jumper per iOS realizzato in Unity con camera ortografica.
Il giocatore tiene premuto per caricare il salto, rilascia per saltare verso la piattaforma successiva.

### Script Principali

| Script | Ruolo |
|--------|-------|
| `BallController` | Input, carica salto, salto parabolico, rimbalzo, esplosione/game over |
| `PlatformSpawner` | Generazione procedurale piattaforme (Forward/Left/Right), shuffle bag, cleanup |
| `DifficultyManager` | Difficolta progressiva (tempo + piattaforme): distanze, scala, gravita, magnetismo |
| `CameraController` | Camera ortografica Y fissa, look-ahead tra piattaforma corrente e prossima |
| `GameOverManager` | Schermata game over, restart, transizione camera |
| `ScoreManager` | Punteggio corrente + best score (PlayerPrefs) |
| `SoundManager` | Singleton DontDestroyOnLoad, toggle musica/effetti |
| `StartGameActions` | Schermata iniziale, avvio gioco, attivazione spawner |

### Componenti Palla

| Script | Ruolo |
|--------|-------|
| `BallGravityController` | Gravita custom: ridotta su piattaforma, normale in volo |
| `BallMagnetism` | Controller PD che attira verso il centro della piattaforma target |
| `BallRotationController` | Rotazione in volo, allineamento pre-landing |
| `BallInflationEffect` | VFX particelle durante la carica |
| `BallScoreTracker` | Calcolo punti: Planet vs Platform, streak centri perfetti, moltiplicatore x3 |
| `PreGameBouncer` | Rimbalzo idle sulla piattaforma iniziale prima del gioco |

### Componenti Piattaforma

| Script | Ruolo |
|--------|-------|
| `PlatformEntryAnimation` | Animazione entrata: sale dal basso + oscillazione Z |
| `PlatformExitAnimation` | Animazione uscita: carica scala + affondamento |
| `PlatformCompression` | Compressione visuale durante carica salto |
| `PlatformVerticalMovement` | Movimento verticale oscillante (probabilita da DifficultyManager) |
| `PlatformAudioFader` | Fade in/out audio ambientale piattaforme |
| `OrientTowardsPreviousPlatform` | Orienta la piattaforma verso quella precedente |
| `FanController` | Ventilatore sotto piattaforme mobili |
| `FogPushAway` | Sposta particelle nebbia al passaggio della palla |

### Flusso di Gioco

1. **Start Screen** - Fade in, palla rimbalza idle (PreGameBouncer)
2. **Tap Start** - Fade out UI, attiva PlatformSpawner, camera zoom out
3. **Gameplay** - Hold per caricare, release per saltare. Magnetismo PD in volo. Atterraggio: rimbalzo + spawn prossima piattaforma + punteggio
4. **Difficolta crescente** - Distanze maggiori, piattaforme piu piccole, magnetismo ridotto, piattaforme mobili
5. **Game Over** - Palla cade sotto explosionHeight, esplosione, UI game over

### Tipi di Piattaforma e Punteggio

| Tipo | Bordo | Centro | Note |
|------|-------|--------|------|
| Platform | +5 | +10 | Resetta streak Planet |
| Planet | +1 | +2 (cumulativo) | 5 centri consecutivi = x3 |

---

## Struttura Canvas (GameScene)

2 Canvas root + 2 pannelli figli. Tutti usano Screen Space - Camera tranne SETTINGS che usa Overlay.

```
GameScene
├── Main Camera              (+ CameraController)
├── Directional Light
├── UI_CANVAS                [Screen Space - Camera, ref 1920x1080, Match Width, sorting 0]
│   ├── ScoreText            (TMP punteggio, inattivo inizialmente)
│   ├── GAMEOVER_CANVAS      (pannello, inattivo inizialmente)
│   │   ├── TextGAMEOVER
│   │   ├── TextActualScore
│   │   ├── TextBestScore
│   │   ├── PlayAgain        (container + bottone)
│   │   ├── Back_Button
│   │   ├── versione
│   │   └── ADBANNER
│   └── SETTINGS_CANVAS      (pannello, inattivo inizialmente)
│       ├── TextSETTINGS
│       ├── EffectsToggleContainer  (CustomSoundToggle)
│       ├── MusicToggleContainer    (CustomMusicToggle)
│       ├── Back_Button / <-Back
│       ├── Effects, Sound_track    (labels)
│       ├── ImageBestScore, ResetBestScore
│       ├── versione
│       └── ADBANNER
├── START_CANVAS             [Screen Space - Camera, ref 1920x1080, Match Width, sorting 1, Override Sorting]
│   ├── Logo
│   ├── START                (container + bottone)
│   ├── Settings             (bottone, apre SETTINGS_CANVAS)
│   ├── StartBestScore       (TMP "Best Score: X")
│   ├── versione
│   └── ADBANNER
├── StartGameActions
├── GameOverManager
├── ScoreManager
├── DifficultyManager
├── SkyBoxManager
├── SoundManager             (DontDestroyOnLoad)
├── EventSystem
├── Global Volume
├── Platform_Generator
├── iOSAudioFix
└── [Ball + piattaforma iniziale come prefab]
```

### Dettagli Canvas

| Canvas | Render Mode | Scaler | Sorting | CanvasGroup | AutoFadeCanvas | Stato iniziale |
|--------|-------------|--------|---------|-------------|----------------|----------------|
| UI_CANVAS | Screen Space - Camera | Scale With Screen Size 1920x1080 | 0 | No | No | Attivo |
| START_CANVAS | Screen Space - Camera | Scale With Screen Size 1920x1080 | 1 (override) | Si (alpha 0) | Si (1.2s) | Attivo (fade in) |
| GAMEOVER_CANVAS | Figlio di UI_CANVAS | Eredita dal padre | Eredita | Si (alpha 1) | Si (1.2s) | Inattivo |
| SETTINGS_CANVAS | Figlio di UI_CANVAS, Overlay | Constant Pixel Size | 0 | Si | No | Inattivo |

### Flusso Canvas

1. **Avvio** - START_CANVAS fade in (StartScreenFadeIn), ScoreText nascosto
2. **Tap Start** - START_CANVAS fade out, ScoreText fade in, gioco inizia
3. **Game Over** - GAMEOVER_CANVAS attivato (AutoFadeCanvas fade in)
4. **Play Again** - GAMEOVER_CANVAS fade out, ricarica scena con `skipStartScreen = true`
5. **Back** - Ricarica scena normalmente (mostra START_CANVAS)
6. **Settings** - SETTINGS_CANVAS attivato da START_CANVAS o GAMEOVER_CANVAS

### Note per nuovi Canvas

- Usare **Screen Space - Camera** con ref **1920x1080** e **Match Width** per coerenza
- SETTINGS_CANVAS usa Overlay/Constant Pixel Size (inconsistenza da correggere eventualmente)
- ADBANNER presente in 3 canvas (Start, GameOver, Settings)
- I pannelli figli (GAMEOVER, SETTINGS) usano anchors stretch (0,0)-(1,1) per riempire il padre
- Per fade in/out aggiungere **CanvasGroup** + **AutoFadeCanvas** (fadeDuration 1.2s)

---

## TODO

### Gameplay & Bilanciamento
- [ ] Ribilanciare la difficolta (DifficultyManager)
- [ ] Rivedere il magnetismo
- [ ] Verificare feeling del salto

### UI/UX
- [ ] Migliorare schermata di start
- [ ] Rivedere punteggio in gioco
- [ ] Migliorare schermata Game Over

### Leaderboard
- [ ] Implementare Game Center (classifica globale + amici)

### Monetizzazione
- [ ] App gratuita
- [ ] Interstitial ad al Game Over (LevelPlay gia integrato)
- [ ] In-app purchase "Rimuovi pubblicita" (prezzo da definire)

### DevOps
- [x] Setup GitHub con .gitignore Unity
- [ ] Workflow commit ad ogni funzionalita stabile

### Store & Launch
- [ ] Icona app
- [ ] Screenshot App Store
- [ ] Descrizione e keywords
- [ ] TestFlight per beta test
