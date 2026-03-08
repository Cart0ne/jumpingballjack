# Ads System - Design Document

## Strategia
- **Interstitial** al Game Over (unico formato ad)
- **IAP "Remove Ads"** per eliminare gli interstitial (implementazione separata, fase successiva)
- **Niente banner, niente rewarded**
- I placeholder ADBANNER nei canvas restano disattivati (rimovibili in futuro)

## Flow Utente

```
Giocatore muore
    |
    v
Schermata Game Over (punteggi visibili, bottoni attivi)
    |
    +-- Preme "Play Again" o "Back"
    |
    v
Sono passati >= 30s dall'ultimo ad? (parametro Inspector)
    |
    +-- SI --> Mostra Interstitial --> Finito ad --> Esegue azione (restart o home)
    |
    +-- NO --> Esegue azione direttamente (niente ad)
```

## Setup Account (da fare su browser)

### Passo 1: Creare account LevelPlay
- Vai su https://www.is.com/ e registrati
- E' gratuito

### Passo 2: Registrare l'app
- Nella dashboard, crea una nuova app
- Inserisci il Bundle ID iOS (quello in Unity: Player Settings > Other Settings > Bundle Identifier)
- Seleziona piattaforma iOS
- Ottieni l'**App Key** (stringa tipo "1a2b3c4d5")

### Passo 3: Configurare Ad Unit
- Crea un Ad Unit di tipo **Interstitial**
- ironSource stesso funziona come ad network di default
- Puoi aggiungere altri network (AdMob, Meta, ecc.) in futuro per aumentare i guadagni

### Passo 4: Annotare le credenziali
- App Key: servirà nel codice Unity
- Non servono Placement ID per ora (usiamo il default)

## Modifiche Codice

### Nuovo file: AdManager.cs (Singleton)
Gestisce tutto il ciclo di vita degli ads.

```
Responsabilita:
- Inizializzare LevelPlay SDK (con App Key)
- Richiedere ATT (App Tracking Transparency) su iOS
- Caricare interstitial
- Mostrare interstitial (se abilitati e se tempo >= minTimeBetweenAds)
- Pre-caricare il prossimo interstitial dopo che uno viene chiuso
- Gestire callback (ad caricato, ad chiuso, ad fallito)
- Eseguire l'azione pendente (restart o home) dopo che l'ad si chiude
```

Campi Inspector:
```csharp
[Header("Ads Configuration")]
[Tooltip("Attiva/disattiva gli ads. OFF durante sviluppo, ON per release.")]
public bool enableAds = false;

[Tooltip("App Key fornito da LevelPlay dashboard")]
public string appKey = "";

[Tooltip("Tempo minimo in secondi tra un interstitial e l'altro")]
public float minTimeBetweenAds = 30f;

[Tooltip("Attiva modalita test (ads finti per verificare il flow)")]
public bool testMode = true;
```

Logica principale:
```
Start():
    if (!enableAds) return
    Richiedi ATT (solo iOS)
    Attendi risposta ATT
    Inizializza LevelPlay SDK con appKey
    Carica primo interstitial

ShowInterstitialThenExecute(Action pendingAction):
    if (!enableAds || tempo dall'ultimo ad < minTimeBetweenAds):
        Esegui pendingAction direttamente
        return
    if (interstitial caricato):
        Salva pendingAction
        Mostra interstitial
    else:
        Esegui pendingAction direttamente

OnInterstitialClosed():  // callback LevelPlay
    Registra timestamp ultimo ad
    Esegui pendingAction salvata
    Pre-carica prossimo interstitial
```

### Modifica: GameOverManager.cs
- Riferimento ad AdManager
- `RestartGame()`: invece di procedere direttamente, chiama `AdManager.ShowInterstitialThenExecute(() => { /* logica restart attuale */ })`
- `GoBackToStart()`: stessa cosa, wrappa la logica attuale nella callback

### ATT (App Tracking Transparency) - Obbligo Apple
- Va richiesto al primo avvio, PRIMA di inizializzare gli ads
- LevelPlay ha API integrata: `IronSource.Agent.SetConsent(true/false)`
- Su iOS serve anche aggiungere la chiave `NSUserTrackingUsageDescription` nel Info.plist (testo tipo "Usiamo questi dati per mostrarti pubblicita pertinenti")
- Questo testo viene mostrato nel popup nativo iOS
- Se l'utente rifiuta, gli ads funzionano lo stesso ma pagano meno

## Testing

### Fase Alpha/Beta (ORA)
- `enableAds = false` nell'Inspector
- Il gioco funziona esattamente come adesso
- Zero pubblicita, zero fastidi

### Test del flow (quando vuoi verificare)
- `enableAds = true`
- `testMode = true`
- Funziona nell'Editor Unity: mostra ad finti (rettangoli grigi)
- Verifica che il flow sia corretto: game over > premi bottone > ad appare > ad si chiude > azione eseguita

### Test reale su iPhone
- `enableAds = true`
- `testMode = false`
- Serve il device fisico (simulatore non supporta ads)
- Appaiono ad veri
- Verifica una volta che funzioni, poi puoi rispegnere

### Release
- `enableAds = true`
- `testMode = false`
- Pubblicita attive per tutti
- Chi compra IAP "Remove Ads": il flag enableAds viene messo a false per quell'utente (gestito da IAP, fase successiva)

## File coinvolti
- **NUOVO** `Assets/Scripts/AdManager.cs` - Singleton gestione ads
- **MODIFICA** `Assets/Scripts/GameOverManager.cs` - Integrazione con AdManager
- **NESSUNA MODIFICA** agli altri script

## Dipendenze
- LevelPlay SDK (gia presente in Assets/LevelPlay/)
- Potrebbe servire aggiornare il package — verificare versione attuale vs ultima disponibile
- CocoaPods per iOS (gestito automaticamente dal build process Unity)

## Ordine implementazione
1. Creare account LevelPlay e ottenere App Key
2. Creare AdManager.cs con enableAds = false
3. Modificare GameOverManager.cs per usare AdManager
4. Testare flow nell'Editor con testMode = true
5. Testare su iPhone con ads reali
6. Implementare IAP "Remove Ads" (documento separato, fase successiva)

## Note
- LevelPlay SDK config files gia presenti: Assets/LevelPlay/Editor/
- I 3 placeholder ADBANNER nei canvas (Start, GameOver, Settings) restano disattivati
- Il parametro minTimeBetweenAds (30s) e facilmente modificabile dall'Inspector
