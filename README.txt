MP1 SKIN STUDIO - PROTOTYP 0.9
Stand: 18.10.2026

START
1. Das gesamte ZIP in einen eigenen Ordner entpacken.
2. MP1SkinStudio.exe starten. Die mitgelieferte Demo wird automatisch geladen.
3. Ein Element in der Vorschau oder im Baum links auswählen.
4. Mit der Maus verschieben; der kleine Griff unten rechts ändert die Größe.
5. Rechts Eigenschaften ändern und „Eigenschaften übernehmen“ anklicken.
6. Über „Änderungen als ZIP“ die geänderten XML-Dateien exportieren.

NEU IN VERSION 0.9
- Sammlungs-Vorschau für Listen, kleine/große Symbole, Filmstreifen und eine
  statisch angenäherte Coverflow-Ansicht. Vorhandene XML-Maße, Zeilenabstände,
  Textoffsets, Bildgrößen und Fokusgrafiken werden verwendet.
- Bei Facades wird nur eine passende Unteransicht angezeigt. Eine fehlende
  Wunschansicht fällt auf die erste vorhandene Vorlage zurück. Gemeinsame IDs
  der Facade-Unteransichten gelten für Prüfung und Navigation als eine logische
  Identität. Echte Kollisionen außerhalb der Facade bleiben geschützt.
- Über Sammlung oder Element > Sammlungs-Vorschau Ansicht, ausgewählten Eintrag
  und Beispieldaten bearbeiten. Format: Titel | Zusatz | Bild relativ zu Media.
  Bis zu 50 Einträge, Bilder optional, leere Sammlungen möglich. Unbekannte
  Plugin-Ausdrücke bleiben ungeprüft; facadeview.list usw. werden simuliert.
- Beispieleinträge, Ansicht und Auswahl gehören nur zum Editorprojekt. Sie
  verändern weder Skin-XML noch XML-Undo und landen nicht im Skin-Export.
  Neue Editorprojekte verwenden Format 4 und benötigen mindestens Version 0.9,
  damit ältere Versionen die Sammlungsdaten nicht unbemerkt verwerfen. Die
  Formate 1, 2 und 3 bleiben lesbar.
- Schriftfamilie, fett/kursiv und Aliase werden aus fonts.xml übernommen. Die
  Höhenskalierung verwendet die MP1-Basis von 576 Bildzeilen. Nicht installierte
  Schriftfamilien werden durch Segoe UI ersetzt; die Metriken bleiben angenähert.
- Textboxen brechen mehrzeilig um. Innere Textausrichtung und Textversatz von
  Schaltflächen, vertikale Ausrichtung, Schatten sowie Bildfarbe/Alpha werden
  dargestellt. Die neuen Werte sind rechts direkt und mit Auswahlmenüs editierbar.
- Eigene Demo: demo.sammlungen.xml. Beispielbilder stammen aus Media/Studio.
- 189 Kernprüfungen, 185 Oberflächenprüfungen, vollständiger Titan-Durchlauf.
  Zusätzlich 10 direkte Prüfungen mit originaler Core.dll aus MP1 1.39 x64.
  Der native Nachweis betrifft statische StackLayout-Klassen, nicht die gesamte
  Direct3D-Engine. Details und reproduzierbarer Prüfcode: Pruefungen/MP1-Nativ.
- Runtime-Abgleich enthält einen geprüften Testexport für Titans Einstellungen,
  ein Editorprojekt und die noch auszuführenden Schritte für den Clientvergleich.
  Die Originaldateien wurden durch alle Exportproben unverändert gelassen.

STATUS DER MP1-INSTALLATION AUF DEM ENTWICKLUNGS-PC
Das offizielle MP1-1.39-x64-Paket wurde heruntergeladen und gegen die vom Release
veröffentlichte SHA256-Prüfsumme geprüft. Die Client-Dateien liegen separat unter
apps/MediaPortal-1.39-Testinstallation. Datenpfade sind in dieser Kopie isoliert.
Die systemweite Installation und Configuration.exe konnten wegen verweigertem
Administratorstart nicht ausgeführt werden. Diese Dateikopie ist deshalb keine
vollständig eingerichtete MP1-Installation. Die Original-Core-Bibliothek ließ sich
für die direkten Komponentenprüfungen ausführen. Für den vollständigen Client-
und Bildvergleich muss das offizielle Setup am PC mit der Windows-Freigabe
abgeschlossen und MP1 erstkonfiguriert werden. Der Clientvergleich wurde NICHT
als bestanden gewertet. Die bestehende MP2-Installation wurde nicht bearbeitet.

NEU IN VERSION 0.8
- Automatische Gruppenlayouts: StackLayout ordnet Inhalte untereinander oder
  nebeneinander an. Abstände, Gruppen-Margin, Ausrichtung und das Nachrücken
  ausgeblendeter Einträge werden in der statischen Vorschau berücksichtigt.
- Gruppe im Baum auswählen und Element > Gruppenlayout bearbeiten öffnen.
  Richtung, Abstand und Nachrücken lassen sich ohne XML-Eingabe ändern.
  Eine Übernahme ist ein Undo-Schritt; die XML-Positionen der Kinder bleiben
  unverändert. Gesperrte Gruppen bleiben geschützt. Vorhandene Bindungen und
  andere Layouttypen benötigen eine eigene ausdrückliche Freigabe im Dialog.
- Mausauswahl, Auswahlrahmen und Navigationslinien verwenden die angeordneten
  Rechtecke. Direkte Geometrieänderungen der automatisch angeordneten Kinder
  bleiben geschützt. Jedes Include-Vorkommen bekommt eigene Vorschau-Rechtecke.
- Unbekannte Sichtbarkeitsbedingungen reservieren einen Platz. Das Ausblenden
  eines Elements nur im Editor verändert das Laufzeitlayout nicht.
- Eigene Demo: demo.stack.xml. Sie zeigt vertikale und horizontale Gruppen sowie
  einen unsichtbaren Eintrag, dessen Nachfolger automatisch nachrücken.
- Grundlage: GUIGroup.cs, StackLayout.cs und GUIControlFactory.cs aus MP1,
  Commit 43675ce01fcc9a199107b7e9fa414d408a79fa32. Dies bleibt eine statische
  Annäherung: zeitabhängige Animationen, nachträgliche Größenänderungen durch
  Plugins und alle bedingten Ladezweige werden nicht vollständig simuliert.
  Andere Layouttypen, etwa GridLayout/RingLayout/TableLayout, bleiben ungeprüft
  und erzeugen einen Hinweis. Der echte MediaPortal-Laufzeitabgleich steht aus.

