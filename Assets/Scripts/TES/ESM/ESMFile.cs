﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TESUnity.ESM.Records;
using UnityEngine;

namespace TESUnity.ESM
{
    public class ESMFile
    {
        private const int recordHeaderSizeInBytes = 16;
        private static List<string> DeprecatedRecordsAPI = new List<string>();

        public Record[] Records { get; private set; }
        public Dictionary<Type, List<Record>> RecordsByType { get; private set; }
        public Dictionary<string, Record> ObjectsByIDString { get; private set; }
        public Dictionary<Vector2i, CELLRecord> ExteriorCELLRecordsByIndices { get; private set; }
        public Dictionary<Vector2i, LANDRecord> LANDRecordsByIndices { get; private set; }

        public ESMFile(string filePath)
        {
            RecordsByType = new Dictionary<Type, List<Record>>();
            ObjectsByIDString = new Dictionary<string, Record>();
            ExteriorCELLRecordsByIndices = new Dictionary<Vector2i, CELLRecord>();
            LANDRecordsByIndices = new Dictionary<Vector2i, LANDRecord>();

            ReadRecords(filePath);
            PostProcessRecords();
        }

        public void Merge(ESMFile file)
        {
            var list = new List<Record>();
            list.AddRange(Records);
            list.AddRange(file.Records);

            Records = list.ToArray();

            ObjectsByIDString
                .Concat(file.ObjectsByIDString)
                .GroupBy(d => d.Key)
                .ToDictionary(d => d.Key, d => d.First().Value);

            ExteriorCELLRecordsByIndices
                .Concat(file.ExteriorCELLRecordsByIndices)
                .GroupBy(d => d.Key)
                .ToDictionary(d => d.Key, d => d.First().Value);

            LANDRecordsByIndices
                .Concat(file.LANDRecordsByIndices)
                .GroupBy(d => d.Key)
                .ToDictionary(d => d.Key, d => d.First().Value);
        }

        public List<Record> GetRecordsOfType<T>() where T : Record
        {
            if (RecordsByType.TryGetValue(typeof(T), out List<Record> records))
            {
                return records;
            }

            return null;
        }

        public List<T> GetRecords<T>() where T : Record
        {
            var type = typeof(T);

            if (!RecordsByType.ContainsKey(type))
            {
                return null;
            }

            var list = new List<T>();
            var collection = RecordsByType[type];

            foreach (var item in collection)
            {
                list.Add((T)item);
            }

            return list;
        }

        public static Record CreateRecordOfType(string recordName)
        {
            switch (recordName)
            {
                case "TES3": return new TES3Record();
                case "GMST": return new GMSTRecord();
                case "GLOB": return new GLOBRecord();
                case "SOUN": return new SOUNRecord();
                case "REGN": return new REGNRecord();
                case "LTEX": return new LTEXRecord();
                case "STAT": return new STATRecord();
                case "DOOR": return new DOORRecord();
                case "MISC": return new MISCRecord();
                case "WEAP": return new WEAPRecord();
                case "CONT": return new CONTRecord();
                case "LIGH": return new LIGHRecord();
                case "ARMO": return new ARMORecord();
                case "CLOT": return new CLOTRecord();
                case "REPA": return new REPARecord();
                case "ACTI": return new ACTIRecord();
                case "APPA": return new APPARecord();
                case "LOCK": return new LOCKRecord();
                case "PROB": return new PROBRecord();
                case "INGR": return new INGRRecord();
                case "BOOK": return new BOOKRecord();
                case "ALCH": return new ALCHRecord();
                case "CELL": return new CELLRecord();
                case "LAND": return new LANDRecord();
                case "CLAS": return new CLASRecord();
                case "FACT": return new FACTRecord();
                case "RACE": return new RACERecord();
                case "SKIL": return new SKILRecord();
                case "MGEF": return new MGEFRecord();
                case "SCPT": return new SCPTRecord();
                case "SPEL": return new SPELRecord();
                case "BODY": return new BODYRecord();
                case "ENCH": return new ENCHRecord();
                case "LEVI": return new LEVIRecord();
                case "LEVC": return new LEVCRecord();
                case "PGRD": return new PGRDRecord();
                case "SNDG": return new SNDGRecord();
                case "DIAL": return new DIALRecord();
                case "INFO": return new INFORecord();
                case "BSGN": return new BSGNRecord();
                case "CREA": return new CREARecord();
                case "NPC_": return new NPC_Record();
                default:
                    Debug.LogWarning("Unsupported ESM record type: " + recordName);
                    return null;
            }
        }

