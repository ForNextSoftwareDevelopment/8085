# CPU-Prüfung und Übergabe – 2026-09-21

## Stand

Arbeitsverzeichnis: `C:\Users\toro\source\repos\8085-cpu-fixes`.
Branch: `fix/cpu-instructions`, Basis: Original-Upstream `c46000a`.
`origin` ist der Benutzer-Fork `https://github.com/sirtet/8085.git`.
Original: `https://github.com/ForNextSoftwareDevelopment/8085.git`.
`upstream/main` ist ein geholter Vergleichs-Ref, kein eingerichteter Remote.

Alle 256 Opcode-Werte wurden gegen ein unabhängig formuliertes Referenzmodell
geprüft, einschließlich der zehn undokumentierten Opcodes. Abgedeckt sind
Transport, Stack, Arithmetik/Logik, Rotationen, Sprünge, CALL/RET/RST, IN/OUT,
RIM/SIM, EI/DI, HLT und NOP. Dies ist eine systematische Softwareprüfung,
kein Nachweis über sämtliche Speicherzustände oder ein Vergleich mit realem Silizium.

**Offen: DSUB AC/P.** Der vorhandene Code belässt AC und berechnet Parität über
16 Bit. Diese Eigenschaften sind nicht als korrekt bestätigt und werden nicht
als bestandene Tests gezählt. DSUB-Ergebnis, S/Z/C und V/K werden geprüft.
Die Quellen liefern keine ausreichende Absicherung für einen Ersatz von AC/P.
Erforderlich sind Messwerte einer echten Intel-8085-CPU oder eine überprüfbare
Analyse der konkreten Steuersignale. Keine geratenen Ersatzwerte eingesetzt.

Nicht geprüft: Assembler-/Disassembler-Roundtrips, PKW-Peripherie, vollständige
Interrupt-Priorisierung, READY/HOLD-Buszyklen oder GUI-Abläufe. HLT liefert
weiterhin `System Halted`; Anhalten und Wiederaufnehmen sind Aufgabe des Hosts.

## Tests und Ergebnisse

Aufrufe: [README.md](README.md). Jede Suite liefert bei Fehlern einen Exitcode ungleich null.

| Prüfung | Umfang | Ergebnis |
|---|---|---|
| Opcode-Vektoren | 256 × 32 = 8.192 | 0 Fehler |
| CALL/RET-Bedingungen | 256 Fälle | 0 Fehler |
| Gültige BCD-Additionen + DAA | 20.000 | 0 Fehler |
| Zusatzflags und Grenzfälle | 1.327.931 Prüfungen | 0 Fehler |
| Anwendung | Release-Build, .NET Framework 4.7.2 | erfolgreich |

`OpcodeAudit.cs` verwendet einen eigenständigen gruppenbasierten Decoder.
Verglichen werden Register, PC/Folge-PC, SP, dokumentierte Flags, T-Zustände,
kompletter RAM und alle Ports. Enthalten sind PC-/SP-Grenzen bei 0000/FFFF,
überlaufende Operanden- und Stackzugriffe. DSUB AC/P sind ausgenommen;
V/K werden gesondert geprüft.

`FlagAudit.cs` ergänzt:

- 1.048.576 ALU-Fälle: acht Operationen, alle 256 × 256 Operanden, beide Carry-Werte,
  einschließlich S/Z/AC/P/C/V/K und unverändertem A bei CMP.
- INR/DCR: alle acht Register-/Speicheroperanden, alle Bytewerte und Carry-Werte.
- DAA: alle 256 A-Werte und sämtliche AC/C-Eingänge, zusätzlich zur BCD-Suite.
- ARHL/RDEL: alle 65.536 Eingänge mit zwei Flag-Vorbelegungen.
- LDHI/LDSI: sämtliche Offsets und acht Grenzadressen; Quellen unverändert.
- INX/DCX-Grenzen, DAD-Überläufe, alle A-Werte für RLC/RRC/RAL/RAR,
  alle PSW-Bitmuster, RIM/SIM-Bitmuster sowie EI-Befehlsfolgen.
- DSUB-Grenzen und Vorbelegungen; AC/P ausdrücklich ausgenommen.

Die jeweiligen Fehler wurden vor ihrer Korrektur durch die Tests sichtbar.
Das ursprüngliche kleine Testpaket lieferte gegen unveränderten Upstream
20.038 fehlgeschlagene Assertions. Die neuen Tests kopieren nicht die produktive
lange if/else-Befehlskette. Ein grüner Test ersetzt keine Hardwarevalidierung.

## Korrektur-Commits

Das erste Paket existierte bereits vor dem Auftrag, weitere Fehler einzeln zu
committen. Danach hat jede unabhängige Korrektur ihren eigenen Commit;
gemeinsame ALU-Ursachen werden als ein Fehler behandelt.