NEU IN VERSION 0.7
- Sprachvorschau: über "Sprache" eine MediaPortal-Datei strings_*.xml laden.
  Eine Hauptsprache und ein Fallback sind möglich; Präfixe wie Meine/Mein lassen
  sich abschalten. Nur der Bereich Section name="unmapped" wird verwendet.
- Numerische Beschriftungen werden für die Vorschau aufgelöst. XML-IDs bleiben
  unverändert. Zahlen aus dynamischen Beispieldaten werden nicht als Sprach-IDs
  interpretiert. Fehlende IDs erscheinen als [ID] und werden bei "Prüfen" gemeldet.
- Sprachkatalog nach ID oder Text filtern. Im Texteditor öffnet "Sprachtext wählen"
  den Katalog und übernimmt eine ID, ohne den Ersetzungsschutz zu umgehen.
- Übersetzte Beschriftungen sind im Baum, bei der Auswahl und in der Elementsuche
  nutzbar. Allgemeine Beschreibungen aus references.xml verdecken sie nicht mehr.
- Sprachdateien werden nur gelesen. Ihre Originalbytes werden als Schnappschüsse
  im Editorprojekt gespeichert; nach dem Laden sind die externen Sprachdateien
  nicht mehr erforderlich. Höchstens 2 MB und 10000 Texte pro Datei; maximal zwei.
- Neue Editorprojekte verwenden Format 3 und benötigen mindestens Version 0.7.
  Formate 1 und 2 bleiben lesbar. Sprach-Schnappschüsse gehören zum Editorprojekt,
  nicht zum exportierten Skin-ZIP. Original-Sprachdateien werden nicht bearbeitet.
- Define-Kontexte in unterstützten Sichtbarkeitsbedingungen werden berücksichtigt.
  Übergeordnete Gruppen und Include-Bedingungen behalten ihren ursprünglichen
  Kontext, auch wenn Kinder eigene Defines laden. Otherwise-Bedingungen werden
  gegen den vor dem Include festgehaltenen Kontext geprüft.
- Reine Define-Includes sind gültig und erzeugen keinen falschen Controls-Hinweis.
  Gruppen mit ausschließlich Includes werden geladen. Sperren und Ausblendungen
  wirken auf eingebundene Kinder; eine geschützte Verwendung kann nicht über
  eine zweite Verwendung derselben XML-Quelle umgangen werden.
- Fenster-Standardfokus wird übernommen. Die Auswahl eines nicht fokussierbaren
  Labels ersetzt diesen Vorschaufokus nicht. Der Fernbedienungstest bleibt vereinfacht.
- "Element > Ungeprüfte Bedingungen anzeigen" hilft beim Betrachten komplexer
  Skins. Ausschalten ist nur eine Ansichtsentscheidung, kein Beweis für Unsichtbarkeit
  in MP1. Unbekannte Funktionen und Ausdrücke werden weiterhin nicht geraten.
- Texturwert "-" bedeutet ausdrücklich kein Bild; dafür wird keine Grafik als
  fehlend gemeldet und kein Ersatzhintergrund gezeichnet.
- Vollständiger Titan-Quellstand geprüft: 1877 Dateien, 528 XML-Dateien, zwei
  Varianten (Standard + blue background). Ergebnisse stehen unter Pruefungen/Titan.
  Quellstand: MediaPortal/MediaPortal-1, Commit
  43675ce01fcc9a199107b7e9fa414d408a79fa32.
- Projekte können auch als Dateipfad an die EXE übergeben werden.

SPRACHDEMO
1. Seite demo.sprache.xml wählen.
2. Sprache > Hauptsprache laden: Samples/Titan-Auszug/Languages/strings_de.xml.
3. Fallback laden: strings_en.xml aus demselben Ordner, dann Übernehmen.
4. "Meine Filme" doppelt anklicken und "Sprachtext wählen" ausprobieren.
5. Mit Projekt sichern die Vorschau einschließlich Sprach-Schnappschüssen behalten.
Die Beispieldateien sind eigene kleine Editor-Sprachen. Echte MP1-Sprachen liegen
in der MediaPortal-Installation unter language. Plugins können weitere Ressourcen
verwenden, die hier noch nicht automatisch zusammengeführt werden.

NEU IN VERSION 0.6.3
- Text direkt per Doppelklick auf ein sichtbares Control in der Vorschau
  bearbeiten. Alternativ: "Element > Text bearbeiten ..." oder F2, wenn die
  Arbeitsfläche oder der Baum den Fokus hat. Im Navigationsmodus bleibt der
  Doppelklick für die Textbearbeitung deaktiviert.
- Der Dialog bearbeitet den Haupttext (label) von Labels, Fade-Labels, Textboxen
  und Schaltflächen. Er zeigt den gespeicherten Wert sowie eine angenäherte
  Textvorschau mit den aktuellen Beispieldaten. Mehrzeilige Eingabe ist möglich;
  die Canvas-Vorschau bleibt je nach Control vereinfacht und einzeilig.
- Strg+Enter übernimmt, Esc bricht ab. Abbrechen oder unverändertes Übernehmen
  erzeugt keinen XML-/Undo-Schritt. Eine Änderung bleibt ein eigener Undo-Schritt.
- Bestehende #Bindungen und numerische Werte werden nur nach eigener Freigabe
  ersetzt. Zahlen werden als mögliche Sprachressourcen-IDs behandelt; eine
  Sprachdatei-Auflösung oder Bearbeitung von Sprachressourcen ist nicht enthalten.
- Geerbte Texte werden lokal im gewählten Control überschrieben; Style- und
  Referenzdateien bleiben erhalten. Bei Includes wirkt der Text auf alle
  Verwendungen der geänderten XML-Quelle. Vorhandene Kommentare, Erweiterungen,
  Text-Leerzeichen und unbekannte XML-Tags bleiben über die Quelltextänderung
  und Undo erhalten. Höchstens 32768 Zeichen; ungültige XML-Zeichen werden abgewiesen.
- Gesperrte Controls, Gruppenrahmen, Bilder sowie mehrdeutige oder verschachtelte
  label-Eigenschaften sind für diesen Texteditor gesperrt. Text in automatischen
  Gruppenlayouts bleibt ausdrücklich bearbeitbar; deren Geometrieschutz gilt weiter.
