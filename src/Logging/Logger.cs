using System;
using System.Xml;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace Logging
{
  public interface IConsole
  {
    void ConsoleWriteLine(String text);
  }


  public class Logger
  {
    static XmlDataDocument doc;
    static XmlDataDocument dbgDoc;

    private static XmlNode bodyNode;
    private static XmlNode dbgBodyNode;

    static DateTime startTime;
    static List<DateTime> times;
    static List<Stopwatch> stopwatches;
    static List<int> fpss;
    static bool bInitialized = false;

    public static bool bWriteToOutput = false;
    public static bool bWriteToConsole = true;
    //static IConsole console;

    const string path = "..//..//..//Logs//";

    public static bool IsInitialized
    {
      get { return Logger.bInitialized; }
    }

    public static void InitLogger()
    {
      AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);

      startTime = DateTime.Now;
      times = new List<DateTime>();
      fpss = new List<int>(1000);
      stopwatches = new List<Stopwatch>();

      doc = new XmlDataDocument();
      doc.CreateXmlDeclaration("1.0", "UTF-8", "yes");

      dbgDoc = new XmlDataDocument();
      dbgDoc.CreateXmlDeclaration("1.0", "UTF-8", "yes");

      XmlElement html = doc.CreateElement("html");
      XmlElement head = doc.CreateElement("head");
      XmlElement link = doc.CreateElement("link");
      XmlAttribute type = doc.CreateAttribute("type");
      type.InnerText = "text/css";
      link.Attributes.Append(type);
      XmlAttribute rel = doc.CreateAttribute("rel");
      rel.InnerText = "Stylesheet";
      link.Attributes.Append(rel);
      XmlAttribute href = doc.CreateAttribute("href");
      href.InnerText = "logging.css";
      link.Attributes.Append(href);

      head.AppendChild(link);
      html.AppendChild(head);

      XmlElement body = doc.CreateElement("body");
      XmlElement text = doc.CreateElement("div");
      text.InnerXml = "Logger verze 0.3; cas zacatku: " + startTime.ToString();
      body.AppendChild(text);

      html.AppendChild(body);
      doc.AppendChild(html);

      dbgDoc = (XmlDataDocument)doc.CloneNode(true);

      bodyNode = doc.GetElementsByTagName("body")[0];
      dbgBodyNode = dbgDoc.GetElementsByTagName("body")[0];


      //Save();
      bInitialized = true;
      Console.WriteLine("Logger spusten");
      AddError("test");
    }

    static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
      AddError("V aplikaci doslo k neodchycene vyjimce!" + e.ExceptionObject.ToString());
    }

    //public static void AttachConsole(IConsole console)
    //{
    //    Logger.console = console;
    //}

    //public static void WriteToConsole(string text)
    //{
    //    console.ConsoleWriteLine(text);
    //}

    public static int AppTime
    {
      get
      {
        return ((int)(DateTime.Now - startTime).TotalMilliseconds);
      }
    }

    private static void AddText(string message, string style)
    {
      AddText(message, style, false);
    }

    private static void AddText(string message, string style, bool isProblem)
    {
      XmlElement table = doc.CreateElement("table");
      XmlElement tr = doc.CreateElement("tr");
      XmlElement tdTime = doc.CreateElement("td");
      XmlAttribute aTime = doc.CreateAttribute("class");
      aTime.InnerText = "time";
      tdTime.Attributes.Append(aTime);
      tdTime.InnerText = AppTime.ToString();
      XmlElement tdText = doc.CreateElement("td");
      XmlAttribute aclass = doc.CreateAttribute("class");
      aclass.InnerText = style;
      tdText.Attributes.Append(aclass);

      string encodedMessage = System.Web.HttpUtility.HtmlEncode(message);

      try
      {
        tdText.InnerXml = encodedMessage;
      }
      catch (Exception ex)
      {
        tdText.InnerText = "!!! Chyba pri zaznamenavani zpravy!!!!";
        AddError("Chyba pri vkladani zpravy do logu! delka=" + message.Length + " - " + ex.ToString());
      }
      table.AppendChild(tr);
      tr.AppendChild(tdTime);
      tr.AppendChild(tdText);

      bodyNode.AppendChild(table);

      if (bWriteToOutput)
      {
        System.Diagnostics.Debug.WriteLine(message);
        //System.Console.WriteLine(message);
      }

      if (isProblem)
      {
        AddDebugText(message, style);
      }
    }

    private static void AddDebugText(string message, string style)
    {
      XmlElement table = dbgDoc.CreateElement("table");
      XmlElement tr = dbgDoc.CreateElement("tr");
      XmlElement tdTime = dbgDoc.CreateElement("td");
      XmlAttribute aTime = dbgDoc.CreateAttribute("class");
      aTime.InnerText = "time";
      tdTime.Attributes.Append(aTime);
      tdTime.InnerText = AppTime.ToString();
      XmlElement tdText = dbgDoc.CreateElement("td");
      XmlAttribute aclass = dbgDoc.CreateAttribute("class");
      aclass.InnerText = style;
      tdText.Attributes.Append(aclass);

      string encodedMessage = System.Web.HttpUtility.HtmlEncode(message);

      try
      {
        tdText.InnerXml = encodedMessage;
      }
      catch (Exception ex)
      {
        tdText.InnerText = "!!! Chyba pri zaznamenavani debug zpravy!!!!";
        AddError("Chyba pri vkladani zpravy do logu! delka=" + message.Length + " - " + ex.ToString());
      }
      table.AppendChild(tr);
      tr.AppendChild(tdTime);
      tr.AppendChild(tdText);

      dbgBodyNode.AppendChild(table);
    }

    public static bool Save()
    {
      bool saved = false;
      try
      {
        if (fpss.Count > 10)
        {
          CalculateAverageFPS();
        }
        AddInfo("Ukladam...");
        Console.WriteLine("Ukladani logu do " + path);
        string name = "log" + startTime.ToString() + ".html";
        name = name.Replace(":", "-");
        lock (doc)
        {
          doc.Save(path + "log.html");
          doc.Save(path + name);
          doc.Save("log.html");
          saved = true;
        }
        lock (dbgDoc)
        {
          dbgDoc.Save("debug_log.html");
        }
      }
      catch (Exception ex)
      {
        Console.WriteLine("Chyba ukladani logu!" + ex.ToString());
      }
      return saved;
    }

    private static void CalculateAverageFPS()
    {
      if (fpss.Count > 5)
      {
        try
        {
          ulong fpsSum = 0;
          int fpsMax = 0;
          int fpsMin = 10000;
          foreach (int i in fpss)
          {
            fpsSum += (ulong)i;
            if (i > fpsMax)
              fpsMax = i;
            if (i < fpsMin && i > 0)
              fpsMin = i;
          }
          AddImportant("Prumerny pocet fps je: " + (fpsSum / (ulong)fpss.Count));
          AddImportant("Max fps = " + fpsMax.ToString() + " Min fps = " + fpsMin.ToString());
        }
        catch (Exception ex)
        {
          AddError("Chyba pocitani prumerne hodnoty fps.\n" + ex.ToString());
          fpss.Clear();
        }
      }
    }

    public static void AddInfo(string info)
    {
      AddText(info, "info");
    }
    public static void AddImportant(string info)
    {
      AddText(info, "important");
    }
    public static void AddWarning(string info)
    {
      AddText(info, "warning", true);
    }
    public static void AddError(string info)
    {
      AddText(info, "error", true);
      Save();
    }
    public static void AddThreadStart(System.Threading.Thread t)
    {
      try
      {
        AddText("Startuji thread " + t.Name + "; priorita: " + t.Priority + "; stav: " + t.ThreadState + "; na pozadi=" + t.IsBackground, "info");
      }
      catch (Exception ex)
      {
        AddError("Chyba pro loggovani threadu\n" + ex.ToString());
      }
    }

    public static void AddFPSInfo(int fps)
    {
      try
      {
        if (fpss.Count < fpss.Capacity - 1)
        {
          fpss.Add(fps);
        }
        else
        {
          CalculateAverageFPS();
          fpss.Clear();
          fpss.Add(fps);
        }
      }
      catch (Exception ex)
      {
        AddWarning(ex.ToString());
        Save();
        fpss.Clear();
      }
    }
    public static void StartTimer(string discribe)
    {
      AddText("Stopovani: " + discribe, "duration");
      Stopwatch stopwatch = new Stopwatch();
      stopwatch.Start();
      stopwatches.Add(stopwatch);
      times.Add(DateTime.Now);
    }
    public static void StopTimer(string discribe)
    {
      try
      {
        //string time = (DateTime.Now - times[times.Count - 1]).TotalMilliseconds.ToString();
        string time = stopwatches[stopwatches.Count - 1].ElapsedMilliseconds.ToString();
        AddText("Konec stopovani " + discribe + ". Doba trvani: " + time, "duration");

        //TODO vyresit definovani stopek, tak aby se neprekryvaly
        times.RemoveAt(times.Count - 1);
        stopwatches.RemoveAt(stopwatches.Count - 1);
      }
      catch (Exception ex)
      {
        AddWarning("Zkousim zastavit casovac ktery neexistuje. " + ex.ToString());
      }
    }
  }
}
