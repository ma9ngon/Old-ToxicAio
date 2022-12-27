using System;
using System.Net;
using EnsoulSharp;
using EnsoulSharp.SDK;
using ToxicAio.Champions;

namespace ToxicAio
{

    public class Program
    {

        public static void Main(string[] args)
        {
            GameEvent.OnGameLoad += OnLoadingComplete;
        }

        private static void OnLoadingComplete()
        {
            if (ObjectManager.Player == null)
                return;
            try
            {
                switch (GameObjects.Player.CharacterName)
                {

                    case "KogMaw":
                        KogMaw.OnGameLoad();
                        Game.Print("<font color='#ff0000' size='25'> [ToxicAio]:  </font>" + ObjectManager.Player.CharacterName + " Loaded");
                        Game.Print("<font color='#ff0000' size='25'> [ToxicAio]:  </font>" + "<font color='#F7FF00' size='25'>Devloped By Akane </font>");
                        break;

                    case "Khazix":
                        Khazix.OnGameLoad();
                        Game.Print("<font color='#ff0000' size='25'> [ToxicAio]:  </font>" + ObjectManager.Player.CharacterName + " Loaded");
                        Game.Print("<font color='#ff0000' size='25'> [ToxicAio]:  </font>" + "<font color='#F7FF00' size='25'>Devloped By Akane </font>");
                        break;
                    
                    case "Ashe":
                        Ashe.OnGameLoad();
                        Game.Print("<font color='#ff0000' size='25'> [ToxicAio]:  </font>" + ObjectManager.Player.CharacterName + " Loaded");
                        Game.Print("<font color='#ff0000' size='25'> [ToxicAio]:  </font>" + "<font color='#F7FF00' size='25'>Devloped By Akane </font>");
                        break;
                    
                    case "Vladimir":
                        Vladimir.OnGameLoad();
                        Game.Print("<font color='#ff0000' size='25'> [ToxicAio]:  </font>" + ObjectManager.Player.CharacterName + " Loaded");
                        Game.Print("<font color='#ff0000' size='25'> [ToxicAio]:  </font>" + "<font color='#F7FF00' size='25'>Devloped By Akane </font>");
                        break;
                    
                    case "Blitzcrank":
                        Blitzcrank.OnGameLoad();
                        Game.Print("<font color='#ff0000' size='25'> [ToxicAio]:  </font>" + ObjectManager.Player.CharacterName + " Loaded");
                        Game.Print("<font color='#ff0000' size='25'> [ToxicAio]:  </font>" + "<font color='#F7FF00' size='25'>Devloped By Akane </font>");
                        break;
                    
                    case "Kindred":
                        Kindred.OnGameLoad();
                        Game.Print("<font color='#ff0000' size='25'> [ToxicAio]:  </font>" + ObjectManager.Player.CharacterName + " Loaded");
                        Game.Print("<font color='#ff0000' size='25'> [ToxicAio]:  </font>" + "<font color='#F7FF00' size='25'>Devloped By Akane </font>");
                        break;
                    
                    case "Brand":
                        Brand.OnGameLoad();
                        Game.Print("<font color='#ff0000' size='25'> [ToxicAio]:  </font>" + ObjectManager.Player.CharacterName + " Loaded");
                        Game.Print("<font color='#ff0000' size='25'> [ToxicAio]:  </font>" + "<font color='#F7FF00' size='25'>Devloped By Akane </font>");
                        break;
                    
                    case "Ziggs":
                        Ziggs.OnGameLoad();
                        Game.Print("<font color='#ff0000' size='25'> [ToxicAio]:  </font>" + ObjectManager.Player.CharacterName + " Loaded");
                        Game.Print("<font color='#ff0000' size='25'> [ToxicAio]:  </font>" + "<font color='#F7FF00' size='25'>Devloped By Akane </font>");
                        break;
                    
                    case "Rengar":
                        Rengar.OnGameLoad();
                        Game.Print("<font color='#ff0000' size='25'> [ToxicAio]:  </font>" + ObjectManager.Player.CharacterName + " Loaded");
                        Game.Print("<font color='#ff0000' size='25'> [ToxicAio]:  </font>" + "<font color='#F7FF00' size='25'>Devloped By Akane </font>");
                        break;
                    
                    case "Tryndamere":
                        Tryndamere.OnGameLoad();
                        Game.Print("<font color='#ff0000' size='25'> [ToxicAio]:  </font>" + ObjectManager.Player.CharacterName + " Loaded");
                        Game.Print("<font color='#ff0000' size='25'> [ToxicAio]:  </font>" + "<font color='#F7FF00' size='25'>Devloped By Akane </font>");
                        break;
                    
                    case "Morgana":
                        Morgana.OnGameLoad();
                        Game.Print("<font color='#ff0000' size='25'> [ToxicAio]:  </font>" + ObjectManager.Player.CharacterName + " Loaded");
                        Game.Print("<font color='#ff0000' size='25'> [ToxicAio]:  </font>" + "<font color='#F7FF00' size='25'>Devloped By Akane </font>");
                        break;
                    
                    case "Annie":
                        Annie.OnGameLoad();
                        Game.Print("<font color='#ff0000' size='25'> [ToxicAio]:  </font>" + ObjectManager.Player.CharacterName + " Loaded");
                        Game.Print("<font color='#ff0000' size='25'> [ToxicAio]:  </font>" + "<font color='#F7FF00' size='25'>Devloped By Akane </font>");
                        break;
                    
                    case "Yone":
                        Annie.OnGameLoad();
                        Game.Print("<font color='#ff0000' size='25'> [ToxicAio]:  </font>" + ObjectManager.Player.CharacterName + " Loaded");
                        Game.Print("<font color='#ff0000' size='25'> [ToxicAio]:  </font>" + "<font color='#F7FF00' size='25'>Devloped By Akane </font>");
                        break;
                    
                    case "Kassadin":
                        Kassadin.OnGameLoad();
                        Game.Print("<font color='#ff0000' size='25'> [ToxicAio]:  </font>" + ObjectManager.Player.CharacterName + " Loaded");
                        Game.Print("<font color='#ff0000' size='25'> [ToxicAio]:  </font>" + "<font color='#F7FF00' size='25'>Devloped By Akane </font>");
                        break;
                    
                    default:
                        Game.Print("<font color='#ff0000' size='25'>[ToxicAio] Does Not Support :" + ObjectManager.Player.CharacterName + "</font>");
                        Console.WriteLine("[ToxicAio] Does Not Support " + ObjectManager.Player.CharacterName);
                        break;

                }
                string stringg;
                string uri = "https://raw.githubusercontent.com/Senthixx/ToxicAio/main/version.txt?token=AH6ZRVY5W7PTWYAB3VVHJIDAXRPVU";
                using (WebClient client = new WebClient())
                {
                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                    stringg = client.DownloadString(uri);
                }
                string versionas = "1.0.0.13\n";
                if (versionas != stringg)
                {
                    Game.Print("<font color='#ff0000'> [ToxicAio]: </font> <font color='#ffe6ff' size='25'>You don't have the current version, please UPDATE !</font>");
                }
                else if (versionas == stringg)
                {
                    Game.Print("<font color='#ff0000' size='25'> [ToxicAio]: </font> <font color='#ffe6ff' size='25'>Is updated to the latest version!</font>");
                }
            }
            catch (Exception ex)
            {
                Game.Print("Error in loading");
            }
        }
    }
}