- Offene Eingaben rechts müssen vor einem Doppelklick oder Auswahlwechsel im
  Canvas übernommen werden. Kleine Mausbewegungen unter der Windows-Ziehschwelle
  verschieben oder skalieren kein Element, auch bei ausgeschaltetem Raster.
- Die Schriftgröße und das Layout werden nicht automatisch an neuen Text angepasst.

NEU IN VERSION 0.6.2
- Der Elementbaum zeigt verschachtelte XML-Controls unter ihren Gruppen.
  Quelldateien bleiben die oberste Ebene. Mehrfache Includes derselben Quelle
  werden weiterhin als gemeinsame XML-Quelle behandelt.
- Bei Suchtreffern bleiben die übergeordneten Gruppen als Kontext sichtbar.
  Diese zusätzlichen Knoten zählen nicht als Treffer und werden beim Wechsel
  mit Enter übersprungen. Gruppen sind mit [Gruppe] markiert.
- Auswahl, Sperren, Ausblendungen und Sprünge aus der Prüfung erreichen auch
  tief verschachtelte Controls. Beim Neuaufbau werden die Baumäste aufgeklappt.
- Gruppe auswählen, dann "Element > Enthaltene Einzel-Controls auswählen".
  Dadurch werden alle geladenen Blatt-Controls aus demselben XML-Unterbaum
  gemeinsam ausgewählt. Der Suchfilter wird aufgehoben. Gruppenrahmen sowie
  Inhalte aus separat eingebundenen XML-Dateien werden nicht mit ausgewählt.
- Frei positionierte Inhalte lassen sich gemeinsam verschieben. Ausrichten
  und Verteilen funktionieren bei verschachtelten Controls innerhalb derselben
  unmittelbaren Gruppe. Unterschiedliche verschachtelte Eltern bleiben gesperrt.
- Gesperrte oder im Editor ausgeblendete Inhalte bleiben geschützt. Sie werden
  nicht stillschweigend aus der Inhaltsauswahl entfernt; eine solche Auswahl
  kann daher vor dem Verschieben erst entsperrt bzw. eingeblendet werden müssen.
- Gruppenrahmen werden nicht direkt per Canvas verschoben oder skaliert.
  Das würde die enthaltenen Controls im bisherigen Vorschau-Modell nicht korrekt
  mitnehmen. Ihre XML-Eigenschaften bleiben ausdrücklich bearbeitbar.
- Controls unter Gruppen mit einer wirksamen layout-Eigenschaft werden vor
  automatischem Verschieben, Skalieren und Ausrichten geschützt. Das gilt auch
  für geerbte Layoutwerte und mehrfach verwendete Include-Quellen der aktiven
  Seite. StackLayout wird statisch angenähert; Transformationen bleiben ungeprüft.
- Demo: "demo.gruppen.xml" öffnen; Medienbereich oder Aktionen im Baum wählen.
  Gruppieren/Ungruppieren, das Verschieben zwischen Eltern und vollständiges
  Nachbilden der MediaPortal-Gruppenlayouts sind noch nicht enthalten.

Technische Orientierung: MediaPortal GUIGroup.cs, Commit
43675ce01fcc9a199107b7e9fa414d408a79fa32; Arrange verwendet die zugewiesene ILayout.
https://github.com/MediaPortal/MediaPortal-1/blob/43675ce01fcc9a199107b7e9fa414d408a79fa32/mediaportal/Core/guilib/GUIGroup.cs

NEU IN VERSION 0.6.1
- Elementsuche über dem Baum; Strg+F setzt den Fokus ins Suchfeld.
- Durchsucht Namen, IDs, Typen, Texte, Quellen und Eigenschaftswerte der
  geöffneten Seite samt geladenen Includes. Mehrere Wörter müssen alle passen.
- Gezielte Suche: id:10 (numerisch exakt), typ:button (Typ exakt), quelle:menu,
  text:Film oder name:Uhr. Groß-/Kleinschreibung spielt keine Rolle.
- Die Suche filtert nur den Baum. Die Vorschau und aktuelle Auswahl bleiben
  vollständig erhalten. Jede XML-Quelle wird mit ihren passenden Controls
  angezeigt; mehrfache Einbindungen desselben Controls ergeben einen Treffer.
- Enter wählt den nächsten, Umschalt+Enter den vorherigen Treffer. Die Suche
  läuft am Listenende weiter. Esc oder × leert den Filter. Ein Seitenwechsel
  behält den Filter; ein Projektwechsel setzt ihn zurück. Die Suchanfrage ist
  vorübergehend und wird nicht im Editorprojekt gespeichert.
- Offene Eigenschaftseingaben bleiben beim Filtern erhalten. Der Wechsel zu
  einem Treffer oder einem anderen Baum-Control verlangt vorheriges Übernehmen.
- Rechtsklick im Canvas listet Elemente unter dem Mauszeiger auf, vorn zuerst.
  Ein Eintrag wählt das gewünschte überlagerte Element ohne Verschieben aus.
  Gesperrte Elemente sind zum Prüfen auswählbar; ihr Schutz bleibt wirksam.
  Im Editor ausgeblendete Elemente bleiben aus der Liste ausgeschlossen.
  Bedingt unsichtbare Controls werden nur bei "Versteckte zeigen" angeboten.
- Suchfilter und Überlagerungsauswahl erzeugen keine XML-Änderung und keinen
  Undo-Schritt. Ebenen, Sperren und andere Ansichtsparameter bleiben erhalten.

NEU IN VERSION 0.6
- Im Menü "Element" oder per Rechtsklick im Baum: eine Ebene nach vorn/hinten
  oder ganz nach vorn/hinten. Spätere Controls im XML liegen weiter vorn.
- Die Reihenfolge lässt sich innerhalb desselben Elternknotens und derselben
  Quelldatei ändern. Include-/Import-Elemente bilden Grenzen. Gruppen bewegen
  sich zusammen mit ihren Kindern; gesperrte Kinder verhindern die Aktion.
- "Auswahl entfernen ..." prüft vorher auffindbare statische Navigation und
  Control-Bedingungen der geöffneten Seite samt geladenen Includes. Hinweise
  auf gemeinsame Include-Dateien und gefundene Verweise müssen bestätigt werden.
  Andere Seiten und dynamische Verweise werden nicht vollständig untersucht.
  Bestätigtes Entfernen schreibt Verweise nicht automatisch um.
