using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Foundation;
using Windows.Storage.Streams;

namespace LunaQuest
{
    // Base stations SteamVR 2.0 : allumage et mise en veille (le moteur s'arrête) par le Bluetooth du PC,
    // comme le fait SteamVR, mais sans casque ni SteamVR lancés.
    static class BaseStations
    {
        static readonly Guid PowerService = new Guid("00001523-1212-efde-1523-785feabcd124");
        static readonly Guid PowerCharacteristic = new Guid("00001525-1212-efde-1523-785feabcd124");

        // Base stations à portée (nom « LHB-… »), écoutées pendant ms millisecondes.
        public static Dictionary<ulong, string> Scan(int ms)
        {
            var found = new Dictionary<ulong, string>();
            var watcher = new BluetoothLEAdvertisementWatcher { ScanningMode = BluetoothLEScanningMode.Active };
            watcher.Received += (sender, e) =>
            {
                string name = e.Advertisement.LocalName;
                if (name == null || !name.StartsWith("LHB-", StringComparison.OrdinalIgnoreCase)) return;
                lock (found) found[e.BluetoothAddress] = name;
            };
            watcher.Start();
            Thread.Sleep(ms);
            watcher.Stop();
            if (watcher.Status == BluetoothLEAdvertisementWatcherStatus.Aborted)
                throw new InvalidOperationException("Bluetooth indisponible : vérifie qu'il est activé sur le PC.");
            lock (found) return new Dictionary<ulong, string>(found);
        }

        // on : true pour allumer, false pour la veille. Renvoie le nombre de base stations commandées.
        public static int SetPower(bool on, Action<string> log)
        {
            var found = Scan(5000);
            if (found.Count == 0) { log("Aucune base station trouvée en Bluetooth (branchées sur le secteur et à portée ?)."); return 0; }
            int done = 0;
            foreach (var kv in found)
            {
                string error = null;
                for (int attempt = 0; attempt < 3; attempt++)
                {
                    error = Write(kv.Key, on ? (byte)1 : (byte)0);
                    if (error == null) break;
                    Thread.Sleep(400);
                }
                if (error == null) { done++; log(kv.Value + (on ? " : allumée." : " : en veille.")); }
                else log(kv.Value + " : " + error);
            }
            return done;
        }

        // Attend la fin d'une opération Windows (sans async/await, comme le reste de LunaQuest).
        static T Wait<T>(IAsyncOperation<T> op)
        {
            var clock = Stopwatch.StartNew();
            while (op.Status == AsyncStatus.Started)
            {
                if (clock.ElapsedMilliseconds > 10000) { op.Cancel(); throw new TimeoutException("pas de réponse (délai dépassé)."); }
                Thread.Sleep(15);
            }
            if (op.Status == AsyncStatus.Error) throw op.ErrorCode;
            if (op.Status == AsyncStatus.Canceled) throw new OperationCanceledException();
            return op.GetResults();
        }

        static string Write(ulong address, byte value)
        {
            try
            {
                using (var device = Wait(BluetoothLEDevice.FromBluetoothAddressAsync(address)))
                {
                    if (device == null) return "injoignable.";
                    var services = Wait(device.GetGattServicesForUuidAsync(PowerService, BluetoothCacheMode.Uncached));
                    if (services.Status != GattCommunicationStatus.Success || services.Services.Count == 0) return "pas de réponse.";
                    using (var service = services.Services[0])
                    {
                        var chars = Wait(service.GetCharacteristicsForUuidAsync(PowerCharacteristic, BluetoothCacheMode.Uncached));
                        if (chars.Status != GattCommunicationStatus.Success || chars.Characteristics.Count == 0) return "commande d'alimentation absente (base station 1.0 ?).";
                        var writer = new DataWriter();
                        writer.WriteByte(value);
                        var status = Wait(chars.Characteristics[0].WriteValueAsync(writer.DetachBuffer(), GattWriteOption.WriteWithResponse));
                        return status == GattCommunicationStatus.Success ? null : "commande refusée.";
                    }
                }
            }
            catch (Exception e) { return e.GetBaseException().Message; }
        }
    }
}
