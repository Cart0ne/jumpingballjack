# Respawn System - Design Document

## Concetto
5 centri perfetti consecutivi su Planet (meccanica gia esistente) danno, oltre al bonus punti x3, anche 1 respawn.
Al Game Over, se il giocatore ha un respawn disponibile, puo continuare la partita.

## Regole
- Max 1 respawn alla volta (non si accumulano)
- Uguale per tutti (nessun vantaggio per chi ha IAP)
- Al continue: si riparte dalla initial_platform
- Punteggio mantenuto
- Difficolta mantenuta (niente ResetDifficulty)
- Il respawn si consuma al primo utilizzo

## Modifiche Codice

### 1. BallScoreTracker.cs
- Aggiungere `private int availableRespawns = 0;`
- Nel blocco `if (planetCenterStreak >= 5)` (riga 127), aggiungere:
  ```csharp
  if (availableRespawns < 1)
      availableRespawns = 1;
  ```
- Metodo pubblico `public bool HasRespawn()` e `public void ConsumeRespawn()`
- Metodo pubblico `public void ResetRespawns()` (chiamato al restart)
- Evento o callback per notificare la UI quando si ottiene il respawn (per mostrare l'icona)

### 2. GameOverManager.cs
- Riferimento a BallScoreTracker per controllare `HasRespawn()`
- In `TriggerGameOver()`: se HasRespawn, mostrare bottone Continue
- Nuovo metodo `ContinueGame()`:
  - Consumare il respawn (`ConsumeRespawn()`)
  - Nascondere il pannello Game Over
  - Riposizionare la palla sulla initial_platform
  - Riattivare il CameraController
  - Ripristinare lo spawner (pulire piattaforme vecchie, ricominciare a spawnare dalla initial_platform)
  - NON resettare difficolta
  - NON resettare punteggio
- In `RestartGame()`: aggiungere `ResetRespawns()`

### 3. PlatformSpawner.cs
- Metodo pubblico `ResetFromRespawn()`:
  - Distruggere tutte le piattaforme tranne initial_platform
  - Resettare currentPlatform a initial_platform
  - Resettare i contatori (consecutivePlanetCount, ecc.)
  - NON resettare la difficolta

### 4. BallController.cs
- Metodo pubblico `ResetToInitialPlatform()`:
  - Riposizionare la palla sopra la initial_platform
  - Resettare lo stato (velocita, animazione, ecc.)
  - Rimettere la palla in stato "idle" pronta a saltare

## Modifiche UI (nell'Editor Unity)

### In-game (durante il gioco)
- Icona vita (cuore/stella) nell'angolo dello schermo
- Nascosta di default, appare con animazione quando si ottengono i 5 centri
- Scompare quando usata

### Game Over Canvas
- Bottone "Continue" sopra il bottone "Restart"
- Visibile solo se availableRespawns > 0
- Stile diverso dal Restart per distinguerlo (es. colore diverso, icona cuore)

## Flow Completo

```
Gioco in corso
    |
    v
5 centri perfetti consecutivi su Planet
    |
    v
Bonus punti x3 (gia esistente) + icona respawn appare in-game
    |
    v
Giocatore cade / muore
    |
    v
TriggerGameOver()
    |
    +-- Ha respawn? --SI--> Mostra "Continue" + "Restart"
    |                           |
    |                      [Continue]
    |                           |
    |                           v
    |                    Riparte da initial_platform
    |                    (punteggio e difficolta mantenuti)
    |                    Icona respawn scompare
    |
    +-- Ha respawn? --NO--> Mostra solo "Restart" (flow attuale)
                                |
                           [Restart]
                                |
                                v
                         Ricomincia da zero
```

## Complessita Stimata
- ~50 righe di codice nuovo distribuite su 4 file
- 2 elementi UI da creare nell'editor (icona + bottone)
- Rischio basso: la logica del moltiplicatore x5 e gia testata e funzionante

## Dubbi Aperti
- L'icona respawn in-game: che aspetto? Cuore? Stella? Testo "1UP"?
- Animazione quando si ottiene il respawn (flash, bounce, suono)?
- Serve un suono specifico per il continue?
- La camera al continue: fa l'animazione di ritorno alla initial_platform o teletrasporto istantaneo?