- Mehrere Elemente, auch aus mehreren Dateien, werden gemeinsam entfernt und
  mit einem Undo wiederhergestellt. Entf wirkt nur im Canvas oder auf einem
  Control im Baum; in Textfeldern bleibt Entf normale Textbearbeitung.
- Auswahl, Sperren und ausgeblendete Elemente folgen ihrer XML-Identität bei
  Reihenfolgeänderungen, Entfernen, Undo/Redo und beim Laden von Projekten.
- Nicht übernommene Eigenschaftseingaben blockieren Strukturänderungen.
- Neue Editorprojekte verwenden Format 2 und benötigen Version 0.6 oder neuer.
  Projekte im bisherigen Format 1 können weiterhin geöffnet werden. Das Laden
  stellt Änderungen als einen Undo-Schritt wieder her; frühere Historie wird
  nicht gespeichert. Original-Skindateien werden weiterhin nicht überschrieben.

EBENEN AUSPROBIEREN
Im Seitenmenü "demo.ebenen.xml" wählen. "Ebene A" auswählen und über "Element"
ganz nach vorn holen. Die überlappenden Flächen machen den Effekt sichtbar.
Die Vorschau bleibt eine Annäherung und ersetzt keinen Test in MediaPortal.

KORREKTUR IN VERSION 0.5.1
- Absturz "Auf das verworfene Objekt kann nicht zugegriffen werden.
  Objektname: ContextMenuStrip" beim Auswählen eines Menüeintrags behoben.
- Menüs werden pro Fenster wiederverwendet und erst beim Freigeben des Fensters
  aufgeräumt. Sie bleiben während des Schließens und der Auswahlaktion gültig,
  auch wenn die Aktion ihre eigene Eigenschafts-Schaltfläche neu erstellt.
- Gilt für Seite, Theme, Testdaten-Vorlage, Schrift, Ausrichtung, Einfügen,
  Elementschutz und Rücksetzen lokaler Eigenschaften.
- Neue Regressionstests durchlaufen den tatsächlichen WinForms-Menüpfad über
  Auswahl und Enter/Escape statt ausschließlich Auswahlwerte direkt zu setzen.
- Nicht abgefangene Fehler werden auch beim normalen Start ohne Argumente in
  error.log neben der EXE protokolliert. Falls dieser Ordner nicht beschreibbar
  ist, wird MP1SkinStudio-error.log im temporären Benutzerordner verwendet.

NEU IN VERSION 0.5
- "Reset" neben einer Eigenschaft entfernt ihre lokale XML-Überschreibung.
  Das Menü zeigt vorab den danach wirksamen Style-/Referenzwert und seine Herkunft.
- Gemeinsames Rücksetzen bei Mehrfachauswahl, auch über Include-Dateien hinweg.
- "Testdaten" konfiguriert Beispielwerte und simulierte Plugin-Zustände.
- Vorlagen: Basisdaten, langer Titel, Wetter und Nachrichten.
- Vorschauzustände werden mit dem Editorprojekt gespeichert und wiederhergestellt.
- Die Demo enthält einen gebundenen Filmtitel und eine bedingte Nachrichtenzeile.

LOKALE EIGENSCHAFTEN ZURÜCKSETZEN
1. Ein oder mehrere Controls auswählen.
2. Neben der gewünschten Eigenschaft auf "Reset" klicken.
3. Im Menü den danach wirksamen Wert und seine Herkunft prüfen.
4. "Lokale Überschreibung entfernen" wählen. Die Änderung wirkt unmittelbar.

Die Reihenfolge entspricht dem bestehenden Editor-Lademodell: references.xml,
dann Style, dann lokaler Wert. Rücksetzen entfernt den lokalen Eigenschaftstag;
Style und Referenzdatei bleiben unverändert. Wenn kein geerbter Wert vorhanden
ist, bleibt die Eigenschaft ungesetzt. Die Vorschau verwendet dann ihren Ersatzwert.
Das ist nicht dasselbe wie der ursprüngliche Wert vor einer früheren Bearbeitung:
Diesen stellt "Rückgängig" wieder her. Rücksetzen ist ein gemeinsamer Undo-Schritt.

Eine Auswahl kann teils lokale und teils bereits geerbte Werte enthalten.
Entfernt werden nur die lokalen Einträge; jeder Control behält seine eigene
Style-/Referenzzuordnung. Gesperrte Controls blockieren die gemeinsame Aktion.
Lokale Bindungen mit # erfordern weiterhin die ausdrückliche Bindungsfreigabe.
Andere noch nicht übernommene Eigenschaftseingaben werden vor Rücksetzen geschützt:
Diese zuerst übernehmen. Die Eingabe im gerade zurückgesetzten Feld wird verworfen.
Eigenschaften mit XML-Attributen, verschachtelten Elementen oder mehreren lokalen
Vorkommen bleiben geschützt. Kommentare und Verarbeitungsanweisungen innerhalb
eines entfernten skalaren Tags bleiben als XML-Nachbarn erhalten; Werte und CDATA
werden entfernt. ID und Control-Typ können weiterhin nicht zurückgesetzt werden.

BEISPIELDATEN UND PLUGIN-ZUSTÄNDE
"Testdaten" öffnet einen separaten Dialog. Die Tabelle enthält die Basiswerte,
eigene Werte und erkannte Variablen der geladenen Seite. "— ungeprüft —" bedeutet,
dass noch kein Beispielwert gesetzt ist; solche Werte bleiben beim Übernehmen
unaufgelöst. Eigene Zeilen sind möglich. Variablennamen beginnen für diese
Vorschau mit # und verwenden Buchstaben, Ziffern, Punkt und Unterstrich.
Ein leerer Wert simuliert leeren Text. Eine gelöschte Zeile fällt auf den Basiswert
zurück, falls es einen gibt. Höchstens 128 eigene Werte mit jeweils 2048 Zeichen.

Auf "Plugin-Zustände" sind Namen aus plugin.isenabled(...) der geöffneten Seite
vorbelegt. Weitere Namen können ergänzt werden. Anhaken simuliert aktiv,
Abhaken simuliert ausgeschaltet. Ein Plugin wird dabei nicht ausgeführt.
Vorlagen ersetzen zunächst nur den Arbeitsstand im Dialog; "Abbrechen" erhält
die laufende Vorschau. Erst "Übernehmen" aktualisiert Text, Bilder und die vom
Editor unterstützten Bedingungen. Offene Eigenschaftseingaben bleiben erhalten.

