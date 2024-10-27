using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using ClientPlugin.Utils;
using HarmonyLib;
using Newtonsoft.Json;
using VRage.Network;

namespace ClientPlugin.Patches
{
    public static class DumpReplicationTables
    {
        public const uint Static = 0xffffu;

        public static readonly Dictionary<uint, ReplicationTypeInfo> ReplicatedTypes = new Dictionary<uint, ReplicationTypeInfo>();

        private static string path;
        private static bool dirty;

        public static ReplicationTypeInfo ReplicatedStaticEvents => ReplicatedTypes[Static];

        public static void Init()
        {
            // Static events
            ReplicatedTypes[Static] = new ReplicationTypeInfo(Static, 0, "Static");

            // Dump JSON to this path
            path = Path.Combine(Plugin.DataDir, "ReplicatedTypes.json");
        }

        public static void Update(long tick)
        {
            if (tick % 120 != 0)
                return;

            if (!dirty)
                return;

            lock (ReplicatedTypes)
            {
                var data = ReplicatedTypes.Values.ToList();
                data.Sort((a, b) => a.TypeId.CompareTo(b.TypeId));
                var json = JsonConvert.SerializeObject(data, Formatting.Indented);
                File.WriteAllText(path, json);
                dirty = false;
            }
        }

        public static void SetDirty()
        {
            dirty = true;
        }
    }

    public class ReplicationTypeInfo
    {
        public uint TypeId { get; set; }
        public int TypeHash { get; set; }
        public string FullTypeName { get; set; }
        public Dictionary<uint, string> Events { get; set; } = new Dictionary<uint, string>();

        public ReplicationTypeInfo(uint typeId, int typeHash, string fullTypeName)
        {
            TypeId = typeId;
            TypeHash = typeHash;
            FullTypeName = fullTypeName;
        }
    }

    [HarmonyPatch(typeof(MyTypeTable))]
    public static class MyTypeTablePatch
    {
        [HarmonyPostfix]
        [HarmonyPatch(nameof(MyTypeTable.Register), typeof(Type))]
        private static void RegisterPostfix(ref MySynchronizedTypeInfo __result)
        {
            var ti = __result;
            if (ti == null)
                return;

            lock (DumpReplicationTables.ReplicatedTypes)
            {
                if (!DumpReplicationTables.ReplicatedTypes.ContainsKey(ti.TypeId))
                {
                    var replicationTypeInfo = new ReplicationTypeInfo(ti.TypeId, ti.TypeHash, ti.FullTypeName);
                    DumpReplicationTables.ReplicatedTypes[ti.TypeId] = replicationTypeInfo;
                    DumpReplicationTables.SetDirty();
                }
            }
        }
    }

    [HarmonyPatch(typeof(MyEventTable))]
    public static class MyEventTablePatch
    {
        [HarmonyPostfix]
        [HarmonyPatch("RegisterEvents", typeof(Type), typeof(BindingFlags))]
        private static void RegisterEventsPostfix(MyEventTable __instance, Dictionary<uint, CallSite> ___m_idToEvent)
        {
            lock (DumpReplicationTables.ReplicatedTypes)
            {
                var eventTable = __instance;
                if (eventTable.Type == null)
                {
                    // Static events
                    var replicationTypeInfo = DumpReplicationTables.ReplicatedStaticEvents;
                    UpdateEvents(replicationTypeInfo.Events, ___m_idToEvent);
                }
                else
                {
                    // Instance events
                    var typeInfo = eventTable.Type;
                    if (!DumpReplicationTables.ReplicatedTypes.TryGetValue(typeInfo.TypeId, out var replicationTypeInfo))
                    {
                        replicationTypeInfo = new ReplicationTypeInfo(typeInfo.TypeId, typeInfo.TypeHash, typeInfo.FullTypeName);
                        DumpReplicationTables.ReplicatedTypes[typeInfo.TypeId] = replicationTypeInfo;
                    }

                    UpdateEvents(replicationTypeInfo.Events, ___m_idToEvent);
                }

                DumpReplicationTables.SetDirty();
            }
        }

        private static void UpdateEvents(Dictionary<uint, string> events, Dictionary<uint, CallSite> callSites)
        {
            foreach (var (eventId, callSite) in callSites)
            {
                events[eventId] = callSite.MethodInfo.GetSignature();
            }
        }
    }
}