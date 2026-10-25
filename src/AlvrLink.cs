// Lien avec la partie PC d'ALVR par son interface web locale (celle qu'utilise ALVR Dashboard) :
// - statistiques en direct (images par seconde, latence, débit, pertes), reçues une fois par seconde ;
// - début et fin d'un enregistrement vidéo du flux envoyé au casque.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Web.Script.Serialization;

namespace LunaQuest
{
    class AlvrStats
    {
        public float ClientFps, ServerFps, LatencyMs, NetworkMs, EncodeMs, DecodeMs, Mbps;
        public int LostPerSec;
        public DateTime Time;
    }

    static class AlvrLink
    {
        public static volatile int Port = 8082;
        static volatile AlvrStats latest;
        static Thread thread;

        // Dernières statistiques, ou null si ALVR n'en a pas envoyé depuis 3 secondes (pas de flux).
        public static AlvrStats Latest
        {
            get
            {
                var s = latest;
                return s != null && (DateTime.UtcNow - s.Time).TotalSeconds < 3 ? s : null;
            }
        }

        public static void Start()
        {
            if (thread != null) return;
            thread = new Thread(Loop) { IsBackground = true, Name = "LunaQuest statistiques ALVR" };
            thread.Start();
        }

        static void Loop()
        {
            var buffer = new byte[64 * 1024];
            while (true)
            {
                if (Process.GetProcessesByName("vrserver").Length == 0) { Thread.Sleep(2000); continue; }
                try
                {
                    using (var ws = new ClientWebSocket())
                    {
                        ws.Options.SetRequestHeader("X-ALVR", "true");
                        ws.ConnectAsync(new Uri("ws://127.0.0.1:" + Port + "/api/events"), CancellationToken.None).Wait(5000);
                        if (ws.State != WebSocketState.Open) { Thread.Sleep(3000); continue; }
                        var message = new MemoryStream();
                        while (ws.State == WebSocketState.Open)
                        {
                            var task = ws.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                            if (!task.Wait(15000)) break; // plus rien depuis 15 s : on se reconnecte
                            var r = task.Result;
                            if (r.MessageType == WebSocketMessageType.Close) break;
                            message.Write(buffer, 0, r.Count);
                            if (!r.EndOfMessage) continue;
                            string text = Encoding.UTF8.GetString(message.GetBuffer(), 0, (int)message.Length);
                            message.SetLength(0);
                            if (text.Contains("\"StatisticsSummary\"")) Parse(text);
                        }
                    }
                }
                catch { }
                Thread.Sleep(3000);
            }
        }

        // {"timestamp": "...", "event_type": {"id": "StatisticsSummary", "data": {...}}}
        static void Parse(string json)
        {
            try
            {
                var root = new JavaScriptSerializer().DeserializeObject(json) as Dictionary<string, object>;
                object o;
                var ev = root != null && root.TryGetValue("event_type", out o) ? o as Dictionary<string, object> : null;
                var data = ev != null && ev.TryGetValue("data", out o) ? o as Dictionary<string, object> : null;
                if (data == null) return;
                Func<string, float> f = key => data.TryGetValue(key, out o) && o != null ? Convert.ToSingle(o, System.Globalization.CultureInfo.InvariantCulture) : 0f;
                latest = new AlvrStats
                {
                    ClientFps = f("client_fps"), ServerFps = f("server_fps"),
                    LatencyMs = f("total_latency_ms"), NetworkMs = f("network_latency_ms"),
                    EncodeMs = f("encode_latency_ms"), DecodeMs = f("decode_latency_ms"),
                    Mbps = f("video_mbits_per_sec"), LostPerSec = (int)f("packets_lost_per_sec"),
                    Time = DateTime.UtcNow
                };
            }
            catch { }
        }

        // Demande au serveur ALVR (« StartRecording », « StopRecording »…). Renvoie null si c'est bon, sinon l'erreur.
        public static string Request(string name)
        {
            try
            {
                var req = (HttpWebRequest)WebRequest.Create("http://127.0.0.1:" + Port + "/api/dashboard-request");
                req.Method = "POST";
                req.ContentType = "application/json";
                req.Headers["X-ALVR"] = "true";
                req.Timeout = 5000;
                byte[] body = Encoding.UTF8.GetBytes("\"" + name + "\"");
                using (var s = req.GetRequestStream()) s.Write(body, 0, body.Length);
                using (var resp = (HttpWebResponse)req.GetResponse())
                    return resp.StatusCode == HttpStatusCode.OK ? null : "réponse " + (int)resp.StatusCode;
            }
            catch (Exception e) { return e.Message; }
        }
    }
}