Ausdrücklich gesetzte Beispielwerte haben in der Vorschau Vorrang auch vor einem
gleichnamigen lokalen Define. Das Quell-XML und seine Bindungen bleiben erhalten.
Variablen werden als vollständige Namen ersetzt; eingesetzte Texte werden nicht
nochmals auf darin enthaltene Variablen ausgewertet. Unbekannte Ausdrücke,
Animationen und komplexe Bedingungen bleiben weiterhin ungeprüft.
Beispielwerte betreffen die bestehende Text-/Bildvorschau und unterstützte
string.equals-Bedingungen; gebundene Layout-Koordinaten werden dadurch nicht
automatisch zu einer vollständigen Laufzeitsimulation. Eine Vorlage wirkt nur
dort sichtbar, wo die betreffende Bindung oder Bedingung verwendet wird.

Die Demo zeigt "Langer Titel" am zusätzlichen Filmtitel und "Nachrichten" an
einer bedingten Nachrichtenzeile. "Wetter" aktiviert den World-Weather-Zustand
und einen Temperaturwert; weitere Titan-Wetterbedingungen können ungeprüft bleiben.
"Basisdaten" setzt eigene Werte und aktive Plugin-Zustände zurück.
Der Fernbedienungs-Fokus folgt weiterhin dem im Editor ausgewählten Control.

Diese Vorschau-Einstellungen erzeugen keine XML-Undo-Schritte. Sie werden als
Editoränderung markiert und durch "Projekt sichern" gespeichert. Der Skin-ZIP-
Export enthält sie nicht. Ältere Editorprojekte laden ohne Vorschauabschnitt mit
den bisherigen Basisdaten. Zu große Projektbeschreibungen werden vor Ersetzen
einer bestehenden Projektdatei abgewiesen; die letzte gespeicherte Datei bleibt.

NEU IM ARBEITSSTAND 0.4.1
- "Prüfen" oder F8 öffnet eine Problemliste für die aktuelle Seite samt Includes.
- Prüffunde: mehrfach vorkommende positive Fokus-IDs, fehlende oder mehrdeutige
  Navigationsziele und statisch nicht auffindbare Bilddateien.
- Suche über Elemente, Eigenschaften, Quellpfade, Meldungen und Prüfcodes.
- Doppelklick oder "Zum Element" wählt das Quellcontrol und seine Eigenschaft.
- Details mit XML-Wert, aufgelöstem Wert, Herkunft sowie Quelldatei und Zeile.

Die Prüfung verändert weder XML noch Editor-Einstellungen oder Undo-Verlauf.
Sie arbeitet mit dem geladenen Bearbeitungsstand, einschließlich noch nicht
exportierter Änderungen. Nach einer Korrektur die Prüfung erneut aufrufen.
Sie prüft ausschließlich die geöffnete Seite und deren geladene Includes,
nicht alle XML-Dateien im Skinordner. Das ausgewählte Theme wird berücksichtigt.
Auch ausgeblendete oder gesperrte Controls bleiben Bestandteil der Prüfung.

Mehrfach vorkommende IDs sind Prüffunde, keine bewiesenen Laufzeitfehler:
Bedingungen können die Vorkommen gegenseitig ausschließen. Die ID 0 wird bei
der Konflikt- und Zielprüfung ausgelassen. Dynamische Ziele und Sonderwerte
werden als ungeprüfte Hinweise behandelt. Ein Zieltyp, den der Navigationstest
nicht unterstützt, wird ebenfalls als Hinweis ausgewiesen.

Die Bildprüfung prüft Dateiexistenz, nicht Bildinhalt oder MP1-Texturcaches.
Geprüft werden vorhandene texture*-Eigenschaften im aufgelösten Controlmodell,
mit Theme-/Media-Vorrang und Rückfall auf Media des Basis-Skins. Dynamische
Bildpfade bleiben Hinweise. Plugin-erzeugte Controls, externe Bilder und
weitere Laufzeitregeln werden nicht nachgebildet. Im mitgelieferten Titan-Auszug
werden absichtlich nicht mitgelieferte Titan-Grafiken als fehlend gemeldet.
Include-/Laderwarnungen erscheinen als Seitenfund ohne erfundenen Control-Sprung.

Der Schalter "Ungeprüfte Werte / Hinweise" blendet die Hinweise zusätzlich ein.
Sperren und Editor-Ausblendungen werden beim Anspringen nicht aufgehoben.
IDs bleiben im Editor geschützt; erkannte ID-Konflikte im Quell-Skin prüfen.
Ein leerer Bericht ersetzt weiterhin keinen Test in MediaPortal.

NEU IN VERSION 0.4
- Zoom mit dem Mausrad am Mauszeiger; +/- zoomt um die Mitte der Arbeitsfläche.
- Mittlere Maustaste ziehen: Arbeitsfläche verschieben; Escape bricht dies ab.
- "Einpassen" setzt Zoom und Verschiebung zurück. "100 %" zeigt einen Skinpixel
  pro Pixel der Arbeitsfläche. Die Windows-Anzeigeskalierung bleibt unabhängig.
- "Element" oder Rechtsklick im Elementbaum: Auswahl sperren / entsperren oder
  nur im Editor ausblenden / wieder einblenden. [S] und [A] zeigen den Zustand.
- Zoom, Verschiebung, Sperren und Ausblendungen werden im Editorprojekt gesichert.
- Ältere Editorprojekte ohne diese Angaben öffnen mit eingepasster Ansicht.

ARBEITSFLÄCHE UND ELEMENTSCHUTZ
Der Zoom und die Verschiebung gelten für die Arbeitsfläche, auch beim Wechsel
des Bildschirms. "Einpassen" stellt jederzeit die Gesamtansicht wieder her.
Die Ansichtsleiste zeigt den tatsächlichen Zoom; das Mausrad funktioniert über
der Arbeitsfläche. Zum Verschieben wird eine Maus mit mittlerer Taste benötigt.

