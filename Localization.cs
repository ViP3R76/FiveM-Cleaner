using System.Collections.Generic;

namespace FiveMCleaner;

public enum AppLanguage { German, English }

public static class Localization
{
    public static readonly IReadOnlyDictionary<string,string> German = new Dictionary<string,string>
    {
        ["StatusHeader"]="FIVEM STATUS", ["StatusNotRunning"]="FiveM ist nicht gestartet", ["StatusRunning"]="FiveM ist gestartet",
        ["CleanupReady"]="Cleanup bereit", ["CleanupLocked"]="Cleanup gesperrt", ["InstallationHeader"]="FIVEM INSTALLATION",
        ["InstallationChecking"]="Installation wird geprüft …", ["InstallationDetected"]="Installation erkannt",
        ["InvalidInstallation"]="Keine gültige FiveM-Installation", ["InstallationRootUnavailable"]="Programmverzeichnis nicht verfügbar.", ["FiveMExeMissing"]="FiveM.exe wurde nicht gefunden.", ["AppDirMissing"]="FiveM.app wurde nicht gefunden.", ["DataDirMissing"]="FiveM.app\\data wurde nicht gefunden.", ["ProtectedTarget"]="Ein geschütztes Verzeichnis wurde als Löschziel erkannt.", ["UnauthorizedTarget"]="Ein nicht freigegebenes Löschziel wurde erkannt.", ["ReparseTarget"]="Das Löschziel ist ein Reparse Point und wurde aus Sicherheitsgründen abgelehnt.", ["FiveMStartedDuringCleanup"]="FiveM wurde während des Vorgangs gestartet. Der Cleanup wurde gestoppt.", ["CleanupBlockedByRunning"]="FiveM ist gestartet. Der Cleanup wurde aus Sicherheitsgründen abgebrochen.", ["SizeChecking"]="wird ermittelt …", ["Cache"]="CACHE", ["CacheDescription"]="Temporäre FiveM-Daten",
        ["CacheDetail"]="4 Bereiche", ["Logs"]="LOGS", ["LogsDescription"]="FiveM Log-Dateien", ["LogsDetail"]="Logs",
        ["Crashes"]="CRASH DUMPS", ["CrashesDescription"]="FiveM Absturz-Daten", ["CrashesDetail"]="Crash-Dumps",
        ["CleanCache"]="CACHE BEREINIGEN", ["CleanLogs"]="LOGS BEREINIGEN", ["CleanCrashes"]="DUMPS BEREINIGEN",
        ["CleanAll"]="ALLES BEREINIGEN", ["Status"]="STATUS", ["Ready"]="Bereit. FiveM wird überwacht.",
        ["Confirm"]="Bereinigung durchführen?", ["ConfirmAll"]="Vollständige Bereinigung wirklich durchführen?",
        ["ConfirmPrompt"]="Bitte Auswahl bestätigen:", ["Yes"]="JA", ["No"]="NEIN", ["Cancelled"]="Bereinigung abgebrochen.",
        ["RunningNoDelete"]="FiveM läuft. Keine Daten wurden gelöscht.", ["RunningLocked"]="FiveM läuft. Cleanup ist derzeit gesperrt.",
        ["Prepare"]="Vorbereitung …", ["Processing"]="Wird verarbeitet: ", ["AllProcessed"]="Alle Ziele verarbeitet.",
        ["CleanupRunning"]=" wird ausgeführt …", ["CleanupSuccess"]="Cleanup erfolgreich abgeschlossen!",
        ["CacheSuccess"]="Cache-Cleanup erfolgreich abgeschlossen!", ["LogsSuccess"]="Logs-Cleanup erfolgreich abgeschlossen!",
        ["CrashesSuccess"]="Crash-Dumps-Cleanup erfolgreich abgeschlossen!", ["Removed"]="entfernt", ["Files"]="Dateien",
        ["Areas"]="Bereiche", ["Duration"]="Dauer", ["DisclaimerHeader"]="DISCLAIMER & RECHTE",
        ["Disclaimer1"]="FiveM CLEANER ist ein unabhängiges Community-Hilfsprogramm und steht in keiner offiziellen Verbindung zu Cfx.re, FiveM oder Rockstar Games. Das Programm wurde weder von diesen Unternehmen entwickelt, veröffentlicht, geprüft, autorisiert noch unterstützt.",
        ["Disclaimer2"]="FiveM, Cfx.re, Rockstar Games sowie zugehörige Namen, Logos, Marken, Software und Inhalte sind Eigentum ihrer jeweiligen Rechteinhaber. Alle Rechte verbleiben bei den jeweiligen Rechteinhabern. Namen und Marken werden ausschließlich zur Identifikation der unterstützten Software verwendet.",
        ["Disclaimer3"]="Die Nutzung erfolgt auf eigene Verantwortung. FiveM CLEANER verarbeitet ausschließlich die fest definierten Cache-, Log- und Crash-Dump-Verzeichnisse. Für Datenverlust, beschädigte Dateien, Fehlfunktionen oder sonstige Schäden wird – soweit gesetzlich zulässig – keine Haftung übernommen. Benötigte Logs oder Crash-Dumps sollten vor der Bereinigung gesichert werden.",
        ["Twitch"]="TWITCH", ["Discord"]="DISCORD", ["GitHub"]="GITHUB", ["Close"]="SCHLIESSEN", ["Language"]="SPRACHE", ["StatusRefresh"]="Status aktualisieren", ["ErrorPrefix"]="Fehler: "
    };