        private void ReadRecords(string filePath)
        {
            var recordList = new List<Record>();

            using (var reader = new UnityBinaryReader(File.Open(filePath, FileMode.Open, FileAccess.Read)))
            {
                while (reader.BaseStream.Position < reader.BaseStream.Length)
                {
                    var recordStartStreamPosition = reader.BaseStream.Position;

                    var recordHeader = new RecordHeader();
                    recordHeader.Deserialize(reader);

                    var recordName = recordHeader.name;
                    var record = CreateRecordOfType(recordName);

                    // Read or skip the record.
                    if (record != null)
                    {
                        record.header = recordHeader;

                        var recordDataStreamPosition = reader.BaseStream.Position;

                        if (record.NewFetchMethod)
                        {
                            record.DeserializeDataNew(reader);
                        }
                        else
                        {
                            record.DeserializeData(reader);

                            if (!DeprecatedRecordsAPI.Contains(record.header.name))
                            {
                                DeprecatedRecordsAPI.Add(record.header.name);
                                Debug.LogWarning($"The Record {record.header.name} uses the old/deprecated API.");
                            }
                        }

                        if (reader.BaseStream.Position != (recordDataStreamPosition + record.header.dataSize))
                        {
                            throw new FormatException("Failed reading " + recordName + " record at offset " + recordStartStreamPosition + " in " + filePath);
                        }

                        recordList.Add(record);
                    }
                    else
                    {
                        // Skip the record.
                        reader.BaseStream.Position += recordHeader.dataSize;
                        recordList.Add(null);
                    }
                }
            }

            Records = recordList.ToArray();
        }

        private void PostProcessRecords()
        {
            IIdRecord nameRecord;

            foreach (var record in Records)
            {
                if (record == null)
                {
                    continue;
                }

                // Add the record to the list for it's type.
                var recordType = record.GetType();
                List<Record> recordsOfSameType;

                if (RecordsByType.TryGetValue(recordType, out recordsOfSameType))
                {
                    recordsOfSameType.Add(record);
                }
                else
                {
                    recordsOfSameType = new List<Record>();
                    recordsOfSameType.Add(record);

                    RecordsByType.Add(recordType, recordsOfSameType);
                }

                nameRecord = record as IIdRecord;

                if (nameRecord != null)
                {
                    // TODO
                    // We keep that here during the transition on the new system to be sure to don't break anything.
                    // It'll be removed just after that
                    try
                    {
                        ObjectsByIDString.Add(nameRecord.Id, record);
                    }
                    catch (Exception ex)
                    {
                        Debug.Log(ex.Message);
                    }
                }
                else
                {
                    // TODO: Record.FriendlyName
                    // Add the record to the object dictionary if applicable.
                    if (record is CONTRecord)
                    {
                        ObjectsByIDString.Add(((CONTRecord)record).NAME.value, record);
                    }
                    else if (record is ARMORecord)
                    {
                        ObjectsByIDString.Add(((ARMORecord)record).NAME.value, record);
                    }
                    else if (record is CLOTRecord)
                    {
                        ObjectsByIDString.Add(((CLOTRecord)record).NAME.value, record);
                    }
                    else if (record is NPC_Record)
                    {
                        ObjectsByIDString.Add(((NPC_Record)record).NAME.value, record);
                    }
                }

                // Add the record to exteriorCELLRecordsByIndices if applicable.
                if (record is CELLRecord)
                {
                    var CELL = (CELLRecord)record;

                    if (!CELL.isInterior)
                    {
                        ExteriorCELLRecordsByIndices[CELL.gridCoords] = CELL;
                    }
                }

                // Add the record to LANDRecordsByIndices if applicable.
                if (record is LANDRecord)
                {
                    var LAND = (LANDRecord)record;

                    LANDRecordsByIndices[LAND.gridCoords] = LAND;
                }
            }
        }
    }
}