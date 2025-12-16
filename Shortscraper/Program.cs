using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;

/// @author Jaakko Pakarinen
/// @version 10/2025
/// <summary>
/// Pieni konsoliohjelma: hakee Finanssivalvonnan (Fiva) aktiiviset shorttipositiot
/// DataTables-rajapinnasta, tulostaa rivit ja lopuksi suurimman position.
/// </summary>


public class Program
{
    /// <summary>
    /// Pääohjelma: koostaa lomakepyynnön (POST), lukee JSON-vastauksen,
    /// tulostaa rivit ja etsi samalla suurimman prosenttiosuuden.
    /// </summary>
    private const string Url = "https://www.finanssivalvonta.fi/api/shortselling/datatable/current";
    private const int MaxRivit = 1000;
    
    public static void Main()
    {
        
        Console.OutputEncoding = Encoding.UTF8; // ääkköset konsoliin

        Console.WriteLine("Haetaan Fivan shorttipositiot...\n");

        // 1) Valmistellaan pyyntö DataTables API
        var url = Url;

        // Form data application, urlencoded
        var form = new Dictionary<string, string>();

        // Haetaan alkaen rivistä 0 ja tarpeeksi pitkä "sivu" (esim. 1000 riittää nykydatalle)
        form["start"] = "0";
        form["length"] = MaxRivit.ToString();

        // Hakukenttä jätetään tyhjäksi = ei suodatusta
        form["search[value]"] = "";
        form["search[regex]"] = "false";

        // Sarakkeiden nimet nämä täsmäävät servun JSON-kenttiin
        form["columns[0][data]"] = "positionHolder";
        form["columns[1][data]"] = "issuerName";
        form["columns[2][data]"] = "isinCode";
        form["columns[3][data]"] = "netShortPositionInPercent";
        form["columns[4][data]"] = "positionDate";

        // Järjestys: sarake 4 (= positionDate), laskeva (uusin ensin)
        form["order[0][column]"] = "4";
        form["order[0][dir]"] = "desc";

        // Kieli
        form["lang"] = "fi";

        try
        {
            // 2) Lähetetään HTTP POST
            using (var client = new HttpClient())
            {
                var content = new FormUrlEncodedContent(form);
                var response = client.PostAsync(url, content).Result; // perus "synkasti", ei awaitteja
                response.EnsureSuccessStatusCode();

                var json = response.Content.ReadAsStringAsync().Result;

                // 3) Puretaan JSONiksi (-> omat luokat alla tiedoston lopussa).
                //    Kenttien nimet täsmää JSON, joten erikoisasetuksia ei tarvita.
                Response data;
                try
                {
                    data = JsonSerializer.Deserialize<Response>(json);
                }
                catch (Exception e)
                {
                    Console.WriteLine("JSON-virhe: " + e.Message);
                    return;
                }

                if (data == null || data.data == null || data.data.Count == 0)
                {
                    Console.WriteLine("Ei dataa.");
                    return;
                }

                Console.WriteLine("Haettu rivejä yhteensä: " + data.data.Count + "\n");

                // 4) Tulostetaan rivit ja samalla etsitään suurin positio
                double suurin = -1.0;          // jokin aloitusarvo
                string suurinHolder = "";
                string suurinIssuer = "";

                for (int i = 0; i < data.data.Count; i++)
                {
                    Position rivi = data.data[i];

                    // Muotoillaan päivä pelkäksi pvm:ksi (esim. 2025-10-16T00:00:00 -> 16.10.2025)
                    string pvm = rivi.positionDate;
                    if (!string.IsNullOrEmpty(pvm))
                    {
                        // Jos serveri palauttaa ISO datetime, otetaan siitä DateTime ja formatoidaan.
                        DateTime dt;
                        if (DateTime.TryParse(pvm, out dt))
                            pvm = dt.ToString("dd.MM.yyyy");
                    }

                    // Tulostus ja sen järjestys.
                    Console.WriteLine(
                        pvm + " | " +
                        rivi.issuerName + " | " +
                        rivi.positionHolder + " | " +
                        rivi.netShortPositionInPercent.ToString("0.##") + "%");

                    // Päivitetään suurin positio, jos löytyy isompi
                    if (rivi.netShortPositionInPercent > suurin)
                    {
                        suurin = rivi.netShortPositionInPercent;
                        suurinHolder = rivi.positionHolder;
                        suurinIssuer = rivi.issuerName;
                    }
                }

                // 5) Yhteenveto suurin positio
                Console.WriteLine();
                Console.WriteLine("Suurin positio: " +
                                  suurin.ToString("0.##") + "% (" +
                                  suurinHolder + " / " + suurinIssuer + ")");
            
                
                // menun lisäys (ohjelma myös jää päälle) 
                while (true)
                {
                    NaytaMenu();
                    string valinta = Console.ReadLine();

                    if (valinta == "0")
                        break;

                    if (valinta == "1")
                    { //näytä kaikki rivit uudelleen
                        TulostaRivit(data.data);
                    }
                    else if (valinta == "2")
                    {
                        Position s = HaeSuurin(data.data);
                        Console.WriteLine();
                        Console.WriteLine("Suurin positio: " +
                                          s.netShortPositionInPercent.ToString("0.##") + "% (" +
                                          s.positionHolder + " / " + s.issuerName + ")");
                    }
                    else if (valinta == "3")
                    { //kaikki positiot per firma yhteen.
                        TulostaYhtioSummat(data.data);
                    }
                    else if (valinta == "4")
                    {
                        HaeYhtionRivit(data.data);
                    }
                    else if (valinta == "5")
                    { //raportti CSV tiedostoon
                        TallennaCsv(data.data);
                    }
                    else
                    {
                        Console.WriteLine("Tuntematon valinta.");
                    }
                }

                Console.WriteLine("Moi!");        

            }
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine("Haku epäonnistui: " + ex.Message);
        }
        catch (Exception ex)
        {
            Console.WriteLine("Virhe: " + ex.Message);
        }
    }
    
    
    /// <summary>
    /// Tulostaa valikon käyttäjälle.
    /// </summary>
    private static void NaytaMenu()
    { //Menu
        Console.WriteLine();
        Console.WriteLine("Valitse toiminto:");
        Console.WriteLine("1 = Näytä kaikki rivit");
        Console.WriteLine("2 = Näytä suurin positio");
        Console.WriteLine("3 = Yhtiökohtainen summa (%)");
        Console.WriteLine("4 = Hae yhtiön shortit (hakusana)");
        Console.WriteLine("5 = Tallenna CSV (report.csv)");
        Console.WriteLine("0 = Lopeta");
        Console.Write("> ");
    }
    
    
/// <summary>
/// Tulostaa kaikki annetut shortit konsoliin.
/// </summary>
/// <param name="rivit">Lista positioriveistä</param>
    private static void TulostaRivit(List<Position> rivit)
    {
        for (int i = 0; i < rivit.Count; i++)
        {
            Position r = rivit[i];
            string pvm = MuotoilePvm(r.positionDate);

            Console.WriteLine(
                pvm + " | " +
                r.issuerName + " | " +
                r.positionHolder + " | " +
                r.netShortPositionInPercent.ToString("0.##") + "%");
        }
    }


/// <summary>
/// Palautus listasta suurimman shorttiposition prosenttiosuuden perusteella.
/// </summary>
/// <param name="rivit">Lista positioriveistä</param>
/// <returns>Suurimman prosenttiosuuden sisältävä positiorivi</returns>
    private static Position HaeSuurin(List<Position> rivit)
    {
        Position suurin = rivit[0];

        for (int i = 1; i < rivit.Count; i++)
        {
            if (rivit[i].netShortPositionInPercent > suurin.netShortPositionInPercent)
                suurin = rivit[i];
        }

        return suurin;
    }


/// <summary>
/// Muotoilee päivämäärän muotoon dd.MM.yyyy, jos mahdollista tulkita päivämääräksi
/// </summary>
/// <param name="pvm">Päivämäärä merkkijonona</param>
/// <returns>Muotoiltu päivämäärä tai alkuperäinen teksti</returns>
    private static string MuotoilePvm(string pvm)
    {
        if (string.IsNullOrEmpty(pvm))
            return "";

        DateTime dt;
        if (DateTime.TryParse(pvm, out dt))
            return dt.ToString("dd.MM.yyyy");

        return pvm;
    }

/// <summary>
/// Laskee yhtiökohtaisen shorttien summan ja tulostaa konsoliin.
/// </summary>
/// <param name="rivit">Lista positioriveistä</param>
    private static void TulostaYhtioSummat(List<Position> rivit)
    { //kaikki positiot per firma yhteen.
        var summat = new Dictionary<string, double>();

        for (int i = 0; i < rivit.Count; i++)
        {
            string y = rivit[i].issuerName;
            double p = rivit[i].netShortPositionInPercent;

            if (summat.ContainsKey(y))
                summat[y] += p;
            else
                summat[y] = p;
        }

        Console.WriteLine();
        Console.WriteLine("Yhtiökohtaiset shortit yhteensä (%):");

        foreach (var pari in summat)
        {
            Console.WriteLine(pari.Key + " | " + pari.Value.ToString("0.##") + "%");
        }
    }


/// <summary>
/// Kysyy hakusanan ja tulostaa kaikki rivit, joissa yhtiön nimi sisältää hakusanan.
/// </summary>
/// <param name="rivit">Lista positioriveistä</param>
    private static void HaeYhtionRivit(List<Position> rivit)
    {
        Console.Write("Anna yhtiön nimi (tai osa): ");
        string haku = Console.ReadLine();

        if (string.IsNullOrEmpty(haku))
            return;

        string h = haku.Trim().ToLower();

        Console.WriteLine();
        bool loytyi = false;

        for (int i = 0; i < rivit.Count; i++)
        {
            if (rivit[i].issuerName != null &&
                rivit[i].issuerName.ToLower().Contains(h))
            {
                loytyi = true;

                Position r = rivit[i];
                string pvm = MuotoilePvm(r.positionDate);

                Console.WriteLine(
                    pvm + " | " +
                    r.issuerName + " | " +
                    r.positionHolder + " | " +
                    r.netShortPositionInPercent.ToString("0.##") + "%");
            }
        }

        if (!loytyi)
            Console.WriteLine("Ei osumia.");
    }


/// <summary>
/// Tallentaa shorttipositiot CSV-tiedostoon (report.csv).
/// </summary>
/// <param name="rivit">Lista positioriveistä</param>
    private static void TallennaCsv(List<Position> rivit)
    {
        string polku = "report.csv";

        using (var sw = new System.IO.StreamWriter(polku, false, Encoding.UTF8))
        {
            sw.WriteLine("date;issuer;holder;percent;isin");

            for (int i = 0; i < rivit.Count; i++)
            {
                Position r = rivit[i];
                string pvm = MuotoilePvm(r.positionDate);

                sw.WriteLine(
                    pvm + ";" +
                    r.issuerName + ";" +
                    r.positionHolder + ";" +
                    r.netShortPositionInPercent.ToString("0.##") + ";" +
                    r.isinCode);
            }
        }
        Console.WriteLine("Tallennettu: " + polku);
        Console.WriteLine("Polku: " + System.IO.Path.GetFullPath(polku));
    }
}



// JSON-rakenteet (serveriltä)

public class Response
{
    // kenttä data lista positioista
    public List<Position> data { get; set; }
}

public class Position
{
    public string positionHolder { get; set; }              
    public string issuerName { get; set; }                  
    public string isinCode { get; set; }                    
    public double netShortPositionInPercent { get; set; }   
    public string positionDate { get; set; }                
}