    public static readonly IReadOnlyDictionary<string,string> English = new Dictionary<string,string>
    {
        ["StatusHeader"]="FIVEM STATUS", ["StatusNotRunning"]="FiveM is not running", ["StatusRunning"]="FiveM is running",
        ["CleanupReady"]="Cleanup ready", ["CleanupLocked"]="Cleanup locked", ["InstallationHeader"]="FIVEM INSTALLATION",
        ["InstallationChecking"]="Checking installation …", ["InstallationDetected"]="Installation detected",
        ["InvalidInstallation"]="No valid FiveM installation", ["InstallationRootUnavailable"]="FiveM program directory is not available.", ["FiveMExeMissing"]="FiveM.exe was not found.", ["AppDirMissing"]="FiveM.app was not found.", ["DataDirMissing"]="FiveM.app\\data was not found.", ["ProtectedTarget"]="A protected directory was selected as a cleanup target.", ["UnauthorizedTarget"]="An unauthorized cleanup target was detected.", ["ReparseTarget"]="The cleanup target is a reparse point and was rejected for safety.", ["FiveMStartedDuringCleanup"]="FiveM was started during the operation. Cleanup was stopped.", ["CleanupBlockedByRunning"]="FiveM is running. Cleanup was aborted for safety reasons.", ["SizeChecking"]="checking …", ["Cache"]="CACHE", ["CacheDescription"]="Temporary FiveM data",
        ["CacheDetail"]="4 locations", ["Logs"]="LOGS", ["LogsDescription"]="FiveM log files", ["LogsDetail"]="Logs",
        ["Crashes"]="CRASH DUMPS", ["CrashesDescription"]="FiveM crash data", ["CrashesDetail"]="Crash dumps",
        ["CleanCache"]="CLEAN CACHE", ["CleanLogs"]="CLEAN LOGS", ["CleanCrashes"]="CLEAN DUMPS",
        ["CleanAll"]="CLEAN EVERYTHING", ["Status"]="STATUS", ["Ready"]="Ready. FiveM is being monitored.",
        ["Confirm"]="Perform cleanup?", ["ConfirmAll"]="Really perform full cleanup?", ["ConfirmPrompt"]="Please confirm:",
        ["Yes"]="YES", ["No"]="NO", ["Cancelled"]="Cleanup cancelled.", ["RunningNoDelete"]="FiveM is running. No data was deleted.",
        ["RunningLocked"]="FiveM is running. Cleanup is locked.", ["Prepare"]="Preparing …", ["Processing"]="Processing: ",
        ["AllProcessed"]="All targets processed.", ["CleanupRunning"]=" is running …", ["CleanupSuccess"]="Cleanup completed successfully!",
        ["CacheSuccess"]="Cache cleanup completed successfully!", ["LogsSuccess"]="Logs cleanup completed successfully!",
        ["CrashesSuccess"]="Crash-dump cleanup completed successfully!", ["Removed"]="removed", ["Files"]="files",
        ["Areas"]="locations", ["Duration"]="Duration", ["DisclaimerHeader"]="DISCLAIMER & RIGHTS",
        ["Disclaimer1"]="FiveM CLEANER is an independent community utility and is not affiliated with Cfx.re, FiveM or Rockstar Games. This program was not developed, published, reviewed, authorized or supported by any of these companies.",
        ["Disclaimer2"]="FiveM, Cfx.re, Rockstar Games and related names, logos, trademarks, software and content are the property of their respective rights holders. All rights remain with their respective rights holders. Names and trademarks are used solely to identify the supported software.",
        ["Disclaimer3"]="Use of this program is at your own risk. FiveM CLEANER only processes the specifically defined cache, log and crash-dump directories. To the extent permitted by law, no liability is assumed for data loss, damaged files, malfunctions or other damages. Required logs or crash dumps should be backed up before cleanup.",
        ["Twitch"]="TWITCH", ["Discord"]="DISCORD", ["GitHub"]="GITHUB", ["Close"]="CLOSE", ["Language"]="LANGUAGE", ["StatusRefresh"]="Refresh status", ["ErrorPrefix"]="Error: "
    };
}