Gesperrte Controls werden beim normalen Anklicken in der Vorschau übersprungen.
Darunter liegende, nicht gesperrte Elemente bleiben dabei auswählbar. Besonders
nützlich ist das Sperren großer Hintergrundbilder. Gesperrte Elemente können im
Baum weiterhin ausgewählt und ihre Eigenschaften gelesen und kopiert werden.
Eigenschaftsänderungen, Drag, Ausrichten und Änderungen an der Navigation eines
gesperrten Controls sind blockiert. Eine Mehrfachauswahl mit gesperrten Controls
wird gemeinsam blockiert. Duplizieren bleibt möglich; es ändert das Original nicht.
Das Hinzufügen von Controls in derselben XML-Datei bleibt ebenfalls möglich.
Rückgängig/Wiederholen bezieht sich weiterhin auf XML-Bearbeitungen und ist auch
bei gesetzten Sperren möglich. Editor-Einstellungen haben keinen Undo-Verlauf.

Sperren und Ausblendungen einer Gruppe gelten auch für darin enthaltene Controls.
Zum Freigeben die Gruppe selbst wählen oder im Elementmenü alle Sperren bzw.
Editor-Ausblendungen aufheben. Direkt am Quellcontrol gesetzte Flags gelten für alle
Verwendungen. Eine übergeordnete Gruppenausblendung gilt nur innerhalb dieser
Gruppe. Eine gesperrte Verwendung schützt dieselbe XML-Quelle auch in anderen
gleichzeitig geladenen Verwendungen vor Bearbeitung.
Ausgeblendete Controls bleiben im Baum erreichbar. "Versteckte zeigen" betrifft
weiterhin nur XML-Sichtbarkeitsbedingungen und hebt Editor-Ausblendungen nicht auf.

Die Einstellungen ändern keine XML-Eigenschaft, insbesondere kein <visible>.
Der Skin-ZIP-Export enthält sie nicht. Zum Behalten "Projekt sichern" verwenden;
auch reine Ansichtsänderungen zeigen deshalb einen Stern im Fenstertitel.
Bei Projektöffnung werden zusätzlich die Originalstände von XML-Dateien mit
Sperr- oder Ausblendemarkierungen geprüft. Externe Änderungen an diesen Quellen
werden abgewiesen, damit Markierungen nicht beim falschen Control landen.
Ein bereits offener Editorstand bleibt dabei erhalten.

NEU IN VERSION 0.3
- Bildbrowser mit Vorschaubildern, Suche, Abmessungen und Transparenzvorschau.
- Bildauswahl neben den Feldern Bild/Textur, Bild mit Fokus und Bild ohne Fokus.
- Basis-Skin und ausgewähltes Theme werden zusammen angezeigt; ein gleichnamiges
  Bild im Theme ersetzt für die Auswahl das Bild aus dem Basis-Skin.
- Editorprojekte (.mp1skinproj) sichern und später wieder öffnen.
- Prüfung der Originalstände der geänderten XML-Dateien beim Wiederöffnen.
- Kennzeichnung ungesicherter Änderungen durch * im Fenstertitel.

EDITORPROJEKT ODER SKIN-EXPORT?
„Projekt sichern“ speichert einen weiterbearbeitbaren Editorstand als
.mp1skinproj. Er enthält die geänderten XML-Dateien und deren Originalstände,
den Skinordner, das Theme, den zuletzt geöffneten Bildschirm und den Ansichtsstand.
„Projekt öffnen“ lädt diese Bearbeitung wieder in den Editor.
Die gespeicherten Änderungen erscheinen nach dem Laden als ein gemeinsamer
Rückgängig-Schritt. Die frühere Folge einzelner Undo-Schritte wird nicht gespeichert.

„Änderungen als ZIP“ erstellt weiterhin die Dateien zum Übernehmen in eine
Kopie des MediaPortal-Skins. Ein ZIP-Export ist keine Editorprojekt-Datei.

Bilder, Schriften und unveränderte XML-Dateien bleiben im vollständigen
Skinordner. Ein Editorprojekt ersetzt daher kein Backup des gesamten Skins.
Beim Öffnen müssen die Originale der darin bearbeiteten XML-Dateien noch
bytegleich vorhanden sein. Bei Änderungen an diesen Dateien wird das Laden
abgewiesen; eine bereits offene Sitzung bleibt dabei erhalten.
XML-Dateien ohne Bearbeitungen oder Editor-Markierungen sowie Bilder werden nicht
auf einen alten Stand festgelegt.
Wenn der ursprüngliche Skinordner fehlt, fragt der Editor nach dem passenden
Ordner. So lässt sich ein Projekt mit einer bytegleichen Skin-Kopie weiterführen.

TASTENKÜRZEL
STRG+S: Editorprojekt sichern. STRG+UMSCHALT+S: Projekt unter anderem Namen sichern.
STRG+O: Editorprojekt öffnen. STRG+E: geänderte XML-Dateien als ZIP exportieren.
STRG+Z / STRG+Y: Rückgängig / Wiederholen (im Textfeld zunächst dessen Bearbeitung).
F8: Geöffnete Seite prüfen; Fundstelle per Doppelklick oder "Zum Element" wählen.

BILDER AUSWÄHLEN
1. Ein Control auswählen und bei Bild/Textur oder einem Fokusbild auf … klicken.
2. Im Bildbrowser nach einem Dateinamen oder Teilpfad suchen.
3. Bild anklicken: rechts erscheinen Abmessungen, Herkunft und größere Vorschau.
4. „Übernehmen“ oder Doppelklick wählt den relativen Media-Pfad.
5. „Eigenschaften übernehmen“ schreibt die Auswahl in den bearbeiteten XML-Stand.

Der Browser durchsucht Media einschließlich Unterordnern sowie den Media-Ordner
des ausgewählten Themes. Unterstützt werden PNG, JPG/JPEG, BMP und GIF; bei GIF
wird ein statisches Vorschaubild gezeigt. Transparenz liegt auf einem Schachbrett.
Bei defekten oder für die Vorschau zu großen Bildern ist Übernehmen deaktiviert.
Die Bilddateien werden dabei weder kopiert noch bearbeitet. Eigene neue Grafiken
zuerst selbst in den Media-Ordner des Skins legen und den Bildbrowser neu öffnen.
Gebundene Bildpfade bleiben durch die vorhandene Bindungsfreigabe geschützt.

NEU IN VERSION 0.2
- STRG+Klick in Vorschau oder Elementbaum: mehrere Elemente auswählen.
  Anschließend gemeinsam verschieben oder gemeinsame Eigenschaften ändern.
- „Ausrichten“: linke/rechte/obere/untere Kanten, Zentrieren und gleiche Abstände.
- „Einfügen“: Text, Schaltfläche oder Bild in die angezeigte Zieldatei einsetzen.
- „Duplizieren“: Kopie des ausgewählten Controls, mit neuer ID und 30 px Versatz.
- „Navigation“: Verbindungen als Pfeile darstellen, mit Klick verbinden und
  mit den Pfeiltasten durchlaufen.
