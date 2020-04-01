using System;
using System.Collections.Generic;
using System.Text;

namespace WiccanRede.AI.Scripts
{
    public class TalkScripting
    {
        public static List<string> Update(string state, string npcName)
        {
            List<string> talks = new List<string>();
            if (npcName == "Story")
            {
                switch (state)
                {
                    case "Start":
                        talks.Add("Vitej ve hre! Pro začátek zkus promluvit s vesničany. Navštiv hospodu.");
                        break;
                    case "Talking":
                        talks.Add("V hostinci vesničané vyhlásili, že se ztratil jejich duchovní vůdce Matheel. Je pro ně velmi důležitý a tak Tě požádali, jestli bys jim nemohl pomoci ho najít.");
                        break;
                    case "TalkObelisc":
                        talks.Add("Promluv si s Wesenou o tom kde by mohl náš vůdce být.");
                        break;
                    case "Obelisc":
                        talks.Add("Prozradili Ti, že na Obeliscích v krajině je zašifrována poloha místa, kde duchovní vykonává své rituály. Nyní tedy musíš prozkoumat 4 obelisky, které jsou kolem vesnice, tak zjistíš polohu duchovního.");
                        break;
                    case "Path":
                        talks.Add("Výborně... zajímavé informace, běž do severo západní části mapy a snad budememe mít štěstí.");
                        break;
                    case "Forest":
                        talks.Add("Rychle! Musíme hned za našim duchovním!");
                        break;
                    case "Final":
                        talks.Add("Duchovní vůdce se zalekl a utíká pryč... Co bude s vesnicí? Co to vůbec má znamenat? Kam utíká? ... To se dozvíte v pokráčování ;-)");
                        break;
                    default:
                        break;
                }
            }
            else
            {
                switch (state)
                {
                    case "Talking":
                        talks.Add("Vítejte v naší vesnici cizinče, v hospodě je zrovna schůzka, možná byste tam mohl zajít.");
                        talks.Add("Vítejte, neznám Vás odněkud... Ano! Vy jste ten slavný učedník. Rád Vás potkávám. V místní hospodě je zrovna setkání, určitě se tam ukažte.");
                        talks.Add("Zdravím Vás! Už jste to slyšel?! Náš milovaný duchovní se ztratil! Co teď s námi bude? Zrovna teď je o tom v hospodě jednání.");
                        talks.Add("Náš duchovní je pryč! Bude s námi Ámen! V hospodě zrovna jednají co dál, možná by jste tam měl zajít");
                        talks.Add("Rád vás poznávám... Slyšel jste o našem duchovním? Je pryč, nevím co si teď počnem. V mítním hostinci se zrovna naši lidé radí, prý chtějí vyhlásit odměnu za pomoc. Podle mě to stejně ale nepomůže.");
                        break;
                    case "Pub":
                        talks.Add("Cizinče, musíš najít cestu k našemu duchovnímu, který se ztratil při své vycházce. Tajné místo, kde prováděl své meditace a rituály je prý popsáno na 4 obeliscích, které jsou v okolí vesnice, měl by jsi je vyhledat, ale dávej si pozor!");
                        talks.Add("V okolí vesnice jsou 4 obelisky, z nich se prý člověk může dovědět pozici místa, kde náš duchovní vykonávál své rituály, možná je to dobré místo kde začít!");
                        talks.Add("Slyšel jsem, že kolem vesnice jsou postaveny 4 obelisky a v nich lze zjistit lokaci posvátného místa, kam chodil náš duchovní meditovat. Určitě je to dobré místo, kde začít hledat...");
                        break;
                    case "Obelisc":
                        talks.Add("Už jsi zjistil pozici našeho duchovního, zjistil jsi něco z obelisků?");
                        talks.Add("Tak co jsi zjistil z obelisků, obešel jsi všechny 4?");
                        talks.Add("Jak je na tvé pouti? Už máš informace ze všech obelisků?");
                        break;
                    case "TalkObelisc":
                        talks.Add("Už jsi určil pozici našeho duchovního. Získal jsi od někoho potřebné informace?");
                        break;
                    case "Path":
                        talks.Add("Jestli jsi zjistil něco nového, zajdi za Ardin, snad bude vědět co dál...");
                        talks.Add("Máš nějaké nové informace? Řekni to Ardin, ta Ti snad poví více");
                        break;
                    case "Forest":
                        talks.Add("Vyražme za naším duchovním! Snad bude tam, jak říkáš!");
                        talks.Add("Výborně, snad ho najdeme, tam kde říkají obelisky.");
                        break;
                    case "Final":
                        talks.Add("Děkujeme! Bohužel jsi nám nepřinesl dobré zprávy, nechce se mi věřit, že nás náš duchovní zradil...");
                        talks.Add("Tak tomu nevěřím, náš duchovní a zradit nás... to je nemožné.");
                        talks.Add("Proč?! Proč nám to udělal... to ne, to nemůže být pravda...");
                        break;
                } 
            }
            return talks;
        }
    }
}
