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
    
    public static void Main()
    {
        Console.OutputEncoding = Encoding.UTF8; // ääkköset konsoliin

        Console.WriteLine("Haetaan Fivan shorttipositiot...\n");

        // 1) Valmistellaan pyyntö DataTables API
        var url = "https://www.finanssivalvonta.fi/api/shortselling/datatable/current";

        // Form data application, urlencoded
        var form = new Dictionary<string, string>();

        // Haetaan alkaen rivistä 0 ja tarpeeksi pitkä "sivu" (esim. 1000 riittää nykydatalle)
        form["start"] = "0";
        form["length"] = "1000";

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