- Gemeinsame Änderungen über mehrere XML-Dateien sind ein Rückgängig-Schritt.
  Wenn eine Teiländerung fehlschlägt, wird die gesamte Aktion zurückgenommen.

MEHRFACHAUSWAHL UND AUSRICHTEN
STRG gedrückt halten und weitere Elemente anklicken. Erneuter STRG+Klick nimmt
ein Element aus der Auswahl. Ein normales Anklicken eines noch nicht ausgewählten
Elements beginnt eine neue Auswahl. Anklicken eines bereits gewählten Elements
erhält die Auswahl, sodass sie gemeinsam gezogen werden kann.
Der blaue Rahmen kennzeichnet das primäre Element, türkise Rahmen die übrigen.
Unterschiedliche Eigenschaften erscheinen als leeres Feld mit entsprechendem
Hinweis. Ein dort eingetragener Wert wird auf die gesamte Auswahl angewendet.
Leere gemischte Felder, die nicht verändert werden, verändern das XML nicht.

Für gleiche Abstände mindestens drei Elemente auswählen. Ausgerichtet wird
an den gemeinsamen äußeren Kanten; rechts-/zentriert ausgerichtete Label-Anker
werden berücksichtigt. Größenänderungen am Griff bleiben einzelnen Elementen
vorbehalten. Eine geschützte Bindung blockiert die gesamte gemeinsame Aktion.
Freie Geschwister-Controls innerhalb derselben verschachtelten Gruppe sind
unterstützt. Gruppenrahmen und automatische Layouts bleiben dabei geschützt.

NEUE ELEMENTE UND KOPIEN
Die Ziel-XML steht beim Einfügen im Menü und rechts bei der Auswahl.
Wenn ein Element ausgewählt ist, wird in dessen Quelldatei eingefügt; sonst
in die aktuelle Bildschirmdatei. Bei gemeinsam verwendeten Includes wirkt
die Ergänzung auf alle Bildschirme, die diese Datei verwenden.
Die neuen Controls werden am Ende des bestehenden <controls>-Bereichs ergänzt.
Vorhandene Controls werden dabei nicht umnummeriert. Freie IDs werden unter
Berücksichtigung der XML-Dateien im Basis-Skin und in den Themeordnern gesucht.
Die unveränderten Originaldateien werden dabei weiterhin nicht überschrieben.
Verschachtelte Controls/Gruppen werden noch nicht dupliziert.
Vorlagen benötigen anschließend eigene Texte, Bilder und ggf. einen Zielbildschirm.
„Zielbildschirm → Fenster-ID“ bearbeitet den XML-Wert hyperlink.

VISUELLE FERNBEDIENUNGSNAVIGATION
1. Eine Schaltfläche oder ein anderes fokussierbares Control auswählen.
2. „Navigation“ einschalten. Bestehende Verbindungen werden als Pfeile angezeigt.
3. Richtung wählen und in der Vorschau das gewünschte Ziel anklicken.
4. Die Vorschau hat danach den Tastaturfokus: Pfeiltasten testen die eingetragenen
   Wege. Die Auswahl und deren Verbindungen folgen dabei dem erreichten Control.
5. Für eine andere Quelle ein Control links im Baum auswählen; zum normalen
   Gestalten „Navigation“ wieder ausschalten.

Ein Klick ändert nur die gewählte Richtung. Für eine Gegenrichtung die andere
Quelle auswählen und eine zweite Verbindung anlegen. Eine Verbindung benötigt
eindeutige positive Control-IDs. Nicht fokussierbare oder mehrdeutige Ziele werden
abgelehnt. Dynamische Navigationsbindungen werden nur nach ausdrücklicher
Freigabe ersetzt. Der Navigationstest bildet MP1-Sonderfälle wie automatisches
Überspringen unsichtbarer Controls und Plugin-Aktionen nicht vollständig nach.

Windows mit .NET Framework 4.8 wird benötigt (auf diesem Rechner getestet).
Keine Installation, kein Internetzugriff und keine MP1-Installation erforderlich.
Der Samples-Unterordner muss neben der EXE liegen, damit die Demo funktioniert.

EIGENEN SKIN ÖFFNEN
„Skinordner öffnen“: den Skinordner wählen, in dem references.xml, fonts.xml
und die Bildschirm-XMLs liegen. Bilder werden aus Media geladen.
„XML öffnen“: einzelne Datei auswählen; ihr Ordner wird zum Skinordner.
Bei Themes zunächst den Basis-Skinordner öffnen, danach das Theme auswählen.
Die Theme-Dateien werden vor den Basisdateien gesucht (Rückfall auf den Basis-Skin).

Der Editor kann auch eingebundene XMLs direkt öffnen. In der Auswahl stehen
deshalb sowohl Bildschirme als auch gemeinsame Teil-Dateien.

UMFANG
- Elemente einzeln oder gemeinsam auswählen und verschieben; einzelne Controls
  an der unteren rechten Ecke vergrößern.
- Optionales Raster mit 10 Pixel Einrasten.
- Position, Größe, Text, Schrift, Farbe, Bilderpfade, Ausrichtung und
  Navigationsziele über Eigenschaften bearbeiten.
- Farbwähler; Schriftauswahl aus fonts.xml; Ausrichtungs-Auswahl.
- Herkunft der Eigenschaften: lokal, gemeinsamer Style oder references.xml.
- Import/Include-Zuordnung und grundlegende Theme-Dateiauflösung.
- Undo/Redo; Projektdateien zum Weiterbearbeiten und ZIP-Export für MediaPortal.
- Schutz von Control-ID und Control-Typ vor Änderungen im Editor.
- Bindungen mit #Property/#Define sind gegen versehentliches Ersetzen geschützt.
  „Bindungen lokal ersetzen“ erlaubt eine bewusste lokale Änderung.
- Export ausschließlich der geänderten XML-Dateien als ZIP.

WIE GESPEICHERT WIRD
Die Originaldateien im geöffneten Skinordner werden nicht überschrieben.
Änderungen existieren zunächst in der laufenden Sitzung. "Projekt sichern"
speichert sie zum Weiterbearbeiten; der Skin-Export bleibt eine eigene Aktion.
Das Export-ZIP enthält die geänderten XML-Dateien mit ihren relativen Pfaden.
Unveränderte Medien und XMLs sind darin nicht enthalten.

