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

## Bug Noti

- [x] **Lag sporadici su iOS (iPhone 16)** — RISOLTO. Cause trovate e fixate:
  - Audio clip con Compressed In Memory + Vorbis causavano decompressione runtime → cambiati a Decompress On Load + ADPCM
  - `SoundManager.UpdateEffectsSound()` usava `FindObjectsByType<AudioSource>` pesante → rimosso, ogni script controlla `SoundManager.soundEnabled` prima di suonare
  - Prima inizializzazione di prefab Platform complessi (Animator, ParticleSystem) → ShaderPreloader ora aspetta piu frame per prefab complessi (5 vs 2)
- [x] **Bug mute non persistente** — RISOLTO. Suoni riapparivano dopo restart partita perche `UpdateEffectsSound` non copriva AudioSource creati dopo il reload scena. Rimosso approccio mute globale, ogni script ora controlla il flag statico.
- [x] **Warning "Setting scale failed"** — RISOLTO. `PlatformExitAnimation` scalava a `Vector3.zero` → cambiato a `Vector3.one * 0.001f`
- [x] **Bounce innaturale su spigoli/lati** — RISOLTO. `OnCollisionEnter` in BallController ora controlla la normale di collisione (angolo < 45°). Impatti laterali vengono gestiti dalla fisica naturale senza freeze/bounce forzato.

---

## TODO

### Gameplay & Bilanciamento
- [ ] Ribilanciare la difficolta (DifficultyManager)
- [ ] Rivedere il magnetismo
- [ ] Verificare feeling del salto
- [x] **Velocizzare la palla in volo** — IMPLEMENTATO. `flightSpeedMultiplier` in BallController scala velocita, BallGravityController scala gravita di S^2 per mantenere la stessa traiettoria. Integrato nel DifficultyManager con `initialFlightSpeedMultiplier` e `ultimateFlightSpeedMultiplier` per progressione automatica.

### UI/UX
- [ ] Migliorare schermata di start
- [ ] Rivedere punteggio in gioco
- [ ] Migliorare schermata Game Over
- [ ] **Cambiare splash screen Unity** — Personalizzare o sostituire lo splash screen iniziale (nota: con licenza Personal il logo Unity e obbligatorio, con Plus/Pro si puo rimuovere)
- [ ] Verificare transizioni tra schermate (Start → Game, Game → GameOver, GameOver → Start/Restart, Settings apri/chiudi)

### Leaderboard
- [ ] Implementare Game Center (classifica globale + amici)

### Monetizzazione
- [ ] App gratuita
- [ ] Interstitial ad al Game Over (LevelPlay gia integrato)
- [ ] In-app purchase "Rimuovi pubblicita" (prezzo da definire)

### DevOps
- [x] Setup GitHub con .gitignore Unity
- [ ] Workflow commit ad ogni funzionalita stabile
- [ ] Gestione versioni Alpha / Beta / Release (versionamento build, branching strategy)

### Store & Launch
- [ ] Icona app
- [ ] Screenshot App Store
- [ ] Descrizione e keywords
- [ ] TestFlight per beta test

---

## Strategia di Lancio e Monetizzazione

### Modello di Business
- App **gratuita** sullo Store
- **Interstitial Ad** al Game Over (automatica, si chiude dopo 5 secondi)
- **Rewarded Ad** al Game Over ("Guarda un video e continua dalla stessa posizione")
- **In-app purchase** "Rimuovi pubblicità" (prezzo da definire, indicativo 1.99€)
- Apple trattiene il 30% su ogni acquisto in-app

### Tipi di Pubblicità
| Tipo | Quando | Note |
|------|--------|------|
| Interstitial | Game Over | Schermata a pieno schermo, automatica |
| Rewarded | Game Over | Volontaria, rende 5-10x più degli interstitial |
| Banner | — | Sconsigliati, rendono poco e peggiorano UX |

### Testing delle Ads
- **Unity Editor** → non funzionano, solo codice
- **Build su telefono** → Test Ads di LevelPlay (già integrato)
- **TestFlight** → Test Ads, simula produzione
- **App Store** → Ads reali, guadagni reali

### Proiezione Entrate (Scenario Base)
Ipotesi: 100 download/giorno, 15% retention, ~250 DAU

| Fonte | Stima/giorno |
|-------|-------------|
| Interstitial Ads | €4-5 |
| Rewarded Ads | €1-2 |
| Remove Ads IAP | €2-3 |
| **Totale** | **€7-10/giorno** |

| Periodo | Entrate stimate |
|---------|----------------|
| Mese 1 | €100-150 |
| Mese 3 | €200-300 |
| Mese 6 | €300-400 |

---

## Classifica Online (Game Center)

### Come funziona per l'utente
- Login automatico con Apple ID
- Classifica globale (tutti i giocatori)
- Classifica amici (rubrica)
- Record personali

### Implementazione tecnica
1. Creare Leaderboard su App Store Connect (gratuito)
2. Aggiungere plugin Game Center in Unity
3. Inviare punteggio con:
```csharp
Social.ReportScore(punteggio, "tua_leaderboard_id", success => {});
```
4. UI standard Apple (veloce) o UI custom (più lavoro)

### Testing Game Center
- **Unity Editor** → non funziona
- **Build su telefono** → funziona con account Sandbox
- **TestFlight** → funziona con account Sandbox, simula produzione

### Account Sandbox
- Apple ID finto creato su App Store Connect (gratuito, 2 minuti)
- Su iPhone: `Impostazioni` → `Game Center` → logout account reale → login sandbox
- NON tocca Apple ID, iCloud o App Store
- Punteggi e acquisti vanno in ambiente separato di test
- Quando finisci i test, rimetti il tuo account reale

---

## Distribuzione Geografica

### Mercati target
- 🇺🇸 USA
- 🇬🇧 UK
- 🇩🇪 Germania
- 🇯🇵 Giappone (ottimo per giochi casual)

### Cina
- Richiede licenza governativa ISBN
- Necessaria società cinese registrata
- Tempi: 6-18 mesi, costi elevati
- **Da valutare solo dopo il successo nel mercato occidentale**

---

## Strategia di Crescita (Organica)

### App Store Optimization (ASO) ← priorità massima
- Titolo con keywords rilevanti
- Descrizione ottimizzata
- Screenshot accattivanti
- Icona che cattura l'occhio
- Mirare alle prime recensioni (chiedere ad amici/parenti)

### Canali gratuiti
- **Reddit** → r/indiegaming, r/gamedev, r/iosgaming
- **Product Hunt** → lancio come "prodotto del giorno"
- **TikTok** → video gameplay (no volto necessario, alto potenziale virale)

### Proiezione download organici
| Periodo | Download/giorno |
|---------|----------------|
| Lancio (mese 1) | 5-20 |
| Con buon ASO (mese 2-3) | 20-50 |
| Spike Reddit/ProductHunt | 50-100 |
| Regime organico | 20-40 |