| Commit | Korrektur |
|---|---|
| `1e41bb3` | Vorheriges Paket: CNZ, DAA, CALL/RET-Zeiten, CMA-Zeit, SIM-RST7.5-Latch |
| `033f19d` | AC bei SUB/SBB/CMP/DCR: interner Carry statt Halbbyte-Borrow |
| `5a19a01` | JC zählt 10/7 T-Zustände |
| `1f88419` | ARHL erhält Vorzeichen statt eingehendem Carry |
| `4c5809c` | LDHI liest HL und schreibt DE |
| `5fcd80d` | RDEL rotiert DE links durch Carry und setzt V |
| `6237919` | RSTV legt Folgeadresse auf den Stack |
| `31367e4` | XTHL zweites Stackbyte bei FFFF liegt auf 0000 |
| `56cc3dc` | PUSH/POP PSW transportieren V/K, Bit 3 bleibt null |
| `83b233d` | HLT liefert Folge-PC und zählt fünf T-Zustände |
| `4718397` | ALU: K = S xor V |
| `3652c3f` | INX/DCX: K nach unsigned 16-Bit-Über-/Unterlauf setzen/löschen |
| `5ace28d` | DAD aktualisiert V, erhält K |
| `649e31b` | RLC/RAL aktualisieren V; RRC/RAR löschen V |
| `f755f3b` | EI verzögert Interruptannahme, RIM sieht IE sofort |
| `fb491a7` | DSUB aktualisiert V/K |

Zusätzlich: separate Test-Commits und Entfernen einer unbenutzten ARHL-Variable
(`d1e141d`). CZ/CPO waren im aktuellen Upstream bereits korrigiert.

## Integrationsvertrag

`intrIE` ist der durch RIM sichtbare Zustand. Für die tatsächliche Annahme eines
maskierbaren Interrupts muss der Host `CanAcceptMaskableInterrupt` prüfen.
Die zwei SDK-Annahmestellen wurden angepasst. Getestet: EI/RIM, EI/NOP,
EI/DI/NOP, EI/EI/NOP und EI/HLT. Die IE-Anzeige bleibt unverändert.
Beim PKW-Merge muss dessen Interruptpfad diesen Vertrag ebenfalls verwenden.
Die vorhandene SDK-Zustellung (z.B. IE-Löschen bei Annahme) ist damit noch kein
vollständig geprüfter Interruptcontroller.

## BIN: zwei Wege

`MainForm.openBinary_Click` öffnet den Disassembler und übernimmt bei OK dessen
ASM-Text ins Hauptfenster. Danach gilt der normale Assemble-/Hardware-Pfad.
`MainForm.loadBinary_Click` aus dem neuen Upstream kopiert Bytes direkt in RAM
und erzeugt bei Bedarf selbst `new Assembler85(...)`. Diese Erzeugungsstelle
braucht beim PKW-Merge ebenfalls die Hardwarebindung. Pauschal zu behaupten,
BIN werde nie disassembliert, wäre falsch: Die Menüs heißen `Open Binary`
und `Load Binary` und haben unterschiedliche Funktionen.

## Quellen

- [Intel Programming Manual, Mai 1981](https://bitsavers.trailing-edge.com/pdf/intel/ISIS_II/9800301-04_8080_8085_Assembly_Language_Programming_Manual_May81.pdf): dokumentierte Befehle, Flags und Zeiten.
- [Ken Shirriff: V/K-Chipanalyse](https://www.righto.com/2013/02/looking-at-silicon-to-understanding.html): primäre Siliziumanalyse; K=S xor V und Überlaufverhalten. Ältere veröffentlichte K-Formeln sind falsch.
- [Ken Shirriff: 8085-ALU](https://www.righto.com/2013/01/inside-alu-of-8085-microprocessor.html): primäre Analyse der acht Bit breiten Rechenstruktur.
- [Dehnhardt/Sorensen, Electronics 1979](https://cdn.hackaday.io/files/1766537557921952/UnDoc8085Instructions.pdf): ursprüngliche Untersuchung undokumentierter Befehle und Zeiten. Die alte K-Formel wird nicht übernommen.
- [MAME i8085](https://github.com/mamedev/mame/blob/master/src/devices/cpu/i8085/i8085.cpp): ergänzender Vergleich für Subtraktions-AC und EI. Kein universelles Orakel: eigene TODOs nennen V/K- und DSUB-H-Unsicherheit.

## Weiterarbeit

1. DSUB auf echtem Intel-8085 messen: HL/BC = 0000/0000, 0000/0001, 0100/0001,
   1000/0001, 8000/0001, 7FFF/FFFF; AC/P jeweils null und eins vorbelegen und
   PSW direkt danach sichern. Das unterscheidet unveränderte, konstante,
   High-Byte- und 16-Bit-Parität sowie die AC-Polarität.
2. CPU-Fixes kontrolliert in PKW übernehmen, Read/WriteMemory- und Interrupt-
   Anbindung erhalten. Merge-Bericht im Haupt-Arbeitsverzeichnis:
   `docs/UPSTREAM-MERGE-2026-09-20.md`.
3. Dann PKW-ROM-/Terminaltests erneut ausführen.

Nichts gepusht oder in den PKW-Hauptbranch gemerged. Dessen uncommittete Arbeiten
bleiben erhalten. Diese Release-EXE ersetzt die PKW-EXE nicht. Lokale bin/obj-
Buildprodukte sind nicht versioniert und gehören nicht in einen späteren PR.