Vor dem Einsatz den vollständigen Skin kopieren/sichern. Die exportierten
Dateien anschließend in diese Kopie übernehmen und in MediaPortal testen.
Die Sitzung bleibt nach einem Export als geändert markiert, damit weitere
Exporte alle Änderungen gegenüber den eingelesenen Originalen enthalten.

Die XML-Dateien werden nicht vollständig neu serialisiert. Der Editor ersetzt
gezielt Quelltextbereiche und fügt bei Bedarf lokale Eigenschaften hinzu.
Unbekannte Einträge, Kommentare, Attribute, IDs, Animationen und Bedingungen
bleiben erhalten, solange sie nicht ausdrücklich bearbeitet werden.
Zeichenkodierung und vorhandenes BOM bleiben erhalten. Nicht darstellbare
Zeichen werden abgelehnt. DTDs/externe Entitäten und UTF-32 sind nicht unterstützt.

GRENZEN DER VORSCHAU
Dies ist eine angenäherte GDI+-Layoutvorschau, NICHT die MP1-Engine.
Das Aussehen in MediaPortal kann abweichen. Verfügbare Schriftfamilien werden
verwendet, sonst Segoe UI. Schriftmetriken, 3D-Perspektive und zeitabhängige
Darstellung sind angenähert. Masken, Animationen und weitere Gruppenlayouts
bleiben ungeprüft. Sammlungen zeigen Beispieldaten; Video einen Platzhalter.
Fehlende Grafiken werden ersetzt und in den Hinweisen aufgeführt.

Grundlegende Sichtbarkeitsbedingungen wie window.isvisible, control.isvisible,
control.hasfocus, plugin.isenabled und string.equals werden für die Vorschau
teilweise ausgewertet. Die Vorschau setzt Plugins auf ausgeschaltet. Uhrzeit,
Datum und einige andere Properties werden mit festen Testwerten dargestellt.
Nicht auswertbare Bedingungen bleiben standardmäßig sichtbar und werden gepunktet
markiert. Über Element > Ungeprüfte Bedingungen anzeigen lassen sie sich separat
aus der Vorschau ausblenden. Die Einstellung wird im Editorprojekt gespeichert.
„Versteckte zeigen“ blendet zusätzlich als unsichtbar erkannte Controls ein.
Der Testzustand verändert keine XML-Werte.

Includes werden zu Bearbeitungszwecken auch bei unbekannten Bedingungen
eingelesen; der Editor bildet die MP-Lade- und Auswertungsreihenfolge nicht
vollständig nach. Es gibt noch keinen Animationseditor, Plugin-Adapter oder
vollständigen Skin-Neubau. Änderungen an gemeinsamen Includes wirken auf alle Seiten,
die diese Dateien verwenden. Globale Styles werden angezeigt; das Bearbeiten
erzeugt lokale Überschreibungen am gewählten Control.

DEMO UND QUELLEN
Samples/Titan-Auszug/common.time.xml, common.default.background.xml,
fonts.xml und references.xml stammen aus MediaPortal/MediaPortal-1,
mediaportal/MediaPortal.Base/skin/Titan, master, abgerufen am 17.10.2026.
Die XML-Inhalte wurden übernommen; Zeilenenden können normalisiert sein.
https://github.com/MediaPortal/MediaPortal-1/tree/master/mediaportal/MediaPortal.Base/skin/Titan

Git-Blob-SHAs der Quellen:
common.time.xml: 5e2378ddbb0b73981e676028b77b73c8306a52b9
common.default.background.xml: 086796cee5511aa2010dfcf93ffb87009fc0bc05
fonts.xml: 7bd804d512c19f02414d6d034bdb6be696c0a4d7
references.xml: c4892f80845ed5c4a4e1a9c8d00e2093aa3e7ad6

demo.xml und demo.menu.xml sind neue Beispiel-Dateien dieses Prototyps.
Sie sind kein originaler Titan-Bildschirm. Die originale Titan-Grafiksammlung
und die Titan-Schriftdateien sind nicht enthalten. Die Demo benötigt sie nicht.
Media/Studio enthält sieben einfache, für diesen Prototyp erzeugte PNGs.
Das sind neue Beispielgrafiken, keine Originalgrafiken des Titan-Skins.

ENTWICKLUNG
Quellcode: src/*.cs. Keine NuGet-Pakete oder externen Laufzeitbibliotheken.
build.ps1 kompiliert mit dem Windows-.NET-Framework-C#-Compiler.
Aus PowerShell im entpackten Ordner: .\build.ps1

Automatische Tests (Zielordner selbst wählen):
MP1SkinStudio.exe --selftest C:\Temp\MP1-SkinStudio-Tests
MP1SkinStudio.exe --ui-smoketest C:\Temp\MP1-SkinStudio-UI-Tests
MP1SkinStudio.exe --generate-demo-assets erzeugt die mitgelieferten Demo-PNGs neu.
Diese Programme haben keine Konsolenausgabe. Ergebnisse stehen im Zielordner.
In PowerShell kann man Start-Process -Wait verwenden, um auf das Ende zu warten.

Die mitgelieferten Ergebnisse dokumentieren 189 Kernprüfungen und 185 Prüfungen
der Windows-Oberfläche, einschließlich Maus-Verschieben, Navigation und Layoutdialog.
Zusätzlich wurde der vollständige Titan-Skin seitenweise geladen, bearbeitet und
mit exaktem Undo sowie Projekt-/Exportproben geprüft (siehe Pruefungen/Titan).
Zehn direkte Prüfungen mit der originalen MP1-1.39-Core-Bibliothek bestanden.
Ein vollständiger MP1-Clientlauf und pixelgenauer Bildvergleich wurden nicht getestet.

WEITERENTWICKLUNG
Kleinere Änderungen werden automatisch geprüft und als Arbeitsstand gesichert.
Eine neue sichtbare Testversion erst nach grundlegenden Änderungen starten,
bei denen eine Kontrolle durch den Benutzer sinnvoll ist. Die zuletzt geöffnete
Testversion wird bei solchen internen Ergänzungen nicht automatisch ersetzt.

LIZENZ
MediaPortal-Beispieldateien: Copyright Team MediaPortal; siehe LICENSE.
Der neue Editor-Quellcode wird unter GPL-3.0-or-later bereitgestellt.
Kein offizielles Produkt von Team MediaPortal.
