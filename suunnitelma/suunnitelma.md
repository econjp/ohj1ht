# Harjoitustyön suunnitelma

## Tietoja

Tekijä: Jaakko Pakarinen

Työ git-varaston osoite: https://github.com/econjp/ohj1ht

Pelin nimi: FIVA: Short Watch

Pelialusta: Windows

Pelaajien lukumäärä: 1

## Pelin tarina

Ei ole peli, vaan pieni tietojenkäsittelyohjelma. Ohjelma seuraa Finanssivalvonnan sivuilta julkisesti näkyviä shorttipositioita eli lyhyitä positioita suomalaisissa pörssiyhtiöissä. Sivusto julkaisee joka päivä klo 10 päivityksen aktiivisista positioista. Ajatus on harjoitella datan käsittelyä ja tiedon poimimista HTML-sivulta C#:n avulla.

## Pelin idea ja tavoitteet

Tavoitteena on tehdä ohjelma, joka lukee paikallisen HTML-tiedoston (esimerkiksi tallennettu versio Finanssivalvonnan shorttipositiot-sivusta) ja poimii siitä taulukon tiedot. Ohjelma tulostaa siistissä muodossa listauksen, jossa nähdään ilmoitusvelvollinen, kohdeyhtiö, prosenttiosuus ja päivämäärä. Lisäksi ohjelma laskee, kuinka monta eri yhtiötä on shortattuna ja mikä on suurin yksittäinen positio. Tarkoitus on opetella lukemaan ja käsittelemään tekstitietoa, käyttämään eri taulukoita ja silmukoita, ja tuottamaan yhteenveto.

## Hahmotelma pelistä

Ohjelma on komentorivipohjainen. Käyttäjä suorittaa ohjelman, joka lukee tiedoston, käsittelee rivit ja tulostaa raportin. Näytöllä näkyy esimerkiksi:

Shorttipositioita yhteensä: 28  
Suurin positio: 1.22 % (Qube Research \& Technologies / QT Group Oyj)



## Toteutuksen suunnitelma

Lokakuu

* Ladataan ja tallennetaan esimerkkisivu (HTML) omalle koneelle
* Rakennetaan tiedostonlukija ja perusrakenne ohjelmalle
* Parsitaan taulukon rivit ja tallennetaan tiedot listaan
* Tulostetaan ensimmäinen raakaversio tiedoista

Loppu lokakuu

* Lisätään funktio, joka laskee suurimman position ja määrän
* Siistitään tuloste ja lisätään virhetarkistukset
* Testataan eri HTML-versioilla

Jos aikaa jää

* Lisätään ominaisuus, joka vertailee kahden päivän dataa ja kertoo, mitkä positiot ovat kasvaneet tai pienentyneet
* Kirjoitetaan lyhyt selostus ohjelman rakenteesta ja testauksesta
